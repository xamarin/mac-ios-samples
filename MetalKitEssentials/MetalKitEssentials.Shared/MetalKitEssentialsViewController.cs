using CoreGraphics;
using Foundation;
using Metal;
using MetalKit;
using ModelIO;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace MetalKitEssentials
{
#if IOS
	public partial class MetalKitEssentialsViewController : UIKit.UIViewController, IMTKViewDelegate {
#elif MAC
	public partial class MetalKitEssentialsViewController : AppKit.NSViewController, IMTKViewDelegate {
#endif
		
		const int maxInflightBuffers = 3;

		// Renderer.
		IMTLDevice device;
		IMTLCommandQueue commandQueue;
		IMTLLibrary defaultLibrary;
		IMTLRenderPipelineState pipelineState;
		IMTLDepthStencilState depthState;

		// View
		MTKView view;

		// Meshes.
		List<MetalKitEssentialsMesh> meshes;
		IMTLBuffer[] frameUniformBuffers = new IMTLBuffer[maxInflightBuffers];

		// View Controller.
		Semaphore inflightSemaphore;
		int constantDataBufferIndex;
        
		// Uniforms.
		Matrix4 projectionMatrix;
		Matrix4 viewMatrix;
		float rotation;

		[Export ("initWithCoder:")]
		public MetalKitEssentialsViewController (NSCoder coder) : base (coder)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			constantDataBufferIndex = 0;
			inflightSemaphore = new Semaphore (maxInflightBuffers, maxInflightBuffers);

			SetupMetal ();
			SetupView ();
			LoadAssets ();
			Reshape ();
		}

		public void DrawableSizeWillChange (MTKView view, CGSize size)
		{
			Reshape();
		}

		public void Draw (MTKView view)
		{
			inflightSemaphore.WaitOne ();

			// Perofm any app logic, including updating any Metal buffers.
			Update ();

			// Create a new command buffer for each renderpass to the current drawable.
			IMTLCommandBuffer commandBuffer = commandQueue.CommandBuffer ();
			commandBuffer.Label = "Main Command Buffer";

			// Obtain a renderPassDescriptor generated from the view's drawable textures.
			MTLRenderPassDescriptor renderPassDescriptor = view.CurrentRenderPassDescriptor;

			// Create a render command encoder so we can render into something.
			IMTLRenderCommandEncoder renderEncoder = commandBuffer.CreateRenderCommandEncoder (renderPassDescriptor);
			renderEncoder.Label = "Final Pass Encoder";

			// Set context state.
			renderEncoder.SetViewport (new MTLViewport (0d, 0d, view.DrawableSize.Width, view.DrawableSize.Height, 0d, 1d));
			renderEncoder.SetDepthStencilState (depthState);
			renderEncoder.SetRenderPipelineState (pipelineState);

			// Set the our per frame uniforms.
			renderEncoder.SetVertexBuffer (frameUniformBuffers[constantDataBufferIndex], 0, (nuint)(int)BufferIndex.FrameUniformBuffer);
			renderEncoder.PushDebugGroup ("Render Meshes");

			// Render each of our meshes.
			foreach (MetalKitEssentialsMesh mesh in meshes)
				mesh.RenderWithEncoder (renderEncoder);

			renderEncoder.PopDebugGroup ();

			// We're done encoding commands.
			renderEncoder.EndEncoding ();

			/*
                Call the view's completion handler which is required by the view since
                it will signal its semaphore and set up the next buffer.
            */
			var drawable = view.CurrentDrawable;
			commandBuffer.AddCompletedHandler (_ => {
				inflightSemaphore.Release ();
				drawable.Dispose ();
			});

			/*
                The renderview assumes it can now increment the buffer index and that
                the previous index won't be touched until we cycle back around to the same index.
            */
			constantDataBufferIndex = (constantDataBufferIndex + 1) % maxInflightBuffers;

			// Schedule a present once the framebuffer is complete using the current drawable.
			commandBuffer.PresentDrawable (drawable);

			// Finalize rendering here & push the command buffer to the GPU.
			commandBuffer.Commit ();
		}

		void SetupMetal ()
		{
			// Set the view to use the default device.
			device = MTLDevice.SystemDefault;

			// Create a new command queue.
			commandQueue = device.CreateCommandQueue ();

			// Load all the shader files with a metal file extension in the project.
			defaultLibrary = device.CreateDefaultLibrary ();
		}

		void SetupView ()
		{
			view = (MTKView)View;
			view.Delegate = this;
			view.Device = device;

			// Setup the render target, choose values based on your app.
			view.SampleCount = 4;
			view.DepthStencilPixelFormat = (MTLPixelFormat) 260;
		}

		void LoadAssets ()
		{
			// Load the fragment program into the library.
			IMTLFunction fragmentProgram = defaultLibrary.CreateFunction ("fragmentLight");
			// Load the vertex program into the library.
			IMTLFunction vertexProgram = defaultLibrary.CreateFunction ("vertexLight");

			/*
                Create a vertex descriptor for our Metal pipeline. Specifies the layout 
                of vertices the pipeline should expect.
            */
			var mtlVertexDescriptor = new MTLVertexDescriptor ();

			// Positions.
			mtlVertexDescriptor.Attributes [(int)VertexAttributes.Position].Format = MTLVertexFormat.Float3;
			mtlVertexDescriptor.Attributes [(int)VertexAttributes.Position].Offset = 0;
			mtlVertexDescriptor.Attributes [(int)VertexAttributes.Position].BufferIndex = (nuint)(int)BufferIndex.MeshVertexBuffer;

			// Normals.
			mtlVertexDescriptor.Attributes[(int)VertexAttributes.Normal].Format = MTLVertexFormat.Float3;
			mtlVertexDescriptor.Attributes[(int)VertexAttributes.Normal].Offset = 12;
			mtlVertexDescriptor.Attributes[(int)VertexAttributes.Normal].BufferIndex = (nuint)(int)BufferIndex.MeshVertexBuffer;

			// Texture coordinates.
			mtlVertexDescriptor.Attributes[(int)VertexAttributes.Texcoord].Format = MTLVertexFormat.Half2;
			mtlVertexDescriptor.Attributes[(int)VertexAttributes.Texcoord].Offset = 24;
			mtlVertexDescriptor.Attributes[(int)VertexAttributes.Texcoord].BufferIndex = (nuint)(int)BufferIndex.MeshVertexBuffer;

			// Single interleaved buffer.
			mtlVertexDescriptor.Layouts[(int)BufferIndex.MeshVertexBuffer].Stride = 28;
			mtlVertexDescriptor.Layouts[(int)BufferIndex.MeshVertexBuffer].StepRate = 1;
			mtlVertexDescriptor.Layouts[(int)BufferIndex.MeshVertexBuffer].StepFunction = MTLVertexStepFunction.PerVertex;

			// Create a reusable pipeline state
			var pipelineStateDescriptor = new MTLRenderPipelineDescriptor {
				Label = "MyPipeline",
				SampleCount = view.SampleCount,
				VertexFunction = vertexProgram,
				FragmentFunction = fragmentProgram,
				VertexDescriptor = mtlVertexDescriptor
			};

			pipelineStateDescriptor.ColorAttachments [0].PixelFormat = view.ColorPixelFormat;
			pipelineStateDescriptor.DepthAttachmentPixelFormat = view.DepthStencilPixelFormat;
			pipelineStateDescriptor.StencilAttachmentPixelFormat = view.DepthStencilPixelFormat;

			NSError error;
			pipelineState = device.CreateRenderPipelineState (pipelineStateDescriptor, out error);

			if (pipelineState == null)
				Console.WriteLine ("Failed to created pipeline state, error {0}", error.LocalizedDescription);

			var depthStateDesc = new MTLDepthStencilDescriptor {
				DepthCompareFunction = MTLCompareFunction.Less,
				DepthWriteEnabled = true,
			};

			depthState = device.CreateDepthStencilState (depthStateDesc);
            
			/*
                From our Metal vertex descriptor, create a Model I/O vertex descriptor we'll
                load our asset with. This specifies the layout of vertices Model I/O should
                format loaded meshes with.
            */
			var mdlVertexDescriptor = MDLVertexDescriptor.FromMetal (mtlVertexDescriptor);

			mdlVertexDescriptor.Attributes.GetItem<MDLVertexAttribute> ((int)VertexAttributes.Position).Name = MDLVertexAttributes.Position;
			mdlVertexDescriptor.Attributes.GetItem<MDLVertexAttribute> ((int)VertexAttributes.Normal).Name = MDLVertexAttributes.Normal;
			mdlVertexDescriptor.Attributes.GetItem<MDLVertexAttribute> ((int)VertexAttributes.Texcoord).Name = MDLVertexAttributes.TextureCoordinate;

			var bufferAllocator = new MTKMeshBufferAllocator (device);
			NSUrl assetUrl = NSBundle.MainBundle.GetUrlForResource ("Data/Assets/realship/realship.obj", string.Empty);

			if (assetUrl == null)
				Console.WriteLine ("Could not find asset.");

			/*
                Load Model I/O Asset with mdlVertexDescriptor, specifying vertex layout and
                bufferAllocator enabling ModelIO to load vertex and index buffers directory
                into Metal GPU memory.
            */

			var asset = new MDLAsset (assetUrl, mdlVertexDescriptor, bufferAllocator);

			// Create MetalKit meshes.
			MDLMesh[] mdlMeshes;
			NSError mtkError;

			var mtkMeshes = MTKMesh.FromAsset (asset, device, out mdlMeshes, out mtkError);

			if (mtkMeshes == null) {
				Console.WriteLine ("Failed to create mesh, error {0}", error.LocalizedDescription);
				return;
			}

			// Create our array of App-Specific mesh wrapper objects.
			meshes = new List<MetalKitEssentialsMesh> ();

			for (int i = 0; i < mtkMeshes.Length; i++) {
				var mtkMesh = mtkMeshes [i];
				var mdlMesh = mdlMeshes [(nuint)i];
				var mesh = new MetalKitEssentialsMesh (mtkMesh, mdlMesh, device);
				meshes.Add (mesh);
			}

			for (int i = 0; i < maxInflightBuffers; i++)
				frameUniformBuffers [i] = device.CreateBuffer ((nuint)Marshal.SizeOf<FrameUniforms> (), MTLResourceOptions.CpuCacheModeDefault);
		}

		void Reshape()
		{
			/*
                When reshape is called, update the view and projection matricies since 
                this means the view orientation or size changed.
            */
			float aspect = (float)Math.Abs(View.Bounds.Size.Width / View.Bounds.Size.Height);
			projectionMatrix = MathHelper.MatrixFromPerspectiveFovAspectLH(65f * (float)Math.PI / 180f, aspect, 0.1f, 100f);

			viewMatrix = Matrix4.Identity;
		}

		void Update ()
		{
			var frameData = Marshal.PtrToStructure <FrameUniforms> (frameUniformBuffers [constantDataBufferIndex].Contents);

			frameData.model = MathHelper.MatrixFromTranslation (0f, 0f, 2f) * MathHelper.MatrixFromRotation (rotation, 1f, 1f, 0f);
			frameData.view = viewMatrix;

			Matrix4 modelViewMatrix = frameData.view * frameData.model;
			frameData.projectionView = projectionMatrix * modelViewMatrix;
			frameData.normal = Matrix4.Invert (Matrix4.Transpose (modelViewMatrix));

			Marshal.StructureToPtr (frameData, frameUniformBuffers [constantDataBufferIndex].Contents, true);

			rotation += .05f;
		}
	}
}
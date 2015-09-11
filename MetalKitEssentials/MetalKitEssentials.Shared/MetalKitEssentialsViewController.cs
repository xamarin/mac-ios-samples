using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using CoreAnimation;
using CoreGraphics;
using Foundation;
using Metal;
using MetalKit;
using ModelIO;
using ObjCRuntime;
using OpenTK;

#if IOS
using UIKit;
#elif MAC
using AppKit;
#endif

namespace MetalKitEssentials {

	#if IOS
	public partial class MetalKitEssentialsViewController : UIViewController, IMTKViewDelegate {
	#elif MAC
	public partial class MetalKitEssentialsViewController : NSViewController, IMTKViewDelegate {
	#endif
		
		const int maxInflightBuffers = 3;

		IMTLDevice device;
		IMTLCommandQueue commandQueue;
		IMTLLibrary defaultLibrary;
		IMTLRenderPipelineState pipelineState;
		IMTLDepthStencilState depthState;
		MTKView view;

		List<MetalKitEssentialsMesh> meshes;
		IMTLBuffer[] frameUniformBuffers = new IMTLBuffer[maxInflightBuffers];
		Semaphore inflightSemaphore;

		int constantDataBufferIndex;
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
		}

		public void DrawableSizeWillChange (MTKView view, CGSize size)
		{
			float aspect = (float)Math.Abs (View.Bounds.Width / View.Bounds.Height);
			projectionMatrix = MathHelper.MatrixFromPerspectiveFovAspectLH (65f * (float)Math.PI / 180f, aspect, .1f, 100f);
			viewMatrix = Matrix4.Identity;
		}

		public void Draw (MTKView view)
		{
			inflightSemaphore.WaitOne ();

			Update ();

			IMTLCommandBuffer commandBuffer = commandQueue.CommandBuffer ();
			commandBuffer.Label = "Main Command Buffer";

			MTLRenderPassDescriptor renderPassDescriptor = view.CurrentRenderPassDescriptor;

			// Create a render command encoder so we can render into something.
			IMTLRenderCommandEncoder renderEncoder = commandBuffer.CreateRenderCommandEncoder (renderPassDescriptor);
			renderEncoder.Label = "Final Pass Encoder";

			renderEncoder.SetViewport (new MTLViewport (0.0, 0.0, view.DrawableSize.Width, view.DrawableSize.Height, 0.0, 1.0));
			renderEncoder.SetDepthStencilState (depthState);
			renderEncoder.SetRenderPipelineState (pipelineState);

			// Set the our per frame uniforms.
			renderEncoder.SetVertexBuffer (frameUniformBuffers[constantDataBufferIndex], 0, (nuint)(int)BufferIndex.FrameUniformBuffer);
			renderEncoder.PushDebugGroup ("Render Meshes");

			// Render each of our meshes.
			foreach (MetalKitEssentialsMesh mesh in meshes)
				mesh.RenderWithEncoder (renderEncoder);

			renderEncoder.PopDebugGroup ();
			renderEncoder.EndEncoding ();

			var drawable = view.CurrentDrawable;
			commandBuffer.AddCompletedHandler (_ => {
				inflightSemaphore.Release ();
				drawable.Dispose ();
			});

			constantDataBufferIndex = (constantDataBufferIndex + 1) % maxInflightBuffers;
			commandBuffer.PresentDrawable (drawable);
			commandBuffer.Commit ();
		}

		void SetupMetal ()
		{
			// Set the view to use the default device.
			#if IOS
			device = MTLDevice.SystemDefault;
			#elif MAC
			// TODO: https://bugzilla.xamarin.com/show_bug.cgi?id=32680
			var devicePointer = MTLCreateSystemDefaultDevice ();
			device = Runtime.GetINativeObject<IMTLDevice> (devicePointer, false);
			#endif
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
			IMTLFunction fragmentProgram = defaultLibrary.CreateFunction ("fragmentLight");
			IMTLFunction vertexProgram = defaultLibrary.CreateFunction ("vertexLight");

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
			var mdlVertexDescriptor = MDLVertexDescriptor.FromMetal (mtlVertexDescriptor);

			mdlVertexDescriptor.Attributes.GetItem<MDLVertexAttribute> ((int)VertexAttributes.Position).Name = MDLVertexAttributes.Position;
			mdlVertexDescriptor.Attributes.GetItem<MDLVertexAttribute> ((int)VertexAttributes.Normal).Name = MDLVertexAttributes.Normal;
			mdlVertexDescriptor.Attributes.GetItem<MDLVertexAttribute> ((int)VertexAttributes.Texcoord).Name = MDLVertexAttributes.TextureCoordinate;

			var bufferAllocator = new MTKMeshBufferAllocator (device);
			NSUrl assetUrl = NSBundle.MainBundle.GetUrlForResource ("Data/Assets/realship/realship.obj", string.Empty);

			if (assetUrl == null)
				Console.WriteLine ("Could not find asset.");

			// Create MetalKit meshes.
			MTKMesh[] mtkMeshes;
			MDLMesh[] mdlMeshes;
			NSError mtkError;

			var asset = new MDLAsset (assetUrl, mdlVertexDescriptor, bufferAllocator);

			mtkMeshes = MTKMesh.FromAsset (asset, device, out mdlMeshes, out mtkError);

			if (mtkMeshes == null) {
				Console.WriteLine ("Failed to create mesh, error {0}", error.LocalizedDescription);
				return;
			}

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

		void Update ()
		{
			var frameData = Marshal.PtrToStructure <FrameUniforms> (frameUniformBuffers [constantDataBufferIndex].Contents);

			frameData.model = MathHelper.MatrixFromTranslation (0f, 0f, 2f) * MathHelper.MatrixFromRotation (rotation, 1f, 1f, 0f);
			frameData.view = viewMatrix;

			Matrix4 modelViewMatrix = frameData.view * frameData.model;
			frameData.projectionView = Matrix4.Transpose (projectionMatrix * modelViewMatrix);
			frameData.normal = Matrix4.Invert (Matrix4.Transpose (modelViewMatrix));

			Marshal.StructureToPtr (frameData, frameUniformBuffers [constantDataBufferIndex].Contents, true);

			rotation += .05f;
		}

		// TODO:remove
		[DllImport ((Constants.MetalKitLibrary))]
		static extern  /* MDLVertexDescriptor */ IntPtr MTLCreateSystemDefaultDevice ();
	}
}

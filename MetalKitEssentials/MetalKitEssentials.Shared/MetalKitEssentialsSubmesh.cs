using Foundation;
using Metal;
using MetalKit;
using ModelIO;
using ObjCRuntime;
using OpenTK;
using System;
using System.Runtime.InteropServices;

namespace MetalKitEssentials {
	public class MetalKitEssentialsSubmesh {
		
		IMTLBuffer materialUniforms;
		IMTLTexture diffuseTexture;
		MTKSubmesh submesh;

		public MetalKitEssentialsSubmesh (MTKSubmesh mtkSubmesh, MDLSubmesh mdlSubmesh, IMTLDevice device)
		{
			materialUniforms = device.CreateBuffer ((nuint)Marshal.SizeOf <MaterialUniforms> (), MTLResourceOptions.CpuCacheModeDefault);
			var uniforms = Marshal.PtrToStructure <MaterialUniforms> (materialUniforms.Contents);
			submesh = mtkSubmesh;

			for (nuint i = 0; i < mdlSubmesh.Material.Count; i++) {
				MDLMaterialProperty property = ObjectAtIndexedSubscript (mdlSubmesh.Material, i);

				if (property == null)
					continue;

				if (property.Name == "baseColorMap") {

					if (property.Type != MDLMaterialPropertyType.String)
						continue;

					var textureURL = new NSUrl (string.Format ("file://{0}", property.StringValue));
					var textureLoader = new MTKTextureLoader (device);

					NSError error;
					diffuseTexture = textureLoader.FromUrl (textureURL, null, out error);

					if (diffuseTexture == null)
						throw new Exception (string.Format ("Diffuse texture load: {0}", error.LocalizedDescription));
				} else if (property.Name == "specularColor") {
					if (property.Type == MDLMaterialPropertyType.Float4)
						uniforms.specularColor = property.Float4Value;
					else if (property.Type == MDLMaterialPropertyType.Float3)
						uniforms.specularColor = new Vector4 (property.Float3Value);
						uniforms.specularColor.W = 1f;
				} else if (property.Name == "emission") {
					if(property.Type == MDLMaterialPropertyType.Float4)
						uniforms.emissiveColor = property.Float4Value;
					else if (property.Type == MDLMaterialPropertyType.Float3)
						uniforms.emissiveColor = new Vector4 (property.Float3Value);
						uniforms.emissiveColor.W = 1f;
				}
			}

			Marshal.StructureToPtr (uniforms, materialUniforms.Contents, true);
		}

		public void RenderWithEncoder (IMTLRenderCommandEncoder encoder)
		{
			if (diffuseTexture != null)
				encoder.SetFragmentTexture (diffuseTexture, (nuint)(int)TextureIndex.Diffuse);

			encoder.SetFragmentBuffer (materialUniforms, 0, (nuint)(int)BufferIndex.MaterialUniformBuffer);
			encoder.SetVertexBuffer (materialUniforms, 0, (nuint)(int)BufferIndex.MaterialUniformBuffer);

			// Draw the submesh.
			encoder.DrawIndexedPrimitives (submesh.PrimitiveType, submesh.IndexCount, submesh.IndexType, submesh.IndexBuffer.Buffer, submesh.IndexBuffer.Offset);
		}

		// TODO: remove
		[Export ("objectAtIndexedSubscript:")]
		internal virtual MDLMaterialProperty ObjectAtIndexedSubscript (MDLMaterial material, nuint idx)
		{
			return Runtime.GetNSObject<MDLMaterialProperty> (IntPtr_objc_msgSend_nuint (material.Handle, Selector.GetHandle ("objectAtIndexedSubscript:"), idx));
		}

		// TODO: remove
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		public static extern IntPtr IntPtr_objc_msgSend_nuint (IntPtr receiver, IntPtr selector, nuint arg1);
	}
}


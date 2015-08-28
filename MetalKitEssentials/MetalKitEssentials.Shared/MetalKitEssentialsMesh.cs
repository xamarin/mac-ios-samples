using System;
using System.Collections.Generic;

using Metal;
using MetalKit;
using ModelIO;

namespace MetalKitEssentials {
	public class MetalKitEssentialsMesh {

		readonly MTKMesh mesh;
		readonly List<MetalKitEssentialsSubmesh> submeshes;

		public MetalKitEssentialsMesh (MTKMesh mtkMesh, MDLMesh mdlMesh, IMTLDevice device)
		{
			mesh = mtkMesh;
			submeshes = new List<MetalKitEssentialsSubmesh> ();

			if ((nuint)mtkMesh.Submeshes.Length != mdlMesh.Submeshes.Count)
				throw new Exception ("Number od submeshes should be equal");

			for (int i = 0; i < mtkMesh.Submeshes.Length; i++) {
				// Create our own app specifc submesh to hold the MetalKit submesh.
				var submesh = new MetalKitEssentialsSubmesh (mtkMesh.Submeshes[i], mdlMesh.Submeshes.GetItem <MDLSubmesh>((nuint)i), device);
				submeshes.Add (submesh);
			}
		}

		public void RenderWithEncoder (IMTLRenderCommandEncoder renderEncoder)
		{
			nuint bufferIndex = 0;
			foreach (MTKMeshBuffer vertexBuffer in mesh.VertexBuffers) {
				if(vertexBuffer.Buffer != null)
					renderEncoder.SetVertexBuffer (vertexBuffer.Buffer, vertexBuffer.Offset, bufferIndex);

				bufferIndex++;
			}

			foreach (MetalKitEssentialsSubmesh submesh in submeshes)
				submesh.RenderWithEncoder (renderEncoder);
		}
	}
}


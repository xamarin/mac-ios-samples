using OpenTK;

namespace MetalKitEssentials
{
	struct FrameUniforms
	{
		public Matrix4 model;
		public Matrix4 view;
		public Matrix4 projection;
		public Matrix4 projectionView;
		public Matrix4 normal;
	}

	struct MaterialUniforms
	{
		public Vector4 emissiveColor;
		public Vector4 diffuseColor;
		public Vector4 specularColor;

		public float specularIntensity;
		public float pad1;
		public float pad2;
		public float pad3;
	}

	enum VertexAttributes {
		Position = 0,
		Normal = 1,
		Texcoord = 2
	}

	enum BufferIndex {
		MeshVertexBuffer = 0,
		FrameUniformBuffer = 1,
		MaterialUniformBuffer = 2
	};

	enum TextureIndex {
		Diffuse = 0
	};
}


using System;

using OpenTK;

namespace MetalKitEssentials {
	public static class MathHelper {
		
		public static Matrix4 MatrixFromPerspectiveFovAspectLH (float fovY, float aspect, float nearZ, float farZ)
		{
			float yscale = 1.0f / (float)Math.Tan (fovY * 0.5f);
			float xscale = yscale / aspect;
			float q = farZ / (farZ - nearZ);

			return new Matrix4 (
				new Vector4 (xscale, 0.0f, 0.0f, 0.0f),
				new Vector4 (0.0f, yscale, 0.0f, 0.0f),
				new Vector4 (0.0f, 0.0f, q, q * -nearZ),
				new Vector4 (0.0f, 0.0f, 1.0f, 0.0f)
			);
		}

		public static Matrix4 MatrixFromTranslation (float x, float y, float z)
		{
			Matrix4 m = Matrix4.Identity;

			m.M14 = x;
			m.M24 = y;
			m.M34 = z;
			m.M44 = 1.0f;

			return m;
		}

		public static Matrix4 MatrixFromRotation (float radians, float x, float y, float z)
		{
			Vector3 v = Vector3.Normalize (new Vector3 (x, y, z));
			float cos = (float)Math.Cos (radians);
			float cosp = 1.0f - cos;
			float sin = (float)Math.Sin (radians);

			return new Matrix4 (
				new Vector4 (cos + cosp * v.X * v.X, cosp * v.X * v.Y - v.Z * sin, cosp * v.X * v.Z + v.Y * sin, 0.0f),
				new Vector4 (cosp * v.X * v.Y + v.Z * sin, cos + cosp * v.Y * v.Y, cosp * v.Y * v.Z - v.X * sin, 0.0f),
				new Vector4 (cosp * v.X * v.Z - v.Y * sin, cosp * v.Y * v.Z + v.X * sin, cos + cosp * v.Z * v.Z, 0.0f),
				new Vector4 (0.0f, 0.0f, 0.0f, 1.0f)
			);
		}
	}
}


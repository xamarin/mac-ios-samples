using System;

#if __IOS__
using UIKit;


#else
using AppKit;
#endif

using CoreGraphics;
using Foundation;
using SpriteKit;

namespace Adventure
{
	public static class GraphicsUtilities
	{
		#region Return parameters

		sealed class ImageData
		{
			public byte[] RawData { get; set; }

			public nint Width { get; set; }

			public nint Height { get; set; }
		}

		#endregion

		public static MapScaner CreateMapScaner (string mapName)
		{
			var imgData = CreateImageData (mapName);
			return new MapScaner (imgData.RawData, imgData.Width);
		}

		static ImageData CreateImageData (string mapName)
		{
			byte[] bitmapData;
			CGImage inImage = GetCGImageNamed (mapName);

			// Create the bitmap context.
			using (CGBitmapContext cgctx = CreateARGBBitmapContext (inImage, out bitmapData)) {
				// error creating context
				if (cgctx == null)
					return null;

				// Get image width, height. We'll use the entire image.
				var w = inImage.Width;
				var h = inImage.Height;
				var rect = new CGRect (0, 0, w, h);

				// Draw the image to the bitmap context. Once we draw, the memory
				// allocated (bitmapData) for the context for rendering will then contain the
				// raw image data in the specified color space.
				cgctx.DrawImage (rect, inImage);

				// Now we can return the image data associated with the bitmap context.
				return new ImageData {
					RawData = bitmapData,
					Width = w,
					Height = h
				};
			}
		}

		#region Loading Images

		public static CGImage CreateCGImageFromFile (NSString path)
		{
			#if __IOS__
			var image = UIImage.FromFile (path);
			if (image == null)
				Console.WriteLine ("UIImage imageWithContentsOfFile failed on file {0}", path);
			return image.CGImage;
			#else
			var image = new NSImage (path);
			return image.CGImage;
			#endif
		}

		static CGImage GetCGImageNamed (string name)
		{
			string[] splitedName = name.Split ('.');
			string path = splitedName [0];
			string extension = splitedName [1];
			string fullPath = NSBundle.MainBundle.PathForResource (path, extension);
			#if __IOS__
			var image = UIImage.FromFile (fullPath);
			if (image == null)
				throw new InvalidOperationException (string.Format ("Couldn't find bundle image resource {0}", name));

			return image.CGImage;
			#else
			return new NSImage (fullPath).CGImage;
			#endif
		}

		#endregion

		#region Bitmap Contexts

		static CGBitmapContext CreateARGBBitmapContext (CGImage inImage, out byte[] bitmapData)
		{
			bitmapData = null;

			CGBitmapContext context;
			CGColorSpace colorSpace;
			nint bitmapByteCount = 0;
			nint bitmapBytesPerRow = 0;

			// Get image width, height. We'll use the entire image.
			var pixelsWide = inImage.Width;
			var pixelsHigh = inImage.Height;

			// Declare the number of bytes per row. Each pixel in the bitmap in this
			// example is represented by 4 bytes; 8 bits each of red, green, blue, and
			// alpha.
			bitmapBytesPerRow = pixelsWide * 4;
			bitmapByteCount = bitmapBytesPerRow * pixelsHigh;

			// Use the generic RGB color space.
			// When finished, release the colorspace before returning
			using (colorSpace = CGColorSpace.CreateDeviceRGB ()) {
				if (colorSpace == null) {
					Console.Error.Write ("Error allocating color space");
					return null;
				}

				// Allocate memory for image data. This is the destination in memory
				// where any drawing to the bitmap context will be rendered.
				bitmapData = new byte[bitmapByteCount];

				// Create the bitmap context. We want pre-multiplied ARGB, 8-bits
				// per component. Regardless of what the source image format is
				// (CMYK, Grayscale, and so on) it will be converted over to the format
				// specified here by CGBitmapContextCreate.
				context = new CGBitmapContext (bitmapData,
					(int)pixelsWide,
					(int)pixelsHigh,
					8,      // bits per component
					(int)bitmapBytesPerRow,
					colorSpace, CGBitmapFlags.PremultipliedFirst);
			}

			return context;
		}

		#endregion

		#region CGPoint Calculations

		public static nfloat RadiansBetweenCGPoints (CGPoint first, CGPoint second)
		{
			var deltaX = second.X - first.X;
			var deltaY = second.Y - first.Y;
			return (nfloat)Math.Atan2 (deltaY, deltaX);
		}

		public static nfloat DistanceBetweenCGPoints (CGPoint first, CGPoint second)
		{
			return Hypotenuse (second.X - first.X, second.Y - first.Y);
		}

		#endregion

		// The assets are all facing Y down, so offset by pi half to get into X right facing
		public static nfloat PalarAdjust (nfloat x)
		{
			return x + (nfloat)Math.PI * 0.5f;
		}

		public static SKEmitterNode EmitterNodeWithEmitterNamed (string emitterFileName)
		{
			string path = NSBundle.MainBundle.PathForResource (emitterFileName, "sks");
			return (SKEmitterNode)NSKeyedUnarchiver.UnarchiveFile (path);
		}

		#region Loading from a Texture Atlas

		public static SKTexture[] LoadFramesFromAtlas (string atlasName, string baseFileName, int numberOfFrames)
		{
			var frames = new SKTexture[numberOfFrames];

			SKTextureAtlas atlas = SKTextureAtlas.FromName (atlasName);
			for (int i = 0; i < numberOfFrames; i++) {
				int imageIndex = i + 1; // because starts from one
				string fileName = string.Format ("{0}{1:D4}.png", baseFileName, imageIndex);
				SKTexture texture = atlas.TextureNamed (fileName);
				frames [i] = texture;
			}

			return frames;
		}

		#endregion

		#region Emitters

		public static void RunOneShotEmitter (SKEmitterNode emitter, float duration)
		{
			emitter.RunAction (SKAction.Sequence (new [] {
				SKAction.WaitForDuration (duration),
				SKAction.Run (() => {
					emitter.ParticleBirthRate = 0;
				}),
				SKAction.WaitForDuration (emitter.ParticleLifetime + emitter.ParticleLifetimeRange),
				SKAction.RemoveFromParent ()
			}));
		}

		#endregion

		public static nfloat Hypotenuse (nfloat dx, nfloat dy)
		{
			return (nfloat)Math.Sqrt (dx * dx + dy * dy);
		}
	}
}


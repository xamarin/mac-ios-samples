using System;
using SpriteKit;
using Foundation;
using CoreGraphics;

namespace Adventure
{
	public class Tree : ParallaxSprite
	{
		private const float OpaqueDistance = 400;

		public bool FadeAlpha { get; set; }

		public Tree(IntPtr handle)
			: base(handle)
		{
		}

		public Tree (SKNode [] sprites, float offset)
			: base(sprites, offset)
		{
		}

		public override object Clone ()
		{
			var tree = (Tree)base.Clone ();
			tree.FadeAlpha = FadeAlpha;

			return tree;
		}

		#region Offsets

		public void UpdateAlphaWithScene(MultiplayerLayeredCharacterScene scene)
		{
			if (scene == null)
				throw new ArgumentNullException ("scene");

			if (!FadeAlpha)
				return;

			nfloat closestHeroDistance = nfloat.MaxValue;

			// See if there are any heroes nearby.
			var ourPosition = Position;
			foreach (SKNode hero in scene.Heroes) {
				var theirPos = hero.Position;
				var distance = GraphicsUtilities.DistanceBetweenCGPoints(ourPosition, theirPos);
				closestHeroDistance = (nfloat)Math.Min (distance, closestHeroDistance);
			}

			if (closestHeroDistance > OpaqueDistance) {
				// No heroes nearby.
				Alpha = 1;
			} else {
				// Adjust the alpha based on how close the hero is.
				var ratio = closestHeroDistance / OpaqueDistance;
				Alpha = 0.1f + ratio * ratio * 0.9f;
			}
		}

		#endregion

	}
}


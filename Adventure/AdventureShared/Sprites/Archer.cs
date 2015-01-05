using System;
using Foundation;

#if __IOS__
using UIKit;
#else
using AppKit;
#endif

using SpriteKit;
using CoreGraphics;

namespace Adventure
{
	public class Archer : HeroCharacter
	{
		private const int ArcherAttackFrames = 10;
		private const int ArcherGetHitFrames = 18;
		private const int ArcherDeathFrames = 42;
		private const float ArcherProjectileSpeed = 8;

		private static SKSpriteNode _sharedProjectile;
		public override SKSpriteNode Projectile {
			get {
				return _sharedProjectile;
			}
		}

		private static SKEmitterNode _sharedProjectileEmitter;
		public override SKEmitterNode ProjectileEmitter {
			get {
				return _sharedProjectileEmitter;
			}
		}

		private static SKTexture[] _sharedIdleAnimationFrames;
		protected override SKTexture[]IdleAnimationFrames {
			get {
				return _sharedIdleAnimationFrames;
			}
		}

		private static SKTexture[] _sharedWalkAnimationFrames;
		protected override SKTexture[] WalkAnimationFrames {
			get {
				return _sharedWalkAnimationFrames;
			}
		}

		private static SKTexture[] _sharedAttackAnimationFrames;
		protected override SKTexture[] AttackAnimationFrames {
			get {
				return _sharedAttackAnimationFrames;
			}
		}

		private static SKTexture[] _sharedGetHitAnimationFrames;
		protected override SKTexture[] GetHitAnimationFrames {
			get {
				return _sharedGetHitAnimationFrames;
			}
		}

		private static SKTexture[] _sharedDeathAnimationFrames;
		protected override SKTexture[] DeathAnimationFrames {
			get {
				return _sharedDeathAnimationFrames;
			}
		}

		private static SKAction _sharedDamageAction;
		protected override SKAction DamageAction {
			get {
				return _sharedDamageAction;
			}
		}

		public Archer (CGPoint position, Player player)
			: base (SKTextureAtlas.FromName ("Archer_Idle").TextureNamed ("archer_idle_0001.png"), position, player)
		{
		}

		#region Shared Assets

		public new static void LoadSharedAssetsOnce ()
		{
			#if __IOS__
			var whiteColor = UIColor.White;
			#else
			NSColor whiteColor = null;
			new NSObject ().InvokeOnMainThread (() => {
				whiteColor = NSColor.White;
			});
			#endif
			_sharedProjectile = SKSpriteNode.FromColor (whiteColor, new CGSize (2, 24));
			_sharedProjectile.PhysicsBody = SKPhysicsBody.CreateCircularBody (ProjectileCollisionRadius);
			_sharedProjectile.Name = @"Projectile";
			_sharedProjectile.PhysicsBody.CategoryBitMask = (uint)ColliderType.Projectile;
			_sharedProjectile.PhysicsBody.CollisionBitMask = (uint)ColliderType.Wall;
			_sharedProjectile.PhysicsBody.ContactTestBitMask = _sharedProjectile.PhysicsBody.CollisionBitMask;

			_sharedProjectileEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("ArcherProjectile");
			_sharedIdleAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Archer_Idle", "archer_idle_", DefaultNumberOfIdleFrames);
			_sharedWalkAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Archer_Walk", "archer_walk_", DefaultNumberOfWalkFrames);
			_sharedAttackAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Archer_Attack", "archer_attack_", ArcherAttackFrames);
			_sharedGetHitAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Archer_GetHit", "archer_getHit_", ArcherGetHitFrames);
			_sharedDeathAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Archer_Death", "archer_death_", ArcherDeathFrames);
			_sharedDamageAction = SKAction.Sequence (new SKAction[] {
				SKAction.ColorizeWithColor (whiteColor, 10, 0),
				SKAction.WaitForDuration (0.75),
				SKAction.ColorizeWithColorBlendFactor (0, 0.25)
			});
		}

		#endregion
	}
}


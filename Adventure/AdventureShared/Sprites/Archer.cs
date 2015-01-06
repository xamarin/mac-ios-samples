#if __IOS__
using UIKit;
#else
using AppKit;
using Foundation;
#endif

using SpriteKit;
using CoreGraphics;

namespace Adventure
{
	public class Archer : HeroCharacter
	{
		const int ArcherAttackFrames = 10;
		const int ArcherGetHitFrames = 18;
		const int ArcherDeathFrames = 42;
		const float ArcherProjectileSpeed = 8;

		static SKSpriteNode sharedProjectile;
		public override SKSpriteNode Projectile {
			get {
				return sharedProjectile;
			}
		}

		static SKEmitterNode sharedProjectileEmitter;
		public override SKEmitterNode ProjectileEmitter {
			get {
				return sharedProjectileEmitter;
			}
		}

		static SKTexture[] sharedIdleAnimationFrames;
		protected override SKTexture[]IdleAnimationFrames {
			get {
				return sharedIdleAnimationFrames;
			}
		}

		static SKTexture[] sharedWalkAnimationFrames;
		protected override SKTexture[] WalkAnimationFrames {
			get {
				return sharedWalkAnimationFrames;
			}
		}

		static SKTexture[] sharedAttackAnimationFrames;
		protected override SKTexture[] AttackAnimationFrames {
			get {
				return sharedAttackAnimationFrames;
			}
		}

		static SKTexture[] sharedGetHitAnimationFrames;
		protected override SKTexture[] GetHitAnimationFrames {
			get {
				return sharedGetHitAnimationFrames;
			}
		}

		static SKTexture[] sharedDeathAnimationFrames;
		protected override SKTexture[] DeathAnimationFrames {
			get {
				return sharedDeathAnimationFrames;
			}
		}

		static SKAction sharedDamageAction;
		protected override SKAction DamageAction {
			get {
				return sharedDamageAction;
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
			sharedProjectile = SKSpriteNode.FromColor (whiteColor, new CGSize (2, 24));
			sharedProjectile.PhysicsBody = SKPhysicsBody.CreateCircularBody (ProjectileCollisionRadius);
			sharedProjectile.Name = @"Projectile";
			sharedProjectile.PhysicsBody.CategoryBitMask = (uint)ColliderType.Projectile;
			sharedProjectile.PhysicsBody.CollisionBitMask = (uint)ColliderType.Wall;
			sharedProjectile.PhysicsBody.ContactTestBitMask = sharedProjectile.PhysicsBody.CollisionBitMask;

			sharedProjectileEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("ArcherProjectile");
			sharedIdleAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Archer_Idle", "archer_idle_", DefaultNumberOfIdleFrames);
			sharedWalkAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Archer_Walk", "archer_walk_", DefaultNumberOfWalkFrames);
			sharedAttackAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Archer_Attack", "archer_attack_", ArcherAttackFrames);
			sharedGetHitAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Archer_GetHit", "archer_getHit_", ArcherGetHitFrames);
			sharedDeathAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Archer_Death", "archer_death_", ArcherDeathFrames);
			sharedDamageAction = SKAction.Sequence (new [] {
				SKAction.ColorizeWithColor (whiteColor, 10, 0),
				SKAction.WaitForDuration (0.75),
				SKAction.ColorizeWithColorBlendFactor (0, 0.25)
			});
		}

		#endregion
	}
}


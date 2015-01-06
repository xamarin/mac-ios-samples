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
	public class Warrior : HeroCharacter
	{
		const int WarriorIdleFrames = 29;
		const int WarriorThrowFrames = 10;
		const int WarriorGetHitFrames = 20;
		const int WarriorDeathFrames = 90;

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
		protected override SKTexture[] IdleAnimationFrames {
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

		public Warrior (CGPoint position, Player player)
			: base(SKTextureAtlas.FromName("Warrior_Idle").TextureNamed("warrior_idle_0001.png"), position, player)
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

			SKTextureAtlas atlas = SKTextureAtlas.FromName ("Environment");

			sharedProjectile = SKSpriteNode.FromTexture (atlas.TextureNamed ("warrior_throw_hammer.png"));
			sharedProjectile.PhysicsBody = SKPhysicsBody.CreateCircularBody (ProjectileCollisionRadius);
			sharedProjectile.Name = "Projectile";
			sharedProjectile.PhysicsBody.CategoryBitMask = (uint)ColliderType.Projectile;
			sharedProjectile.PhysicsBody.CollisionBitMask = (uint)ColliderType.Wall;
			sharedProjectile.PhysicsBody.ContactTestBitMask = sharedProjectile.PhysicsBody.CollisionBitMask;

			sharedProjectileEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("WarriorProjectile");
			sharedIdleAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Warrior_Idle", "warrior_idle_", WarriorIdleFrames);
			sharedWalkAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Warrior_Walk", "warrior_walk_", DefaultNumberOfWalkFrames);
			sharedAttackAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Warrior_Attack", "warrior_attack_", WarriorThrowFrames);
			sharedGetHitAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Warrior_GetHit", "warrior_getHit_", WarriorGetHitFrames);
			sharedDeathAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Warrior_Death", "warrior_death_", WarriorDeathFrames);
			sharedDamageAction = SKAction.Sequence (new [] {
				SKAction.ColorizeWithColor (whiteColor, 10, 0),
				SKAction.WaitForDuration (0.5),
				SKAction.ColorizeWithColorBlendFactor (0, 0.25)
			});
		}

		#endregion
	}
}
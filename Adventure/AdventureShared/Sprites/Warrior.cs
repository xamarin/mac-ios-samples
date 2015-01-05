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
	public class Warrior : HeroCharacter
	{
		private const int WarriorIdleFrames = 29;
		private const int WarriorThrowFrames = 10;
		private const int WarriorGetHitFrames = 20;
		private const int WarriorDeathFrames = 90;

		private static SKSpriteNode _sharedProjectile = null;
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
		protected override SKTexture[] IdleAnimationFrames {
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

			_sharedProjectile = SKSpriteNode.FromTexture (atlas.TextureNamed ("warrior_throw_hammer.png"));
			_sharedProjectile.PhysicsBody = SKPhysicsBody.CreateCircularBody (ProjectileCollisionRadius);
			_sharedProjectile.Name = "Projectile";
			_sharedProjectile.PhysicsBody.CategoryBitMask = (uint)ColliderType.Projectile;
			_sharedProjectile.PhysicsBody.CollisionBitMask = (uint)ColliderType.Wall;
			_sharedProjectile.PhysicsBody.ContactTestBitMask = _sharedProjectile.PhysicsBody.CollisionBitMask;

			_sharedProjectileEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("WarriorProjectile");
			_sharedIdleAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Warrior_Idle", "warrior_idle_", WarriorIdleFrames);
			_sharedWalkAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Warrior_Walk", "warrior_walk_", DefaultNumberOfWalkFrames);
			_sharedAttackAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Warrior_Attack", "warrior_attack_", WarriorThrowFrames);
			_sharedGetHitAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Warrior_GetHit", "warrior_getHit_", WarriorGetHitFrames);
			_sharedDeathAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Warrior_Death", "warrior_death_", WarriorDeathFrames);
			_sharedDamageAction = SKAction.Sequence (new SKAction[] {
				SKAction.ColorizeWithColor (whiteColor, 10, 0),
				SKAction.WaitForDuration (0.5),
				SKAction.ColorizeWithColorBlendFactor (0, 0.25)
			});
		}

		#endregion
	}
}
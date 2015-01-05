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
	public class Boss : EnemyCharacter
	{
		private const int BossWalkFrames = 35;
		private const int BossIdleFrames = 32;
		private const int BossAttackFrames = 42;
		private const int BossDeathFrames = 45;
		private const int BossGetHitFrames = 22;

		private const int BossCollisionRadius = 40;
		private const int BossChaseRadius = BossCollisionRadius * 4;

		private static SKEmitterNode _sharedDamageEmitter;
		protected override SKEmitterNode DamageEmitter {
			get {
				return _sharedDamageEmitter;
			}
		}

		private static SKAction _sharedDamageAction;
		protected override SKAction DamageAction {
			get {
				return _sharedDamageAction;
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

		public Boss (CGPoint position)
			: base(SKTextureAtlas.FromName("Boss_Idle").TextureNamed("boss_idle_0001.png"), position)
		{
			MovementSpeed = Velocity * 0.35f;
			AnimationSpeed = 1f / 35f;

			ZPosition = -0.25f;
			Name = "Boss";

			Attacking = false;

			// Make it AWARE!
			ChaseAI intelligence = new ChaseAI (this);
			intelligence.ChaseRadius = BossChaseRadius;
			intelligence.MaxAlertRadius = BossChaseRadius * 4f;
			Intelligence = intelligence;
		}

		#region Overridden Methods

		public override void ConfigurePhysicsBody ()
		{
			PhysicsBody = SKPhysicsBody.CreateCircularBody (BossCollisionRadius);

			// Our object type for collisions.
			PhysicsBody.CategoryBitMask = (uint)ColliderType.GoblinOrBoss;

			// Collides with these objects.
			PhysicsBody.CollisionBitMask = (uint)(ColliderType.GoblinOrBoss
			| ColliderType.Hero
			| ColliderType.Projectile
			| ColliderType.Wall);

			// We want notifications for colliding with these objects.
			PhysicsBody.ContactTestBitMask = (uint)ColliderType.Projectile;
		}

		public override void AnimationDidComplete (AnimationState animation)
		{
			base.AnimationDidComplete (animation);

			if (animation != AnimationState.Death)
				return;

			// In a real game, you'd complete the level here, maybe as shown by commented code below.
			RemoveAllActions ();
			RunAction (SKAction.Sequence (new SKAction[] {
				SKAction.WaitForDuration (3),
				SKAction.FadeOutWithDuration (2),
				SKAction.RemoveFromParent (),
//				SKAction.RunBlock(()=> {
//					CharacterScene.GemeOver();
//				})
			}));
		}

		public override void CollidedWith (SKPhysicsBody other)
		{
			if (Dying)
				return;

			if ((other.CategoryBitMask & (uint)ColliderType.Projectile) == 0)
				return;

			RequestedAnimation = AnimationState.GetHit;

			float damage = 2;
			bool killed = ApplyDamage (damage, other.Node);
			if (killed)
				CharacterScene.AddToScoreAfterEnemyKill (100, other.Node);
		}

		public override void PerformDeath ()
		{
			RemoveAllActions ();
			base.PerformDeath ();
		}

		#endregion

		#region Shared Assets

		public static void LoadSharedAssetsOnce ()
		{
			#if __IOS__
			var whiteColor = UIColor.White;
			#else
			NSColor whiteColor = null;
			new NSObject ().InvokeOnMainThread (() => {
				whiteColor = NSColor.White;
			});
			#endif
			_sharedIdleAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Boss_Idle", "boss_idle_", BossIdleFrames);
			_sharedWalkAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Boss_Walk", "boss_walk_", BossWalkFrames);
			_sharedAttackAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Boss_Attack", "boss_attack_", BossAttackFrames);
			_sharedGetHitAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Boss_GetHit", "boss_getHit_", BossGetHitFrames);
			_sharedDeathAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Boss_Death", "boss_death_", BossDeathFrames);
			_sharedDamageEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("BossDamage");
			_sharedDamageAction = SKAction.Sequence (new SKAction[] {
				SKAction.ColorizeWithColor (whiteColor, 1, 0),
				SKAction.WaitForDuration (0.5),
				SKAction.ColorizeWithColorBlendFactor (0, 0.1)
			});
		}

		#endregion

	}
}
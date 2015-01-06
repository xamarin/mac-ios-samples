
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
	public class Boss : EnemyCharacter
	{
		const int BossWalkFrames = 35;
		const int BossIdleFrames = 32;
		const int BossAttackFrames = 42;
		const int BossDeathFrames = 45;
		const int BossGetHitFrames = 22;

		const int BossCollisionRadius = 40;
		const int BossChaseRadius = BossCollisionRadius * 4;

		static SKEmitterNode sharedDamageEmitter;
		protected override SKEmitterNode DamageEmitter {
			get {
				return sharedDamageEmitter;
			}
		}

		static SKAction sharedDamageAction;
		protected override SKAction DamageAction {
			get {
				return sharedDamageAction;
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

		public Boss (CGPoint position)
			: base(SKTextureAtlas.FromName("Boss_Idle").TextureNamed("boss_idle_0001.png"), position)
		{
			MovementSpeed = Velocity * 0.35f;
			AnimationSpeed = 1f / 35f;

			ZPosition = -0.25f;
			Name = "Boss";

			Attacking = false;

			// Make it AWARE!
			var intelligence = new ChaseAI (this);
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
			RunAction (SKAction.Sequence (new [] {
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

			var damage = 2f;
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
			sharedIdleAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Boss_Idle", "boss_idle_", BossIdleFrames);
			sharedWalkAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Boss_Walk", "boss_walk_", BossWalkFrames);
			sharedAttackAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Boss_Attack", "boss_attack_", BossAttackFrames);
			sharedGetHitAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Boss_GetHit", "boss_getHit_", BossGetHitFrames);
			sharedDeathAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Boss_Death", "boss_death_", BossDeathFrames);
			sharedDamageEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("BossDamage");
			sharedDamageAction = SKAction.Sequence (new [] {
				SKAction.ColorizeWithColor (whiteColor, 1, 0),
				SKAction.WaitForDuration (0.5),
				SKAction.ColorizeWithColorBlendFactor (0, 0.1)
			});
		}

		#endregion

	}
}
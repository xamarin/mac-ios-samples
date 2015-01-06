using System;

#if __IOS__
using UIKit;
#else
using AppKit;
#endif

using SpriteKit;
using Foundation;
using CoreGraphics;

namespace Adventure
{
	public sealed class Goblin : EnemyCharacter
	{
		const float MinGoblinSize = 0.5f;
		const float GoblinSizeVariance = 0.35f;
		const int GoblinCollisionRadius = 10;

		const int GoblinAttackFrames = 33;
		const int GoblinDeathFrames = 31;
		const int GoblinGetHitFrames = 25;

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

		static SKSpriteNode sharedDeathSplort;

		SKSpriteNode deathSplort {
			get {
				return sharedDeathSplort;
			}
		}

		Random rnd;

		Random Random {
			get {
				rnd = rnd ?? new Random ();
				return rnd;
			}
		}

		public Cave Cave { get; set; }

		public Goblin (CGPoint position)
			: base (SKTextureAtlas.FromName ("Goblin_Idle").TextureNamed ("goblin_idle_0001.png"), position)
		{
			MovementSpeed = Velocity * (float)Random.NextDouble ();
			SetScale (MinGoblinSize + (float)Random.NextDouble () * GoblinSizeVariance);
			ZPosition = -0.25f;
			Name = "Enemy";

			// Make it AWARE!
			Intelligence = new ChaseAI (this);
		}

		#region Overridden Methods

		public override void ConfigurePhysicsBody ()
		{
			PhysicsBody = SKPhysicsBody.CreateCircularBody (GoblinCollisionRadius);

			// Our object type for collisions
			PhysicsBody.CategoryBitMask = (uint)ColliderType.GoblinOrBoss;

			// Collides with these objects
			PhysicsBody.CollisionBitMask = (uint)(ColliderType.GoblinOrBoss
			| ColliderType.Hero
			| ColliderType.Projectile
			| ColliderType.Wall
			| ColliderType.Cave);

			// We want notifications for colliding with these objects.
			PhysicsBody.ContactTestBitMask = (uint)ColliderType.Projectile;
		}

		public override void Reset ()
		{
			base.Reset ();

			Alpha = 1;
			RemoveAllChildren ();
			ConfigurePhysicsBody ();
		}

		public override void AnimationDidComplete (AnimationState animation)
		{
			base.AnimationDidComplete (animation);

			if (animation != AnimationState.Death)
				return;

			RemoveAllActions ();

			RunAction (SKAction.Sequence (new [] {
				SKAction.WaitForDuration (0.75),
				SKAction.FadeOutWithDuration (1),
				SKAction.Run (() => {
					RemoveFromParent ();
					Cave.Recycle (this);
				})
			}));
		}

		public override void CollidedWith (SKPhysicsBody other)
		{
			if (Dying)
				return;

			if ((other.CategoryBitMask & (uint)ColliderType.Projectile) != 0) {
				// Apply random damage of either 100% or 50%
				RequestedAnimation = AnimationState.GetHit;

				float damage = Random.Next (2) == 0 ? 50 : 100;
				bool killed = ApplyDamage (damage, other.Node);

				if (killed)
					CharacterScene.AddToScoreAfterEnemyKill (10, other.Node);
			}
		}

		public override void PerformDeath ()
		{
			RemoveAllActions ();

			SKSpriteNode splort = (SKSpriteNode)deathSplort.Copy ();
			splort.ZPosition = -1;
			splort.ZRotation = (float)(Random.NextDouble () * Math.PI);
			splort.Position = Position;
			splort.Alpha = 0.5f;
			CharacterScene.AddNode (splort, WorldLayer.Ground);
			splort.RunAction (SKAction.FadeOutWithDuration (10));

			base.PerformDeath ();

			PhysicsBody.CollisionBitMask = 0;
			PhysicsBody.ContactTestBitMask = 0;
			PhysicsBody.CategoryBitMask = 0;
			PhysicsBody = null;
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

			SKTextureAtlas atlas = SKTextureAtlas.FromName ("Environment");

			sharedIdleAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Goblin_Idle", "goblin_idle_", DefaultNumberOfIdleFrames);
			sharedWalkAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Goblin_Walk", "goblin_walk_", DefaultNumberOfWalkFrames);
			sharedAttackAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Goblin_Attack", "goblin_attack_", GoblinAttackFrames);
			sharedGetHitAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Goblin_GetHit", "goblin_getHit_", GoblinGetHitFrames);
			sharedDeathAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Goblin_Death", "goblin_death_", GoblinDeathFrames);
			sharedDamageEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Damage");
			sharedDeathSplort = SKSpriteNode.FromTexture (atlas.TextureNamed ("minionSplort.png"));
			sharedDamageAction = SKAction.Sequence (new [] {
				SKAction.ColorizeWithColor (whiteColor, 1, 0),
				SKAction.WaitForDuration (0.75f),
				SKAction.ColorizeWithColorBlendFactor (0, 0.1)
			});
		}

		#endregion
	}
}


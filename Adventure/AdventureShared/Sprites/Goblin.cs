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
	public class Goblin : EnemyCharacter
	{
		private const float MinGoblinSize = 0.5f;
		private const float GoblinSizeVariance = 0.35f;
		private const int GoblinCollisionRadius = 10;

		private const int GoblinAttackFrames = 33;
		private const int GoblinDeathFrames = 31;
		private const int GoblinGetHitFrames = 25;

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

		static SKTexture[] _sharedDeathAnimationFrames;

		protected override SKTexture[] DeathAnimationFrames {
			get {
				return _sharedDeathAnimationFrames;
			}
		}

		private static SKSpriteNode _sharedDeathSplort;

		private SKSpriteNode deathSplort {
			get {
				return _sharedDeathSplort;
			}
		}

		private Random _rnd;

		private Random Random {
			get {
				_rnd = _rnd ?? new Random ();
				return _rnd;
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

			RunAction (SKAction.Sequence (new SKAction[] {
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

			SKSpriteNode splort = (SKSpriteNode)((NSObject)deathSplort).Copy ();
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

			_sharedIdleAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Goblin_Idle", "goblin_idle_", DefaultNumberOfIdleFrames);
			_sharedWalkAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Goblin_Walk", "goblin_walk_", DefaultNumberOfWalkFrames);
			_sharedAttackAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Goblin_Attack", "goblin_attack_", GoblinAttackFrames);
			_sharedGetHitAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Goblin_GetHit", "goblin_getHit_", GoblinGetHitFrames);
			_sharedDeathAnimationFrames = GraphicsUtilities.LoadFramesFromAtlas ("Goblin_Death", "goblin_death_", GoblinDeathFrames);
			_sharedDamageEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Damage");
			_sharedDeathSplort = SKSpriteNode.FromTexture (atlas.TextureNamed ("minionSplort.png"));
			_sharedDamageAction = SKAction.Sequence (new SKAction[] {
				SKAction.ColorizeWithColor (whiteColor, 1, 0),
				SKAction.WaitForDuration (0.75f),
				SKAction.ColorizeWithColorBlendFactor (0, 0.1)
			});
		}

		#endregion
	}
}


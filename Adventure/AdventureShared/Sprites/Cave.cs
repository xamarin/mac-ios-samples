using System;
using System.Linq;
using System.Collections.Generic;

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
	public class Cave : EnemyCharacter
	{
		private new const int CollisionRadius = 90;
		private const int CaveCapacity = 50;

		public List<Goblin> ActiveGoblins { get; private set; }

		private List<Goblin> _inactiveGoblins;
		private SKEmitterNode _smokeEmitter;

		private static SKNode _sharedCaveBase = null;
		private static SKNode _sharedCaveTop = null;

		private static SKSpriteNode _sharedDeathSplort;

		private SKSpriteNode DeathSplort {
			get {
				return _sharedDeathSplort;
			}
		}

		private static SKEmitterNode _sharedDamageEmitter;

		protected override SKEmitterNode DamageEmitter {
			get {
				return _sharedDamageEmitter;
			}
		}

		private static SKEmitterNode _sharedDeathEmitter;

		private SKEmitterNode DeathEmitter {
			get {
				return _sharedDeathEmitter;
			}
		}

		private static SKAction _sharedDamageAction;

		protected override SKAction DamageAction {
			get {
				return _sharedDamageAction;
			}
		}

		protected override SKTexture[] IdleAnimationFrames {
			get {
				return null;
			}
		}

		protected override SKTexture[] WalkAnimationFrames {
			get {
				return null;
			}
		}

		protected override SKTexture[] AttackAnimationFrames {
			get {
				return null;
			}
		}

		protected override SKTexture[] GetHitAnimationFrames {
			get {
				return null;
			}
		}

		protected override SKTexture[] DeathAnimationFrames {
			get {
				return null;
			}
		}

		public nfloat TimeUntilNextGenerate { get; set; }

		#region Cap on Generation

		private static int sGlobalAllocation = 0;

		public static int GoblinCap { get; set; }

		#endregion

		public Cave (CGPoint position)
			: base (new SKNode[] {
				(SKNode)((NSObject)_sharedCaveBase).Copy (),
				(SKNode)((NSObject)_sharedCaveTop).Copy ()
			}, position, 50)
		{
			double randomDelay = new Random ().NextDouble ();
			TimeUntilNextGenerate = 5f * (1f + (float)randomDelay);

			ActiveGoblins = new List<Goblin> ();
			_inactiveGoblins = new List<Goblin> ();

			for (int i = 0; i < CaveCapacity; i++) {
				Goblin goblin = new Goblin (Position) {
					Cave = this
				};
				_inactiveGoblins.Add (goblin);
			}

			MovementSpeed = 0f;
			PickRandomFacingFor (position);

			// Make it AWARE!
			Intelligence = new SpawnAI (this);
		}

		private void PickRandomFacingFor (CGPoint position)
		{
			MultiplayerLayeredCharacterScene scene = CharacterScene;

			Random rnd = new Random ();

			// Pick best random facing from 8 test rays.
			nfloat maxDoorCanSee = 0;
			nfloat preferredZRotation = 0;

			for (int i = 0; i < 8; i++) {
				var testZ = rnd.NextDouble () * (2 * Math.PI);
				var x = -Math.Sin (testZ) * 1024 + position.X;
				var y = Math.Cos (testZ) * 1024 + position.Y;

				CGPoint pos2 = new CGPoint ((int)x, (int)y);

				nfloat dist = 0;
				if (scene != null)
					dist = scene.DistanceToWall (position, pos2);

				if (dist > maxDoorCanSee) {
					maxDoorCanSee = dist;
					preferredZRotation = (float)testZ;
				}
			}

			ZRotation = preferredZRotation;
		}

		#region Overridden Methods

		public override void ConfigurePhysicsBody ()
		{
			PhysicsBody = SKPhysicsBody.CreateCircularBody (CollisionRadius);
			PhysicsBody.Dynamic = false;

			Animated = false;
			ZPosition = -0.85f;

			// Our object type for collisions
			PhysicsBody.CategoryBitMask = (uint)ColliderType.Cave;

			// Collides with these objects
			PhysicsBody.CollisionBitMask = (uint)(ColliderType.Projectile | ColliderType.Hero);

			// We want notifications for colliding with these objects
			PhysicsBody.ContactTestBitMask = (uint)ColliderType.Projectile;
		}

		public override void Reset ()
		{
			base.Reset ();

			Animated = false;
		}

		public override void CollidedWith (SKPhysicsBody other)
		{
			if (Health <= 0)
				return;

			if ((other.CategoryBitMask & (uint)ColliderType.Projectile) == 0)
				return;

			float damage = 10f;
			bool killed = ApplyDamage (damage, other.Node);

			if (killed)
				CharacterScene.AddToScoreAfterEnemyKill (25, other.Node);
		}

		public override bool ApplyDamage (nfloat damage)
		{
			if (base.ApplyDamage (damage))
				return true;

			// Show damage
			UpdateSmokeForHealth ();

			// Show damage on parallax stacks.
			foreach (SKNode node in Children)
				node.RunAction (DamageAction);

			return false;
		}

		public override void PerformDeath ()
		{
			base.PerformDeath ();

			var splort = (SKNode)((NSObject)DeathSplort).Copy ();
			splort.ZPosition = -1;
			splort.ZRotation = VirtualZRotation;
			splort.Position = Position;
			splort.Alpha = 0.1f;
			splort.RunAction (SKAction.FadeAlphaTo (1, 0.5));

			MultiplayerLayeredCharacterScene scene = CharacterScene;

			scene.AddNode (splort, WorldLayer.BelowCharacter);

			RunAction (SKAction.Sequence (new SKAction[] {
				SKAction.FadeAlphaTo (0, 0.5f),
				SKAction.RemoveFromParent ()
			}));

			_smokeEmitter.RunAction (SKAction.Sequence (new SKAction[] {
				SKAction.WaitForDuration (2),
				SKAction.Run (() => {
					_smokeEmitter.ParticleBirthRate = 2;
				}),

				SKAction.WaitForDuration (2),
				SKAction.Run (() => {
					_smokeEmitter.ParticleBirthRate = 0;
				}),

				SKAction.WaitForDuration (10),
				SKAction.FadeAlphaTo (0, 0.5),
				SKAction.RemoveFromParent ()
			}));

			_inactiveGoblins.Clear ();
		}

		#endregion

		#region Damage Smoke Emitter

		private void UpdateSmokeForHealth ()
		{
			// Add smoke if health is < 75.
			if (Health > 75f || _smokeEmitter != null)
				return;

			SKEmitterNode emitter = (SKEmitterNode)((NSObject)DeathEmitter).Copy ();
			emitter.Position = Position;
			emitter.ZPosition = -0.8f;
			_smokeEmitter = emitter;
			((MultiplayerLayeredCharacterScene)Scene).AddNode (emitter, WorldLayer.AboveCharacter);
		}

		#endregion

		#region Loop Update

		public override void UpdateWithTimeSinceLastUpdate (double interval)
		{
			base.UpdateWithTimeSinceLastUpdate (interval);

			foreach (Goblin goblin in ActiveGoblins)
				goblin.UpdateWithTimeSinceLastUpdate (interval);
		}

		#endregion

		#region Goblin Targets

		public void StopGoblinsFromTargettingHero (Character target)
		{
			foreach (Goblin goblin in ActiveGoblins)
				goblin.Intelligence.ClearTarget (target);
		}

		#endregion

		#region Generating and Recycling

		public void Generate ()
		{
			if (GoblinCap <= 0 || sGlobalAllocation >= GoblinCap)
				return;

			Goblin character = _inactiveGoblins [_inactiveGoblins.Count - 1];
			if (character == null)
				return;

			var offset = CollisionRadius * 0.75f;
			var rot = GraphicsUtilities.PalarAdjust (VirtualZRotation);
			CGPoint pos = new CGPoint ((float)Math.Cos (rot) * offset, (float)Math.Sin (rot) * offset);
			character.Position = new CGPoint (pos.X + Position.X, pos.Y + Position.Y);

			MultiplayerLayeredCharacterScene scene = CharacterScene;
			character.AddToScene (CharacterScene);

			character.ZPosition = -1f;
			character.FadeIn (0.5f);

			_inactiveGoblins.Remove (character);
			ActiveGoblins.Add (character);
			sGlobalAllocation++;
		}

		public void Recycle (Goblin goblin)
		{
			if (goblin == null)
				throw new ArgumentNullException ("goblin");

			goblin.Reset ();

			ActiveGoblins.Remove (goblin);
			_inactiveGoblins.Add (goblin);

			sGlobalAllocation--;
		}

		#endregion

		#region Shared Resources

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

			SKEmitterNode fire = GraphicsUtilities.EmitterNodeWithEmitterNamed ("CaveFire");
			fire.ZPosition = 1;

			SKEmitterNode smoke = GraphicsUtilities.EmitterNodeWithEmitterNamed ("CaveFireSmoke");

			SKNode torch = new SKNode {
				fire,
				smoke
			};

			_sharedCaveBase = SKSpriteNode.FromTexture (atlas.TextureNamed ("cave_base.png"));

			// Add two torches either side of the entrance.
			torch.Position = new CGPoint (83, 83);
			_sharedCaveBase.AddChild (torch);
			SKNode torchB = (SKNode)((NSObject)torch).Copy ();
			torchB.Position = new CGPoint (-83, 83);
			_sharedCaveBase.AddChild (torchB);

			_sharedCaveTop = SKSpriteNode.FromTexture (atlas.TextureNamed ("cave_top.png"));
			_sharedDeathSplort = SKSpriteNode.FromTexture (atlas.TextureNamed ("cave_destroyed.png"));

			_sharedDamageEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("CaveDamage");
			_sharedDeathEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("CaveDeathSmoke");

			_sharedDamageAction = SKAction.Sequence (new SKAction[] {
				SKAction.ColorizeWithColor (whiteColor, 1, 0),
				SKAction.WaitForDuration (0.25),
				SKAction.ColorizeWithColorBlendFactor (0, 0.1),
			});
		}

		#endregion
	}
}
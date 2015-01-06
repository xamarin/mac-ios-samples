using System;
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
		const int CollisionRadius = 90;
		const int CaveCapacity = 50;

		public List<Goblin> ActiveGoblins { get; private set; }

		List<Goblin> inactiveGoblins;
		SKEmitterNode smokeEmitter;

		static SKNode sharedCaveBase;
		static SKNode sharedCaveTop;

		static SKSpriteNode sharedDeathSplort;

		SKSpriteNode DeathSplort {
			get {
				return sharedDeathSplort;
			}
		}

		static SKEmitterNode sharedDamageEmitter;

		protected override SKEmitterNode DamageEmitter {
			get {
				return sharedDamageEmitter;
			}
		}

		static SKEmitterNode sharedDeathEmitter;

		SKEmitterNode DeathEmitter {
			get {
				return sharedDeathEmitter;
			}
		}

		static SKAction sharedDamageAction;

		protected override SKAction DamageAction {
			get {
				return sharedDamageAction;
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

		static int sGlobalAllocation = 0;

		public static int GoblinCap { get; set; }

		#endregion

		public Cave (CGPoint position)
			: base (new [] {
				(SKNode)sharedCaveBase.Copy (),
				(SKNode)sharedCaveTop.Copy ()
			}, position, 50)
		{
			double randomDelay = new Random ().NextDouble ();
			TimeUntilNextGenerate = 5f * (1f + (float)randomDelay);

			ActiveGoblins = new List<Goblin> ();
			inactiveGoblins = new List<Goblin> ();

			for (int i = 0; i < CaveCapacity; i++) {
				var goblin = new Goblin (Position) {
					Cave = this
				};
				inactiveGoblins.Add (goblin);
			}

			MovementSpeed = 0f;
			PickRandomFacingFor (position);

			// Make it AWARE!
			Intelligence = new SpawnAI (this);
		}

		void PickRandomFacingFor (CGPoint position)
		{
			MultiplayerLayeredCharacterScene scene = CharacterScene;

			var rnd = new Random ();

			// Pick best random facing from 8 test rays.
			nfloat maxDoorCanSee = 0;
			nfloat preferredZRotation = 0;

			for (int i = 0; i < 8; i++) {
				var testZ = rnd.NextDouble () * (2 * Math.PI);
				var x = -Math.Sin (testZ) * 1024 + position.X;
				var y = Math.Cos (testZ) * 1024 + position.Y;

				var pos2 = new CGPoint ((int)x, (int)y);

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

			var splort = (SKNode)DeathSplort.Copy ();
			splort.ZPosition = -1;
			splort.ZRotation = VirtualZRotation;
			splort.Position = Position;
			splort.Alpha = 0.1f;
			splort.RunAction (SKAction.FadeAlphaTo (1, 0.5));

			MultiplayerLayeredCharacterScene scene = CharacterScene;

			scene.AddNode (splort, WorldLayer.BelowCharacter);

			RunAction (SKAction.Sequence (new [] {
				SKAction.FadeAlphaTo (0, 0.5f),
				SKAction.RemoveFromParent ()
			}));

			smokeEmitter.RunAction (SKAction.Sequence (new [] {
				SKAction.WaitForDuration (2),
				SKAction.Run (() => {
					smokeEmitter.ParticleBirthRate = 2;
				}),

				SKAction.WaitForDuration (2),
				SKAction.Run (() => {
					smokeEmitter.ParticleBirthRate = 0;
				}),

				SKAction.WaitForDuration (10),
				SKAction.FadeAlphaTo (0, 0.5),
				SKAction.RemoveFromParent ()
			}));

			inactiveGoblins.Clear ();
		}

		#endregion

		#region Damage Smoke Emitter

		void UpdateSmokeForHealth ()
		{
			// Add smoke if health is < 75.
			if (Health > 75f || smokeEmitter != null)
				return;

			var emitter = (SKEmitterNode)DeathEmitter.Copy ();
			emitter.Position = Position;
			emitter.ZPosition = -0.8f;
			smokeEmitter = emitter;
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

			Goblin character = inactiveGoblins [inactiveGoblins.Count - 1];
			if (character == null)
				return;

			var offset = CollisionRadius * 0.75f;
			var rot = GraphicsUtilities.PalarAdjust (VirtualZRotation);
			var pos = new CGPoint ((float)Math.Cos (rot) * offset, (float)Math.Sin (rot) * offset);
			character.Position = new CGPoint (pos.X + Position.X, pos.Y + Position.Y);

			MultiplayerLayeredCharacterScene scene = CharacterScene;
			character.AddToScene (CharacterScene);

			character.ZPosition = -1f;
			character.FadeIn (0.5f);

			inactiveGoblins.Remove (character);
			ActiveGoblins.Add (character);
			sGlobalAllocation++;
		}

		public void Recycle (Goblin goblin)
		{
			if (goblin == null)
				throw new ArgumentNullException ("goblin");

			goblin.Reset ();

			ActiveGoblins.Remove (goblin);
			inactiveGoblins.Add (goblin);

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

			var sKNode = new SKNode ();
			sKNode.Add (fire);
			sKNode.Add (smoke);
			SKNode torch = sKNode;

			sharedCaveBase = SKSpriteNode.FromTexture (atlas.TextureNamed ("cave_base.png"));

			// Add two torches either side of the entrance.
			torch.Position = new CGPoint (83, 83);
			sharedCaveBase.AddChild (torch);
			var torchB = (SKNode)torch.Copy ();
			torchB.Position = new CGPoint (-83, 83);
			sharedCaveBase.AddChild (torchB);

			sharedCaveTop = SKSpriteNode.FromTexture (atlas.TextureNamed ("cave_top.png"));
			sharedDeathSplort = SKSpriteNode.FromTexture (atlas.TextureNamed ("cave_destroyed.png"));

			sharedDamageEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("CaveDamage");
			sharedDeathEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("CaveDeathSmoke");

			sharedDamageAction = SKAction.Sequence (new [] {
				SKAction.ColorizeWithColor (whiteColor, 1, 0),
				SKAction.WaitForDuration (0.25),
				SKAction.ColorizeWithColorBlendFactor (0, 0.1),
			});
		}

		#endregion
	}
}
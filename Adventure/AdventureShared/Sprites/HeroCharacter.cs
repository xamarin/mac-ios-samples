using System;

using SpriteKit;
using Foundation;
using CoreGraphics;

namespace Adventure
{
	public abstract class HeroCharacter : Character
	{
		const float HeroProjectileSpeed = 480;
		const float HeroProjectileLifetime = 1;
		// 1.0 seconds until the projectile disappears
		const float HeroProjectileFadeOutTime = 0.6f;
		// 0.6 seconds until the projectile starts to fade out

		static SKAction sharedProjectileSoundAction;

		SKAction ProjectileSoundAction {
			get {
				return sharedProjectileSoundAction;
			}
		}

		static SKEmitterNode sharedDeathEmitter;

		SKEmitterNode DeathEmitter {
			get {
				return sharedDeathEmitter;
			}
		}

		static SKEmitterNode sharedDamageEmitter;

		protected override SKEmitterNode DamageEmitter {
			get {
				return sharedDamageEmitter;
			}
		}

		public abstract SKSpriteNode Projectile {
			get;
		}

		public abstract SKEmitterNode ProjectileEmitter {
			get;
		}

		public Player Player { get; private set; }

		protected HeroCharacter (CGPoint position, Player player)
			: this (null, position, player)
		{
		}

		protected HeroCharacter (SKTexture texture, CGPoint position, Player player)
			: base (texture, position)
		{
			Player = player;

			// Rotate by PI radians (180 degrees) so hero faces down rather than toward wall at start of game.
			ZRotation = (float)Math.PI;
			ZPosition = -0.25f;
			Name = "Hero";
		}

		#region Overridden Methods

		public override void ConfigurePhysicsBody ()
		{
			PhysicsBody = SKPhysicsBody.CreateCircularBody (CollisionRadius);

			// Our object type for collisions.
			PhysicsBody.CategoryBitMask = (uint)ColliderType.Hero;

			// Collides with these objects.
			PhysicsBody.CollisionBitMask = (uint)(ColliderType.GoblinOrBoss
			| ColliderType.Hero
			| ColliderType.Wall
			| ColliderType.Cave);

			// We want notifications for colliding with these objects.
			PhysicsBody.ContactTestBitMask = (uint)ColliderType.GoblinOrBoss;
		}

		public override void CollidedWith (SKPhysicsBody other)
		{
			if ((other.CategoryBitMask & (uint)ColliderType.GoblinOrBoss) != 0) {
				var enemy = (Character)other.Node;
				if (!enemy.Dying) {
					ApplyDamage (5);
					RequestedAnimation = AnimationState.GetHit;
				}
			}
		}

		public override void AnimationDidComplete (AnimationState animation)
		{
			switch (animation) {
			case AnimationState.Death:
				var emitter = (SKEmitterNode)DeathEmitter.Copy ();
				emitter.ZPosition = -0.8f;
				AddChild (emitter);
				GraphicsUtilities.RunOneShotEmitter (emitter, 4.5f);

				RunAction (SKAction.Sequence (new [] {
					SKAction.WaitForDuration (4),
					SKAction.Run (() => CharacterScene.HeroWasKilled (this)),
					SKAction.RemoveFromParent ()
				}));
				break;

			case AnimationState.Attack:
				FireProjectile ();
				break;
			}
		}

		#endregion

		#region Projectiles

		public void FireProjectile ()
		{
			var projectile = (SKSpriteNode)Projectile.Copy ();
			projectile.Position = Position;
			projectile.ZRotation = ZRotation;

			var emitter = (SKEmitterNode)ProjectileEmitter.Copy ();
			emitter.TargetNode = CharacterScene.GetChildNode ("world");
			projectile.AddChild (emitter);

			CharacterScene.AddNode (projectile, WorldLayer.Character);

			var rot = ZRotation;

			float x = -(float)Math.Sin (rot) * HeroProjectileSpeed * HeroProjectileLifetime;
			float y = (float)Math.Cos (rot) * HeroProjectileSpeed * HeroProjectileLifetime;
			projectile.RunAction (SKAction.MoveBy (x, y, HeroProjectileLifetime));

			projectile.RunAction (SKAction.Sequence (new [] {
				SKAction.WaitForDuration (HeroProjectileFadeOutTime),
				SKAction.FadeOutWithDuration (HeroProjectileLifetime - HeroProjectileFadeOutTime),
				SKAction.RemoveFromParent ()
			}));
			projectile.RunAction (ProjectileSoundAction);

			var userData = new UserData {
				Player = Player
			};


			projectile.UserData = (NSMutableDictionary)userData.Dictionary;
		}

		#endregion

		#region Shared Assets

		public static void LoadSharedAssetsOnce ()
		{
			sharedProjectileSoundAction = SKAction.PlaySoundFileNamed ("magicmissile.caf", false);
			sharedDeathEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Death");
			sharedDamageEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Damage");
		}

		#endregion
	}
}
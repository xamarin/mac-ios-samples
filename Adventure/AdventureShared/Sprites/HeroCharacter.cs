using System;

using SpriteKit;
using Foundation;
using CoreGraphics;

namespace Adventure
{
	public abstract class HeroCharacter : Character
	{
		private float HeroProjectileSpeed = 480;
		private float HeroProjectileLifetime = 1;
		// 1.0 seconds until the projectile disappears
		private float HeroProjectileFadeOutTime = 0.6f;
		// 0.6 seconds until the projectile starts to fade out

		private static SKAction _sharedProjectileSoundAction;

		private SKAction ProjectileSoundAction {
			get {
				return _sharedProjectileSoundAction;
			}
		}

		private static SKEmitterNode _sharedDeathEmitter;

		private SKEmitterNode DeathEmitter {
			get {
				return _sharedDeathEmitter;
			}
		}

		private static SKEmitterNode _sharedDamageEmitter;

		protected override SKEmitterNode DamageEmitter {
			get {
				return _sharedDamageEmitter;
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
				SKEmitterNode emitter = (SKEmitterNode)((NSObject)DeathEmitter).Copy ();
				emitter.ZPosition = -0.8f;
				AddChild (emitter);
				GraphicsUtilities.RunOneShotEmitter (emitter, 4.5f);

				RunAction (SKAction.Sequence (new SKAction[] {
					SKAction.WaitForDuration (4),
					SKAction.Run (() => {
						CharacterScene.HeroWasKilled (this);
					}),
					SKAction.RemoveFromParent ()
				}));
				break;

			case AnimationState.Attack:
				FireProjectile ();
				break;

			default:
				break;
			}
		}

		#endregion

		#region Projectiles

		public void FireProjectile ()
		{
			SKSpriteNode projectile = (SKSpriteNode)((NSObject)Projectile).Copy ();
			projectile.Position = Position;
			projectile.ZRotation = ZRotation;

			SKEmitterNode emitter = (SKEmitterNode)((NSObject)ProjectileEmitter).Copy ();
			emitter.TargetNode = CharacterScene.GetChildNode ("world");
			projectile.AddChild (emitter);

			CharacterScene.AddNode (projectile, WorldLayer.Character);

			var rot = ZRotation;

			float x = -(float)Math.Sin (rot) * HeroProjectileSpeed * HeroProjectileLifetime;
			float y = (float)Math.Cos (rot) * HeroProjectileSpeed * HeroProjectileLifetime;
			projectile.RunAction (SKAction.MoveBy (x, y, HeroProjectileLifetime));

			projectile.RunAction (SKAction.Sequence (new SKAction[] {
				SKAction.WaitForDuration (HeroProjectileFadeOutTime),
				SKAction.FadeOutWithDuration (HeroProjectileLifetime - HeroProjectileFadeOutTime),
				SKAction.RemoveFromParent ()
			}));
			projectile.RunAction (ProjectileSoundAction);

			UserData userData = new UserData {
				Player = Player
			};


			projectile.UserData = (NSMutableDictionary)userData.Dictionary;
		}

		#endregion

		#region Shared Assets

		public static void LoadSharedAssetsOnce ()
		{
			_sharedProjectileSoundAction = SKAction.PlaySoundFileNamed ("magicmissile.caf", false);
			_sharedDeathEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Death");
			_sharedDamageEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Damage");
		}

		#endregion
	}
}
using System;
using System.Threading;

using SpriteKit;
using Foundation;
using CoreGraphics;

namespace Adventure
{
	public abstract class Character : ParallaxSprite
	{
		public static readonly int CollisionRadius = 40;
		protected static readonly int ProjectileCollisionRadius = 15;

		protected static readonly int DefaultNumberOfWalkFrames = 28;
		protected static readonly int DefaultNumberOfIdleFrames = 28;

		protected static readonly float Velocity = 200;
		private const float RotationSpeed = 0.06f;

		public bool Dying { get; private set; }

		public nfloat Health { get; private set; }

		public bool Attacking { get; protected set; }

		public bool Animated { get; protected set; }

		public float AnimationSpeed { get; protected set; }

		public float MovementSpeed { get; protected set; }

		public string ActiveAnimationKey { get; private set; }

		public AnimationState RequestedAnimation { get; protected set; }

		protected SKSpriteNode ShadowBlob { get; private set; }

		// Provide an emitter to show damage applied to character
		protected abstract SKEmitterNode DamageEmitter {
			get;
		}

		// Action to run when damage is applied
		protected abstract SKAction DamageAction {
			get;
		}

		protected abstract SKTexture[] IdleAnimationFrames {
			get;
		}

		protected abstract SKTexture[] WalkAnimationFrames {
			get;
		}

		protected abstract SKTexture[] AttackAnimationFrames {
			get;
		}

		protected abstract SKTexture[] GetHitAnimationFrames {
			get;
		}

		protected abstract SKTexture[] DeathAnimationFrames {
			get;
		}

		public override void SetScale (nfloat scale)
		{
			base.SetScale (scale);
			ShadowBlob.SetScale (scale);
		}

		public override nfloat Alpha {
			get {
				return base.Alpha;
			}
			set {
				base.Alpha = value;
				ShadowBlob.Alpha = value;
			}
		}

		public MultiplayerLayeredCharacterScene CharacterScene {
			get {
				return (MultiplayerLayeredCharacterScene)Scene;
			}
		}

		public Character (SKTexture texture, CGPoint position)
			: base (texture)
		{
			// standard sprite - there's no parallax
			UsesParallaxEffect = false;
			Initialize (position);
		}

		public Character (SKNode[] sprites, CGPoint position, float offset)
			: base (sprites, offset)
		{
			Initialize (position);
		}

		private void Initialize (CGPoint position)
		{
			var atlas = SKTextureAtlas.FromName ("Environment");

			ShadowBlob = new SKSpriteNode (atlas.TextureNamed ("blobShadow.png")) {
				ZPosition = -1f
			};

			Position = position;

			Health = 100f;
			MovementSpeed = Velocity;
			Animated = true;
			AnimationSpeed = 1f / 28f;

			ConfigurePhysicsBody ();
		}

		// Reset some base states (used when recycling character instances)
		public virtual void Reset ()
		{
			Health = 100f;
			Dying = false;
			Attacking = false;
			Animated = true;
			RequestedAnimation = AnimationState.Idle;
			ShadowBlob.Alpha = 1f;
		}

		#region Overridden Methods

		public abstract void ConfigurePhysicsBody ();

		public abstract void AnimationDidComplete (AnimationState animation);

		public abstract void CollidedWith (SKPhysicsBody other);

		public virtual void PerformDeath ()
		{
			Health = 0;
			Dying = true;
			RequestedAnimation = AnimationState.Death;
		}

		#endregion

		#region Applying Damage - i.e., decrease health

		// Apply damage and return true if death.
		public virtual bool ApplyDamage (nfloat damage)
		{
			Health -= damage;

			if (Health <= 0) {
				PerformDeath ();
				return true;
			}

			var emitter = (SKEmitterNode)((NSObject)DamageEmitter).Copy ();
			CharacterScene.AddNode (emitter, WorldLayer.AboveCharacter);
			emitter.Position = Position;
			GraphicsUtilities.RunOneShotEmitter (emitter, 0.15f);

			// Show the damage.
			RunAction (DamageAction);

			return false;
		}

		// Use projectile alpha to determine potency
		public bool ApplyDamage (nfloat damage, SKNode projectile)
		{
			return ApplyDamage (damage * projectile.Alpha);
		}

		#endregion

		#region Loop Update - called once per frame

		public virtual void UpdateWithTimeSinceLastUpdate (double interval)
		{
			// Shadow always follows our main sprite
			ShadowBlob.Position = Position;

			if (Animated)
				ResolveRequestedAnimation ();
		}

		#endregion

		#region Animation

		protected void ResolveRequestedAnimation ()
		{
			// Determine the animation we want to play.
			string animationKey = null;
			SKTexture[] animationFrames = null;

			switch (RequestedAnimation) {
			default:
			case AnimationState.Idle:
				animationKey = "anim_idle";
				animationFrames = IdleAnimationFrames;
				break;

			case AnimationState.Walk:
				animationKey = "anim_walk";
				animationFrames = WalkAnimationFrames;
				break;

			case AnimationState.Attack:
				animationKey = "anim_attack";
				animationFrames = AttackAnimationFrames;
				break;

			case AnimationState.GetHit:
				animationKey = "anim_gethit";
				animationFrames = GetHitAnimationFrames;
				break;

			case AnimationState.Death:
				animationKey = "anim_death";
				animationFrames = DeathAnimationFrames;
				break;
			}

			if (animationKey != null)
				FireAnimationForState (RequestedAnimation, animationFrames, animationKey);

			RequestedAnimation = Dying ? AnimationState.Death : AnimationState.Idle;
		}

		private void FireAnimationForState (AnimationState animationState, SKTexture[] frames, string key)
		{
			SKAction animAction = GetActionForKey (key);
			if (animAction != null || frames.Length < 1)
				return; // we already have a running animation or there aren't any frames to animate

			ActiveAnimationKey = key;
			RunAction (SKAction.Sequence (new SKAction[] {
				SKAction.AnimateWithTextures (frames, AnimationSpeed, true, false),
				SKAction.Run (() => {
					AnimationHasCompleted (animationState);
				})
			}), key);
		}

		public void FadeIn (float duration)
		{
			// Fade in the main sprite and blob shadow.
			SKAction fadeAction = SKAction.FadeInWithDuration (duration);

			Alpha = 0;
			RunAction (fadeAction);

			ShadowBlob.Alpha = 0;
			ShadowBlob.RunAction (fadeAction);
		}

		private void AnimationHasCompleted (AnimationState animationState)
		{
			if (Dying) {
				Animated = false;
				ShadowBlob.RunAction (SKAction.FadeOutWithDuration (1.5f));
			}

			AnimationDidComplete (animationState);

			if (Attacking)
				Attacking = false;

			ActiveAnimationKey = null;
		}

		#endregion

		#region Working with Scenes

		public void AddToScene (MultiplayerLayeredCharacterScene scene)
		{
			if (scene == null)
				throw new ArgumentNullException ("scene");

			scene.AddNode (this, WorldLayer.Character);
			scene.AddNode (this.ShadowBlob, WorldLayer.BelowCharacter);
		}

		public override void RemoveFromParent ()
		{
			ShadowBlob.RemoveFromParent ();
			base.RemoveFromParent ();
		}

		#endregion

		#region Orientation, Movement, and Attacking

		public void Move (MoveDirection direction, double timeInterval)
		{
			var rot = ZRotation;

			SKAction action = null;
			float x, y;
			// Build up the movement action.
			switch (direction) {
			case MoveDirection.Forward:
				x = -(float)(Math.Sin (rot) * MovementSpeed * timeInterval);
				y = (float)(Math.Cos (rot) * MovementSpeed * timeInterval);
				action = SKAction.MoveBy (x, y, timeInterval);
				break;

			case MoveDirection.Back:
				x = (float)(Math.Sin (rot) * MovementSpeed * timeInterval);
				y = -(float)(Math.Cos (rot) * MovementSpeed * timeInterval);
				action = SKAction.MoveBy (x, y, timeInterval);
				break;

			case MoveDirection.Left:
				action = SKAction.RotateByAngle (RotationSpeed, timeInterval);
				break;

			case MoveDirection.Right:
				action = SKAction.RotateByAngle (-RotationSpeed, timeInterval);
				break;
			}

			// Play the resulting action.
			if (action != null) {
				RequestedAnimation = AnimationState.Walk;
				RunAction (action);
			}
		}

		public void FaceTo (CGPoint position)
		{
			var angle = GraphicsUtilities.RadiansBetweenCGPoints (position, Position);
			var ang = GraphicsUtilities.PalarAdjust (angle);
			SKAction action = SKAction.RotateToAngle (ang, 0);
			RunAction (action);
		}

		public void MoveTowards (CGPoint position, double timeInterval)
		{
			CGPoint curPosition = Position;
			var dx = position.X - curPosition.X;
			var dy = position.Y - curPosition.Y;
			var ds = MovementSpeed * (float)timeInterval;

			var angle = GraphicsUtilities.RadiansBetweenCGPoints (position, curPosition);
			angle = GraphicsUtilities.PalarAdjust (angle);
			ZRotation = angle;

			var distRemaining = GraphicsUtilities.Hypotenuse (dx, dy);
			if (distRemaining < ds) {
				Position = position;
			} else {
				var x = (float)(curPosition.X - Math.Sin (angle) * ds);
				var y = (float)(curPosition.Y + Math.Cos (angle) * ds);
				Position = new CGPoint (x, y);
			}

			RequestedAnimation = AnimationState.Walk;
		}

		public void MoveInDirection (CGPoint direction, double timeInterval)
		{
			var curPosition = Position;
			var dx = MovementSpeed * direction.X;
			var dy = MovementSpeed * direction.Y;
			var ds = MovementSpeed * (float)timeInterval;

			var targetPosition = new CGPoint (curPosition.X + dx, curPosition.Y + dy);

			var angle = GraphicsUtilities.RadiansBetweenCGPoints (targetPosition, curPosition);
			var ang = GraphicsUtilities.PalarAdjust (angle);
			ZRotation = ang;

			var distRemaining = GraphicsUtilities.Hypotenuse (dx, dy);
			if (distRemaining < ds) {
				Position = targetPosition;
			} else {
				float x = (float)(curPosition.X - Math.Sin (ang) * ds);
				float y = (float)(curPosition.Y + Math.Cos (ang) * ds);
				Position = new CGPoint (x, y);
			}

			// Don't change to a walk animation if we planning an attack.
			if (!Attacking)
				RequestedAnimation = AnimationState.Walk;
		}

		public void PerformAttackAction ()
		{
			if (Attacking)
				return;

			Attacking = true;
			RequestedAnimation = AnimationState.Attack;
		}

		#endregion
	}
}


using System;
using SpriteKit;
using CoreGraphics;
using System.Threading;
using System.Collections.Generic;

#if __IOS__
using UIKit;
#else
using AppKit;
#endif

using Foundation;
using ObjCRuntime;
using GameController;

namespace Adventure
{
	public abstract class MultiplayerLayeredCharacterScene : SKScene
	{
		const float MIN_TIME_INTERVAL = 1f / 60f;
		//kNumPlayers
		const int NUM_PLAYERS = 4;
		// minimum distance between hero and edge of camera before moving camera
		const int MIN_HERO_TO_EDGE_DISTANCE = 256;

		const float HERO_PROJECTILE_SPEED = 480f;
		const float HERO_PROJECTILE_LIFETIME = 1f;
		const float HERO_PROJECTILE_FADE_OUT_TIME = 0.6f;

		// player '1' controlled by keyboard/touch
		protected Player DefaultPlayer { get; private set; }
		// array of player objects or NSNull for no player
		List<Player> players;
		// root node to which all game renderables are attached
		SKNode world;

		List<SKNode> layers;

		// the CGPoint at which heroes are spawned
		protected CGPoint DefaultSpawnCGPoint { get; set; }
		// indicates the world moved before or during the current update
		protected bool WorldMovedForUpdate { get; private set; }
		// all heroes in the game
		public List<HeroCharacter> Heroes { get; private set; }

		// keep track of the various nodes for the HUD
		List<SKSpriteNode> hudAvatars;
		// - there are always 'kNumPlayers' instances in each array
		List<SKLabelNode> hudLabels;
		List<SKLabelNode> hudScores;
		// an array of NSArrays of life hearts
		List<List<SKSpriteNode>> hudLifeHeartArrays;

		double lastUpdateTimeInterval;

		NSObject didConnectObserver;
		NSObject didDisconnectObserver;

		bool isAssetsLoaded;

		// Overridden by subclasses to provide an emitter used to indicate when a new hero is spawned.
		protected abstract SKEmitterNode SharedSpawnEmitter { get; }

		public MultiplayerLayeredCharacterScene (CGSize size) : base (size)
		{
		}

		public virtual void Initialize ()
		{
			Console.WriteLine (IntPtr.Size);
			Heroes = new List<HeroCharacter> ();
			players = new List<Player> (NUM_PLAYERS);
			DefaultPlayer = new Player ();
			players.Add (DefaultPlayer);
			for (int i = 1; i < NUM_PLAYERS; i++)
				players.Add (null);

			world = new SKNode () {
				Name = "world"
			};
			layers = new List<SKNode> ((int)WorldLayer.Count);
			for (int i = 0; i < (int)WorldLayer.Count; i++) {
				var layer = new SKNode {
					ZPosition = i - (int)WorldLayer.Count
				};
				world.AddChild (layer);
				layers.Add (layer);
			}
			AddChild (world);

			buildHUD ();
			updateHUDFor (DefaultPlayer, HUDState.Local);
		}

		protected override void Dispose (bool disposing)
		{
			// unsubscribe
			// For more info visit: http://iosapi.xamarin.com/?link=T%3aFoundation.NSNotificationCenter
			didConnectObserver.Dispose ();
			didDisconnectObserver.Dispose ();

			base.Dispose (disposing);
		}

		#region Characters

		protected HeroCharacter AddHeroFor (Player player)
		{
			if (player == null)
				throw new ArgumentNullException ("player", "Player should not be null");

			if (player.Hero != null && !player.Hero.Dying)
				player.Hero.RemoveFromParent ();

			CGPoint spawnPos = DefaultSpawnCGPoint;
			HeroCharacter hero = CreateHeroBy (player.HeroType, spawnPos, player);

			if (hero != null) {
				var emitter = (SKEmitterNode)SharedSpawnEmitter.Copy ();
				emitter.Position = spawnPos;
				AddNode (emitter, WorldLayer.AboveCharacter);
				GraphicsUtilities.RunOneShotEmitter (emitter, 0.15f);

				hero.FadeIn (2f);
				hero.AddToScene (this);
				Heroes.Add (hero);
			}
			player.Hero = hero;

			return hero;
		}

		public virtual void HeroWasKilled (HeroCharacter hero)
		{
			if (hero == null)
				throw new ArgumentNullException ("hero");

			Player player = hero.Player;

			Heroes.Remove (hero);

			#if __IOS__
			player.MoveRequested = false;
			#endif

			if (--player.LivesLeft < 1)
				return; // In a real game, you'd want to end the game when there are no lives left.

			updateHUDAfterHeroDeathFor (hero.Player);

			hero = AddHeroFor (hero.Player);
			CenterWorld (hero);
		}

		// All sprites in the scene should be added through this method to ensure they are placed in the correct world layer.
		public void AddNode (SKNode node, WorldLayer layer)
		{
			SKNode layerNode = layers [(int)layer];
			layerNode.AddChild (node);
		}

		#endregion

		#region HUD and Scores

		/* Determines the relevant player from the given projectile, and adds to that player's score. */
		public void AddToScoreAfterEnemyKill (int amount, SKNode projectile)
		{
			var userData = new UserData (projectile.UserData);
			Player player = userData.Player;
			player.Score += amount;
			updateHUDFor (player);
		}

		void buildHUD ()
		{
			string[] iconNames = new [] {
				"iconWarrior_blue",
				"iconWarrior_green",
				"iconWarrior_pink",
				"iconWarrior_red"
			};
			#if __IOS__
			UIColor[] colors = new [] { UIColor.Green, UIColor.Blue, UIColor.Yellow, UIColor.Red };
			#else
			var colors = new NSColor [] { NSColor.Green, NSColor.Blue, NSColor.Yellow, NSColor.Red };
			#endif
			var hudX = 30.0f;
			var hudY = Frame.Size.Height - 30;
			var hudD = Frame.Size.Width / NUM_PLAYERS;

			hudAvatars = new List<SKSpriteNode> (NUM_PLAYERS);
			hudLabels = new List<SKLabelNode> (NUM_PLAYERS);
			hudScores = new List<SKLabelNode> (NUM_PLAYERS);
			hudLifeHeartArrays = new List<List<SKSpriteNode>> (NUM_PLAYERS);
			var hud = new SKNode ();

			for (int i = 0; i < NUM_PLAYERS; i++) {
				var avatar = new SKSpriteNode (iconNames [i]) {
					XScale = 0.5f,
					YScale = 0.5f,
					Alpha = 0.5f,
				};
				avatar.Position = new CGPoint (hudX + i * hudD + (avatar.Size.Width * 0.5f),
					Frame.Size.Height - avatar.Size.Height * 0.5f - 8f);
				hudAvatars.Add (avatar);
				hud.AddChild (avatar);

				var label = new SKLabelNode ("Copperplate") {
					Text = "NO PLAYER",
					FontColor = colors [i],
					FontSize = 16,
					HorizontalAlignmentMode = SKLabelHorizontalAlignmentMode.Left,
					Position = new CGPoint (hudX + i * hudD + avatar.Size.Width, hudY + 10)
				};
				hudLabels.Add (label);
				hud.AddChild (label);

				var score = new SKLabelNode ("Copperplate") {
					Text = "SCORE: 0",
					FontColor = colors [i],
					FontSize = 16,
					HorizontalAlignmentMode = SKLabelHorizontalAlignmentMode.Left,
					Position = new CGPoint (hudX + i * hudD + avatar.Size.Width, hudY - 40)
				};
				hudScores.Add (score);
				hud.AddChild (score);

				var playerHearts = new List<SKSpriteNode> (Player.StartLives);
				hudLifeHeartArrays.Add (playerHearts);
				for (int j = 0; j < Player.StartLives; j++) {
					var heart = new SKSpriteNode ("lives.png") {
						XScale = 0.4f,
						YScale = 0.4f,
						Alpha = 0.1f
					};
					heart.Position = new CGPoint (hudX + i * hudD + avatar.Size.Width + 18 + (heart.Size.Width + 5) * j, hudY - 10);
					playerHearts.Add (heart);
					hud.AddChild (heart);
				}
			}

			AddChild (hud);
		}

		void updateHUDFor (Player player, HUDState state, string message = null)
		{
			int playerIndex = players.IndexOf (player);

			SKSpriteNode avatar = hudAvatars [playerIndex];
			avatar.RunAction (SKAction.Sequence (new [] {
				SKAction.FadeAlphaTo (1f, 1),
				SKAction.FadeAlphaTo (0.2f, 1),
				SKAction.FadeAlphaTo (1f, 1)
			}));

			SKLabelNode label = hudLabels [playerIndex];
			float heartAlpha = 1f;

			switch (state) {
			case HUDState.Local:
				label.Text = "ME";
				break;

			case HUDState.Connecting:
				heartAlpha = 0.25f;
				label.Text = message ?? "AVAILABLE";
				break;

			case HUDState.Disconnected:
				avatar.Alpha = 0.5f;
				heartAlpha = 0.1f;
				label.Text = "NO PLAYER";
				break;

			case HUDState.Connected:
				label.Text = message ?? "CONNECTED";
				break;

			default:
				throw new NotImplementedException ();
			}

			for (int i = 0; i < player.LivesLeft; i++) {
				SKSpriteNode heart = hudLifeHeartArrays [playerIndex] [i];
				heart.Alpha = heartAlpha;
			}
		}

		void updateHUDFor (Player player)
		{
			int playerIndex = players.IndexOf (player);
			SKLabelNode label = hudScores [playerIndex];
			label.Text = string.Format ("SCORE: {0}", player.Score);
		}

		void updateHUDAfterHeroDeathFor (Player player)
		{
			int playerIndex = players.IndexOf (player);

			// Fade out the relevant heart - one-based livesLeft has already been decremented.
			int heartNumber = player.LivesLeft;

			List<SKSpriteNode> heartArray = hudLifeHeartArrays [playerIndex];
			var heart = heartArray [heartNumber];
			heart.RunAction (SKAction.FadeAlphaTo (0, 3));
		}

		#endregion

		#region Mapping

		protected void CenterWorld (CGPoint position)
		{
			// https://developer.apple.com/library/ios/documentation/GraphicsAnimation/Conceptual/SpriteKit_PG/Nodes/Nodes.html
			world.Position = new CGPoint (-position.X + Frame.GetMidX (), -position.Y + Frame.GetMidY ());
			WorldMovedForUpdate = true;
		}

		protected void CenterWorld (Character character)
		{
			CenterWorld (character.Position);
		}

		public abstract nfloat DistanceToWall (CGPoint pos0, CGPoint pos1);

		public abstract bool CanSee (CGPoint pos0, CGPoint pos1);

		#endregion

		#region Loop Update

		public override void Update (double currentTime)
		{
			// Handle time delta.
			// If we drop below 60fps, we still want everything to move the same distance.
			double timeSinceLast = currentTime - lastUpdateTimeInterval;
			lastUpdateTimeInterval = currentTime;

			// more than a second since last update
			if (timeSinceLast > 1) {
				timeSinceLast = MIN_TIME_INTERVAL;
				WorldMovedForUpdate = true;
			}

			UpdateWithTimeSinceLastUpdate (timeSinceLast);

			var defaultPlayer = DefaultPlayer;
			HeroCharacter hero = Heroes.Count > 0 ? defaultPlayer.Hero : null;

			#if __IOS__
			if (hero != null && !hero.Dying
			    && defaultPlayer.TargetLocation != CGPoint.Empty) {
				if (defaultPlayer.FireAction)
					hero.FaceTo (defaultPlayer.TargetLocation);

				if (defaultPlayer.MoveRequested) {
					if (defaultPlayer.TargetLocation != hero.Position)
						hero.MoveTowards (defaultPlayer.TargetLocation, timeSinceLast);
					else
						defaultPlayer.MoveRequested = false;
				}
			}
			#endif

			foreach (var player in players) {
				if (player == null)
					continue;

				hero = player.Hero;
				if (hero == null || hero.Dying)
					continue;

				// heroMoveDirection is used by game controllers.
				CGPoint heroMoveDirection = player.HeroMoveDirection;
				if (GraphicsUtilities.Hypotenuse (heroMoveDirection.X, heroMoveDirection.Y) > 0f)
					hero.MoveInDirection (heroMoveDirection, timeSinceLast);
				else {
					if (player.MoveForward)
						hero.Move (MoveDirection.Forward, timeSinceLast);
					else if (player.MoveBack)
						hero.Move (MoveDirection.Back, timeSinceLast);

					if (player.MoveLeft)
						hero.Move (MoveDirection.Left, timeSinceLast);
					else if (player.MoveRight)
						hero.Move (MoveDirection.Right, timeSinceLast);
				}

				if (player.FireAction)
					hero.PerformAttackAction ();
			}
		}

		// Overridden by subclasses to update the scene - called once per frame.
		public abstract void UpdateWithTimeSinceLastUpdate (double timeSinceLast);

		public override void DidSimulatePhysics ()
		{
			base.DidSimulatePhysics ();

			HeroCharacter defaultHero = DefaultPlayer.Hero;

			// Move the world relative to the default player position.
			if (defaultHero != null) {
				CGPoint heroPosition = defaultHero.Position;
				CGPoint worldPos = world.Position;

				var yCoordinate = worldPos.Y + heroPosition.Y;
				if (yCoordinate < MIN_HERO_TO_EDGE_DISTANCE) {
					worldPos.Y = -heroPosition.Y + MIN_HERO_TO_EDGE_DISTANCE;
					WorldMovedForUpdate = true;
				} else if (yCoordinate > Frame.Size.Height - MIN_HERO_TO_EDGE_DISTANCE) {
					worldPos.Y = Frame.Size.Height - heroPosition.Y - MIN_HERO_TO_EDGE_DISTANCE;
					WorldMovedForUpdate = true;
				}

				var xCoordinate = worldPos.X + heroPosition.X;
				if (xCoordinate < MIN_HERO_TO_EDGE_DISTANCE) {
					worldPos.X = -heroPosition.X + MIN_HERO_TO_EDGE_DISTANCE;
					WorldMovedForUpdate = true;
				} else if (xCoordinate > Frame.Size.Width - MIN_HERO_TO_EDGE_DISTANCE) {
					worldPos.X = Frame.Size.Width - heroPosition.X - MIN_HERO_TO_EDGE_DISTANCE;
					WorldMovedForUpdate = true;
				}

				world.Position = worldPos;
			}

			// Using performSelector:withObject:afterDelay: withg a delay of 0.0 means that the selector call occurs after
			// the current pass through the run loop.
			// This means the property will be cleared after the subclass implementation of didSimluatePhysics completes.
			PerformSelector (new Selector ("clearWorldMoved"), null, 0.0f);
		}

		[Export ("clearWorldMoved")]
		void ClearWorldMoved ()
		{
			WorldMovedForUpdate = false;
		}

		#endregion

		#if __IOS__

		#region Event Handling - iOS
		
		public override void TouchesBegan (NSSet touches, UIEvent evt)
		{
			if (Heroes.Count < 1)
				return;

			var touch = (UITouch)touches.AnyObject;

			var defaultPlayer = DefaultPlayer;
			if (defaultPlayer.MovementTouch != null)
				return;

			defaultPlayer.TargetLocation = touch.LocationInNode (defaultPlayer.Hero.Parent);

			bool wantsAttack = false;
			var nodes = GetNodesAtPoint (touch.LocationInNode (this));
			foreach (var node in nodes) {
				if (node == null || node.PhysicsBody == null)
					continue;

				wantsAttack |= (node.PhysicsBody.CategoryBitMask & (uint)(ColliderType.Cave | ColliderType.GoblinOrBoss)) > 0;
			}

			defaultPlayer.FireAction = wantsAttack;
			defaultPlayer.MoveRequested = !wantsAttack;
			defaultPlayer.MovementTouch = touch;
		}

		public override void TouchesMoved (NSSet touches, UIEvent evt)
		{
			if (Heroes.Count < 1)
				return;

			UITouch touch = DefaultPlayer.MovementTouch;

			if (touch == null)
				return;

			if (touches.Contains (touch)) {
				DefaultPlayer.TargetLocation = touch.LocationInNode (DefaultPlayer.Hero.Parent);
				if (!DefaultPlayer.FireAction)
					DefaultPlayer.MoveRequested = true;
			}
		}

		public override void TouchesEnded (NSSet touches, UIEvent evt)
		{
			if (Heroes.Count < 1)
				return;

			UITouch touch = DefaultPlayer.MovementTouch;
			if (touch != null && touches.Contains (touch)) {
				DefaultPlayer.MovementTouch = null;
				DefaultPlayer.FireAction = false;
			}
		}

		#endregion

		#else

		#region Event Handling - OSX

		private void HandleKeyEvent (NSEvent keyEvent, bool downOrUp)
		{
			// First check the arrow keys since they are on the numeric keypad.
			if ((keyEvent.ModifierFlags & NSEventModifierMask.NumericPadKeyMask) > 0) { // arrow keys have this mask
				string theArrow = keyEvent.CharactersIgnoringModifiers;
				char keyChar = ' ';
				if (theArrow.Length == 1) {
					keyChar = theArrow [0];
					switch (keyChar) {
					case (char)NSKey.UpArrow:
						DefaultPlayer.MoveForward = downOrUp;
						break;
					case (char)NSKey.LeftArrow:
						DefaultPlayer.MoveLeft = downOrUp;
						break;
					case (char)NSKey.RightArrow:
						DefaultPlayer.MoveRight = downOrUp;
						break;
					case (char)NSKey.DownArrow:
						DefaultPlayer.MoveBack = downOrUp;
						break;
					}
				}
			}

			// Now check the rest of the keyboard
			string characters = keyEvent.Characters;
			for (int s = 0; s < characters.Length; s++) {
				char character = characters [s];
				switch (character) {
				case 'w':
					DefaultPlayer.MoveForward = downOrUp;
					break;
				case 'a':
					DefaultPlayer.MoveLeft = downOrUp;
					break;
				case 'd':
					DefaultPlayer.MoveRight = downOrUp;
					break;
				case 's':
					DefaultPlayer.MoveBack = downOrUp;
					break;
				case ' ':
					DefaultPlayer.FireAction = downOrUp;
					break;
				}
			}
		}

		public override void KeyDown (NSEvent theEvent)
		{
			HandleKeyEvent (theEvent, true);
		}

		public override void KeyUp (NSEvent theEvent)
		{
			HandleKeyEvent (theEvent, false);
		}

		#endregion

		#endif

		#region Game Controllers

		/* This method should be called when the level is loaded to set up currently-connected game controllers,
  		   and register for the relevant notifications to deal with new connections/disconnections. */
		public void ConfigureGameControllers ()
		{
			// Receive notifications when a controller connects or disconnects.
			didConnectObserver = GCController.Notifications.ObserveDidConnect (GameControllerDidConnect);
			didDisconnectObserver = GCController.Notifications.ObserveDidDisconnect (GameControllerDidDisconnect);

			// Configure all the currently connected game controllers.
			ConfigureConnectedGameControllers ();

			// And start looking for any wireless controllers.
			GCController.StartWirelessControllerDiscovery (() => Console.WriteLine ("Finished finding controllers"));
		}

		void ConfigureConnectedGameControllers ()
		{
			if (GCController.Controllers == null)
				return;

			// First deal with the controllers previously set to a player.
			foreach (var controller in GCController.Controllers) {
				var playerIndex = controller.PlayerIndex;
				if (playerIndex == GCController.PlayerIndexUnset)
					continue;

				AssignPresetController (controller, playerIndex);
			}

			// Now deal with the unset controllers.
			foreach (var controller in GCController.Controllers) {
				var playerIndex = controller.PlayerIndex;
				if (playerIndex != GCController.PlayerIndexUnset)
					continue;

				AssignUnknownController (controller);
			}
		}

		void GameControllerDidConnect (object sender, NSNotificationEventArgs e)
		{
			var controller = (GCController)e.Notification.Object;
			Console.WriteLine ("Connected game controller: {0}", controller);

			var playerIndex = controller.PlayerIndex;
			if (playerIndex == GCController.PlayerIndexUnset)
				AssignUnknownController (controller);
			else
				AssignPresetController (controller, playerIndex);
		}

		void GameControllerDidDisconnect (object sender, NSNotificationEventArgs e)
		{
			var controller = (GCController)e.Notification.Object;
			foreach (Player player in players) {
				if (player == null)
					continue;

				if (player.Controller == controller)
					player.Controller = null;
			}

			Console.WriteLine ("Disconnected game controller: {0}", controller);
		}

		void AssignUnknownController (GCController controller)
		{
			for (int playerIndex = 0; playerIndex < NUM_PLAYERS; playerIndex++) {
				Player player = ConnectPlayerFor (playerIndex);
				if (player.Controller != null)
					continue;

				// Found an unlinked player.
				controller.PlayerIndex = playerIndex;
				ConfigureController (controller, player);
				return;
			}
		}

		void AssignPresetController (GCController controller, nint playerIndex)
		{
			Player player = ConnectPlayerFor (playerIndex);

			if (player.Controller != null
			    && player.Controller != controller) {
				// Taken by another controller so reassign to another player.
				AssignUnknownController (controller);
				return;
			}

			ConfigureController (controller, player);
		}

		void ConfigureController (GCController controller, Player player)
		{
			Console.WriteLine ("Assigning {0} to player {1} [{2}]", controller.VendorName, player, players.IndexOf (player));

			// Assign the controller to the player.
			player.Controller = controller;

			GCControllerDirectionPadValueChangedHandler dpadMoveHandler = (dpad, xValue, yValue) => {
				var length = GraphicsUtilities.Hypotenuse (xValue, yValue);
				if (length > 0f) {
					var invLength = 1 / length;
					player.HeroMoveDirection = new CGPoint (xValue * invLength, yValue * invLength);
				} else {
					player.HeroMoveDirection = CGPoint.Empty;
				}
			};

			// Use either the dpad or the left thumbstick to move the character.
			controller.ExtendedGamepad.LeftThumbstick.ValueChangedHandler = dpadMoveHandler;
			controller.Gamepad.DPad.ValueChangedHandler = dpadMoveHandler;

			GCControllerButtonValueChanged fireButtonHandler = (button, value, pressed) => {
				player.FireAction = pressed;
			};

			controller.Gamepad.ButtonA.SetValueChangedHandler (fireButtonHandler);
			controller.Gamepad.ButtonB.SetValueChangedHandler (fireButtonHandler);

			if (player != DefaultPlayer && player.Hero == null)
				AddHeroFor (player);
		}

		Player ConnectPlayerFor (nint playerIndex)
		{
			var player = players [(int)playerIndex];
			if (player == null) {
				player = new Player ();
				players [(int)playerIndex] = player;
				updateHUDFor (player, HUDState.Connected, "CONTROLLER");
			}

			return player;
		}

		#endregion

		#region Shared Assets

		// Overridden by subclasses to load scene-specific assets.
		protected abstract void LoadSceneAssets ();

		// Overridden by subclasses to release assets used only by this scene.
		public abstract void ReleaseSceneAssets ();

		// Start loading all the shared assets for the scene in the background. This method calls LoadSceneAssets
		// in background thread, then calls the callback handler on the main thread.
		public void LoadSceneAssetsWithCompletionHandler (Action callback)
		{
			if (isAssetsLoaded)
				return;

			ThreadPool.QueueUserWorkItem (_ => {
				LoadSceneAssets ();
				isAssetsLoaded = true;

				if (callback != null)
					InvokeOnMainThread (callback);
			});
		}

		#endregion

		HeroCharacter CreateHeroBy (HeroType type, CGPoint position, Player player)
		{
			switch (type) {
			case HeroType.Archer:
				return new Archer (position, player);

			case HeroType.Warrior:
				return new Warrior (position, player);

			default:
				throw new NotImplementedException ();
			}
		}
	}
}


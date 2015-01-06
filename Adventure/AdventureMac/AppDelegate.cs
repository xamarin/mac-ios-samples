using AppKit;
using SpriteKit;
using Foundation;
using CoreGraphics;

namespace Adventure
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		AdventureScene scene;

		public override void DidFinishLaunching (NSNotification notification)
		{
			// Start the progress indicator animation.
			loadingProgressIndicator.StartAnimation (this);

			gameLogo.Image = new NSImage (NSBundle.MainBundle.PathForResource ("logo", "png"));
			archerButton.Image = new NSImage (NSBundle.MainBundle.PathForResource ("button_archer", "png"));
			warriorButton.Image = new NSImage (NSBundle.MainBundle.PathForResource ("button_warrior", "png"));

			// The size for the primary scene - 1024x768 is good for OS X and iOS.
			var size = new CGSize (1024, 768);
			// Load the shared assets of the scene before we initialize and load it.
			scene = new AdventureScene (size);

			scene.LoadSceneAssetsWithCompletionHandler (() => {
				scene.Initialize ();
				scene.ScaleMode = SKSceneScaleMode.AspectFill;

				SKView.PresentScene (scene);

				loadingProgressIndicator.StopAnimation (this);
				loadingProgressIndicator.Hidden = true;

				NSAnimationContext.CurrentContext.Duration = 2.0f;
				((NSButton)archerButton.Animator).AlphaValue = 1.0f;
				((NSButton)warriorButton.Animator).AlphaValue = 1.0f;

				scene.ConfigureGameControllers();
			});

			SKView.ShowsFPS = true;
			SKView.ShowsDrawCount = true;
			SKView.ShowsNodeCount = true;
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}

		[Export("chooseArcher:")]
		void ChooseArcher(NSObject sender)
		{
			StartGameWithHeroType (HeroType.Archer);
		}

		[Export("chooseWarrior:")]
		void ChooseWarrior(NSObject sender)
		{
			StartGameWithHeroType (HeroType.Warrior);
		}

		void StartGameWithHeroType(HeroType type)
		{
			NSAnimationContext.CurrentContext.Duration = 2.0f;
			((NSImageView)gameLogo.Animator).AlphaValue = 0.0f;
			((NSButton)archerButton.Animator).AlphaValue = 0.0f;
			((NSButton)warriorButton.Animator).AlphaValue = 0.0f;

			scene.DefaultPlayerHeroType = type;
			scene.StartLevel ();
		}
	}
}


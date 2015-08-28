using System;

using CoreGraphics;
using SpriteKit;
using UIKit;

using AgentsCatalog.Shared;

namespace AgentsCatalog.iOS {
	partial class GameViewController : UIViewController {

		public GameScene.SceneType Type { get; set; }

		public GameViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			var skView = (SKView)View;
			skView.ShowsFPS = true;
			skView.ShowsNodeCount = true;
			skView.IgnoresSiblingOrder = true;

			SelectScene (Type);
		}

		void SelectScene(GameScene.SceneType sceneType)
		{
			var scene = GameScene.SceneWith (sceneType, new CGSize (800f, 600f));
			scene.ScaleMode = SKSceneScaleMode.AspectFit;
			var skView = (SKView)View;
			skView.PresentScene (scene);

			NavigationItem.Title = scene.SceneName;
		}

		partial void GoToPreviousScene (UIBarButtonItem sender)
		{
			if (--Type < 0)
				Type = GameScene.SceneType.Count - 1;
			SelectScene(Type);
		}

		partial void GoToNextScene (UIBarButtonItem sender)
		{
			if (++Type >= GameScene.SceneType.Count)
				Type = (GameScene.SceneType)0;
			SelectScene(Type);
		}
	}
}

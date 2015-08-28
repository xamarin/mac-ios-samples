using System;

using AppKit;
using CoreGraphics;
using Foundation;
using SpriteKit;

using AgentsCatalog.Shared;

namespace AgentsCatalog.Mac {
	public partial class MainWindowController : NSWindowController {
		public new MainWindow Window {
			get {
				return (MainWindow)base.Window;
			}
		}

		public MainWindowController (IntPtr handle)
			: base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public MainWindowController (NSCoder coder)
			: base (coder)
		{
		}

		public MainWindowController ()
			: base ("MainWindow")
		{
		}

		public override void AwakeFromNib ()
		{
			Window.TitleVisibility = NSWindowTitleVisibility.Hidden;

			SkView.IgnoresSiblingOrder = true;
			SkView.ShowsFPS = true;
			SkView.ShowsNodeCount = true;

			SelectScene (SceneControl);
		}

		partial void SelectScene (NSSegmentedControl sender)
		{
			var scene = GameScene.SceneWith (
				(GameScene.SceneType)(Enum.ToObject (typeof(GameScene.SceneType),
					(int)sender.SelectedSegment)), new CGSize (800f, 600f));
			scene.ScaleMode = SKSceneScaleMode.AspectFit;
			SkView.PresentScene (scene);
		}
	}
}

// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace AgentsCatalog.Mac
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		AppKit.NSSegmentedControl SceneControl { get; set; }

		[Outlet]
		SpriteKit.SKView SkView { get; set; }

		[Action ("SelectScene:")]
		partial void SelectScene (AppKit.NSSegmentedControl sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (SceneControl != null) {
				SceneControl.Dispose ();
				SceneControl = null;
			}

			if (SkView != null) {
				SkView.Dispose ();
				SkView = null;
			}
		}
	}
}

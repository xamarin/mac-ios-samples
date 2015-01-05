// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;
using UIKit;
using SpriteKit;

namespace Adventure
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet("skView")]
		SKView SKView { get; set; }

		[Outlet]
		UIImageView gameLogo { get; set; }

		[Outlet]
		UIActivityIndicatorView loadingProgressIndicator { get; set; }

		[Outlet]
		UIButton archerButton { get; set; }

		[Outlet]
		UIButton warriorButton { get; set; }

		void ReleaseDesignerOutlets ()
		{
			if (SKView != null) {
				SKView.Dispose ();
				SKView = null;
			}

			if (warriorButton != null) {
				warriorButton.Dispose ();
				warriorButton = null;
			}

			if (archerButton != null) {
				archerButton.Dispose ();
				archerButton = null;
			}

			if (loadingProgressIndicator != null) {
				loadingProgressIndicator.Dispose ();
				loadingProgressIndicator = null;
			}

			if (gameLogo != null) {
				gameLogo.Dispose ();
				gameLogo = null;
			}
		}
	}
}

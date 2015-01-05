using AppKit;
using SpriteKit;
using Foundation;

namespace Adventure
{
	// Should subclass MonoMac.AppKit.NSResponder
	[Foundation.Register ("AppDelegate")]
	public partial class AppDelegate
	{
		[Outlet("skView")]
		SKView SKView { get; set; }

		[Outlet]
		NSImageView gameLogo { get; set; }

		[Outlet]
		NSProgressIndicator loadingProgressIndicator { get; set; }

		[Outlet]
		NSButton archerButton { get; set; }

		[Outlet]
		NSButton warriorButton { get; set; }

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


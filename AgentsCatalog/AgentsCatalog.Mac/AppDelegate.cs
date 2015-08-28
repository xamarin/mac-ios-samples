using Foundation;
using AppKit;

namespace AgentsCatalog.Mac {
	public partial class AppDelegate : NSApplicationDelegate {
		MainWindowController mainWindowController;

		public override void DidFinishLaunching (NSNotification notification)
		{
			mainWindowController = new MainWindowController ();
			mainWindowController.Window.MakeKeyAndOrderFront (this);
		}
	}
}


using System;

using AppKit;
using CoreGraphics;
using Foundation;
using ObjCRuntime;

namespace ExceptionMarshaling.Mac
{
	[Register ("AppDelegate")]
	public class AppDelegate : NSApplicationDelegate
	{
		NSWindow window;

		public override void DidFinishLaunching (NSNotification notification)
		{
			// Handle CMD-Q
			NSMenu mainMenu = NSApplication.SharedApplication.MainMenu = new NSMenu ("Exception marshaling");
			mainMenu.AddItem ("Sub", new ObjCRuntime.Selector ("sub"), "S");
			var subMenu = new NSMenu ("Sub");
			var quit = new NSMenuItem ("Quit", (sender, e) => {
				NSApplication.SharedApplication.Terminate (this);
			});
			quit.Enabled = true;
			quit.KeyEquivalent = "q";
			quit.KeyEquivalentModifierMask = NSEventModifierMask.CommandKeyMask;
			subMenu.AddItem (quit);
			mainMenu.AutoEnablesItems = false;
			mainMenu.SetSubmenu (subMenu, mainMenu.ItemAt (0));

			// Create main window and its UI
			window = new NSWindow (new CGRect (0, 0, 500, 500), NSWindowStyle.Titled | NSWindowStyle.Resizable | NSWindowStyle.Closable | NSWindowStyle.Miniaturizable, NSBackingStore.Buffered, true);
			window.Title = "Exception marshaling";

			var protoCell = new NSButtonCell ();
			protoCell.SetButtonType (NSButtonType.Radio);

			var cellSize = new CGSize (300, 25);
			var boxSize = new CGSize (300, 400);
			var managedBox = new NSBox (new CGRect (new CGPoint (0, 0), boxSize));
			var objectiveCBox = new NSBox (new CGRect (new CGPoint (managedBox.Frame.Right, managedBox.Frame.Y), boxSize));

			var throwManagedException = new NSButton (new CGRect ((boxSize.Width - 200) / 2, 0, 200, cellSize.Height));
			throwManagedException.Title = "Throw managed exception";

			var marshalManagedModeMatrix = new NSMatrix (new CGRect (0, 0, managedBox.Frame.Width, cellSize.Height * 6 + 10), NSMatrixMode.Radio, protoCell, 6, 1);
			marshalManagedModeMatrix.Cells [0].Title = "None";
			marshalManagedModeMatrix.Cells [1].Title = "Default";
			marshalManagedModeMatrix.Cells [2].Title = "Unwind native code";
			marshalManagedModeMatrix.Cells [3].Title = "Throw Objective-C exception";
			marshalManagedModeMatrix.Cells [4].Title = "Abort";
			marshalManagedModeMatrix.Cells [5].Title = "Disable";
			marshalManagedModeMatrix.CellSize = cellSize;

			var marshalManagedMode = new NSBox (new CGRect (0, throwManagedException.Frame.Bottom + 20, marshalManagedModeMatrix.Frame.Width, marshalManagedModeMatrix.Frame.Height + cellSize.Height));
			marshalManagedMode.Title = "Marshaling mode";
			marshalManagedMode.AddSubview (marshalManagedModeMatrix);

			var threadManagedMatrix = new NSMatrix (new CGRect (0, 0, managedBox.Frame.Width, cellSize.Height * 3 + 10), NSMatrixMode.Radio, protoCell, 3, 1);
			threadManagedMatrix.Cells [0].Title = "Main thread";
			threadManagedMatrix.Cells [1].Title = "Background thread";
			threadManagedMatrix.Cells [2].Title = "Threadpool thread";
			threadManagedMatrix.CellSize = cellSize;

			var threadManaged = new NSBox (new CGRect (new CGPoint (0, marshalManagedMode.Frame.Bottom + 20), new CGSize (threadManagedMatrix.Frame.Width, threadManagedMatrix.Frame.Height + cellSize.Height)));
			threadManaged.Title = "Thread";
			threadManaged.AddSubview (threadManagedMatrix);

			var marshalObjectiveCModeMatrix = new NSMatrix (marshalManagedModeMatrix.Frame, NSMatrixMode.Radio, protoCell, 6, 1);
			marshalObjectiveCModeMatrix.Cells [0].Title = "None";
			marshalObjectiveCModeMatrix.Cells [1].Title = "Default";
			marshalObjectiveCModeMatrix.Cells [2].Title = "Unwind managed code";
			marshalObjectiveCModeMatrix.Cells [3].Title = "Throw managed exception";
			marshalObjectiveCModeMatrix.Cells [4].Title = "Abort";
			marshalObjectiveCModeMatrix.Cells [5].Title = "Disable";
			marshalObjectiveCModeMatrix.CellSize = cellSize;

			var marshalObjectiveCMode = new NSBox (marshalManagedMode.Frame);
			marshalObjectiveCMode.Title = "Marshaling mode";
			marshalObjectiveCMode.AddSubview (marshalObjectiveCModeMatrix);

			var threadObjectiveCMatrix = new NSMatrix (threadManagedMatrix.Frame, threadManagedMatrix.Mode, threadManagedMatrix.Prototype, threadManagedMatrix.Rows, threadManagedMatrix.Columns);
			threadObjectiveCMatrix.Cells [0].Title = "Main thread";
			threadObjectiveCMatrix.Cells [1].Title = "Background thread";
			threadObjectiveCMatrix.Cells [2].Title = "Threadpool thread";
			threadObjectiveCMatrix.CellSize = cellSize;

			var threadObjectiveC = new NSBox (threadManaged.Frame);
			threadObjectiveC.Title = "Thread";
			threadObjectiveC.AddSubview (threadObjectiveCMatrix);

			var throwObjectiveCException = new NSButton (throwManagedException.Frame);
			throwObjectiveCException.Title = "Throw Objective-C exception";

			managedBox.Title = "Managed exception";
			managedBox.AddSubview (throwManagedException);
			managedBox.AddSubview (threadManaged);
			managedBox.AddSubview (marshalManagedMode);
			managedBox.Frame = new CGRect (managedBox.Frame.X, managedBox.Frame.Y, managedBox.Frame.Width, threadManaged.Frame.Bottom + cellSize.Height);
			//managedBox.SetBoundsSize (new CGSize (managedBox.Bounds.Width, threadManaged.Frame.Bottom));
			window.ContentView.AddSubview (managedBox);

			objectiveCBox.Title = "Objective-C exception";
			objectiveCBox.AddSubview (throwObjectiveCException);
			objectiveCBox.AddSubview (threadObjectiveC);
			objectiveCBox.AddSubview (marshalObjectiveCMode);
			objectiveCBox.Frame = new CGRect (objectiveCBox.Frame.Location, managedBox.Frame.Size);
			window.ContentView.AddSubview (objectiveCBox);

			var windowContentSize = new CGSize (managedBox.Frame.Width + objectiveCBox.Frame.Width, Math.Max (managedBox.Frame.Height, objectiveCBox.Frame.Height));
			window.SetContentSize (windowContentSize);
			window.ContentMinSize = windowContentSize;
			window.Center ();
			window.MakeKeyAndOrderFront (window);

			Action setModes = () => {
				if (marshalManagedModeMatrix.SelectedRow == 0) {
					Exceptions.ManagedExceptionMode = null;
				} else {
					Exceptions.ManagedExceptionMode = (MarshalManagedExceptionMode) (int) marshalManagedModeMatrix.SelectedRow - 1;
				}
				if (marshalObjectiveCModeMatrix.SelectedRow == 0) {
					Exceptions.ObjectiveCExceptionMode = null;
				} else {
					Exceptions.ObjectiveCExceptionMode = (MarshalObjectiveCExceptionMode) (int) marshalObjectiveCModeMatrix.SelectedRow - 1;
				}
			};

			throwObjectiveCException.Activated += (sender, e) => {
				setModes ();
				Exceptions.ThrowObjectiveCException ((ThreadMode) (int) threadObjectiveCMatrix.SelectedRow);
			};
			throwManagedException.Activated += (sender, e) => {
				setModes ();
				Exceptions.ThrowManagedException ((ThreadMode)(int)threadManagedMatrix.SelectedRow);
			};

		}
	}
}

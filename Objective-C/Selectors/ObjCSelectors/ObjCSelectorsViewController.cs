using System;
using System.Drawing;

using Foundation;
using UIKit;
using ObjCRuntime;
using System.Runtime.InteropServices;
using CoreGraphics;

namespace ObjCSelectors
{
	public partial class ObjCSelectorsViewController : UIViewController
	{
		public ObjCSelectorsViewController (IntPtr handle) : base (handle)
		{
		}

		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}

		[DllImport (Constants.ObjectiveCLibrary, EntryPoint="objc_msgSend")]
		static extern CGSize cgsize_objc_msgSend_IntPtr_float_int (
			IntPtr target, IntPtr selector,
			IntPtr font,
			nfloat width,
			UILineBreakMode mode);

		#region View lifecycle

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			// Define needed objects
			NSString nsString = new NSString ("Hello World!");
			Selector selector = new Selector ("sizeWithFont:forWidth:lineBreakMode:");
			UIFont font = UIFont.FromName ("Helvetica", 12);
			nfloat width = 200;
			UILineBreakMode mode = UILineBreakMode.WordWrap;

			// Invoke message
			CGSize size = cgsize_objc_msgSend_IntPtr_float_int(
				nsString.Handle, selector.Handle,
				font == null ? IntPtr.Zero : font.Handle,
				width,
				mode);

			// Report size
			Console.WriteLine ("Size: {0}", size);
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
		}

		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
		}

		public override void ViewWillDisappear (bool animated)
		{
			base.ViewWillDisappear (animated);
		}

		public override void ViewDidDisappear (bool animated)
		{
			base.ViewDidDisappear (animated);
		}

		#endregion
	}
}


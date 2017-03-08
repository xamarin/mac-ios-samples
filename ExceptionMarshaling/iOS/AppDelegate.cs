using System;

using Foundation;
using ObjCRuntime;
using UIKit;

using MonoTouch.Dialog;

namespace ExceptionMarshaling.IOS
{
	[Register ("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		UIWindow window;
		UINavigationController nav;
		DialogViewController dvc;

		public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
		{
			RadioGroup managed = null;
			RadioGroup objc = null;
			RadioGroup thread_objc = null;
			RadioGroup thread_managed = null;

			Action setModes = () => {
				if (managed.Selected == 0) {
					Exceptions.ManagedExceptionMode = null;
				} else {
					Exceptions.ManagedExceptionMode = (MarshalManagedExceptionMode)managed.Selected - 1;
				}
				if (objc.Selected == 0) {
					Exceptions.ObjectiveCExceptionMode = null;
				} else {
					Exceptions.ObjectiveCExceptionMode = (MarshalObjectiveCExceptionMode)objc.Selected - 1;
				}
			};

			window = new UIWindow (UIScreen.MainScreen.Bounds);
			dvc = new DialogViewController (
				new RootElement ("Root")
				{
					new Section {
						new RootElement ("Throw Objective-C exception ") {
							new Section ("Marshal Objective-C exception mode") {
								new RootElement ("", (objc = new RadioGroup ("objc", 0))) {
									new Section {
										new RadioElement ("None", "objc"),
										new RadioElement ("Default", "objc"),
										new RadioElement ("Unwind managed code", "objc"),
										new RadioElement ("Throw managed exception", "objc"),
										new RadioElement ("Abort", "objc"),
										new RadioElement ("Disable", "objc"),
									}
								}
							},
							new Section ("Thread") {
								new RootElement ("", (thread_objc = new RadioGroup ("thread_objc", 0))) {
									new Section {
										new RadioElement ("Main thread", "thread_objc"),
										new RadioElement ("Background thread", "thread_objc"),
										new RadioElement ("Threadpool thread", "thread_objc"),
									}
								}
							},
							new Section ("Actions") {
								new StringElement ("Throw", () =>
								{
									setModes ();
									Exceptions.ThrowObjectiveCException ((ThreadMode) thread_objc.Selected);
								}),
							}
						}
					},
					new Section {
						new RootElement ("Throw managed exception ") {
							new Section ("Marshal managed exception mode") {
								new RootElement ("", (managed = new RadioGroup ("managed", 0))) {
									new Section {
										new RadioElement ("None", "managed"),
										new RadioElement ("Default", "managed"),
										new RadioElement ("Unwind native code", "managed"),
										new RadioElement ("Throw Objective-C exception", "managed"),
										new RadioElement ("Abort", "managed"),
										new RadioElement ("Disable", "managed"),
									}
								}
							},
							new Section ("Thread") {
								new RootElement ("", (thread_managed = new RadioGroup ("thread_managed", 0))) {
									new Section {
										new RadioElement ("Main thread", "thread_managed"),
										new RadioElement ("Background thread", "thread_managed"),
										new RadioElement ("Threadpool thread", "thread_managed"),
									}
								}
							},
							new Section ("Actions") {
								new StringElement ("Throw", () =>
								{
									setModes ();
									Exceptions.ThrowManagedException ((ThreadMode) thread_managed.Selected);
								}),
							}
						}
					},
				});
			nav = new UINavigationController (dvc);
			window.RootViewController = nav;
			window.MakeKeyAndVisible ();

			return true;
		}
	}
}


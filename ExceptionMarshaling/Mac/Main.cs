using AppKit;

namespace ExceptionMarshaling.Mac
{
	static class MainClass
	{
#pragma warning disable 414 // private field 'app_delegate' is assigned but its value is never used
		static AppDelegate app_delegate;
#pragma warning restore 414
		static void Main (string [] args)
		{
			NSApplication.Init ();
			NSApplication.SharedApplication.Delegate = app_delegate = new AppDelegate ();
			NSApplication.SharedApplication.Run ();
		}
	}
}

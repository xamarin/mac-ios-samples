using System;

using AppKit;
using Foundation;
using ObjCRuntime;

namespace AdventureMac
{
	class MainClass
	{
		static void Main (string[] args)
		{
			NSApplication.Init ();
			NSApplication.Main (args);
		}
	}
}


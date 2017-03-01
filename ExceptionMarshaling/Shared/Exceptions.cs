using System;
using System.Threading;

using Foundation;
using ObjCRuntime;

namespace ExceptionMarshaling
{
	public enum ThreadMode
	{
		MainThread,
		BackgroundThread,
		ThreadPool,
	}

	public static class Exceptions
	{
		public static MarshalManagedExceptionMode? ManagedExceptionMode;
		public static MarshalObjectiveCExceptionMode? ObjectiveCExceptionMode;

		static Exceptions ()
		{
			Runtime.MarshalManagedException += (object sender, MarshalManagedExceptionEventArgs args) =>
			{
				Console.WriteLine ("Marshalling managed exception");
				Console.WriteLine ("    Exception: {0}", args.Exception);
				Console.WriteLine ("    Mode: {0}", args.ExceptionMode);
				Console.WriteLine ("    Target mode: {0}", ManagedExceptionMode);
				if (ManagedExceptionMode.HasValue)
					args.ExceptionMode = ManagedExceptionMode.Value;
				
			};
			Runtime.MarshalObjectiveCException += (object sender, MarshalObjectiveCExceptionEventArgs args) =>
			{
				Console.WriteLine ("Marshalling Objective-C exception");
				Console.WriteLine ("    Exception: {0}", args.Exception);
				Console.WriteLine ("    Mode: {0}", args.ExceptionMode);
				Console.WriteLine ("    Target mode: {0}", ObjectiveCExceptionMode);
				if (ObjectiveCExceptionMode.HasValue)
					args.ExceptionMode = ObjectiveCExceptionMode.Value;
			};
		}

		public static void ThrowObjectiveCException ()
		{
			try {
				var dict = new NSMutableDictionary ();
				dict.LowlevelSetObject (IntPtr.Zero, IntPtr.Zero);
			} catch (Exception e) {
				Console.WriteLine ("Caught managed exception: {0}", e);
			}
		}

		public static void ThrowManagedExceptionThroughNativeCode ()
		{
			try {
				using (var obj = new ExceptionalObject ())
					obj.PerformSelector (new Selector ("throwManagedException"));
			} catch (Exception e) {
				Console.WriteLine ("Caught managed exception: {0}", e);
			}
		}

		public static void ThrowObjectiveCException (ThreadMode mode)
		{
			switch (mode) {
			case ThreadMode.MainThread:
				Exceptions.ThrowObjectiveCException ();
				break;
			case ThreadMode.BackgroundThread:
				new Thread (Exceptions.ThrowObjectiveCException) {
					IsBackground = true,
				}.Start ();
				break;
			case ThreadMode.ThreadPool:
				ThreadPool.QueueUserWorkItem ((v) => Exceptions.ThrowObjectiveCException ());
				break;
			default:
				throw new ArgumentOutOfRangeException (nameof (mode), $"Unknown thread mode: ${mode}");
			}
		}

		public static void ThrowManagedException (ThreadMode mode)
		{ 
				switch (mode) {
			case ThreadMode.MainThread:
				Exceptions.ThrowManagedExceptionThroughNativeCode ();
				break;
			case ThreadMode.BackgroundThread:
				new Thread (Exceptions.ThrowManagedExceptionThroughNativeCode) {
					IsBackground = true,
				}.Start ();
				break;
			case ThreadMode.ThreadPool:
				ThreadPool.QueueUserWorkItem ((v) => Exceptions.ThrowManagedExceptionThroughNativeCode ());
				break;
			default:
				throw new ArgumentOutOfRangeException (nameof (mode), $"Unknown thread mode: ${mode}");
			}
		}
	}

	public class ExceptionalObject : NSObject
	{
		[Export ("throwManagedException")]
		void ThrowManagedException ()
		{
			throw new ApplicationException ("A managed exception");
		}
	}
}

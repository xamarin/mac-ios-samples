Exception marshaling
====================

This is a sample solution with a project for each platform to show how exception marshaling works.

This sample assumes knowledge of how exception marshaling works, which is documented here: [Exception Marshaling][1]

## Example #1 - default behavior

By default no exceptions are marshaled in neither iOS nor Mac projects (except watchOS projects).

### iOS simulator (Debug)

1. Run the iOS project in the simulator using the Debug configuration.
2. Tap `Throw Objective-C exception`.
3. Tap `Throw`.

You'll see the following Application Output:

```
ExceptionMarshaling.IOS[59202:1752359] Marshalling Objective-C exception
ExceptionMarshaling.IOS[59202:1752359]     Exception: *** setObjectForKey: key cannot be nil
ExceptionMarshaling.IOS[59202:1752359]     Mode: UnwindManagedCode
ExceptionMarshaling.IOS[59202:1752359]     Target mode: 
ExceptionMarshaling.IOS[59202:1752359] Marshalling managed exception
ExceptionMarshaling.IOS[59202:1752359]     Exception: Foundation.MonoTouchException: Objective-C exception thrown.  Name: NSInvalidArgumentException Reason: *** setObjectForKey: key cannot be nil
Native stack trace:
	[...]
ExceptionMarshaling.IOS[59202:1752359]     Mode: Disable
ExceptionMarshaling.IOS[59202:1752359]     Target mode: 
ExceptionMarshaling.IOS[59202:1752359] Caught managed exception: Foundation.MonoTouchException: Objective-C exception thrown.  Name: NSInvalidArgumentException Reason: *** setObjectForKey: key cannot be nil
Native stack trace:
	0   CoreFoundation                      0x00988bf2 __exceptionPreprocess + 194
	1   libobjc.A.dylib                     0x0ad90e66 objc_exception_throw + 52
	2   CoreFoundation                      0x00896d17 -[__NSDictionaryM setObject:forKey:] + 1015
	3   ExceptionMarshaling.IOS             0x003e99f6 xamarin_dyn_objc_msgSend + 102
	4   ???                                 0x229117a4 0x0 + 579934116
	[...]
```

What happens here?

1. Since interception of Objective-C exceptions is enabled by default in the
   simulator, the first thing that happens is that the Objective-C exception
   is intercepted, the `MarshalObjectiveCException` event is raised and
   the test project's event handler is called:

	```
	Marshalling Objective-C exception
	    Exception: *** setObjectForKey: key cannot be nil
	    Mode: UnwindManagedCode
	    Target mode: 
	```

2. The default target mode is not changed, which means that the Xamarin.iOS
   runtime will not handle the Objective-C exception, letting the Objective-C
   runtime unwind managed code.

   The Objective-C runtime starts unwinding, but doesn't find any Objective-C
   `@catch` handlers, so it invokes the unhandled Objective-C exception
   handler.

   Xamarin.iOS has an unhandled Objective-C exception handler installed, and
   in that handler we convert the Objective-C exception to a managed
   exception.

   Since at this point we're in native code, we now have a managed exception
   thrown while running native code, and we raise the
   `MarshalManagedException` event (even if managed exception marshaling is
   disabled, we still process some managed exceptions when there's no
   performance penalty), so the test project's event handler is invoked:

	```
	ExceptionMarshaling.IOS[59202:1752359] Marshalling managed exception
	ExceptionMarshaling.IOS[59202:1752359]     Exception: Foundation.MonoTouchException: Objective-C exception thrown.  Name: NSInvalidArgumentException Reason: *** setObjectForKey: key cannot be nil
	Native stack trace:
		0   CoreFoundation                      0x00988bf2 __exceptionPreprocess + 194
		1   libobjc.A.dylib                     0x0ad90e66 objc_exception_throw + 52
		2   CoreFoundation                      0x00896d17 -[__NSDictionaryM setObject:forKey:] + 1015
		3   ExceptionMarshaling.IOS             0x003e99f6 xamarin_dyn_objc_msgSend + 102
		4   ???                                 0x229117a4 0x0 + 579934116
		[...]
	ExceptionMarshaling.IOS[59202:1752359]     Mode: Disable
	ExceptionMarshaling.IOS[59202:1752359]     Target mode: 
	```

3. The event handler doesn't change the target mode, and since the default
   mode is `Disable`, the Mono runtime goes ahead and unwinds any frames it
   finds. Eventually if finds the `catch` handler from the test project:

	```
	ExceptionMarshaling.IOS[59202:1752359] Caught managed exception: Foundation.MonoTouchException: Objective-C exception thrown.  Name: NSInvalidArgumentException Reason: *** setObjectForKey: key cannot be nil
	Native stack trace:
		0   CoreFoundation                      0x00988bf2 __exceptionPreprocess + 194
		1   libobjc.A.dylib                     0x0ad90e66 objc_exception_throw + 52
		2   CoreFoundation                      0x00896d17 -[__NSDictionaryM setObject:forKey:] + 1015
		3   ExceptionMarshaling.IOS             0x003e99f6 xamarin_dyn_objc_msgSend + 102
		4   ???                                 0x229117a4 0x0 + 579934116
		[...]
	```

### iOS Device (Debug)

1. Run the iOS project on a device using the Debug configuration.
2. Tap `Throw Objective-C exception`.
3. Tap `Throw`.

Since Objective-C exceptions are not intercepted by default when building for
device, the `MarshalObjectiveCException` event is not raised.

However, the rest is just like the simulator case:

* Xamarin.iOS' uncaught exception handler is invoked.
* A managed exception is created and thrown for the the Objective-C exception.
* The `MarshalManagedException` event is raised because the managed exception
  is thrown in native code.
* Default managed exception marshaling is in effect, which means that the Mono
  runtime unwinds all types of frames, and eventually finds the managed
  exception handler in the test project.

	```
	ExceptionMarshaling.IOS[429:187373] Xamarin.iOS: Received unhandled ObjectiveC exception: NSInvalidArgumentException *** setObjectForKey: key cannot be nil
	ExceptionMarshaling.IOS[429:187373] Marshalling managed exception
	ExceptionMarshaling.IOS[429:187373]     Exception: Foundation.MonoTouchException: Objective-C exception thrown.  Name: NSInvalidArgumentException Reason: *** setObjectForKey: key cannot be nil
	Native stack trace:
		0   CoreFoundation                      0x000000018a4751d0 <redacted> + 148
		1   libobjc.A.dylib                     0x0000000188eac55c objc_exception_throw + 56
		2   CoreFoundation                      0x000000018a355a0c <redacted> + 0
		3   libXamarin.iOS.dll.dylib            0x0000000100551dd8 wrapper_managed_to_native_ObjCRuntime_Messaging_void_objc_msgSend_IntPtr_IntPtr_intptr_intptr_intptr_intptr + 120
		[...]
	ExceptionMarshaling.IOS[429:187373]     Mode: Default
	ExceptionMarshaling.IOS[429:187373]     Target mode: 
	ExceptionMarshaling.IOS[429:187373] Caught managed exception: Foundation.MonoTouchException: Objective-C exception thrown.  Name: NSInvalidArgumentException Reason: *** setObjectForKey: key cannot be nil
	Native stack trace:
		0   CoreFoundation                      0x000000018a4751d0 <redacted> + 148
		1   libobjc.A.dylib                     0x0000000188eac55c objc_exception_throw + 56
		2   CoreFoundation                      0x000000018a355a0c <redacted> + 0
		3   libXamarin.iOS.dll.dylib            0x0000000100551dd8 wrapper_managed_to_native_ObjCRuntime_Messaging_void_objc_msgSend_IntPtr_IntPtr_intptr_intptr_intptr_intptr + 120
		4   libXamarin.iOS.dll.dylib            0x00000001004f74f4 Foundation_NSMutableDictionary_LowlevelSetObject_intptr_intptr + 52
		[...]
	```

### Mac (Debug)

1. Run the Mac project using the Debug configuration.
2. Click on `Throw Objective-C exception`.

The following shows up in the Application Output:

```
Marshalling Objective-C exception
    Exception: *** setObjectForKey: key cannot be nil
    Mode: UnwindManagedCode
    Target mode: 
ExceptionMarshaling.Mac[59823:1818105] An uncaught exception was raised
ExceptionMarshaling.Mac[59823:1818105] *** setObjectForKey: key cannot be nil
ExceptionMarshaling.Mac[59823:1818105] (
	0   CoreFoundation                      0x00007fff8670a452 __exceptionPreprocess + 178
	1   libobjc.A.dylib                     0x00007fff961def7e objc_exception_throw + 48
	2   CoreFoundation                      0x00007fff86604374 -[__NSDictionaryM setObject:forKey:] + 1236
	3   ExceptionMarshaling.Mac             0x0000000104fd2999 xamarin_dyn_objc_msgSend + 217
	4   ???                                 0x000000010d80c8e3 0x0 + 4521511139
	[...]
ExceptionMarshaling.Mac[59823:1818105] *** Terminating app due to uncaught exception 'NSInvalidArgumentException', reason: '*** setObjectForKey: key cannot be nil'
*** First throw call stack:
(
	0   CoreFoundation                      0x00007fff8670a452 __exceptionPreprocess + 178
	1   libobjc.A.dylib                     0x00007fff961def7e objc_exception_throw + 48
	2   CoreFoundation                      0x00007fff86604374 -[__NSDictionaryM setObject:forKey:] + 1236
	3   ExceptionMarshaling.Mac             0x0000000104fd2999 xamarin_dyn_objc_msgSend + 217
	4   ???                                 0x000000010d80c8e3 0x0 + 4521511139
	[...]
libc++abi.dylib: terminating with uncaught exception of type NSException
Stacktrace:

  at <unknown> <0xffffffff>
  at (wrapper managed-to-native) ObjCRuntime.Messaging.void_objc_msgSend_IntPtr_IntPtr (intptr,intptr,intptr,intptr) [0x0000c] in <6b706bf98acd4090a1bf967718c15076>:0
  at Foundation.NSMutableDictionary.LowlevelSetObject (intptr,intptr) [0x0000e] in /work/maccore/master/xamarin-macios/src/Foundation/NSMutableDictionary.cs:319
  at ExceptionMarshaling.Exceptions.ThrowObjectiveCException () [0x00013] in /Users/rolf/Projects/exceptionmarshaling/Shared/Exceptions.cs:48
  [...]

Native stacktrace:

	0   ExceptionMarshaling.Mac             0x00000001050c3dba mono_handle_native_sigsegv + 282
	1   libsystem_platform.dylib            0x00007fff844d152a _sigtramp + 26
	2   ???                                 0x00000000936666c1 0x0 + 2472961729
	3   libsystem_c.dylib                   0x00007fff936556df abort + 129
	4   libc++abi.dylib                     0x00007fff8e91cc11 __cxa_bad_cast + 0
	5   libc++abi.dylib                     0x00007fff8e942e17 _ZL26default_unexpected_handlerv + 0
	6   libobjc.A.dylib                     0x00007fff961e16ae _ZL15_objc_terminatev + 103
	7   libc++abi.dylib                     0x00007fff8e94000e _ZSt11__terminatePFvvE + 8
	8   libc++abi.dylib                     0x00007fff8e93fa7a _ZN10__cxxabiv1L22exception_cleanup_funcE19_Unwind_Reason_CodeP17_Unwind_Exception + 0
	9   libobjc.A.dylib                     0x00007fff961df08c objc_exception_throw + 318
	10  ExceptionMarshaling.Mac             0x0000000104fccf20 xamarin_process_nsexception_using_mode + 272
	11  ExceptionMarshaling.Mac             0x0000000104fcce07 xamarin_process_nsexception + 23
	12  ExceptionMarshaling.Mac             0x0000000104fd29c9 xamarin_dyn_objc_msgSend + 265
	13  ???                                 0x000000010d80c8e3 0x0 + 4521511139
	[...]

=================================================================
Got a SIGABRT while executing native code. This usually indicates
a fatal error in the mono runtime or one of the native libraries 
used by your application.
=================================================================
```

What happens here?

1. Since interception of Objective-C exceptions is enabled by default when
   running with the debug configuration, the first thing that happens is that
   the Objective-C exception is intercepted, and the
   `MarshalObjectiveCException` event is raised:

	```
	Marshalling Objective-C exception
	    Exception: *** setObjectForKey: key cannot be nil
	    Mode: UnwindManagedCode
	    Target mode: 
	```

2. In this scenario the default target mode is not changed, which means that
   the Xamarin.Mac runtime will not handle the Objective-C exception, letting
   the Objective-C runtime unwind managed code.

   The Objective-C runtime starts unwinding, and but doesn't find any
   Objective-C `@catch` handlers, so it invokes the unhandled Objective-C
   exception handler.

   Xamarin.Mac installed a handler for uncaught Objective-C exceptions, but it
   turns out AppKit _also_ installs a handler [1], and that handler terminates
   the app (since it's installed last):

   ```
	ExceptionMarshaling.Mac[59823:1818105] An uncaught exception was raised
	ExceptionMarshaling.Mac[59823:1818105] *** setObjectForKey: key cannot be nil
	ExceptionMarshaling.Mac[59823:1818105] (
		0   CoreFoundation                      0x00007fff8670a452 __exceptionPreprocess + 178
		1   libobjc.A.dylib                     0x00007fff961def7e objc_exception_throw + 48
		2   CoreFoundation                      0x00007fff86604374 -[__NSDictionaryM setObject:forKey:] + 1236
		3   ExceptionMarshaling.Mac             0x0000000104fd2999 xamarin_dyn_objc_msgSend + 217
		4   ???                                 0x000000010d80c8e3 0x0 + 4521511139
		[...]
	ExceptionMarshaling.Mac[59823:1818105] *** Terminating app due to uncaught exception 'NSInvalidArgumentException', reason: '*** setObjectForKey: key cannot be nil'
	*** First throw call stack:
		(
		0   CoreFoundation                      0x00007fff8670a452 __exceptionPreprocess + 178
		1   libobjc.A.dylib                     0x00007fff961def7e objc_exception_throw + 48
		2   CoreFoundation                      0x00007fff86604374 -[__NSDictionaryM setObject:forKey:] + 1236
		3   ExceptionMarshaling.Mac             0x0000000104fd2999 xamarin_dyn_objc_msgSend + 217
		4   ???                                 0x000000010d80c8e3 0x0 + 4521511139
		[...]
	libc++abi.dylib: terminating with uncaught exception of type NSException
	```	

	and the process crashes.

	[1]: Running the Mac test project in a native debugger and putting a
	breakpoint on `NSSetUncaughtExceptionHandler` shows that `[NSApplication init]`
	is the offender who installs a different uncaught exception
	handler:

	```
	  * frame #0: 0x00007fff853d56af Foundation`NSSetUncaughtExceptionHandler
        frame #1: 0x00007fff8c0f9a64 AppKit`-[NSApplication init] + 246
        frame #2: 0x00007fff8c0f978b AppKit`+[NSApplication sharedApplication] + 142
    ```

### Mac (Release)

1. Run the Mac project using the Release configuration.
2. Click on `Throw Objective-C exception`.

This scenario is similar to the Debug scenario, except that Objective-C
exceptions aren't intercepted, so the `MarshalObjectiveCException` event won't
be raised.

The app will however crash due to the exact same reasons as in the Debug
configuration.

```
ExceptionMarshaling.Mac[59901:1825680] An uncaught exception was raised
ExceptionMarshaling.Mac[59901:1825680] *** setObjectForKey: key cannot be nil
ExceptionMarshaling.Mac[59901:1825680] (
	0   CoreFoundation                      0x00007fff8670a452 __exceptionPreprocess + 178
	1   libobjc.A.dylib                     0x00007fff961def7e objc_exception_throw + 48
	2   CoreFoundation                      0x00007fff86604374 -[__NSDictionaryM setObject:forKey:] + 1236
	3   ???                                 0x0000000109ec2dc2 0x0 + 4461440450
	[...]
)
ExceptionMarshaling.Mac[59901:1825680] *** Terminating app due to uncaught exception 'NSInvalidArgumentException', reason: '*** setObjectForKey: key cannot be nil'
*** First throw call stack:
(
	0   CoreFoundation                      0x00007fff8670a452 __exceptionPreprocess + 178
	1   libobjc.A.dylib                     0x00007fff961def7e objc_exception_throw + 48
	2   CoreFoundation                      0x00007fff86604374 -[__NSDictionaryM setObject:forKey:] + 1236
	3   ???                                 0x0000000109ec2dc2 0x0 + 4461440450
	[...]
)
libc++abi.dylib: terminating with uncaught exception of type NSException
```

## Example #2 - converting Objective-C exceptions to managed exceptions.

### iOS simulator (Debug)

1. Run the iOS project in the simulator using the Debug configuration.
2. Tap `Throw Objective-C exception`.
3. Enter `Marshal Objective-C exception mode` and select `Throw managed exception`.
3. Go back and tap `Throw`.

Since Objective-C exceptions are automatically intercepted in the simulator,
the `MarshalManagedException` event will be raised and the event handler in
the test project will be called. Since the target mode is modified in the
event handler to marshal the Objective-C exception to a managed exception, we
end up in the test project's managed exception handler:

```
ExceptionMarshaling.IOS[60036:1836611] Marshalling Objective-C exception
ExceptionMarshaling.IOS[60036:1836611]     Exception: *** setObjectForKey: key cannot be nil
ExceptionMarshaling.IOS[60036:1836611]     Mode: UnwindManagedCode
ExceptionMarshaling.IOS[60036:1836611]     Target mode: ThrowManagedException
ExceptionMarshaling.IOS[60036:1836611] Caught managed exception: Foundation.MonoTouchException: Objective-C exception thrown.  Name: NSInvalidArgumentException Reason: *** setObjectForKey: key cannot be nil
Native stack trace:
	0   CoreFoundation                      0x009b7bf2 __exceptionPreprocess + 194
	1   libobjc.A.dylib                     0x0adbfe66 objc_exception_throw + 52
	2   CoreFoundation                      0x008c5d17 -[__NSDictionaryM setObject:forKey:] + 1015
	3   ExceptionMarshaling.IOS             0x004189f6 xamarin_dyn_objc_msgSend + 102
	4   ???                                 0x229fcac4 0x0 + 580897476
	[...]
```

### iOS Device (Debug)

1. Run the iOS project on a device using the Debug configuration.
2. Tap `Throw Objective-C exception`.
3. Enter `Marshal Objective-C exception mode` and select `Throw managed exception`.
3. Go back and tap `Throw`.

Since interception of Objective-C exception is disabled when building for
device, this case is identical to the one running without selecting
`Throw managed exception`:

```
ExceptionMarshaling.IOS[430:188789] Xamarin.iOS: Received unhandled ObjectiveC exception: NSInvalidArgumentException *** setObjectForKey: key cannot be nil
ExceptionMarshaling.IOS[430:188789] Marshalling managed exception
ExceptionMarshaling.IOS[430:188789]     Exception: Foundation.MonoTouchException: Objective-C exception thrown.  Name: NSInvalidArgumentException Reason: *** setObjectForKey: key cannot be nil
Native stack trace:
	0   CoreFoundation                      0x000000018a4751d0 <redacted> + 148
	1   libobjc.A.dylib                     0x0000000188eac55c objc_exception_throw + 56
	2   CoreFoundation                      0x000000018a355a0c <redacted> + 0
	3   libXamarin.iOS.dll.dylib            0x00000001005a1dd8 wrapper_managed_to_native_ObjCRuntime_Messaging_void_objc_msgSend_IntPtr_IntPtr_intptr_intptr_intptr_intptr + 120
	4   libXamarin.iOS.dll.dylib            0x00000001005474f4 Foundation_NSMutableDictionary_LowlevelSetObject_intptr_intptr + 52
	[...]
ExceptionMarshaling.IOS[430:188789]     Mode: Default
ExceptionMarshaling.IOS[430:188789]     Target mode: 
ExceptionMarshaling.IOS[430:188789] Caught managed exception: Foundation.MonoTouchException: Objective-C exception thrown.  Name: NSInvalidArgumentException Reason: *** setObjectForKey: key cannot be nil
Native stack trace:
	0   CoreFoundation                      0x000000018a4751d0 <redacted> + 148
	1   libobjc.A.dylib                     0x0000000188eac55c objc_exception_throw + 56
	2   CoreFoundation                      0x000000018a355a0c <redacted> + 0
	3   libXamarin.iOS.dll.dylib            0x00000001005a1dd8 wrapper_managed_to_native_ObjCRuntime_Messaging_void_objc_msgSend_IntPtr_IntPtr_intptr_intptr_intptr_intptr + 120
	4   libXamarin.iOS.dll.dylib            0x00000001005474f4 Foundation_NSMutableDictionary_LowlevelSetObject_intptr_intptr + 52
	[...]
```

### iOS Device (Debug) - making it work

1. Add `--marshal-objectivec-exceptions=throwmanagedexception` to the additional mtouch arguments in the iOS project's iOS Build options.
2. Run the iOS project on a device using the Debug configuration.
3. Tap `Throw Objective-C exception`.
4. Enter `Marshal Objective-C exception mode` and select `Throw managed exception`.
5. Go back and tap `Throw`.

And now the `MarshalObjectiveCException` event handler in the test project is
called and the Objective-C exception is converted to a managed exception
without going through the uncaught exception handler:

```
ExceptionMarshaling.IOS[432:189894] Marshalling Objective-C exception
ExceptionMarshaling.IOS[432:189894]     Exception: *** setObjectForKey: key cannot be nil
ExceptionMarshaling.IOS[432:189894]     Mode: ThrowManagedException
ExceptionMarshaling.IOS[432:189894]     Target mode: ThrowManagedException
ExceptionMarshaling.IOS[432:189894] Caught managed exception: Foundation.MonoTouchException: Objective-C exception thrown.  Name: NSInvalidArgumentException Reason: *** setObjectForKey: key cannot be nil
Native stack trace:
	0   CoreFoundation                      0x000000018a4751d0 <redacted> + 148
	1   libobjc.A.dylib                     0x0000000188eac55c objc_exception_throw + 56
	2   CoreFoundation                      0x000000018a355a0c <redacted> + 0
	3   libpinvokes.dylib                   0x000000010055935c xamarin_pinvoke_wrapper_void_objc_msgSend_IntPtr_IntPtr + 44
	4   libXamarin.iOS.dll.dylib            0x00000001005f1dd8 wrapper_managed_to_native_ObjCRuntime_Messaging_void_objc_msgSend_IntPtr_IntPtr_intptr_intptr_intptr_intptr + 120
	5   libXamarin.iOS.dll.dylib            0x00000001005974f4 Foundation_NSMutableDictionary_LowlevelSetObject_intptr_intptr + 52
	[...]
```

### Mac (Debug)

1. Run the Mac project using the Debug configuration.
2. Select `Throw managed exception` in the `Objective-C exception` section.
3. Click on `Throw Objective-C exception`.

Since Objective-C exceptions are automatically intercepted in Debug
configurations, the `MarshalObjectiveCException` event will be raised, and
since the target mode is modified in the event handler to marshal the
Objective-C exception to a managed exception, the app doesn't crash anymore:

```
Marshalling Objective-C exception
    Exception: *** setObjectForKey: key cannot be nil
    Mode: UnwindManagedCode
    Target mode: ThrowManagedException
Caught managed exception: Foundation.ObjCException: NSInvalidArgumentException: *** setObjectForKey: key cannot be nil
  at (wrapper managed-to-native) ObjCRuntime.Messaging:void_objc_msgSend_IntPtr_IntPtr (intptr,intptr,intptr,intptr)
  at Foundation.NSMutableDictionary.LowlevelSetObject (System.IntPtr obj, System.IntPtr key) [0x0000e] in /work/maccore/master/xamarin-macios/src/Foundation/NSMutableDictionary.cs:319 
  at ExceptionMarshaling.Exceptions.ThrowObjectiveCException () [0x00013] in /Users/rolf/Projects/exceptionmarshaling/Shared/Exceptions.cs:48 
```

### Mac (Release)

1. Run the Mac project using the Release configuration.
2. Select `Throw managed exception` in the `Objective-C exception` section.
3. Click on `Throw Objective-C exception`.

Since Objective-C exceptions are not intercepted by default in Release
configurations, the app will crash with an uncaught exception message (just
like if `Throw managed exception` hadn't been selected):

```
ExceptionMarshaling.Mac[59918:1827973] An uncaught exception was raised
ExceptionMarshaling.Mac[59918:1827973] *** setObjectForKey: key cannot be nil
ExceptionMarshaling.Mac[59918:1827973] (
	0   CoreFoundation                      0x00007fff8670a452 __exceptionPreprocess + 178
	1   libobjc.A.dylib                     0x00007fff961def7e objc_exception_throw + 48
	2   CoreFoundation                      0x00007fff86604374 -[__NSDictionaryM setObject:forKey:] + 1236
	3   ???                                 0x00000001155dddc2 0x0 + 4653440450
	[...]
)
ExceptionMarshaling.Mac[59918:1827973] *** Terminating app due to uncaught exception 'NSInvalidArgumentException', reason: '*** setObjectForKey: key cannot be nil'
*** First throw call stack:
(
	0   CoreFoundation                      0x00007fff8670a452 __exceptionPreprocess + 178
	1   libobjc.A.dylib                     0x00007fff961def7e objc_exception_throw + 48
	2   CoreFoundation                      0x00007fff86604374 -[__NSDictionaryM setObject:forKey:] + 1236
	3   ???                                 0x00000001155dddc2 0x0 + 4653440450
	[...]
)
libc++abi.dylib: terminating with uncaught exception of type NSException
```

### Mac (Release) - making it work

1. Add `--marshal-objectivec-exceptions=throwmanagedexception` to the additional mmp arguments in the Mac project's Mac Build options.
2. Run the Mac project using the Release configuration.
3. Select `Throw managed exception` in the `Objective-C exception` section.
4. Click on `Throw Objective-C exception`.

And now the Objective-C exception is marshaled to a managed exception:

```
Marshalling Objective-C exception
    Exception: *** setObjectForKey: key cannot be nil
    Mode: ThrowManagedException
    Target mode: ThrowManagedException
Caught managed exception: Foundation.ObjCException: NSInvalidArgumentException: *** setObjectForKey: key cannot be nil
  at (wrapper managed-to-native) ObjCRuntime.Messaging:void_objc_msgSend_IntPtr_IntPtr (intptr,intptr,intptr,intptr)
  at Foundation.NSMutableDictionary.LowlevelSetObject (System.IntPtr obj, System.IntPtr key) [0x00007] in <a5a64914d2154d6892f86a9aea94f373>:0 
  at ExceptionMarshaling.Exceptions.ThrowObjectiveCException () [0x00006] in <5306ef04713643e1acc578b9512d7e95>:0 
```

[1] https://developer.xamarin.com/guides/ios/advanced_topics/exception_marshaling
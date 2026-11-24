# Window Sharing Shower

Forces selected windows that were excluded from screen capture to become visible to compatible capture tools. Windows 10 version 2004 or later is required for the `WDA_EXCLUDEFROMCAPTURE` behavior used by the smoke tests.

Build the solution with Visual Studio 2022 Build Tools and the .NET Framework 4.8 targeting pack. The x64 application requests administrator privileges at startup so that it can access elevated applications.

Check a window to keep attempting to clear its capture exclusion. Unchecking stops further attempts; it does not restore the original exclusion. No windows are changed automatically unless explicitly selected using `--show-window 0xHWND` (the handle is hexadecimal).

The list reports the current capture flag, including an unknown state when it cannot be read. Failures show the failing operation and Win32 error; failed requests retry after five seconds. Timeouts stop retries for that selection. Details are written to `%LOCALAPPDATA%\WindowSharingShower\diagnostics.log`.

The x64 controller supports both x64 and x86 targets, resolves their own module exports, and invokes `SetWindowDisplayAffinity` inside the owning process. It checks the remote API result and reads the capture flag back. A target can reject access or restore its own exclusion, so a successful change is not a guarantee for every capture method.

Run `tests\Run-SmokeTests.ps1` on an interactive Windows desktop to verify actual x64/x86 protected windows, repeated remote calls, handle cleanup, an already-visible window, and invalid-window errors. Tests use disposable windows and do not touch third-party applications.

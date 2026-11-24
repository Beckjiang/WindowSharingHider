# Window Sharing Shower
Forces windows that were hidden from screen sharing to show up again. Works with Teams, Zoom, Discord, etc.

Check a window to force it into screen capture. Unchecking stops forcing it; this app never hides windows.

Currently hidden windows are selected on first scan so they can be revealed immediately.

Single app, no dll's, works on both x86/x64

Relies on [SetWindowDisplayAffinity](https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity). Microsoft specifically restricted it to only work on windows where the current process is the owner of the window. This works by creating a thread in the target process to bypass that restriction.

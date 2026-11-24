# Window Sharing Hider
 Hides Windows during screen sharing. Works with Teams, Zoom, Discord, etc.

 Two modes:
 - **Hide mode**: checked windows are excluded from screen capture.
 - **Show mode**: checked windows are forced back into screen capture, including windows that were previously hidden.

 Single app, no dll's, works on both x86/x64

 Relies on [SetWindowDisplayAffinity](https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity). Microsoft specifically restricted it to only work on windows where the current process is the owner of the window. This works by creating a thread in the target process to bypass that restriction.

using System;
using System.Runtime.InteropServices;

namespace Hardcodet.Wpf.TaskbarNotification.Interop
{
	internal static class WinApi
	{
		private const string User32 = "user32.dll";

		[DllImport("shell32.Dll", CharSet = CharSet.Unicode)]
		public static extern bool Shell_NotifyIcon(NotifyCommand cmd, [In] ref NotifyIconData data);

		[DllImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true)]
		public static extern IntPtr CreateWindowEx(int dwExStyle, [MarshalAs(UnmanagedType.LPWStr)] string lpClassName, [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

		[DllImport("user32.dll")]
		public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wparam, IntPtr lparam);

		[DllImport("user32.dll", EntryPoint = "RegisterClassW", SetLastError = true)]
		public static extern short RegisterClass(ref WindowClass lpWndClass);

		[DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW")]
		public static extern uint RegisterWindowMessage([MarshalAs(UnmanagedType.LPWStr)] string lpString);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool DestroyWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern int GetDoubleClickTime();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool GetPhysicalCursorPos(ref Point lpPoint);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool GetCursorPos(ref Point lpPoint);
	}
}

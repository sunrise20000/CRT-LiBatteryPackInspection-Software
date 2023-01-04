using System;
using System.Windows;
using System.Windows.Interop;

namespace WpfStyleableWindow.StyleableWindow
{
	public static class SystemMenuManager
	{
		public static void ShowMenu(Window targetWindow, Point menuLocation)
		{
			if (targetWindow == null)
			{
				throw new ArgumentNullException("TargetWindow is null.");
			}
			int x;
			int y;
			try
			{
				x = Convert.ToInt32(menuLocation.X);
				y = Convert.ToInt32(menuLocation.Y);
			}
			catch (OverflowException)
			{
				x = 0;
				y = 0;
			}
			uint msg = 274u;
			uint num = 0u;
			uint num2 = 256u;
			IntPtr handle = new WindowInteropHelper(targetWindow).Handle;
			IntPtr systemMenu = NativeMethods.GetSystemMenu(handle, bRevert: false);
			int num3 = NativeMethods.TrackPopupMenuEx(systemMenu, num | num2, x, y, handle, IntPtr.Zero);
			if (num3 != 0)
			{
				NativeMethods.PostMessage(handle, msg, new IntPtr(num3), IntPtr.Zero);
			}
		}
	}
}

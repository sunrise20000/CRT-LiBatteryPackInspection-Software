using System;

namespace Hardcodet.Wpf.TaskbarNotification.Interop
{
	public delegate IntPtr WindowProcedureHandler(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
}

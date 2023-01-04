using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Hardcodet.Wpf.TaskbarNotification.Interop
{
	public class AppBarInfo
	{
		public enum ScreenEdge
		{
			Undefined = -1,
			Left = 0,
			Top = 1,
			Right = 2,
			Bottom = 3
		}

		private struct APPBARDATA
		{
			public uint cbSize;

			public IntPtr hWnd;

			public uint uCallbackMessage;

			public uint uEdge;

			public RECT rc;

			public int lParam;
		}

		private struct RECT
		{
			public int left;

			public int top;

			public int right;

			public int bottom;
		}

		private const int ABE_BOTTOM = 3;

		private const int ABE_LEFT = 0;

		private const int ABE_RIGHT = 2;

		private const int ABE_TOP = 1;

		private const int ABM_GETTASKBARPOS = 5;

		private const uint SPI_GETWORKAREA = 48u;

		private APPBARDATA m_data;

		public ScreenEdge Edge => (ScreenEdge)m_data.uEdge;

		public Rectangle WorkArea => GetRectangle(m_data.rc);

		[DllImport("user32.dll")]
		private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("shell32.dll")]
		private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA data);

		[DllImport("user32.dll")]
		private static extern int SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

		private Rectangle GetRectangle(RECT rc)
		{
			return new Rectangle(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
		}

		public void GetPosition(string strClassName, string strWindowName)
		{
			m_data = default(APPBARDATA);
			m_data.cbSize = (uint)Marshal.SizeOf(m_data.GetType());
			IntPtr intPtr = FindWindow(strClassName, strWindowName);
			if (intPtr != IntPtr.Zero)
			{
				uint num = SHAppBarMessage(5u, ref m_data);
				if (num != 1)
				{
					throw new Exception("Failed to communicate with the given AppBar");
				}
				return;
			}
			throw new Exception("Failed to find an AppBar that matched the given criteria");
		}

		public void GetSystemTaskBarPosition()
		{
			GetPosition("Shell_TrayWnd", null);
		}
	}
}

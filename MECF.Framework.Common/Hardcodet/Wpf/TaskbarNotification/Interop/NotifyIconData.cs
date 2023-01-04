using System;
using System.Runtime.InteropServices;

namespace Hardcodet.Wpf.TaskbarNotification.Interop
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct NotifyIconData
	{
		public uint cbSize;

		public IntPtr WindowHandle;

		public uint TaskbarIconId;

		public IconDataMembers ValidMembers;

		public uint CallbackMessageId;

		public IntPtr IconHandle;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string ToolTipText;

		public IconState IconState;

		public IconState StateMask;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string BalloonText;

		public uint VersionOrTimeout;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string BalloonTitle;

		public BalloonFlags BalloonFlags;

		public Guid TaskbarIconGuid;

		public IntPtr CustomBalloonIconHandle;

		public static NotifyIconData CreateDefault(IntPtr handle)
		{
			NotifyIconData notifyIconData = default(NotifyIconData);
			if (Environment.OSVersion.Version.Major >= 6)
			{
				notifyIconData.cbSize = (uint)Marshal.SizeOf(notifyIconData);
			}
			else
			{
				notifyIconData.cbSize = 952u;
				notifyIconData.VersionOrTimeout = 10u;
			}
			notifyIconData.WindowHandle = handle;
			notifyIconData.TaskbarIconId = 0u;
			notifyIconData.CallbackMessageId = 1024u;
			notifyIconData.VersionOrTimeout = 0u;
			notifyIconData.IconHandle = IntPtr.Zero;
			notifyIconData.IconState = IconState.Hidden;
			notifyIconData.StateMask = IconState.Hidden;
			notifyIconData.ValidMembers = IconDataMembers.Message | IconDataMembers.Icon | IconDataMembers.Tip;
			notifyIconData.ToolTipText = (notifyIconData.BalloonText = (notifyIconData.BalloonTitle = string.Empty));
			return notifyIconData;
		}
	}
}

using System;

namespace Hardcodet.Wpf.TaskbarNotification.Interop
{
	[Flags]
	public enum IconDataMembers
	{
		Message = 1,
		Icon = 2,
		Tip = 4,
		State = 8,
		Info = 0x10,
		Realtime = 0x40,
		UseLegacyToolTips = 0x80
	}
}

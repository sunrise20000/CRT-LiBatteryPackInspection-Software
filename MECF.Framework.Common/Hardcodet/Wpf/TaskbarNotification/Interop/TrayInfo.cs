using System.Drawing;

namespace Hardcodet.Wpf.TaskbarNotification.Interop
{
	public static class TrayInfo
	{
		public static Point GetTrayLocation()
		{
			int num = 2;
			AppBarInfo appBarInfo = new AppBarInfo();
			appBarInfo.GetSystemTaskBarPosition();
			Rectangle workArea = appBarInfo.WorkArea;
			int x = 0;
			int y = 0;
			switch (appBarInfo.Edge)
			{
			case AppBarInfo.ScreenEdge.Left:
				x = workArea.Right + num;
				y = workArea.Bottom;
				break;
			case AppBarInfo.ScreenEdge.Bottom:
				x = workArea.Right;
				y = workArea.Bottom - workArea.Height - num;
				break;
			case AppBarInfo.ScreenEdge.Top:
				x = workArea.Right;
				y = workArea.Top + workArea.Height + num;
				break;
			case AppBarInfo.ScreenEdge.Right:
				x = workArea.Right - workArea.Width - num;
				y = workArea.Bottom;
				break;
			}
			Point point = default(Point);
			point.X = x;
			point.Y = y;
			return GetDeviceCoordinates(point);
		}

		public static Point GetDeviceCoordinates(Point point)
		{
			Point result = default(Point);
			result.X = (int)((double)point.X / SystemInfo.DpiFactorX);
			result.Y = (int)((double)point.Y / SystemInfo.DpiFactorY);
			return result;
		}
	}
}

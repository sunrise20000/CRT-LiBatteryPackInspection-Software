using System.Windows;
using System.Windows.Interop;

namespace Hardcodet.Wpf.TaskbarNotification.Interop
{
	public static class SystemInfo
	{
		private static readonly System.Windows.Point DpiFactors;

		public static double DpiFactorX => DpiFactors.X;

		public static double DpiFactorY => DpiFactors.Y;

		static SystemInfo()
		{
			using HwndSource hwndSource = new HwndSource(default(HwndSourceParameters));
			HwndTarget compositionTarget = hwndSource.CompositionTarget;
			if (compositionTarget != null)
			{
				_ = compositionTarget.TransformToDevice;
				if (true)
				{
					DpiFactors = new System.Windows.Point(hwndSource.CompositionTarget.TransformToDevice.M11, hwndSource.CompositionTarget.TransformToDevice.M22);
					return;
				}
			}
			DpiFactors = new System.Windows.Point(1.0, 1.0);
		}
	}
}

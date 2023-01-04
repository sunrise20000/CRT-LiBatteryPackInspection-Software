using System;
using System.IO;
using System.Threading.Tasks;
using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;

namespace MECF.Framework.Common.DataCenter
{
	public class DiskManager
	{
		private PeriodicJob _threadMonitorDiskSpace;

		public bool IsEnableMonitorDiskSpaceFunc { get; set; }

		public DiskManager()
		{
			if (SC.ContainsItem("System.IsEnableMonitorDiskSpaceFunc"))
			{
				IsEnableMonitorDiskSpaceFunc = SC.GetValue<bool>("System.IsEnableMonitorDiskSpaceFunc");
			}
		}

		public void Run()
		{
			_threadMonitorDiskSpace = new PeriodicJob(1800000, MonitorDiskSpace, "MonitorDiskSpace Thread");
			Task.Delay(10000).ContinueWith(delegate
			{
				_threadMonitorDiskSpace.Start();
			});
		}

		private bool MonitorDiskSpace()
		{
			try
			{
				long num = 0L;
				long num2 = 0L;
				string text = PathManager.GetAppDir().Substring(0, 3);
				DriveInfo[] drives = DriveInfo.GetDrives();
				DriveInfo[] array = drives;
				foreach (DriveInfo driveInfo in array)
				{
					if (driveInfo.Name == text)
					{
						num = driveInfo.TotalFreeSpace;
						num2 = driveInfo.TotalSize;
					}
				}
				if ((double)num < (double)num2 * 0.05)
				{
					EV.PostAlarmLog("System", text + " Hard disk Free Space is less than 5% ，need release");
				}
				else if ((double)num < (double)num2 * 0.1)
				{
					EV.PostWarningLog("System", text + " Hard disk Free Space is less than 10%，need release");
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return true;
		}

		public void Stop()
		{
			_threadMonitorDiskSpace.Stop();
		}

		~DiskManager()
		{
			Stop();
		}
	}
}

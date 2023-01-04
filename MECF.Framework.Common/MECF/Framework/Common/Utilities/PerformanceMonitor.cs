using System;
using System.Diagnostics;
using System.Text;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Utilities
{
	public class PerformanceMonitor
	{
		public static DeviceTimer _timer = new DeviceTimer();

		public static void TraceLog()
		{
			if (_timer.GetElapseTime() < 1000.0 && !_timer.IsIdle())
			{
				return;
			}
			_timer.Start(0.0);
			Action action = delegate
			{
				try
				{
					Process[] processes = Process.GetProcesses();
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.Append("【System Performance Monitor Report】\r\n");
					stringBuilder.Append(string.Format("{0,30}", "ProcessName"));
					stringBuilder.Append(string.Format("{0,10}", "PID"));
					stringBuilder.Append(string.Format("{0,10}", "Priority"));
					stringBuilder.Append(string.Format("{0,10}", "Handle"));
					stringBuilder.Append(string.Format("{0,10}", "Thread"));
					stringBuilder.Append(string.Format("{0,15}", "PriMemory(MB)"));
					stringBuilder.Append(string.Format("{0,15}", " WorkingSet(MB)\r\n"));
					for (int i = 0; i < processes.Length - 1; i++)
					{
						Process process = processes[i];
						stringBuilder.Append(string.Format("{0,30}", process.ProcessName));
						stringBuilder.Append(string.Format("{0,10}", Convert.ToString(process.Id.ToString("0000"))));
						stringBuilder.Append(string.Format("{0,10}", process.BasePriority));
						stringBuilder.Append(string.Format("{0,10}", process.HandleCount));
						stringBuilder.Append(string.Format("{0,10}", process.Threads.Count));
						stringBuilder.Append(string.Format("{0,15}", ((double)process.PrivateMemorySize64 / 1000000.0).ToString("F1")));
						stringBuilder.Append(string.Format("{0,15}", ((double)process.WorkingSet64 / 1000000.0).ToString("F1")));
						stringBuilder.Append("\r\n");
					}
					LOG.Write(stringBuilder.ToString());
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
			};
			action.BeginInvoke(null, null);
		}
	}
}

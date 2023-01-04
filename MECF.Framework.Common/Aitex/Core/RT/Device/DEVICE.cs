using System;
using System.Collections.Generic;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.Device
{
	public class DEVICE
	{
		public delegate bool? DeviceFunc(out string reason, int time, params object[] param);

		private static object _locker = new object();

		private static Dictionary<string, DeviceFunc> deviceFuncs = new Dictionary<string, DeviceFunc>();

		public static string Module = "Device";

		public static IDeviceManager Manager { private get; set; }

		public static void Register(string name, DeviceFunc func)
		{
			lock (_locker)
			{
				if (!deviceFuncs.ContainsKey(name))
				{
					deviceFuncs.Add(name, func);
				}
			}
		}

		public static void UnRegister(string name)
		{
			lock (_locker)
			{
				deviceFuncs.Remove(name);
			}
		}

		public static bool CanDo(string name)
		{
			return deviceFuncs.ContainsKey(name);
		}

		public static bool? Do(string name, int time, bool isClientCmd, params object[] param)
		{
			bool? result = false;
			lock (_locker)
			{
				try
				{
					result = deviceFuncs[name](out var reason, time, param);
					string text = $"Execute：{name}，{reason}";
					if (result.HasValue && !result.Value)
					{
						string text2 = $"Failed to do {name}, {reason}";
						EV.PostMessage(Module, EventEnum.GuiCmdExecFailed, Module, text2);
					}
					else
					{
						EV.PostMessage(Module, EventEnum.GuiCmdExecSucc, Module, text);
					}
				}
				catch (Exception ex)
				{
					LOG.Write(ex, $"Device:执行{name}命令发生异常");
					result = false;
				}
			}
			return result;
		}

		public static List<IDevice> GetAllDevice()
		{
			if (Manager != null)
			{
				return Manager.GetAllDevice();
			}
			return null;
		}

		public static T GetDevice<T>(string name) where T : class, IDevice
		{
			if (Manager != null)
			{
				return Manager.GetDevice<T>(name);
			}
			return null;
		}

		public static object GetDevice(string name)
		{
			if (Manager != null)
			{
				return Manager.GetDevice(name);
			}
			return null;
		}

		public static List<T> GetDevice<T>() where T : class, IDevice
		{
			if (Manager != null)
			{
				return Manager.GetDevice<T>();
			}
			return null;
		}

		public static object GetOptionDevice(string name, Type type)
		{
			if (Manager != null)
			{
				return Manager.GetOptionDevice(name, type);
			}
			return null;
		}
	}
}

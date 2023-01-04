using System.Collections.Generic;
using MECF.Framework.Common.SCCore;

namespace Aitex.Core.RT.SCCore
{
	public static class SC
	{
		public static ISCManager Manager { private get; set; }

		public static Dictionary<string, ISCManager> ModularManager { get; private set; } = new Dictionary<string, ISCManager>();


		public static void SetItemValue(string name, object value)
		{
			if (Manager != null)
			{
				Manager.SetItemValue(name, value);
			}
		}

		public static void SetItemValueStringFormat(string name, string value)
		{
			if (Manager != null)
			{
				Manager.SetItemValueStringFormat(name, value);
			}
		}

		public static void SetItemValue(string name, bool value)
		{
			if (Manager != null)
			{
				Manager.SetItemValue(name, value);
			}
		}

		public static void SetItemValue(string name, int value)
		{
			if (Manager != null)
			{
				Manager.SetItemValue(name, value);
			}
		}

		public static void SetItemValue(string name, double value)
		{
			if (Manager != null)
			{
				Manager.SetItemValue(name, value);
			}
		}

		public static void SetItemValue(string name, string value)
		{
			if (Manager != null)
			{
				Manager.SetItemValue(name, value);
			}
		}

		public static void SetItemValueFromString(string name, string value)
		{
			if (Manager != null)
			{
				Manager.SetItemValueFromString(name, value);
			}
		}

		public static SCConfigItem GetConfigItem(string name)
		{
			if (Manager != null)
			{
				return Manager.GetConfigItem(name);
			}
			return null;
		}

		public static bool ContainsItem(string name)
		{
			if (Manager != null)
			{
				return Manager.ContainsItem(name);
			}
			return false;
		}

		public static SCConfigItem GetConfigItem(string path, string name)
		{
			return GetConfigItem(path + "." + name);
		}

		public static T GetValue<T>(string name) where T : struct
		{
			if (Manager != null)
			{
				return Manager.GetValue<T>(name);
			}
			return default(T);
		}

		public static string GetStringValue(string name)
		{
			if (Manager != null)
			{
				return Manager.GetStringValue(name);
			}
			return null;
		}

		public static T SafeGetValue<T>(string name, T defaultValue) where T : struct
		{
			if (Manager != null)
			{
				return Manager.SafeGetValue(name, defaultValue);
			}
			return default(T);
		}

		public static string SafeGetStringValue(string name, string defaultValue)
		{
			if (Manager != null)
			{
				return Manager.SafeGetStringValue(name, defaultValue);
			}
			return null;
		}

		public static List<SCConfigItem> GetItemList()
		{
			if (Manager != null)
			{
				return Manager.GetItemList();
			}
			return null;
		}

		public static string GetConfigFileContent()
		{
			if (Manager != null)
			{
				return Manager.GetFileContent();
			}
			return "";
		}

		public static string GetConfigFileContent(string module)
		{
			if (Manager != null && ModularManager != null && ModularManager.ContainsKey(module) && ModularManager[module] != null)
			{
				return ModularManager[module].GetFileContent();
			}
			return GetConfigFileContent();
		}
	}
}

using System;

namespace Aitex.Core.Util
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class SubscriptionAttribute : Attribute
	{
		public enum FLAG
		{
			SaveDB = 0,
			IgnoreSaveDB = 1
		}

		public readonly string Key;

		public readonly string Method;

		public readonly int Flag;

		public string ModuleKey => string.IsNullOrEmpty(Module) ? Key : (Module + "." + Key);

		public string Module { get; internal set; }

		public SubscriptionAttribute(string key, int flag = 0, string module = "")
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException("key");
			}
			Key = key;
			Flag = flag;
			Module = module;
		}

		public SubscriptionAttribute(string key, string module)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException("key");
			}
			Key = key;
			Flag = 0;
			Module = module;
		}

		public SubscriptionAttribute(object key, string module)
		{
			if (string.IsNullOrWhiteSpace(key.ToString()))
			{
				throw new ArgumentNullException("key");
			}
			Key = key.ToString();
			Flag = 1;
			Module = module;
		}

		public SubscriptionAttribute(object key)
		{
			if (string.IsNullOrWhiteSpace(key.ToString()))
			{
				throw new ArgumentNullException("key");
			}
			Key = key.ToString();
			Flag = 1;
			Module = "";
		}

		public void SetModule(string module)
		{
			Module = module;
		}

		public SubscriptionAttribute(string key, string module, string deviceName, string deviceType)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException("key");
			}
			Key = $"{deviceType}.{deviceName}.{key}";
			Flag = 0;
			Module = module;
		}

		public SubscriptionAttribute(string module, string method, params object[] args)
		{
			if (string.IsNullOrWhiteSpace(module))
			{
				throw new ArgumentNullException("module");
			}
			Module = module;
			Method = method;
		}
	}
}

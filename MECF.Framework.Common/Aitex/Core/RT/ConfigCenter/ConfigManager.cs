using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aitex.Common.Util;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;

namespace Aitex.Core.RT.ConfigCenter
{
	public class ConfigManager : Singleton<ConfigManager>, ICommonConfig
	{
		private Dictionary<string, object> _dic = new Dictionary<string, object>();

		private ConcurrentDictionary<string, DataItem<object>> _keyValueMap = new ConcurrentDictionary<string, DataItem<object>>(StringComparer.OrdinalIgnoreCase);

		private object _locker = new object();

		public void Initialize()
		{
			CONFIG.InnerConfigManager = this;
			OP.Subscribe("SetConfig", InvokeSetConfig);
		}

		private bool InvokeSetConfig(string arg1, object[] arg2)
		{
			return true;
		}

		public void Terminate()
		{
		}

		public string GetFileContent(string fileName)
		{
			if (!Path.IsPathRooted(fileName))
			{
				fileName = PathManager.GetCfgDir() + "\\" + fileName;
			}
			if (!File.Exists(fileName))
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			try
			{
				using StreamReader streamReader = new StreamReader(fileName);
				while (!streamReader.EndOfStream)
				{
					stringBuilder.Append(streamReader.ReadLine());
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return "";
			}
			return stringBuilder.ToString();
		}

		public object GetConfig(string config)
		{
			return _dic.ContainsKey(config) ? _dic[config] : null;
		}

		public void SetConfig(string config, object value)
		{
			_dic[config] = value;
		}

		public void Subscribe(string module, string key, Func<object> getter)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException("key");
			}
			if (!string.IsNullOrEmpty(module))
			{
				key = module + "." + key;
			}
			if (_keyValueMap.ContainsKey(key))
			{
				throw new Exception($"Duplicated Key:{key}");
			}
			if (getter == null)
			{
				throw new ArgumentNullException("getter");
			}
			_keyValueMap.TryAdd(key, new DataItem<object>(getter));
		}

		public object Poll(string key)
		{
			return _keyValueMap.ContainsKey(key) ? _keyValueMap[key].Value : null;
		}

		public Dictionary<string, object> PollConfig(IEnumerable<string> keys)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			foreach (string key in keys)
			{
				if (_keyValueMap.ContainsKey(key))
				{
					dictionary[key] = _keyValueMap[key].Value;
				}
				else
				{
					LOG.Error("undefined config：" + key);
				}
			}
			return dictionary;
		}
	}
}

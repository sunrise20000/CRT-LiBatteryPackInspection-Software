using System.Collections.Generic;
using System.Xml.Linq;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device
{
	public class DeviceDefineManager : Singleton<DeviceDefineManager>
	{
		private readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>();

		private XDocument _xml;

		private string _xmlConfigPath;

		public T? GetValue<T>(string name) where T : struct
		{
			try
			{
				if (!_dictionary.ContainsKey(name))
				{
					return default(T);
				}
				if (typeof(T) == typeof(bool))
				{
					return (T)(object)(_dictionary[name].ToLower() == "true");
				}
				if (typeof(T) == typeof(int))
				{
					return (T)(object)int.Parse(_dictionary[name]);
				}
				if (typeof(T) == typeof(double))
				{
					return (T)(object)double.Parse(_dictionary[name]);
				}
				return default(T);
			}
			catch
			{
				return null;
			}
		}

		public string GetValue(string name)
		{
			_dictionary.TryGetValue(name, out var value);
			return value;
		}

		private void Load()
		{
			foreach (XNode item in _xml.DescendantNodes())
			{
				if (item is XElement xElement && !(xElement.Name == _xml.Root?.Name))
				{
					_dictionary.Add(xElement.Name.ToString(), xElement.Value);
				}
			}
		}

		private void Subscribe()
		{
			foreach (KeyValuePair<string, string> define in _dictionary)
			{
				DATA.Subscribe("DeviceDefine." + define.Key, () => _dictionary[define.Key]);
			}
		}

		public void Initialize(string xmlConfigPath)
		{
			_xmlConfigPath = xmlConfigPath;
			_xml = XDocument.Load(_xmlConfigPath);
			Load();
			Subscribe();
		}
	}
}

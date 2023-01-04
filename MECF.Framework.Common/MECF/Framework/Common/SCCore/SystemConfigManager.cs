#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Aitex.Common.Util;
using Aitex.Core.RT.ConfigCenter;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.FAServices;

namespace MECF.Framework.Common.SCCore
{
	public class SystemConfigManager : Singleton<SystemConfigManager>, ISCManager
	{
		private Dictionary<string, SCConfigItem> _items = new Dictionary<string, SCConfigItem>(StringComparer.OrdinalIgnoreCase);

		private object _itemLocker = new object();

		private string _scConfigFile;

		private string _scDataFile = PathManager.GetCfgDir() + "_sc.data";

		private string _scDataBackupFile = PathManager.GetCfgDir() + "_sc.data.bak";

		private string _scDataErrorFile = PathManager.GetCfgDir() + "_sc.data.err.";

		public List<VIDItem> VidConfigList
		{
			get
			{
				List<VIDItem> list = new List<VIDItem>();
				foreach (KeyValuePair<string, SCConfigItem> item in _items)
				{
					list.Add(new VIDItem
					{
						DataType = "",
						Description = item.Value.Description,
						Index = 0,
						Name = item.Key,
						Unit = ""
					});
				}
				return list;
			}
		}

		public void Initialize(string scConfigPathName)
		{
			_scConfigFile = scConfigPathName;
			BuildItems(_scConfigFile);
			BackupAndRecoverDataFile();
			CustomData();
			GenerateDataFile();
			foreach (KeyValuePair<string, SCConfigItem> item in _items)
			{
				CONFIG.Subscribe("", item.Key, () => item.Value.Value);
			}
			OP.Subscribe("System.SetConfig", InvokeSetConfig);
			SC.Manager = this;
			Singleton<DatabaseManager>.Instance.StartDataCleaner();
		}

		private bool InvokeSetConfig(string cmd, object[] parameters)
		{
			string text = (string)parameters[0];
			if (!ContainsItem(text))
			{
				EV.PostWarningLog("System", $"Not find SC with name {text}");
				return false;
			}
			object configValueAsObject = GetConfigValueAsObject(text);
			if (InitializeItemValue(_items[(string)parameters[0]], parameters[1].ToString()))
			{
				GenerateDataFile();
				EV.PostInfoLog("System", $"SC {text} value changed from {configValueAsObject} to {parameters[1]}");
			}
			return true;
		}

		public void Terminate()
		{
		}

		public string GetFileContent()
		{
			if (!File.Exists(_scConfigFile))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			try
			{
				using StreamReader streamReader = new StreamReader(_scConfigFile);
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

		private void BuildItems(string xmlFile)
		{
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.Load(xmlFile);
				XmlNodeList xmlNodeList = xmlDocument.SelectNodes("root/configs");
				foreach (XmlElement item in xmlNodeList)
				{
					BuildPathConfigs(item.GetAttribute("name"), item);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		private void BuildPathConfigs(string parentPath, XmlElement configElement)
		{
			XmlNodeList xmlNodeList = configElement.SelectNodes("configs");
			foreach (XmlElement item in xmlNodeList)
			{
				if (string.IsNullOrEmpty(parentPath))
				{
					BuildPathConfigs(item.GetAttribute("name"), item);
				}
				else
				{
					BuildPathConfigs(parentPath + "." + item.GetAttribute("name"), item);
				}
			}
			XmlNodeList xmlNodeList2 = configElement.SelectNodes("config");
			foreach (XmlElement item2 in xmlNodeList2)
			{
				SCConfigItem sCConfigItem = new SCConfigItem
				{
					Default = item2.GetAttribute("default"),
					Name = item2.GetAttribute("name"),
					Description = item2.GetAttribute("description"),
					Max = item2.GetAttribute("max"),
					Min = item2.GetAttribute("min"),
					Parameter = item2.GetAttribute("paramter"),
					Path = parentPath,
					Tag = item2.GetAttribute("tag"),
					Type = item2.GetAttribute("type"),
					Unit = item2.GetAttribute("unit")
				};
				InitializeItemValue(sCConfigItem, sCConfigItem.Default);
				if (_items.ContainsKey(sCConfigItem.PathName))
				{
					LOG.Error("Duplicated SC item, " + sCConfigItem.PathName);
				}
				_items[sCConfigItem.PathName] = sCConfigItem;
			}
		}

		private void BackupAndRecoverDataFile()
		{
			try
			{
				if (File.Exists(_scDataFile) && IsXmlFileLoadable(_scDataFile))
				{
					File.Copy(_scDataFile, _scDataBackupFile, overwrite: true);
				}
				else if (File.Exists(_scDataBackupFile) && IsXmlFileLoadable(_scDataBackupFile))
				{
					if (File.Exists(_scDataFile))
					{
						File.Copy(_scDataFile, _scDataErrorFile + DateTime.Now.ToString("yyyyMMdd_HHmmss"), overwrite: true);
					}
					File.Copy(_scDataBackupFile, _scDataFile, overwrite: true);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		private void CustomData()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			try
			{
				if (!File.Exists(_scDataFile))
				{
					return;
				}
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(_scDataFile);
				XmlNodeList xmlNodeList = xmlDocument.SelectNodes("root/scdata");
				foreach (XmlElement item in xmlNodeList)
				{
					string attribute = item.GetAttribute("name");
					if (_items.ContainsKey(attribute))
					{
						InitializeItemValue(_items[attribute], item.GetAttribute("value"));
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		private void GenerateDataFile()
		{
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><root></root>");
				XmlElement xmlElement = xmlDocument.SelectSingleNode("root") as XmlElement;
				foreach (KeyValuePair<string, SCConfigItem> item in _items)
				{
					XmlElement xmlElement2 = xmlDocument.CreateElement("scdata");
					xmlElement2.SetAttribute("name", item.Key);
					xmlElement2.SetAttribute("value", item.Value.Value.ToString());
					xmlElement.AppendChild(xmlElement2);
				}
				if (File.Exists(_scDataFile) && IsXmlFileLoadable(_scDataFile))
				{
					File.Copy(_scDataFile, _scDataBackupFile, overwrite: true);
				}
				using FileStream fileStream = new FileStream(_scDataFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 1024, FileOptions.WriteThrough);
				fileStream.SetLength(0L);
				XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
				xmlWriterSettings.Indent = true;
				xmlWriterSettings.OmitXmlDeclaration = false;
				using XmlWriter w = XmlWriter.Create(fileStream, xmlWriterSettings);
				xmlDocument.Save(w);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		private bool IsXmlFileLoadable(string file)
		{
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(file);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return false;
			}
			return true;
		}

		public SCConfigItem GetConfigItem(string name)
		{
			if (!_items.ContainsKey(name))
			{
				return null;
			}
			return _items[name];
		}

		public bool ContainsItem(string name)
		{
			return _items.ContainsKey(name);
		}

		public object GetConfigValueAsObject(string name)
		{
			SCConfigItem configItem = GetConfigItem(name);
			return configItem.Type switch
			{
				"Bool" => configItem.BoolValue, 
				"Integer" => configItem.IntValue, 
				"Double" => configItem.DoubleValue, 
				"String" => configItem.StringValue, 
				_ => null, 
			};
		}

		public T GetValue<T>(string name) where T : struct
		{
			try
			{
				if (typeof(T) == typeof(bool))
				{
					return (T)(object)_items[name].BoolValue;
				}
				if (typeof(T) == typeof(int))
				{
					return (T)(object)_items[name].IntValue;
				}
				if (typeof(T) == typeof(double))
				{
					return (T)(object)_items[name].DoubleValue;
				}
			}
			catch (KeyNotFoundException)
			{
				EV.PostAlarmLog("System", "Can not find system config item " + name);
				return default(T);
			}
			catch (Exception)
			{
				EV.PostAlarmLog("System", "Can not get valid system config item value " + name);
				return default(T);
			}
			Debug.Assert(condition: false, "unsupported type");
			return default(T);
		}

		public string GetStringValue(string name)
		{
			if (!_items.ContainsKey(name))
			{
				return null;
			}
			return _items[name].StringValue;
		}

		public T SafeGetValue<T>(string name, T defaultValue) where T : struct
		{
			try
			{
				if (typeof(T) == typeof(bool))
				{
					return (T)(object)_items[name].BoolValue;
				}
				if (typeof(T) == typeof(int))
				{
					return (T)(object)_items[name].IntValue;
				}
				if (typeof(T) == typeof(double))
				{
					return (T)(object)_items[name].DoubleValue;
				}
			}
			catch (KeyNotFoundException)
			{
				return defaultValue;
			}
			catch (Exception)
			{
				return defaultValue;
			}
			Debug.Assert(condition: false, "unsupported type");
			return defaultValue;
		}

		public string SafeGetStringValue(string name, string defaultValue)
		{
			if (!_items.ContainsKey(name))
			{
				return defaultValue;
			}
			return _items[name].StringValue;
		}

		public List<SCConfigItem> GetItemList()
		{
			return _items.Values.ToList();
		}

		public void SetItemValueFromString(string name, string value)
		{
			if (InitializeItemValue(_items[name], value))
			{
				GenerateDataFile();
			}
		}

		private bool InitializeItemValue(SCConfigItem item, string value)
		{
			bool result = false;
			switch (item.Type)
			{
			case "Bool":
			{
				if (bool.TryParse(value, out var result2) && result2 != item.BoolValue)
				{
					item.BoolValue = result2;
					result = true;
				}
				break;
			}
			case "Integer":
			{
				if (int.TryParse(value, out var result6) && result6 != item.IntValue)
				{
					int.TryParse(item.Min, out var result7);
					int.TryParse(item.Max, out var result8);
					if (result6 < result7 || result6 > result8)
					{
						EV.PostWarningLog("System", $"SC {item.PathName} value  {result6} out of setting range ({item.Min}, {item.Max})");
					}
					else
					{
						item.IntValue = result6;
						result = true;
					}
				}
				break;
			}
			case "Double":
			{
				if (double.TryParse(value, out var result3) && Math.Abs(result3 - item.DoubleValue) > 0.0001)
				{
					double.TryParse(item.Min, out var result4);
					double.TryParse(item.Max, out var result5);
					if (result3 < result4 || result3 > result5)
					{
						EV.PostWarningLog("System", $"SC {item.PathName}  value  {result3} out of setting range ({item.Min}, {item.Max})");
					}
					else
					{
						item.DoubleValue = result3;
						result = true;
					}
				}
				break;
			}
			case "String":
				if (value != item.StringValue)
				{
					item.StringValue = value;
					result = true;
				}
				break;
			}
			return result;
		}

		public void SetItemValue(string name, object value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);
			if (!_items.ContainsKey(name))
			{
				return;
			}
			bool flag = false;
			switch (_items[name].Type)
			{
			case "Bool":
			{
				bool flag2 = (bool)value;
				if (flag2 != _items[name].BoolValue)
				{
					_items[name].BoolValue = flag2;
					flag = true;
				}
				break;
			}
			case "Integer":
			{
				int num = (int)value;
				if (num != _items[name].IntValue)
				{
					_items[name].IntValue = num;
					flag = true;
				}
				break;
			}
			case "Double":
			{
				double num2 = (double)value;
				if (Math.Abs(num2 - _items[name].DoubleValue) > 0.0001)
				{
					_items[name].DoubleValue = num2;
					flag = true;
				}
				break;
			}
			case "String":
			{
				string text = (string)value;
				if (text != _items[name].StringValue)
				{
					_items[name].StringValue = text;
					flag = true;
				}
				break;
			}
			}
			if (flag)
			{
				GenerateDataFile();
			}
		}

		public void SetItemValueStringFormat(string name, string value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);
			if (!_items.ContainsKey(name))
			{
				return;
			}
			bool flag = false;
			switch (_items[name].Type)
			{
			case "Bool":
			{
				bool flag2 = Convert.ToBoolean(value);
				if (flag2 != _items[name].BoolValue)
				{
					_items[name].BoolValue = flag2;
					flag = true;
				}
				break;
			}
			case "Integer":
			{
				int num = Convert.ToInt32(value);
				if (num != _items[name].IntValue)
				{
					_items[name].IntValue = num;
					flag = true;
				}
				break;
			}
			case "Double":
			{
				double num2 = Convert.ToDouble(value);
				if (Math.Abs(num2 - _items[name].DoubleValue) > 0.0001)
				{
					_items[name].DoubleValue = num2;
					flag = true;
				}
				break;
			}
			case "String":
				if (value != _items[name].StringValue)
				{
					_items[name].StringValue = value;
					flag = true;
				}
				break;
			}
			if (flag)
			{
				GenerateDataFile();
			}
		}

		public void SetItemValue(string name, bool value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);
			Debug.Assert(_items[name].Type == "Bool", "sc type not bool, defined as" + _items[name].Type);
			if (value != _items[name].BoolValue)
			{
				_items[name].BoolValue = value;
				GenerateDataFile();
			}
		}

		public void SetItemValue(string name, int value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);
			Debug.Assert(_items[name].Type == "Integer", "sc type not bool, defined as" + _items[name].Type);
			if (value != _items[name].IntValue)
			{
				_items[name].IntValue = value;
				GenerateDataFile();
			}
		}

		public void SetItemValue(string name, double value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);
			Debug.Assert(_items[name].Type == "Double", "sc type not bool, defined as" + _items[name].Type);
			if (Math.Abs(value - _items[name].DoubleValue) > 0.0001)
			{
				_items[name].DoubleValue = value;
				GenerateDataFile();
			}
		}

		public void SetItemValue(string name, string value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);
			if (value != _items[name].StringValue)
			{
				_items[name].StringValue = value;
				GenerateDataFile();
			}
		}
	}
}

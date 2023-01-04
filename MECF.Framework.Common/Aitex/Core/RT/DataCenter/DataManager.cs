using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aitex.Core.RT.ConfigCenter;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.FAServices;
using MECF.Framework.Common.Log;

namespace Aitex.Core.RT.DataCenter
{
	public class DataManager : ICommonData
	{
		private ConcurrentDictionary<string, DataItem<object>> _keyValueMap;

		private SortedDictionary<string, Func<object>> _dbRecorderList;

		private Func<object, bool> _isSubscriptionAttribute;

		private Func<MemberInfo, bool> _hasSubscriptionAttribute;

		private object _locker = new object();

		private LogCleaner logCleaner = new LogCleaner();

		private DiskManager diskManager = new DiskManager();

		public List<string> NumericDataList
		{
			get
			{
				List<string> list = new List<string>();
				foreach (KeyValuePair<string, DataItem<object>> item in _keyValueMap)
				{
					object value = item.Value.Value;
					if (value != null)
					{
						Type type = value.GetType();
						if (type == typeof(bool) || type == typeof(double) || type == typeof(float) || type == typeof(bool) || type == typeof(int) || type == typeof(ushort) || type == typeof(short))
						{
							list.Add(item.Key);
						}
					}
				}
				return list;
			}
		}

		public List<VIDItem> VidDataList
		{
			get
			{
				List<VIDItem> list = new List<VIDItem>();
				foreach (KeyValuePair<string, DataItem<object>> item in _keyValueMap)
				{
					object value = item.Value.Value;
					if (value != null)
					{
						Type type = value.GetType();
						if (type == typeof(bool) || type == typeof(double) || type == typeof(float) || type == typeof(bool) || type == typeof(int) || type == typeof(ushort) || type == typeof(short))
						{
							list.Add(new VIDItem
							{
								DataType = type.ToString(),
								Description = "",
								Index = 0,
								Name = item.Key,
								Unit = ""
							});
						}
					}
				}
				return list;
			}
		}

		public List<string> BuiltInDataList
		{
			get
			{
				List<string> list = new List<string>();
				foreach (KeyValuePair<string, DataItem<object>> item in _keyValueMap)
				{
					object value = item.Value.Value;
					if (value != null)
					{
						Type type = value.GetType();
						if (type == typeof(bool) || type == typeof(double) || type == typeof(float) || type == typeof(bool) || type == typeof(int) || type == typeof(ushort) || type == typeof(short) || type == typeof(string))
						{
							list.Add(item.Key);
						}
					}
				}
				return list;
			}
		}

		public List<string> FullDataList => _keyValueMap.Keys.ToList();

		public void Initialize()
		{
			Initialize(enableService: true);
		}

		public void Initialize(bool enableService, bool enableStats = true)
		{
			_dbRecorderList = new SortedDictionary<string, Func<object>>();
			_keyValueMap = new ConcurrentDictionary<string, DataItem<object>>();
			_isSubscriptionAttribute = (object attribute) => attribute is SubscriptionAttribute;
			_hasSubscriptionAttribute = (MemberInfo mi) => mi.GetCustomAttributes(inherit: false).Any(_isSubscriptionAttribute);
			DATA.InnerDataManager = this;
			if (enableService)
			{
				Singleton<WcfServiceManager>.Instance.Initialize(new Type[1] { typeof(QueryDataService) });
			}
			if (enableStats)
			{
				Singleton<StatsDataManager>.Instance.Initialize();
			}
			CONFIG.Subscribe("System", "NumericDataList", () => NumericDataList);
			logCleaner.Run();
			diskManager.Run();
			OP.Subscribe("System.DBExecute", DatabaseExecute);
		}

		private bool DatabaseExecute(string arg1, object[] arg2)
		{
			try
			{
				string text = (string)arg2[0];
				DB.Insert(text);
				LOG.Write("execute sql: " + text);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return true;
		}

		public void Terminate()
		{
			logCleaner.Stop();
			diskManager.Stop();
		}

		public Type GetDataType(string name)
		{
			if (_keyValueMap.ContainsKey(name))
			{
				object value = _keyValueMap[name].Value;
				if (value != null)
				{
					return value.GetType();
				}
			}
			return null;
		}

		public SortedDictionary<string, Func<object>> GetDBRecorderList()
		{
			lock (_locker)
			{
				return new SortedDictionary<string, Func<object>>(_dbRecorderList);
			}
		}

		public void Subscribe<T>(T instance, string keyPrefix = null) where T : class
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}
			Traverse(instance, keyPrefix);
		}

		public void Subscribe(string key, Func<object> getter, SubscriptionAttribute.FLAG flag)
		{
			Subscribe(key, new DataItem<object>(getter), flag);
			if (flag != SubscriptionAttribute.FLAG.IgnoreSaveDB)
			{
				lock (_locker)
				{
					_dbRecorderList[key] = getter;
				}
			}
		}

		public void Subscribe(string key, DataItem<object> dataItem, SubscriptionAttribute.FLAG flag)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException("key");
			}
			if (_keyValueMap.ContainsKey(key))
			{
				throw new Exception($"Duplicated Key:{key}");
			}
			if (dataItem == null)
			{
				throw new ArgumentNullException("dataItem");
			}
			_keyValueMap.TryAdd(key, dataItem);
		}

		public Dictionary<string, object> Poll(IEnumerable<string> keys)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			foreach (string key in keys)
			{
				if (_keyValueMap.ContainsKey(key))
				{
					dictionary[key] = _keyValueMap[key].Value;
				}
			}
			return dictionary;
		}

		public object Poll(string key)
		{
			return _keyValueMap.ContainsKey(key) ? _keyValueMap[key].Value : null;
		}

		public void Traverse(object instance, string keyPrefix)
		{
			Parallel.ForEach(((IEnumerable<FieldInfo>)instance.GetType().GetFields()).Where((Func<FieldInfo, bool>)_hasSubscriptionAttribute), delegate(FieldInfo fi)
			{
				string text2 = Parse(fi);
				text2 = (string.IsNullOrWhiteSpace(keyPrefix) ? text2 : $"{keyPrefix}.{text2}");
				Subscribe(text2, () => fi.GetValue(instance), SubscriptionAttribute.FLAG.SaveDB);
			});
			Parallel.ForEach(((IEnumerable<PropertyInfo>)instance.GetType().GetProperties()).Where((Func<PropertyInfo, bool>)_hasSubscriptionAttribute), delegate(PropertyInfo property)
			{
				string text = Parse(property);
				text = (string.IsNullOrWhiteSpace(keyPrefix) ? text : $"{keyPrefix}.{text}");
				Subscribe(text, () => property.GetValue(instance, null), SubscriptionAttribute.FLAG.SaveDB);
			});
		}

		private string Parse(MemberInfo member)
		{
			return _hasSubscriptionAttribute(member) ? (member.GetCustomAttributes(inherit: false).First(_isSubscriptionAttribute) as SubscriptionAttribute).Key : null;
		}

		public Dictionary<string, object> PollData(IEnumerable<string> keys)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			foreach (string key in keys)
			{
				if (_keyValueMap.ContainsKey(key))
				{
					dictionary[key] = _keyValueMap[key].Value;
				}
			}
			return dictionary;
		}
	}
}

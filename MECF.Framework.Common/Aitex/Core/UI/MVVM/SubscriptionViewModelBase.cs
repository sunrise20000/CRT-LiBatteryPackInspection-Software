using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.UI.MVVM
{
	public class SubscriptionViewModelBase : TimerViewModelBase
	{
		private const string BindingPathName = "ElementData";

		protected Func<IEnumerable<string>, Dictionary<string, object>> PollDataFunction;

		protected Action<string[]> InvokeFunction;

		protected Action<string, string> DeviceFunction;

		protected Action<object[]> DeviceControlFunction;

		protected Dictionary<string, Func<string, bool>> PreCommand = new Dictionary<string, Func<string, bool>>();

		private ConcurrentBag<string> _subscribedKeys = new ConcurrentBag<string>();

		private Func<object, bool> _isSubscriptionAttribute;

		private Func<MemberInfo, bool> _hasSubscriptionAttribute;

		private List<DelegateCommand<string>> _lstICommand = new List<DelegateCommand<string>>();

		private List<IViewModelControl> viewModelControls = new List<IViewModelControl>();

		public ICommand InvokeCommand { get; set; }

		public ICommand DeviceCommand { get; set; }

		public ICommand DeviceControlCommand { get; set; }

		public SubscriptionViewModelBase(string name)
			: base(name)
		{
			_isSubscriptionAttribute = (object attribute) => attribute is SubscriptionAttribute;
			_hasSubscriptionAttribute = (MemberInfo mi) => mi.GetCustomAttributes(inherit: false).Any(_isSubscriptionAttribute);
			InvokeCommand = new DelegateCommand<string>(delegate(string param)
			{
				PerformInvokeFunction(param.Split(','));
			}, (string functionName) => true);
			DeviceCommand = new DelegateCommand<string>(delegate(string operation)
			{
				string[] array = operation.Split(',');
				DeviceFunction(array[0], array[1]);
			}, (string functionName) => true);
			DeviceControlCommand = new DelegateCommand<object>(delegate(object args)
			{
				DeviceControlFunction((object[])args);
			}, (object functionName) => true);
			TraverseKeys();
		}

		public virtual bool PerformInvokeFunction(string[] param)
		{
			return true;
		}

		public static string UIKey(string param1, string param2)
		{
			return param1 + "." + param2;
		}

		public static string UIKey(string param1, string param2, string param3)
		{
			return param1 + "." + param2 + "." + param3;
		}

		public static string UIKey(string param1, string param2, string param3, string param4)
		{
			return param1 + "." + param2 + "." + param3 + "." + param4;
		}

		protected override void Poll()
		{
			if (PollDataFunction == null || _subscribedKeys.Count <= 0)
			{
				return;
			}
			Dictionary<string, object> result = PollDataFunction(_subscribedKeys);
			if (result == null)
			{
				LOG.Error("获取RT数据失败");
				return;
			}
			if (result.Count != _subscribedKeys.Count)
			{
				string text = string.Empty;
				foreach (string subscribedKey in _subscribedKeys)
				{
					if (!result.ContainsKey(subscribedKey))
					{
						text = text + subscribedKey + "\r\n";
					}
				}
			}
			InvokeBeforeUpdateProperty(result);
			UpdateValue(result);
			Application.Current.Dispatcher.Invoke(delegate
			{
				InvokePropertyChanged();
				foreach (IViewModelControl viewModelControl in viewModelControls)
				{
					viewModelControl.InvokePropertyChanged();
				}
				InvokeAfterUpdateProperty(result);
			});
		}

		protected virtual void InvokeBeforeUpdateProperty(Dictionary<string, object> data)
		{
		}

		protected virtual void InvokeAfterUpdateProperty(Dictionary<string, object> data)
		{
		}

		private void UpdateValue(Dictionary<string, object> data)
		{
			if (data == null)
			{
				return;
			}
			UpdateSubscribe(data, this);
			IEnumerable<PropertyInfo> enumerable = from p in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
				where p.GetCustomAttribute<SubscriptionModuleAttribute>() != null
				select p;
			foreach (PropertyInfo item in enumerable)
			{
				SubscriptionModuleAttribute customAttribute = item.GetCustomAttribute<SubscriptionModuleAttribute>();
				UpdateSubscribe(data, item.GetValue(this), customAttribute.Module);
			}
			foreach (IViewModelControl viewModelControl in viewModelControls)
			{
				UpdateSubscribe(data, viewModelControl);
			}
		}

		private void TraverseKeys()
		{
			SubscribeKeys(this);
		}

		private void Subscribe(Binding binding, string bindingPathName)
		{
			if (binding == null)
			{
				return;
			}
			string path = binding.Path.Path;
			if (path.Contains(bindingPathName) && path.Contains('[') && path.Contains(']'))
			{
				try
				{
					Subscribe(path.Substring(path.IndexOf('[') + 1, path.IndexOf(']') - path.IndexOf('[') - 1));
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
			}
		}

		protected void Subscribe(string key)
		{
			if (!string.IsNullOrEmpty(key))
			{
				_subscribedKeys.Add(key);
			}
		}

		protected void Subscribe(string key, object value)
		{
			if (!string.IsNullOrEmpty(key))
			{
				_subscribedKeys.Add(key);
			}
		}

		public void SubscribeKeys(IViewModelControl target)
		{
			Parallel.ForEach(target.GetType().GetProperties().Where(_hasSubscriptionAttribute), delegate(MemberInfo property)
			{
				SubscriptionAttribute subscriptionAttribute2 = property.GetCustomAttributes(inherit: false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
				string moduleKey2 = subscriptionAttribute2.ModuleKey;
				if (!_subscribedKeys.Contains(moduleKey2))
				{
					_subscribedKeys.Add(moduleKey2);
				}
			});
			Parallel.ForEach(target.GetType().GetFields().Where(_hasSubscriptionAttribute), delegate(MemberInfo method)
			{
				SubscriptionAttribute subscriptionAttribute = method.GetCustomAttributes(inherit: false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
				string moduleKey = subscriptionAttribute.ModuleKey;
				if (!_subscribedKeys.Contains(moduleKey))
				{
					_subscribedKeys.Add(moduleKey);
				}
			});
			IEnumerable<PropertyInfo> enumerable = from p in target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
				where p.GetCustomAttribute<SubscriptionModuleAttribute>() != null
				select p;
			foreach (PropertyInfo item in enumerable)
			{
				SubscriptionModuleAttribute customAttribute = item.GetCustomAttribute<SubscriptionModuleAttribute>();
				string module = customAttribute.Module;
				Type propertyType = item.PropertyType;
				IEnumerable<PropertyInfo> enumerable2 = from p in propertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
					where p.GetCustomAttribute<SubscriptionAttribute>() != null
					select p;
				foreach (PropertyInfo item2 in enumerable2)
				{
					SubscriptionAttribute customAttribute2 = item2.GetCustomAttribute<SubscriptionAttribute>();
					string text = $"{module}.{customAttribute2.ModuleKey}";
					if (!_subscribedKeys.Contains(text))
					{
						_subscribedKeys.Add(text);
					}
				}
			}
			if (target != this)
			{
				viewModelControls.Add(target);
			}
		}

		public void UpdateSubscribe(Dictionary<string, object> data, object target, string module = null)
		{
			Parallel.ForEach(target.GetType().GetProperties().Where(_hasSubscriptionAttribute), delegate(MemberInfo property)
			{
				PropertyInfo propertyInfo = (PropertyInfo)property;
				SubscriptionAttribute subscriptionAttribute2 = property.GetCustomAttributes(inherit: false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
				string moduleKey2 = subscriptionAttribute2.ModuleKey;
				moduleKey2 = ((module == null) ? moduleKey2 : $"{module}.{moduleKey2}");
				if (_subscribedKeys.Contains(moduleKey2) && data.ContainsKey(moduleKey2))
				{
					try
					{
						object obj = Convert.ChangeType(data[moduleKey2], propertyInfo.PropertyType);
						object obj2 = Convert.ChangeType(propertyInfo.GetValue(target, null), propertyInfo.PropertyType);
						if (obj2 != obj)
						{
							if (propertyInfo.Name == "PumpLimitSetPoint")
							{
								propertyInfo.SetValue(target, obj, null);
							}
							else
							{
								propertyInfo.SetValue(target, obj, null);
							}
						}
					}
					catch (Exception ex2)
					{
						LOG.Error("由RT返回的数据更新失败" + moduleKey2, ex2);
					}
				}
			});
			Parallel.ForEach(target.GetType().GetFields().Where(_hasSubscriptionAttribute), delegate(MemberInfo property)
			{
				FieldInfo fieldInfo = (FieldInfo)property;
				SubscriptionAttribute subscriptionAttribute = property.GetCustomAttributes(inherit: false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
				string moduleKey = subscriptionAttribute.ModuleKey;
				if (_subscribedKeys.Contains(moduleKey) && data.ContainsKey(moduleKey))
				{
					try
					{
						object value = Convert.ChangeType(data[moduleKey], fieldInfo.FieldType);
						fieldInfo.SetValue(target, value);
					}
					catch (Exception ex)
					{
						LOG.Error("由RT返回的数据更新失败" + moduleKey, ex);
					}
				}
			});
		}
	}
}

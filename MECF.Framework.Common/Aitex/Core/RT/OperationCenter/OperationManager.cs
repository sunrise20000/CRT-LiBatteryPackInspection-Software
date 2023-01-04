using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using MECF.Framework.Common.OperationCenter;

namespace Aitex.Core.RT.OperationCenter
{
	public class OperationManager : ICommonOperation, IOperation
	{
		private ConcurrentDictionary<string, Func<string, object[], bool>> _operationMap = new ConcurrentDictionary<string, Func<string, object[], bool>>();

		private ConcurrentDictionary<string, OperationFunction> _timeOperationMap = new ConcurrentDictionary<string, OperationFunction>();

		private Func<object, bool> _isSubscriptionAttribute;

		private Func<MemberInfo, bool> _hasSubscriptionAttribute;

		private Dictionary<string, List<IInterlockChecker>> _operationInterlockCheck = new Dictionary<string, List<IInterlockChecker>>();

		public event Func<string, object[], bool> OnDoOperation;

		public void Initialize(bool enableService)
		{
			_isSubscriptionAttribute = (object attribute) => attribute is SubscriptionAttribute;
			_hasSubscriptionAttribute = (MemberInfo mi) => mi.GetCustomAttributes(inherit: false).Any(_isSubscriptionAttribute);
			OP.InnerOperationManager = this;
			OP.OperationManager = this;
			if (enableService)
			{
				Singleton<WcfServiceManager>.Instance.Initialize(new Type[1] { typeof(InvokeService) });
			}
		}

		public void Initialize()
		{
			Initialize(enableService: true);
		}

		public void Subscribe<T>(T instance, string keyPrefix = null) where T : class
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}
			Traverse(instance, keyPrefix);
		}

		public void Subscribe(string key, OperationFunction op)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException("key");
			}
			if (_timeOperationMap.ContainsKey(key))
			{
				throw new Exception($"Duplicated Key:{key}");
			}
			if (op == null)
			{
				throw new ArgumentNullException("op");
			}
			_timeOperationMap[key] = op;
		}

		public void Subscribe(string key, Func<string, object[], bool> op)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException("key");
			}
			if (_operationMap.ContainsKey(key))
			{
				throw new Exception($"Duplicated Key:{key}");
			}
			if (op == null)
			{
				throw new ArgumentNullException("op");
			}
			_operationMap[key] = op;
		}

		public bool DoOperation(string operation, out string reason, int time, params object[] args)
		{
			if (!CanDoOperation(operation, out reason, args))
			{
				EV.PostWarningLog("OP", "Can not execute " + operation + ", " + reason);
				return false;
			}
			if (!_timeOperationMap[operation](out reason, time, args))
			{
				LOG.Error("Do operation " + operation + " failed, " + reason);
				return false;
			}
			return true;
		}

		public bool ContainsOperation(string operation)
		{
			return _timeOperationMap.ContainsKey(operation) || _operationMap.ContainsKey(operation);
		}

		public bool AddCheck(string operation, IInterlockChecker check)
		{
			if (!_operationInterlockCheck.ContainsKey(operation))
			{
				_operationInterlockCheck[operation] = new List<IInterlockChecker>();
			}
			_operationInterlockCheck[operation].Add(check);
			return true;
		}

		public bool CanDoOperation(string operation, out string reason, params object[] args)
		{
			if (!ContainsOperation(operation))
			{
				reason = "Call unregistered function, " + operation;
				return false;
			}
			if (_operationInterlockCheck.ContainsKey(operation))
			{
				foreach (IInterlockChecker item in _operationInterlockCheck[operation])
				{
					if (!item.CanDo(out reason, args))
					{
						return false;
					}
				}
			}
			reason = string.Empty;
			return true;
		}

		public bool DoOperation(string operation, params object[] args)
		{
			if (this.OnDoOperation != null && this.OnDoOperation(operation, args))
			{
				return true;
			}
			if (!ContainsOperation(operation))
			{
				throw new ApplicationException("调用了未发布的接口" + operation);
			}
			if (!CanDoOperation(operation, out var reason, args))
			{
				EV.PostWarningLog("OP", "Can not execute " + operation + ", " + reason);
				return false;
			}
			string text = "";
			if (args == null || args.Length == 0)
			{
				text = "()";
			}
			else
			{
				text += "(";
				foreach (object obj in args)
				{
					if (obj == null)
					{
						continue;
					}
					string text2 = "";
					if (obj.GetType() == typeof(Dictionary<string, object>))
					{
						text2 += "[";
						foreach (KeyValuePair<string, object> item in (Dictionary<string, object>)obj)
						{
							string text3 = item.Value.ToString();
							if (text3.Length > 10)
							{
								text3 = text3.Substring(0, 7) + "...";
							}
							text2 += item.Value.ToString();
							text2 += ", ";
						}
						if (text2.Length > 2)
						{
							text2 = text2.Remove(text2.Length - 2, 2);
						}
						text2 += "]";
						text += text2;
					}
					else
					{
						text += obj.ToString();
					}
					text += ", ";
				}
				if (text.Length > 2)
				{
					text = text.Remove(text.Length - 2, 2);
				}
				text += ")";
			}
			if (text.Length > 50)
			{
				text = text.Substring(0, 50) + "...";
				LOG.Write("long parameter: " + text);
			}
			EV.PostInfoLog("OP", "Invoke operation " + operation + text);
			string reason2;
			if (_operationMap.ContainsKey(operation))
			{
				_operationMap[operation](operation, args);
			}
			else if (!_timeOperationMap[operation](out reason2, 0, args))
			{
				EV.PostWarningLog("OP", reason2);
				return false;
			}
			return true;
		}

		public void Traverse(object instance, string keyPrefix)
		{
			Parallel.ForEach(((IEnumerable<MethodInfo>)instance.GetType().GetMethods()).Where((Func<MethodInfo, bool>)_hasSubscriptionAttribute), delegate(MethodInfo fi)
			{
				string text = Parse(fi);
				text = (string.IsNullOrWhiteSpace(keyPrefix) ? text : $"{keyPrefix}.{text}");
			});
		}

		private string Parse(MethodInfo member)
		{
			return _hasSubscriptionAttribute(member) ? (member.GetCustomAttributes(inherit: false).First(_isSubscriptionAttribute) as SubscriptionAttribute).Key : null;
		}
	}
}

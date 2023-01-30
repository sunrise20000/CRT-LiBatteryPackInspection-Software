using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Event;

namespace Aitex.Core.RT.Device
{
	public class BaseDevice : IAlarmHandler
	{
		public string UniqueName { get; set; }

		public string Module { get; set; }

		public string Name { get; set; }

		public string Display { get; set; }

		public string DeviceID { get; set; }

		public string ScBasePath { get; set; }

		public string IoBasePath { get; set; }

		private Dictionary<string, AlarmEventItem> AlarmList { get; set; }

		public bool HasAlarm => AlarmList != null && AlarmList.Values.FirstOrDefault((AlarmEventItem x) => !x.IsAcknowledged && x.Level == EventLevel.Alarm) != null;

		public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;

		public BaseDevice()
		{
			ScBasePath = ModuleName.System.ToString();
			IoBasePath = ModuleName.System.ToString();
			AlarmList = new Dictionary<string, AlarmEventItem>();
		}

		public BaseDevice(string module, string name, string display, string id)
			: this()
		{
			display = (string.IsNullOrEmpty(display) ? name : display);
			id = (string.IsNullOrEmpty(id) ? name : id);
			Module = module;
			Name = name;
			Display = display;
			DeviceID = id;
			UniqueName = module + "." + name;
		}

		public void AlarmStateChanged(AlarmEventItem args)
		{
			if (this.OnDeviceAlarmStateChanged != null)
			{
				this.OnDeviceAlarmStateChanged(UniqueName ?? "", args);
			}
		}

		protected AlarmEventItem SubscribeAlarm(string name, string description, Func<bool> resetChecker, EventLevel level = EventLevel.Alarm)
		{
			AlarmEventItem alarmEventItem = new AlarmEventItem(Module, name, description, resetChecker, this);
			alarmEventItem.Level = level;
			AlarmList[name] = alarmEventItem;
			EV.Subscribe(alarmEventItem);
			return alarmEventItem;
		}

		protected void ResetAlarm()
		{
			foreach (KeyValuePair<string, AlarmEventItem> alarm in AlarmList)
			{
				alarm.Value.Reset();
			}
		}

		public DOAccessor ParseDoNode(string name, XmlElement node, string ioModule = "")
		{
			if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
			{
				return IO.DO[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : (ioModule + "." + node.GetAttribute(name).Trim())];
			}
			return null;
		}

		public DIAccessor ParseDiNode(string name, XmlElement node, string ioModule = "")
		{
			if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
			{
				return IO.DI[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : (ioModule + "." + node.GetAttribute(name).Trim())];
			}
			return null;
		}

		public AOAccessor ParseAoNode(string name, XmlElement node, string ioModule = "")
		{
			if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
			{
				return IO.AO[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : (ioModule + "." + node.GetAttribute(name).Trim())];
			}
			return null;
		}

		public AIAccessor ParseAiNode(string name, XmlElement node, string ioModule = "")
		{
			if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
			{
				return IO.AI[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : (ioModule + "." + node.GetAttribute(name).Trim())];
			}
			return null;
		}

		public SCConfigItem ParseScNode(string name, XmlElement node, string ioModule = "", string defaultScPath = "")
		{
			SCConfigItem sCConfigItem = null;
			if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
			{
				sCConfigItem = SC.GetConfigItem(node.GetAttribute(name));
			}
			if (sCConfigItem == null && !string.IsNullOrEmpty(defaultScPath) && SC.ContainsItem(defaultScPath))
			{
				sCConfigItem = SC.GetConfigItem(defaultScPath);
			}
			return sCConfigItem;
		}

		public static T ParseDeviceNode<T>(string name, XmlElement node) where T : class, IDevice
		{
			if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
			{
				return DEVICE.GetDevice<T>(node.GetAttribute(name));
			}
			LOG.Write($"{node.InnerXml}，未定义{name}");
			return null;
		}

		public static T ParseDeviceNode<T>(string module, string name, XmlElement node) where T : class, IDevice
		{
			string attribute = node.GetAttribute(name);
			if (!string.IsNullOrEmpty(attribute) && !string.IsNullOrEmpty(attribute.Trim()))
			{
				return DEVICE.GetDevice<T>(module + "." + attribute);
			}
			LOG.Write($"{node.InnerXml}，未定义{name}");
			return null;
		}
	}
}

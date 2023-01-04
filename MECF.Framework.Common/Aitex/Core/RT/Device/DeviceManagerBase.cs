#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;

namespace Aitex.Core.RT.Device
{
	public class DeviceManagerBase : IDeviceManager
	{
		private Dictionary<string, IDevice> _nameDevice = new Dictionary<string, IDevice>();

		private Dictionary<Type, List<IDevice>> _typeDevice = new Dictionary<Type, List<IDevice>>();

		private DeviceTimer _peformanceTimer = new DeviceTimer();

		private R_TRIG _trigExceed500 = new R_TRIG();

		private List<IDevice> _optionDevice = new List<IDevice>();

		private object _lockerDevice = new object();

		protected XmlElement DeviceModelNodes { get; private set; }

		public bool DisableAsyncInitialize { get; set; }

		public DeviceManagerBase()
		{
			DEVICE.Manager = this;
		}

		public virtual void Terminate()
		{
			lock (_lockerDevice)
			{
				foreach (IDevice value in _nameDevice.Values)
				{
					value.Terminate();
				}
			}
		}

		public void Monitor()
		{
			lock (_lockerDevice)
			{
				foreach (IDevice value in _nameDevice.Values)
				{
					try
					{
						_peformanceTimer.Start(0.0);
						value.Monitor();
						_trigExceed500.CLK = _peformanceTimer.GetElapseTime() > 500.0;
						if (_trigExceed500.Q)
						{
							LOG.Warning($"{value.Module}.{value.Name} monitor time {_peformanceTimer.GetElapseTime()} ms");
						}
					}
					catch (Exception ex)
					{
						LOG.Write(ex, $"Monitor {value.Name} Exception");
					}
				}
			}
		}

		public void Reset()
		{
			lock (_lockerDevice)
			{
				foreach (IDevice value in _nameDevice.Values)
				{
					value.Reset();
				}
				_trigExceed500.RST = true;
			}
		}

		public void Initialize(string modelFile, string type, ModuleName mod = ModuleName.System, string ioModule = "", bool endCallInit = true)
		{
			if (!File.Exists(modelFile))
			{
				throw new ApplicationException($"did not find the device model file {modelFile} ");
			}
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.Load(modelFile);
				if (!(xmlDocument.SelectSingleNode("DeviceModelDefine") is XmlElement xmlElement))
				{
					throw new ApplicationException($"device mode file {modelFile} is not valid");
				}
				string attribute = xmlElement.GetAttribute("type");
				if (attribute != type)
				{
					throw new ApplicationException($"the type {attribute} in device mode file {modelFile} is inaccordance with the system type {type}");
				}
				DeviceModelNodes = xmlElement;
				foreach (XmlNode childNode in xmlElement.ChildNodes)
				{
					if (childNode.NodeType == XmlNodeType.Comment || !(childNode is XmlElement xmlElement2))
					{
						continue;
					}
					string text = xmlElement2.GetAttribute("assembly");
					if (string.IsNullOrEmpty(text))
					{
						text = "MECF.Framework.RT.EquipmentLibrary";
					}
					string text2 = xmlElement2.Name.Substring(0, xmlElement2.Name.Length - 1);
					string text3 = xmlElement2.GetAttribute("classType");
					if (string.IsNullOrEmpty(text3))
					{
						text3 = "Aitex.Core.RT.Device.Unit." + text2;
					}
					Assembly assembly = Assembly.Load(text);
					Type type2 = assembly.GetType(text3);
					if (type2 == null)
					{
						continue;
					}
					foreach (object childNode2 in xmlElement2.ChildNodes)
					{
						if (!(childNode2 is XmlElement xmlElement3))
						{
							LOG.Write("Device Model File contains non element node, " + (childNode2 as XmlNode).Value);
							continue;
						}
						IDevice device = Activator.CreateInstance(type2, mod.ToString(), xmlElement3, ioModule) as IDevice;
						if (xmlElement3.HasAttribute("option") && Convert.ToBoolean(xmlElement3.Attributes["option"].Value))
						{
							_optionDevice.Add(device);
							continue;
						}
						QueueDevice(device);
						DATA.Subscribe(device, $"{device.Module}.{text2}.{device.Name}");
						if (!_typeDevice.ContainsKey(type2))
						{
							_typeDevice[type2] = new List<IDevice>();
						}
						_typeDevice[type2].Add(device);
						if (DisableAsyncInitialize)
						{
							InitDevice(device);
							continue;
						}
						Task task = Task.Run(delegate
						{
							InitDevice(device);
						});
					}
				}
				if (endCallInit)
				{
					Initialize();
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		public void Initialize(string modelFile, string modelType, string moduleName, string ioPath, string scPath)
		{
			if (!File.Exists(modelFile))
			{
				throw new ApplicationException($"did not find the device model file {modelFile} ");
			}
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.Load(modelFile);
				if (!(xmlDocument.SelectSingleNode("DeviceModelDefine") is XmlElement xmlElement))
				{
					throw new ApplicationException($"device mode file {modelFile} is not valid");
				}
				string attribute = xmlElement.GetAttribute("type");
				if (attribute != modelType)
				{
					throw new ApplicationException($"the type {attribute} in device mode file {modelFile} is different with the system type {modelType}");
				}
				DeviceModelNodes = xmlElement;
				foreach (XmlNode childNode in xmlElement.ChildNodes)
				{
					if (childNode.NodeType == XmlNodeType.Comment || !(childNode is XmlElement xmlElement2))
					{
						continue;
					}
					string text = xmlElement2.GetAttribute("assembly");
					if (string.IsNullOrEmpty(text))
					{
						text = "MECF.Framework.RT.EquipmentLibrary";
					}
					string text2 = xmlElement2.Name.Substring(0, xmlElement2.Name.Length - 1);
					string text3 = xmlElement2.GetAttribute("classType");
					if (string.IsNullOrEmpty(text3))
					{
						text3 = "Aitex.Core.RT.Device.Unit." + text2;
					}
					Assembly assembly = Assembly.Load(text);
					Type type = assembly.GetType(text3);
					if (type == null)
					{
						continue;
					}
					foreach (object childNode2 in xmlElement2.ChildNodes)
					{
						if (!(childNode2 is XmlElement xmlElement3))
						{
							LOG.Write("Device Model File contains non element node, " + (childNode2 as XmlNode).Value);
							continue;
						}
						xmlElement3.SetAttribute("ioPath", ioPath);
						xmlElement3.SetAttribute("scPath", scPath);
						IDevice device = Activator.CreateInstance(type, moduleName, xmlElement3, ioPath) as IDevice;
						if (xmlElement3.HasAttribute("option") && Convert.ToBoolean(xmlElement3.Attributes["option"]))
						{
							_optionDevice.Add(device);
							continue;
						}
						DATA.Subscribe(device, $"{device.Module}.{text2}.{device.Name}");
						QueueDevice(device);
						if (!_typeDevice.ContainsKey(type))
						{
							_typeDevice[type] = new List<IDevice>();
						}
						_typeDevice[type].Add(device);
						if (DisableAsyncInitialize)
						{
							InitDevice(device);
							continue;
						}
						Task task = Task.Run(delegate
						{
							InitDevice(device);
						});
					}
				}
				Initialize();
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		private void InitDevice(IDevice device)
		{
			try
			{
				if (!device.Initialize())
				{
					LOG.Write(device.Name + " initialize failed.");
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		protected virtual void QueueDevice(IDevice device)
		{
			QueueDevice(device.Name, device);
		}

		protected void QueueDevice(string key, IDevice device)
		{
			lock (_lockerDevice)
			{
				Debug.Assert(!_nameDevice.ContainsKey(key), "DeviceModel config file contains duplicated name " + key);
				_nameDevice.Add(key, device);
			}
		}

		public IDevice AddCustomDevice(IDevice device, string groupType, Type deviceType)
		{
			lock (_lockerDevice)
			{
				DATA.Subscribe(device, $"{device.Module}.{groupType}.{device.Name}");
				if (!string.IsNullOrEmpty(device.Module) && device.Module != "System")
				{
					_nameDevice.Add(device.Module + "." + device.Name, device);
				}
				else
				{
					_nameDevice.Add(device.Name, device);
				}
				if (!_typeDevice.ContainsKey(deviceType))
				{
					_typeDevice[deviceType] = new List<IDevice>();
				}
				_typeDevice[deviceType].Add(device);
				device.Initialize();
			}
			return device;
		}

		public IDevice AddCustomDevice(IDevice device, Type deviceType)
		{
			lock (_lockerDevice)
			{
				try
				{
					DATA.Subscribe(device, $"{device.Module}.{device.Name}");
					_nameDevice.Add(device.Name, device);
					if (!_typeDevice.ContainsKey(deviceType))
					{
						_typeDevice[deviceType] = new List<IDevice>();
					}
					_typeDevice[deviceType].Add(device);
					device.Initialize();
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
			}
			return device;
		}

		public IDevice AddCustomDevice(IDevice device)
		{
			lock (_lockerDevice)
			{
				try
				{
					DATA.Subscribe(device, $"{device.Module}.{device.Name}");
					_nameDevice.Add(device.Name, device);
					if (!_typeDevice.ContainsKey(device.GetType()))
					{
						_typeDevice[device.GetType()] = new List<IDevice>();
					}
					_typeDevice[device.GetType()].Add(device);
					device.Initialize();
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
			}
			return device;
		}

		public IDevice AddCustomModuleDevice(IDevice device)
		{
			lock (_lockerDevice)
			{
				try
				{
					DATA.Subscribe(device, $"{device.Module}.{device.Name}");
					_nameDevice.Add(device.Module + "." + device.Name, device);
					if (!_typeDevice.ContainsKey(device.GetType()))
					{
						_typeDevice[device.GetType()] = new List<IDevice>();
					}
					_typeDevice[device.GetType()].Add(device);
					device.Initialize();
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
			}
			return device;
		}

		public virtual bool Initialize()
		{
			return true;
		}

		public T GetDevice<T>(string name) where T : class, IDevice
		{
			if (!_nameDevice.ContainsKey(name))
			{
				return null;
			}
			return _nameDevice[name] as T;
		}

		public object GetDevice(string name)
		{
			if (!_nameDevice.ContainsKey(name))
			{
				return null;
			}
			return _nameDevice[name];
		}

		public List<T> GetDevice<T>() where T : class, IDevice
		{
			if (!_typeDevice.ContainsKey(typeof(T)))
			{
				return null;
			}
			List<T> list = new List<T>();
			foreach (IDevice item in _typeDevice[typeof(T)])
			{
				list.Add(item as T);
			}
			return list;
		}

		public List<IDevice> GetAllDevice()
		{
			return _nameDevice.Values.ToList();
		}

		public object GetOptionDevice(string name, Type type)
		{
			foreach (IDevice item in _optionDevice)
			{
				if (item.Module + "." + item.Name == name && (type == null || item.GetType() == type))
				{
					return item;
				}
			}
			return null;
		}
	}
}

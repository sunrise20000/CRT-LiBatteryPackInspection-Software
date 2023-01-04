using System.Collections;
using System.Collections.Generic;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.Device
{
	public class DeviceFactory<T> : IEnumerable where T : class, IDevice
	{
		private object locker = new object();

		protected Dictionary<string, T> devices = null;

		public string Type { get; protected set; }

		public T this[string name]
		{
			get
			{
				T result = null;
				lock (locker)
				{
					if (devices.ContainsKey(name))
					{
						result = devices[name];
						return result;
					}
				}
				return result;
			}
			set
			{
				devices[name] = value;
			}
		}

		public IEnumerator GetEnumerator()
		{
			return devices.Values.GetEnumerator();
		}

		public List<T> GetAll()
		{
			return new List<T>(devices.Values);
		}

		public DeviceFactory()
		{
			devices = new Dictionary<string, T>();
		}

		public bool Initialize()
		{
			lock (locker)
			{
				Type = "Devices";
				Product();
				foreach (KeyValuePair<string, T> device in devices)
				{
					if (device.Value != null && !device.Value.Initialize())
					{
						LOG.Warning("Device {0} initialize false", 2, device.Key);
					}
				}
			}
			return true;
		}

		public void Terminate()
		{
			lock (locker)
			{
				foreach (T value in devices.Values)
				{
					value?.Terminate();
				}
			}
		}

		protected virtual void Product()
		{
		}
	}
}

using System;
using MECF.Framework.Common.Event;

namespace Aitex.Core.RT.Device
{
	public interface IDevice
	{
		string Module { get; set; }

		string Name { get; set; }

		bool HasAlarm { get; }

		event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;

		bool Initialize();

		void Monitor();

		void Terminate();

		void Reset();
	}
}

using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.OperationCenter;

namespace MECF.Framework.Common.Device.Bases
{
	public abstract class PumpBase : BaseDevice, IDevice
	{
		public virtual bool IsOn { get; set; }

		public virtual bool IsError { get; set; }

		public virtual bool IsStable { get; set; }

		public virtual bool IsOverTemperature { get; set; }

		public virtual float Speed { get; set; }

		public virtual float Temperature { get; set; }

		public virtual AITPumpData DeviceData { get; set; }

		public virtual AITPumpData DeviceDataMP { get; set; }

		public virtual AITPumpData DeviceDataBP { get; set; }

		protected PumpBase()
		{
		}

		protected PumpBase(string module, string name)
			: base(module, name, name, name)
		{
		}

		public virtual bool Initialize()
		{
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceData", () => DeviceData);
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceDataMP", () => DeviceDataMP);
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceDataBP", () => DeviceDataBP);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsOn", () => IsOn);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsError", () => IsError);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsOverTemperature", () => IsOverTemperature);
			DATA.Subscribe(base.Module + "." + base.Name + ".Speed", () => Speed);
			DATA.Subscribe(base.Module + "." + base.Name + ".Temperature", () => Temperature);
			OP.Subscribe(base.Module + "." + base.Name + ".SetPumpOn", delegate
			{
				SetPumpOnOff(isOn: true);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetPumpOff", delegate
			{
				SetPumpOnOff(isOn: false);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".PumpOn", delegate
			{
				SetPumpOnOff(isOn: true);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".PumpOff", delegate
			{
				SetPumpOnOff(isOn: false);
				return true;
			});
			return true;
		}

		public virtual void SetPumpOnOff(bool isOn)
		{
		}

		public virtual void Terminate()
		{
		}

		public virtual void Monitor()
		{
		}

		public virtual void Reset()
		{
		}
	}
}

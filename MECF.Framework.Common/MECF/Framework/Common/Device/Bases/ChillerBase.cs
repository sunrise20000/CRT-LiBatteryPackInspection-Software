using System;
using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.OperationCenter;
using MECF.Framework.Common.CommonData.DeviceData;

namespace MECF.Framework.Common.Device.Bases
{
	public abstract class ChillerBase : BaseDevice, IDevice
	{
		public virtual bool IsConnected => true;

		public virtual bool IsCH1Alarm { get; set; }

		public virtual bool IsCH2Alarm { get; set; }

		public virtual bool IsCH1Warning { get; set; }

		public virtual bool IsCH2Warning { get; set; }

		public virtual bool IsCH1On { get; set; }

		public virtual bool IsCH2On { get; set; }

		public virtual float CH1TemperatureSetpoint { get; set; }

		public virtual float CH1TemperatureFeedback { get; set; }

		public virtual float CH2TemperatureSetpoint { get; set; }

		public virtual float CH2TemperatureFeedback { get; set; }

		public virtual float CH1WaterFlow { get; set; }

		public virtual float CH2WaterFlow { get; set; }

		public virtual float TemperatureHighLimit { get; set; }

		public virtual float TemperatureLowLimit { get; set; }

		public virtual AITChillerData1 DeviceData { get; set; }

		protected ChillerBase()
		{
		}

		protected ChillerBase(string module, XmlElement node, string ioModule = "")
		{
			base.Module = (string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module"));
			base.Name = node.GetAttribute("id");
		}

		public virtual bool Initialize()
		{
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceData", () => DeviceData);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsCH1On", () => IsCH1On);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsCH2On", () => IsCH2On);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsCH1Alarm", () => IsCH1Alarm);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsCH2Alarm", () => IsCH2Alarm);
			DATA.Subscribe(base.Module + "." + base.Name + ".CH1TemperatureSetpoint", () => CH1TemperatureSetpoint);
			DATA.Subscribe(base.Module + "." + base.Name + ".CH2TemperatureSetpoint", () => CH2TemperatureSetpoint);
			DATA.Subscribe(base.Module + "." + base.Name + ".CH1TemperatureFeedback", () => CH1TemperatureFeedback);
			DATA.Subscribe(base.Module + "." + base.Name + ".CH2TemperatureFeedback", () => CH2TemperatureFeedback);
			DATA.Subscribe(base.Module + "." + base.Name + ".CH1WaterFlow", () => CH1WaterFlow);
			DATA.Subscribe(base.Module + "." + base.Name + ".CH2WaterFlow", () => CH2WaterFlow);
			OP.Subscribe(base.Module + "." + base.Name + ".SetChillerCH1On", delegate(string function, object[] args)
			{
				SetChillerCH1OnOff(Convert.ToBoolean(args[0]));
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetChillerCH2On", delegate(string function, object[] args)
			{
				SetChillerCH2OnOff(Convert.ToBoolean(args[0]));
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetChillerCH1Temperature", delegate(string function, object[] args)
			{
				SetChillerCH1Temperature(Convert.ToSingle(args[0]));
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetChillerCH2Temperature", delegate(string function, object[] args)
			{
				SetChillerCH2Temperature(Convert.ToSingle(args[0]));
				return true;
			});
			return true;
		}

		public virtual void SetChillerCH1OnOff(bool isOn)
		{
		}

		public virtual void SetChillerCH2OnOff(bool isOn)
		{
		}

		public virtual void SetChillerCH1Temperature(float temperature)
		{
		}

		public virtual void SetChillerCH2Temperature(float temperature)
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

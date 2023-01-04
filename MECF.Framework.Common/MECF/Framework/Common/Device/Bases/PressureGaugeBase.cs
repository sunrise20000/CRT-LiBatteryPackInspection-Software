using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;

namespace MECF.Framework.Common.Device.Bases
{
	public abstract class PressureGaugeBase : BaseDevice, IDevice
	{
		public string Unit { get; set; }

		public virtual float SetPoint { get; set; }

		public virtual float FeedBack { get; set; }

		public virtual string FormatString { get; set; }

		public virtual AITPressureMeterData DeviceData => new AITPressureMeterData
		{
			Module = base.Module,
			DeviceName = base.Name,
			DeviceSchematicId = base.DeviceID,
			DisplayName = base.Display,
			FeedBack = FeedBack,
			FormatString = FormatString
		};

		public PressureGaugeBase()
		{
		}

		public PressureGaugeBase(string module, XmlElement node, string ioModule = "")
		{
			Unit = node.GetAttribute("unit");
			base.Module = (string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module"));
			base.Name = node.GetAttribute("id");
			base.Display = node.GetAttribute("display");
			base.DeviceID = node.GetAttribute("schematicId");
		}

		public virtual bool Initialize()
		{
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceData", () => DeviceData);
			DATA.Subscribe(base.Module + "." + base.Name + ".FeedBack", () => FeedBack);
			return true;
		}

		public virtual void Monitor()
		{
		}

		public virtual void Reset()
		{
		}

		public virtual void Terminate()
		{
		}
	}
}

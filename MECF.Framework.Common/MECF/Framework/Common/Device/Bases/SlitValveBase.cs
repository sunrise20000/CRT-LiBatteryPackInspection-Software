using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;

namespace MECF.Framework.Common.Device.Bases
{
	public abstract class SlitValveBase : BaseDevice, IDevice
	{
		public virtual bool OpenFeedback { get; set; }

		public virtual bool CloseFeedback { get; set; }

		public virtual bool OpenSetPoint { get; set; }

		public virtual bool CloseSetPoint { get; set; }

		public virtual AITCylinderData DeviceData { get; set; }

		protected SlitValveBase()
		{
		}

		protected SlitValveBase(string module, string name)
			: base(module, name, name, name)
		{
		}

		public virtual bool Initialize()
		{
			DATA.Subscribe(base.Module + "." + base.Name + ".OpenFeedback", () => OpenFeedback);
			DATA.Subscribe(base.Module + "." + base.Name + ".OpenSetPoint", () => OpenSetPoint);
			DATA.Subscribe(base.Module + "." + base.Name + ".CloseFeedback", () => CloseFeedback);
			DATA.Subscribe(base.Module + "." + base.Name + ".CloseSetPoint", () => CloseSetPoint);
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceData", () => DeviceData);
			DEVICE.Register(base.Module + "." + base.Name + ".Open", delegate(out string reason, int time, object[] param)
			{
				reason = "";
				return SetSlitValve(open: true, out reason);
			});
			DEVICE.Register(base.Module + "." + base.Name + ".Close", delegate(out string reason, int time, object[] param)
			{
				reason = "";
				return SetSlitValve(open: false, out reason);
			});
			return true;
		}

		public virtual void Monitor()
		{
		}

		public virtual void Terminate()
		{
		}

		public virtual bool CheckIsClose()
		{
			return CloseFeedback && !OpenFeedback;
		}

		public virtual bool CheckIsOpen()
		{
			return !CloseFeedback && OpenFeedback;
		}

		public virtual bool SetSlitValve(bool open, out string reason)
		{
			reason = "";
			return true;
		}

		public virtual void Reset()
		{
		}
	}
}

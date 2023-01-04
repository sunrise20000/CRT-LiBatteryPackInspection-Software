using Aitex.Core.RT.Device;
using Aitex.Core.RT.OperationCenter;

namespace MECF.Framework.Common.Device.Bases
{
	public abstract class SMIFBase : BaseDevice, IDevice
	{
		public virtual bool IsIdle { get; set; }

		public virtual bool IsHomed { get; set; }

		public virtual bool IsPodPresent { get; set; }

		public virtual bool IsArmRetract { get; set; }

		public virtual bool IsAlarm { get; set; }

		public virtual bool IsConnected { get; set; }

		protected SMIFBase()
		{
		}

		protected SMIFBase(string module, string name)
			: base(module, name, name, name)
		{
		}

		public virtual bool Initialize()
		{
			OP.Subscribe(base.Module + "." + base.Name + ".Home", delegate
			{
				HomeSmif(out var _);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".Abort", delegate
			{
				Stop();
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".Reset", delegate
			{
				Reset();
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".Unload", delegate
			{
				UnloadCassette(out var _);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".Load", delegate
			{
				LoadCassette(out var _);
				return true;
			});
			return true;
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

		public virtual bool HomeSmif(out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public virtual bool LoadCassette(out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public virtual bool UnloadCassette(out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public virtual void Stop()
		{
		}
	}
}

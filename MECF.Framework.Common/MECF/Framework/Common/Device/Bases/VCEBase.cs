using Aitex.Core.RT.Device;
using Aitex.Core.RT.OperationCenter;

namespace MECF.Framework.Common.Device.Bases
{
	public abstract class VCEBase : BaseDevice, IDevice
	{
		public virtual bool IsIdle { get; set; }

		public virtual bool IsHomed { get; set; }

		public virtual bool IsDoorOpened { get; set; }

		public virtual bool IsDoorClosed { get; set; }

		public virtual bool IsAlarm { get; set; }

		public virtual bool IsConnected { get; set; }

		public virtual bool IsMapped { get; set; }

		protected VCEBase()
		{
		}

		protected VCEBase(string module, string name)
			: base(module, name, name, name)
		{
		}

		public virtual bool Initialize()
		{
			OP.Subscribe(base.Module + "." + base.Name + ".Home", delegate
			{
				Home(out var _);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".Abort", delegate
			{
				Abort();
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".Reset", delegate
			{
				Reset();
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".Unload", delegate
			{
				Unload(out var _);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".Load", delegate
			{
				Load(out var _);
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

		public virtual bool Home(out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public virtual bool Load(out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public virtual bool Unload(out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public virtual bool OpenDoor(out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public virtual bool CloseDoor(out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public virtual bool MapCassette(out string reason)
		{
			reason = string.Empty;
			return true;
		}

		public virtual void Abort()
		{
		}
	}
}

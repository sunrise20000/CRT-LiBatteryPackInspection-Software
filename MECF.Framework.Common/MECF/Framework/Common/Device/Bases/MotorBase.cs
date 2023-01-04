using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.OperationCenter;

namespace MECF.Framework.Common.Device.Bases
{
	public abstract class MotorBase : BaseDevice, IDevice
	{
		public virtual bool IsServoOn { get; set; }

		public virtual bool IsError { get; set; }

		public virtual bool IsHomed { get; set; }

		public virtual bool IsMoving { get; set; }

		public virtual bool IsInTargetPosition { get; set; }

		public virtual float CurrentPosition { get; set; }

		public virtual float Target { get; set; }

		public virtual AITServoMotorData DeviceData { get; set; }

		protected MotorBase()
		{
		}

		protected MotorBase(string module, string name)
			: base(module, name, name, name)
		{
		}

		public virtual bool Initialize()
		{
			DATA.Subscribe(base.Module + "." + base.Name + ".DeviceData", () => DeviceData);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsServoOn", () => IsServoOn);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsError", () => IsError);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsHomed", () => IsHomed);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsMoving", () => IsMoving);
			DATA.Subscribe(base.Module + "." + base.Name + ".IsInTargetPosition", () => IsInTargetPosition);
			DATA.Subscribe(base.Module + "." + base.Name + ".CurrentPosition", () => CurrentPosition);
			OP.Subscribe(base.Module + "." + base.Name + ".Home", delegate
			{
				Home();
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".ServoOn", delegate
			{
				ServoOn();
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetSpeed", delegate(string function, object[] args)
			{
				SetSpeed((float)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".SetPosition", delegate(string function, object[] args)
			{
				SetPosition((float)args[0]);
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".StopMotion", delegate
			{
				StopMotion();
				return true;
			});
			OP.Subscribe(base.Module + "." + base.Name + ".ResetError", delegate
			{
				ResetError();
				return true;
			});
			return true;
		}

		public virtual void Home()
		{
		}

		public virtual void ServoOn()
		{
		}

		public virtual void SetSpeed(float speed)
		{
		}

		public virtual void SetPosition(float position)
		{
		}

		public virtual void StopMotion()
		{
		}

		public virtual void ResetError()
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

using System.Threading.Tasks;
using Aitex.Core.Util;
using MECF.Framework.RT.Core.IoProviders;

namespace Aitex.Core.RT.IOCore
{
	public class DOAccessor : IOAccessor<bool>
	{
		public DOAccessor(string name, int index, bool[] values)
			: base(name, index, values)
		{
			setter = SetValueSafe;
		}

		public bool SetValue(bool value, out string reason)
		{
			if (IO.CanSetDO(name, value, out reason))
			{
				IIoProvider provider = Singleton<IoProviderManager>.Instance.GetProvider(base.Provider);
				if (provider != null && !provider.SetValue(this, value))
				{
					reason = "Write DO[" + base.Name + "] failed";
					return false;
				}
				values[index] = value;
				return true;
			}
			return false;
		}

		public async Task<bool> SetPulseValue(bool value, int milliSecondsDelay, bool holdValue = false)
		{
			if (IO.CanSetDO(name, value, out var reason))
			{
				values[index] = value;
				await Task.Delay(milliSecondsDelay);
				if (IO.CanSetDO(name, holdValue, out reason))
				{
					values[index] = holdValue;
					return true;
				}
				return false;
			}
			return false;
		}

		public bool Check(bool value, out string reason)
		{
			return IO.CanSetDO(name, value, out reason);
		}

		private void SetValueSafe(int index, bool value)
		{
			if (IO.CanSetDO(name, value, out var _))
			{
				values[index] = value;
				Singleton<IoProviderManager>.Instance.GetProvider(base.Provider)?.SetValue(this, value);
			}
		}
	}
}

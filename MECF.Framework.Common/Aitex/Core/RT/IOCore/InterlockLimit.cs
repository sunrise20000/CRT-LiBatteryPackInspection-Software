using System.Collections.Generic;
using Aitex.Core.Util;

namespace Aitex.Core.RT.IOCore
{
	public abstract class InterlockLimit
	{
		private string _name;

		private bool _limitValue;

		private string _tip;

		private Dictionary<string, string> _cultureTip = new Dictionary<string, string>();

		private R_TRIG _trigger = new R_TRIG();

		public string Name => _name;

		public abstract bool CurrentValue { get; }

		public abstract string LimitReason { get; }

		public bool LimitValue => _limitValue;

		public string Tip => _tip;

		public InterlockLimit(string name, bool value, string tip, Dictionary<string, string> cultureTip)
		{
			_name = name;
			_limitValue = value;
			_tip = tip;
			_cultureTip = cultureTip;
		}

		public bool IsSame(string name, bool value)
		{
			return name == _name && _limitValue == value;
		}

		public bool IsSame(InterlockLimit limit)
		{
			return limit.Name == _name && _limitValue == limit.LimitValue;
		}

		public bool IsTriggered()
		{
			_trigger.CLK = CurrentValue != _limitValue;
			return _trigger.Q;
		}

		public bool CanDo(out string reason)
		{
			reason = string.Empty;
			if (CurrentValue == _limitValue)
			{
				return true;
			}
			reason = LimitReason;
			return false;
		}
	}
}

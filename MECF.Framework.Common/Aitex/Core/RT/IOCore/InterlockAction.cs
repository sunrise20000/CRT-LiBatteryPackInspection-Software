using System.Collections.Generic;

namespace Aitex.Core.RT.IOCore
{
	internal class InterlockAction
	{
		private List<InterlockLimit> _limits = new List<InterlockLimit>();

		private DOAccessor _do;

		private bool _actionValue;

		private string _tip;

		private Dictionary<string, string> _cultureTip = new Dictionary<string, string>();

		public string ActionName => (_do != null) ? _do.Name : "";

		public InterlockAction(DOAccessor doItem, bool value, string tip, Dictionary<string, string> cultureTip, List<InterlockLimit> limits)
		{
			_do = doItem;
			_actionValue = value;
			_tip = tip;
			_cultureTip = cultureTip;
			_limits = limits;
		}

		public bool IsSame(string doName, bool value)
		{
			return doName == _do.Name && _actionValue == value;
		}

		public bool Reverse(out string reason)
		{
			reason = string.Empty;
			if (_do.Value != _actionValue)
			{
				return false;
			}
			if (_do.SetValue(!_actionValue, out reason))
			{
				reason = string.Format("Force setting DO-{0}({1}) = [{2}]", _do.IoTableIndex, _do.Name, (!_actionValue) ? "ON" : "OFF");
			}
			return true;
		}

		public bool CanDo(out string reason)
		{
			reason = string.Empty;
			bool result = true;
			foreach (InterlockLimit limit in _limits)
			{
				if (!limit.CanDo(out var reason2))
				{
					if (reason.Length > 0)
					{
						reason += "\n";
					}
					else
					{
						reason = string.Format("Interlock triggered, DO-{0}({1}) can not be [{2}], {3} \n", _do.IoTableIndex, _do.Name, _actionValue ? "ON" : "OFF", _tip);
					}
					reason += reason2;
					result = false;
				}
			}
			return result;
		}
	}
}

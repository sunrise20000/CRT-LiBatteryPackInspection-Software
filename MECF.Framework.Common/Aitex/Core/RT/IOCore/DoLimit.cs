using System.Collections.Generic;

namespace Aitex.Core.RT.IOCore
{
	internal class DoLimit : InterlockLimit
	{
		private DOAccessor _do;

		public override bool CurrentValue => _do.Value;

		public override string LimitReason => string.Format("DO-{0}({1}) = [{2}],{3}", _do.IoTableIndex, _do.Name, _do.Value ? "ON" : "OFF", base.Tip);

		public DoLimit(DOAccessor doItem, bool value, string tip, Dictionary<string, string> cultureTip)
			: base(doItem.Name, value, tip, cultureTip)
		{
			_do = doItem;
		}
	}
}

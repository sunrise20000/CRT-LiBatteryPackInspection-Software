using System.Collections.Generic;

namespace Aitex.Core.RT.IOCore
{
	internal class DiLimit : InterlockLimit
	{
		private DIAccessor _di;

		public override bool CurrentValue => _di.Value;

		public override string LimitReason => string.Format("DI-{0}({1}) = [{2}],{3}", _di.IoTableIndex, _di.Name, _di.Value ? "ON" : "OFF", base.Tip);

		public DiLimit(DIAccessor diItem, bool value, string tip, Dictionary<string, string> cultureTip)
			: base(diItem.Name, value, tip, cultureTip)
		{
			_di = diItem;
		}
	}
}

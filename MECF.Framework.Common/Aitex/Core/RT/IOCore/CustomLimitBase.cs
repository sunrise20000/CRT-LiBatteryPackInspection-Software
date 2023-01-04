using System.Collections.Generic;

namespace Aitex.Core.RT.IOCore
{
	public class CustomLimitBase : InterlockLimit
	{
		public override bool CurrentValue { get; }

		public override string LimitReason { get; }

		public CustomLimitBase(string name, bool limitValue, string tip, Dictionary<string, string> cultureTip)
			: base(name, limitValue, tip, cultureTip)
		{
		}
	}
}

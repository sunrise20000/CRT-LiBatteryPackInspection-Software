using System;

namespace Aitex.Core.Util
{
	[AttributeUsage(AttributeTargets.Property)]
	public class SubscriptionModuleAttribute : Attribute
	{
		public string Module { get; set; }

		public SubscriptionModuleAttribute(string module)
		{
			Module = module;
		}
	}
}

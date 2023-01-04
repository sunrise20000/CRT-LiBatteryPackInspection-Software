using System;

namespace Aitex.Core.Utilities
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class IgnorePropertyChangeAttribute : Attribute
	{
	}
}

using System;

namespace Aitex.Core.Utilities
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class TagAttribute : Attribute
	{
		public readonly string Tag;

		public TagAttribute(string tag)
		{
			Tag = tag;
		}
	}
}

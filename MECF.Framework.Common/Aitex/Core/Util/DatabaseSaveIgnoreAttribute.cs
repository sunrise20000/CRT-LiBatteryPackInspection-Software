using System;

namespace Aitex.Core.Util
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class DatabaseSaveIgnoreAttribute : Attribute
	{
	}
}

using System;
using System.ServiceModel;

namespace Aitex.Core.Account
{
	public sealed class NotificationService
	{
		public static string ClientHostName => null;

		public static string ClientUserName
		{
			get
			{
				if (OperationContext.Current == null || OperationContext.Current.IncomingMessageHeaders == null)
				{
					return "";
				}
				int num = OperationContext.Current.IncomingMessageHeaders.FindHeader("IdentityUserName", "ns");
				if (num == -1)
				{
					return "";
				}
				return OperationContext.Current.IncomingMessageHeaders.GetHeader<string>("IdentityUserName", "ns");
			}
		}

		public static Guid ClientGuid => Guid.Empty;
	}
}

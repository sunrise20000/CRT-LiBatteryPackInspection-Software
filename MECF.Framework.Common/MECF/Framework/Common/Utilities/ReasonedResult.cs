namespace MECF.Framework.Common.Utilities
{
	public class ReasonedResult
	{
		public bool Result;

		public string Reason;

		public ReasonedResult(bool result, string reason)
		{
			Result = result;
			Reason = reason;
		}
	}
}

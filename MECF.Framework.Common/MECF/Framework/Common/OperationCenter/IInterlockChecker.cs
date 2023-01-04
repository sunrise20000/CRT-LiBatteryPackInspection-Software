namespace MECF.Framework.Common.OperationCenter
{
	public interface IInterlockChecker
	{
		bool CanDo(out string reason, object[] args);
	}
}

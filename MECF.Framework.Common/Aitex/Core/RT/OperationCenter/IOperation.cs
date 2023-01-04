namespace Aitex.Core.RT.OperationCenter
{
	public interface IOperation
	{
		void Subscribe(string key, OperationFunction op);

		bool DoOperation(string operationName, out string reason, int time, params object[] args);
	}
}

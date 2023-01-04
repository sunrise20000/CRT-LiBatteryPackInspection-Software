namespace Aitex.Core.RT.OperationCenter
{
	public delegate bool OperationFunction(out string reason, int time, params object[] param);
}

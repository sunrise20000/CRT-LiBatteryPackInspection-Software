namespace Aitex.Core.RT.Fsm
{
	public interface IModuleEntity : IEntity
	{
		bool IsInit { get; }

		bool IsBusy { get; }

		bool IsIdle { get; }

		bool IsError { get; }

		bool IsOnline { get; set; }

		int Invoke(string function, params object[] args);

		bool CheckAcked(int msg);
	}
}

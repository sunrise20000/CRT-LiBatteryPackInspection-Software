namespace Aitex.Core.RT.Fsm
{
	public interface IEntity
	{
		bool Initialize();

		void Terminate();

		void PostMsg<T>(T msg, params object[] args) where T : struct;

		bool Check(int msg, out string reason, params object[] args);
	}
}

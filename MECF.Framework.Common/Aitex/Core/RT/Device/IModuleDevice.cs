namespace Aitex.Core.RT.Device
{
	public interface IModuleDevice
	{
		bool IsReady { get; }

		bool IsError { get; }

		bool IsInit { get; }

		bool Home(out string reason);
	}
}

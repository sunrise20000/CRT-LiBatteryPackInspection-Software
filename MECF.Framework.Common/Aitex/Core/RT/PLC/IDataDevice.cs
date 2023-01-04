using System.Collections.Generic;

namespace Aitex.Core.RT.PLC
{
	public interface IDataDevice
	{
		bool IsOpened { get; }

		bool Open();

		void Close();

		object Read<T>(string type);

		bool Write<T>(string type, T buffer);

		List<string> GetTypes();

		bool IsOpen(string type);

		bool Open(string type);

		void Close(string type);
	}
}

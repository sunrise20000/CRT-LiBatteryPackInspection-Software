using System;

namespace Aitex.Core.RT.Routine
{
	public class RoutineFaildException : ApplicationException
	{
		public RoutineFaildException()
		{
		}

		public RoutineFaildException(string message)
			: base(message)
		{
		}
	}
}

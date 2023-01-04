using System;

namespace Aitex.Core.RT.Routine
{
	public class RoutineBreakException : ApplicationException
	{
		public RoutineBreakException()
		{
		}

		public RoutineBreakException(string message)
			: base(message)
		{
		}
	}
}

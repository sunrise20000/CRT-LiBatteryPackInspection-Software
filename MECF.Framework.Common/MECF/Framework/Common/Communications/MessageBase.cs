namespace MECF.Framework.Common.Communications
{
	public abstract class MessageBase
	{
		public bool IsResponse { get; set; }

		public bool IsAck { get; set; }

		public bool IsError { get; set; }

		public bool IsEvent { get; set; }

		public bool IsComplete { get; set; }

		public bool IsFormatError { get; set; }

		public bool IsBusy { get; set; }

		public bool IsNak { get; set; }

		public int MutexTag { get; set; }
	}
}

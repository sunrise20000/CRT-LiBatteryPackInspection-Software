namespace MECF.Framework.Common.Communications
{
	public class AsciiMessage : MessageBase
	{
		public string RawMessage { get; set; }

		public string[] MessagePart { get; set; }
	}
}

using System.Collections.Generic;

namespace Aitex.Core.Util
{
	public class ModuleStatusBackground
	{
		public const string Init = "Yellow";

		public const string Error = "OrangeRed";

		public const string Busy = "LawnGreen";

		public const string Idle = "White";

		public const string Offline = "Gray";

		public const string UnInstalled = "Gray";

		private static Dictionary<string, string> _dicCustom = new Dictionary<string, string>();

		public static string GetStatusBackground(string status)
		{
			if (status != null)
			{
				status = status.Trim().ToLower();
			}
			switch (status)
			{
			case "error":
				return "OrangeRed";
			case "processidle":
			case "vacidle":
			case "idle":
			case "manual":
				return _dicCustom.ContainsKey("White") ? _dicCustom["White"] : "White";
			case "notconnect":
			case "init":
				return "Yellow";
			case "offline":
				return "Gray";
			case "notinstall":
				return "Gray";
			default:
				return "LawnGreen";
			}
		}

		public static void CustomColor(string type, string color)
		{
			_dicCustom[type] = color;
		}
	}
}

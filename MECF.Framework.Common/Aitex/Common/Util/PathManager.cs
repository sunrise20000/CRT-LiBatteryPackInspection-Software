using System.Diagnostics;
using System.IO;

namespace Aitex.Common.Util
{
	public class PathManager
	{
		private static string _appPath;

		public static string GetAppDir()
		{
			return (!string.IsNullOrWhiteSpace(_appPath)) ? _appPath : (_appPath = GetAppStartupDirectory());
		}

		public static string GetCfgDir()
		{
			return GetDirectory("Config");
		}

		public static string GetLogDir()
		{
			return GetDirectory("Logs");
		}

		public static string GetRecipeDir()
		{
			return GetDirectory("Recipes");
		}

		public static string GetAccountFilePath()
		{
			return GetDirectory("Account");
		}

		private static string GetAppStartupDirectory()
		{
			string fileName = Process.GetCurrentProcess().MainModule.FileName;
			return Path.GetDirectoryName(fileName);
		}

		public static string GetDirectory(string directoryPath)
		{
			string text = Path.Combine(GetAppDir(), directoryPath);
			if (!text.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				text += Path.DirectorySeparatorChar;
			}
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			return text;
		}
	}
}

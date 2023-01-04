using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MECF.Framework.Common.Utilities
{
	public class FileAssociation
	{
		[DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

		public static void SetAssociation(string Extension, string KeyName, string OpenWith, string FileDescription)
		{
			RegistryKey registryKey = Registry.ClassesRoot.CreateSubKey(Extension);
			registryKey.SetValue("", KeyName);
			RegistryKey registryKey2 = Registry.ClassesRoot.CreateSubKey(KeyName);
			registryKey2.SetValue("", FileDescription);
			registryKey2.CreateSubKey("DefaultIcon").SetValue("", "\"" + OpenWith + "\",0");
			RegistryKey registryKey3 = registryKey2.CreateSubKey("Shell");
			registryKey3.CreateSubKey("edit").CreateSubKey("command").SetValue("", "\"" + OpenWith + "\" \"%1\"");
			registryKey3.CreateSubKey("open").CreateSubKey("command").SetValue("", "\"" + OpenWith + "\" \"%1\"");
			registryKey.Close();
			registryKey2.Close();
			registryKey3.Close();
			RegistryKey registryKey4 = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\" + Extension, writable: true);
			registryKey4.DeleteSubKey("UserChoice", throwOnMissingSubKey: false);
			registryKey4.Close();
			SHChangeNotify(134217728u, 0u, IntPtr.Zero, IntPtr.Zero);
		}
	}
}

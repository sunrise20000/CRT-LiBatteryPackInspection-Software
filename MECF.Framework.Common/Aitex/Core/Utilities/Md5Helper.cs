using System;
using System.Security.Cryptography;
using System.Text;
using Aitex.Core.RT.Log;

namespace Aitex.Core.Utilities
{
	public class Md5Helper
	{
		public static bool VerifyMd5Hash(string input, string hash)
		{
			string md5Hash = GetMd5Hash(input);
			StringComparer ordinalIgnoreCase = StringComparer.OrdinalIgnoreCase;
			if (ordinalIgnoreCase.Compare(md5Hash, hash) == 0)
			{
				return true;
			}
			return false;
		}

		public static string GetMd5Hash(string input)
		{
			MD5 mD = MD5.Create();
			byte[] array = mD.ComputeHash(Encoding.Default.GetBytes(input));
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < array.Length; i++)
			{
				stringBuilder.Append(array[i].ToString("x2"));
			}
			return stringBuilder.ToString();
		}

		public static string GenerateDynamicPassword(string serialNum)
		{
			try
			{
				string input = "promaxy" + serialNum + DateTime.Now.ToString("yyyyMMdd");
				string md5Hash = GetMd5Hash(input);
				return md5Hash.Substring(0, 8);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return "";
			}
		}
	}
}

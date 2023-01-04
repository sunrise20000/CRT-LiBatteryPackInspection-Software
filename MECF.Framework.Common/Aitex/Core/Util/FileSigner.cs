#define DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Aitex.Core.RT.Log;

namespace Aitex.Core.Util
{
	public static class FileSigner
	{
		public static bool IsValid(string fileName)
		{
			bool result = false;
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(fileName);
				Debug.Assert(xmlDocument != null && xmlDocument.DocumentElement != null);
				XmlElement documentElement = xmlDocument.DocumentElement;
				XmlElement xmlElement = documentElement["Signature"];
				if (xmlElement == null)
				{
					return false;
				}
				documentElement.RemoveChild(xmlElement);
				UnicodeEncoding unicodeEncoding = new UnicodeEncoding();
				SHA1CryptoServiceProvider sHA1CryptoServiceProvider = new SHA1CryptoServiceProvider();
				byte[] inArray = sHA1CryptoServiceProvider.ComputeHash(unicodeEncoding.GetBytes(documentElement.InnerXml));
				string text = Convert.ToBase64String(inArray);
				documentElement.AppendChild(xmlElement);
				if (xmlElement.InnerText == text)
				{
					result = true;
				}
			}
			catch (Exception ex)
			{
				result = false;
				LOG.Write(ex);
				throw;
			}
			finally
			{
			}
			return result;
		}

		public static void Sign(string fileName)
		{
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				bool flag = true;
				if (File.Exists(fileName) && (File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
				{
					flag = false;
					File.SetAttributes(fileName, FileAttributes.Normal);
				}
				xmlDocument.Load(fileName);
				XmlElement documentElement = xmlDocument.DocumentElement;
				XmlElement xmlElement = documentElement["Signature"];
				if (xmlElement != null)
				{
					documentElement.RemoveChild(xmlElement);
				}
				UnicodeEncoding unicodeEncoding = new UnicodeEncoding();
				SHA1CryptoServiceProvider sHA1CryptoServiceProvider = new SHA1CryptoServiceProvider();
				byte[] inArray = sHA1CryptoServiceProvider.ComputeHash(unicodeEncoding.GetBytes(documentElement.InnerXml));
				string innerText = Convert.ToBase64String(inArray);
				xmlElement = xmlDocument.CreateElement("Signature");
				xmlElement.InnerText = innerText;
				documentElement.AppendChild(xmlElement);
				xmlDocument.Save(fileName);
				if (!flag)
				{
					File.SetAttributes(fileName, FileAttributes.ReadOnly);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				throw;
			}
			finally
			{
			}
		}
	}
}

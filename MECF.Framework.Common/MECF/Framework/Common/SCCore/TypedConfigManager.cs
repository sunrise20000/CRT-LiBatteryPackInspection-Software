using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Aitex.Common.Util;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace MECF.Framework.Common.SCCore
{
	public class TypedConfigManager : Singleton<TypedConfigManager>
	{
		public void Initialize(Dictionary<string, string> typedXmlFile)
		{
			string text = $"{Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\\config\\DataHistoryTypedConfig.default.xml";
			string text2 = $"{Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\\config\\DataHistoryTypedConfig.xml";
			if (!File.Exists(text) && !File.Exists(text2))
			{
				throw new ApplicationException("没有找到DataHistoryTypedConfig配置文件.\r\n" + text + "\r\n或者" + text2);
			}
			if (File.Exists(text))
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(text);
				try
				{
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
			}
			if (File.Exists(text2))
			{
				XmlDocument xmlDocument2 = new XmlDocument();
				xmlDocument2.Load(text2);
				try
				{
				}
				catch (Exception ex2)
				{
					LOG.Write(ex2);
				}
			}
		}

		public string GetTypedConfigContent(string type, string path)
		{
			if (type != "UserDefine")
			{
				return GetCustomConfigContent(type);
			}
			string text = $"{Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\\config\\DataHistoryTypedConfig.xml";
			if (!File.Exists(text))
			{
				return null;
			}
			XDocument xDocument = XDocument.Load(text);
			XElement xElement = xDocument.Root.Elements().SingleOrDefault((XElement x) => x.Attribute("Type").Value == type);
			if (xElement == null)
			{
				return null;
			}
			List<string> list = new List<string>();
			foreach (XElement item in xElement.Elements().ToList())
			{
				list.Add(item.Attribute("Name").Value);
			}
			return string.Join(",", list.ToArray());
		}

		public void SetTypedConfigContent(string type, string path, string content)
		{
			if (type != "UserDefine")
			{
				SetCustomConfigContent(type, content);
				return;
			}
			string text = $"{Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\\config\\DataHistoryTypedConfig.xml";
			XmlDocument xmlDocument = new XmlDocument();
			if (File.Exists(text))
			{
				xmlDocument.Load(text);
			}
			else
			{
				xmlDocument.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\" ?><TypedConfig></TypedConfig>");
			}
			XmlNode xmlNode = xmlDocument.SelectSingleNode($"TypedConfig");
			if (xmlNode == null)
			{
				xmlNode = xmlDocument.CreateElement("TypedConfig");
				xmlDocument.AppendChild(xmlNode);
			}
			XmlElement xmlElement = xmlDocument.SelectSingleNode($"TypedConfig/Configs[@Type='{type}']") as XmlElement;
			if (xmlElement == null)
			{
				xmlElement = xmlDocument.CreateElement("Configs");
				xmlElement.SetAttribute("Type", type);
				xmlNode.AppendChild(xmlElement);
			}
			xmlElement.RemoveAll();
			xmlElement.SetAttribute("Type", type);
			string[] array = content.Split(',');
			string[] array2 = array;
			foreach (string value in array2)
			{
				if (!string.IsNullOrEmpty(value))
				{
					XmlElement xmlElement2 = xmlDocument.CreateElement("Config");
					xmlElement2.SetAttribute("Name", value);
					xmlElement.AppendChild(xmlElement2);
				}
			}
			xmlDocument.Save(text);
		}

		private bool IsXmlFileLoadable(string file)
		{
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(file);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return false;
			}
			return true;
		}

		public void InitCustomConfigFile(string type)
		{
			string text = PathManager.GetCfgDir() + type + ".xml";
			string text2 = PathManager.GetCfgDir() + "_" + type + ".xml";
			try
			{
				if (File.Exists(text2))
				{
					return;
				}
				if (File.Exists(text))
				{
					File.Copy(text, text2, overwrite: true);
					return;
				}
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\" ?><" + type + "Config></" + type + "Config>");
				using FileStream fileStream = new FileStream(text2, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 1024, FileOptions.WriteThrough);
				fileStream.SetLength(0L);
				XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
				xmlWriterSettings.Indent = true;
				xmlWriterSettings.OmitXmlDeclaration = false;
				using XmlWriter w = XmlWriter.Create(fileStream, xmlWriterSettings);
				xmlDocument.Save(w);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		public string GetCustomConfigContent(string type)
		{
			InitCustomConfigFile(type);
			string path = PathManager.GetCfgDir() + "_" + type + ".xml";
			if (!File.Exists(path))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			try
			{
				using StreamReader streamReader = new StreamReader(path);
				while (!streamReader.EndOfStream)
				{
					stringBuilder.Append(streamReader.ReadLine());
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return "";
			}
			return stringBuilder.ToString();
		}

		public void SetCustomConfigContent(string type, string content)
		{
			try
			{
				string text = PathManager.GetCfgDir() + "_" + type + ".xml";
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(content);
				using (FileStream fileStream = new FileStream(text, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 1024, FileOptions.WriteThrough))
				{
					fileStream.SetLength(0L);
					XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
					xmlWriterSettings.Indent = true;
					xmlWriterSettings.OmitXmlDeclaration = false;
					using XmlWriter w = XmlWriter.Create(fileStream, xmlWriterSettings);
					xmlDocument.Save(w);
				}
				LOG.Write(" " + text + " updated");
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}
	}
}

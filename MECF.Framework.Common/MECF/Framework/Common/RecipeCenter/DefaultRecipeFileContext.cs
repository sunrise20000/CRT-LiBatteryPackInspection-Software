using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.RecipeCenter;

namespace MECF.Framework.Common.RecipeCenter
{
	public class DefaultRecipeFileContext : IRecipeFileContext
	{
		public string GetRecipeDefiniton(string chamberType)
		{
			try
			{
				string filename = PathManager.GetCfgDir() + "\\Recipe\\" + chamberType + "\\RecipeFormat.xml";
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(filename);
				return xmlDocument.OuterXml;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return "";
			}
		}

		public IEnumerable<string> GetRecipes(string path, bool includingUsedRecipe)
		{
			try
			{
				List<string> list = new List<string>();
				string path2 = PathManager.GetRecipeDir() + path + "\\";
				if (!Directory.Exists(path2))
				{
					return list;
				}
				DirectoryInfo directoryInfo = new DirectoryInfo(path2);
				FileInfo[] files = directoryInfo.GetFiles("*.rcp", SearchOption.AllDirectories);
				FileInfo[] array = files;
				foreach (FileInfo fileInfo in array)
				{
					string text = fileInfo.FullName.Substring(directoryInfo.FullName.Length);
					text = text.Substring(0, text.LastIndexOf('.'));
					if (includingUsedRecipe || (!includingUsedRecipe && !text.Contains("HistoryRecipe\\")))
					{
						list.Add(text);
					}
				}
				return list;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return new List<string>();
			}
		}

		public void PostInfoEvent(string message)
		{
			EV.PostMessage("System", EventEnum.GeneralInfo, message);
		}

		public void PostWarningEvent(string message)
		{
			EV.PostMessage("System", EventEnum.DefaultWarning, message);
		}

		public void PostAlarmEvent(string message)
		{
			EV.PostMessage("System", EventEnum.DefaultAlarm, message);
		}

		public void PostDialogEvent(string message)
		{
			EV.PostNotificationMessage(message);
		}

		public void PostInfoDialogMessage(string message)
		{
			EV.PostMessage("System", EventEnum.GeneralInfo, message);
			EV.PostPopDialogMessage(EventLevel.Information, "System Information", message);
		}

		public void PostWarningDialogMessage(string message)
		{
			EV.PostMessage("System", EventEnum.GeneralInfo, message);
			EV.PostPopDialogMessage(EventLevel.Warning, "System Warning", message);
		}

		public void PostAlarmDialogMessage(string message)
		{
			EV.PostMessage("System", EventEnum.GeneralInfo, message);
			EV.PostPopDialogMessage(EventLevel.Alarm, "System Alarm", message);
		}

		public string GetRecipeTemplate(string chamberId)
		{
			string recipeDefiniton = GetRecipeDefiniton(chamberId);
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(recipeDefiniton);
			XmlNode xmlNode = xmlDocument.SelectSingleNode("/Aitex/TableRecipeData");
			return xmlNode.OuterXml;
		}
	}
}

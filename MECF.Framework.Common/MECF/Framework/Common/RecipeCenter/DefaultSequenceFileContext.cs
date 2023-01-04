using System;
using System.Xml;
using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;

namespace MECF.Framework.Common.RecipeCenter
{
	public class DefaultSequenceFileContext : ISequenceFileContext
	{
		public string GetConfigXml()
		{
			try
			{
				string filename = PathManager.GetCfgDir() + "\\SequenceFormat.xml";
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(filename);
				CustomSequenceItem(xmlDocument);
				return xmlDocument.OuterXml;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return "";
			}
		}

		public virtual bool Validation(string content)
		{
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(content);
				CustomValidation(xmlDocument);
				return CustomValidation(xmlDocument);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				EV.PostWarningLog("Recipe", "sequence file not valid, " + ex.Message);
				return false;
			}
		}

		public virtual void CustomSequenceItem(XmlDocument xmlContent)
		{
		}

		public virtual bool CustomValidation(XmlDocument xmlContent)
		{
			return true;
		}
	}
}

using System.IO;
using System.Xml.Linq;

namespace MECF.Framework.Common.Account.Extends
{
	public abstract class XmlLoader
	{
		protected string m_strPath;

		protected XDocument m_xdoc;

		protected XmlLoader(string p_strPath)
		{
			m_strPath = p_strPath;
		}

		protected virtual void AnalyzeXml()
		{
		}

		public void Load()
		{
			if (File.Exists(m_strPath))
			{
				m_xdoc = XDocument.Load(m_strPath);
				AnalyzeXml();
				return;
			}
			throw new FileNotFoundException("File " + m_strPath + " not be found");
		}
	}
}

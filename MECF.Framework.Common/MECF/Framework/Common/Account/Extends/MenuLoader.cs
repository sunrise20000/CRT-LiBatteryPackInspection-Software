using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MECF.Framework.Common.Account.Extends
{
	public class MenuLoader : XmlLoader
	{
		private List<AppMenu> _Menulist = new List<AppMenu>();

		public List<AppMenu> MenuList
		{
			get
			{
				return _Menulist;
			}
			set
			{
				_Menulist = value;
			}
		}

		public MenuLoader(string p_strPath)
			: base(p_strPath)
		{
		}

		protected override void AnalyzeXml()
		{
			if (m_xdoc != null)
			{
				List<AppMenu> list = (_Menulist = TranslateMenus(m_xdoc.Root));
			}
		}

		private List<AppMenu> TranslateMenus(XElement pElement)
		{
			List<AppMenu> list = new List<AppMenu>();
			IEnumerable<XElement> enumerable = from r in pElement.Elements("menuItem")
				select (r);
			foreach (XElement item in enumerable)
			{
				string empty = string.Empty;
				string empty2 = string.Empty;
				string empty3 = string.Empty;
				if (!item.HasElements)
				{
					empty = item.Attribute("id").Value;
					empty2 = item.Attribute("viewmodel").Value;
					empty3 = item.Attribute("resKey").Value;
					AppMenu appMenu = new AppMenu(empty, empty2, empty3, null);
					if (item.Attribute("System") != null)
					{
						appMenu.System = item.Attribute("System").Value;
					}
					if (item.Attribute("AlarmModule") != null)
					{
						appMenu.AlarmModule = item.Attribute("AlarmModule").Value;
					}
					list.Add(appMenu);
					continue;
				}
				empty = item.Attribute("id").Value;
				empty3 = item.Attribute("resKey").Value;
				List<AppMenu> list2 = new List<AppMenu>();
				list2 = TranslateMenus(item);
				AppMenu appMenu2 = new AppMenu(empty, empty2, empty3, list2);
				if (item.Attribute("System") != null)
				{
					appMenu2.System = item.Attribute("System").Value;
				}
				if (item.Attribute("AlarmModule") != null)
				{
					appMenu2.AlarmModule = item.Attribute("AlarmModule").Value;
				}
				list.Add(appMenu2);
			}
			return list;
		}
	}
}

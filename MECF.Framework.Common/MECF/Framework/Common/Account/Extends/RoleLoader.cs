using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FabConnect.SecsGemInterface.Application.Objects.ProcessManagement;

namespace MECF.Framework.Common.Account.Extends
{
	public class RoleLoader : XmlLoader
	{
		private List<Role> m_rolelist;

		private List<AccountEx> m_accountlist;

		public List<Role> RoleList
		{
			get
			{
				return m_rolelist;
			}
			set
			{
				m_rolelist = value;
			}
		}

		public List<AccountEx> AccountList
		{
			get
			{
				return m_accountlist;
			}
			set
			{
				m_accountlist = value;
			}
		}

		public RoleLoader(string p_strPath)
			: base(p_strPath)
		{
		}

		public List<Role> GetRoles()
		{
			return m_rolelist.Where((Role e) => !e.IsSuper).ToList();
		}

		public List<AccountEx> GetAccounts()
		{
			return m_accountlist.Where((AccountEx e) => !e.IsSuper).ToList();
		}

		protected override void AnalyzeXml()
		{
			if (m_xdoc == null)
			{
				return;
			}
			IEnumerable<XElement> enumerable = from r in m_xdoc.Descendants("roleItem")
				select (r);
			List<Role> list = new List<Role>();
			bool flag = false;
			foreach (XElement item5 in enumerable)
			{
				string value = item5.Attribute("id").Value;
				string value2 = item5.Attribute("name").Value;
				string value3 = item5.Attribute("autologout").Value;
				string value4 = item5.Attribute("logouttime").Value;
				int.TryParse(value4, out var result);
				flag = ((value3 == "1") ? true : false);
				string p_strDescription = "";
				if (item5.Attribute("description") != null)
				{
					p_strDescription = item5.Attribute("description").Value;
				}
				string value5 = item5.Value;
				Role item = new Role(value, value2, flag, result, value5, p_strDescription);
				list.Add(item);
			}
			Role item2 = new Role("-1", "Administrators", p_bAutoLogout: true, 20, null, null)
			{
				IsSuper = true
			};
			list.Add(item2);
			m_rolelist = list;
			enumerable = from r in m_xdoc.Descendants("userItem")
				select (r);
			List<AccountEx> list2 = new List<AccountEx>();
			foreach (XElement item6 in enumerable)
			{
				List<string> list3 = new List<string>();
				string value6 = item6.Attribute("id").Value;
				string value7 = item6.Attribute("loginname").Value;
				string p_strPassword = Decrypt(item6.Attribute("password").Value);
				string value8 = item6.Attribute("firstname").Value;
				string value9 = item6.Attribute("lastname").Value;
				string value10 = item6.Attribute("email").Value;
				string p_strDescription2 = "";
				if (item6.Attribute("description") != null)
				{
					p_strDescription2 = item6.Attribute("description").Value;
				}
				IEnumerable<XElement> enumerable2 = from ro in item6.Descendants("role")
					select (ro);
				foreach (XElement item7 in enumerable2)
				{
					string value11 = item7.Attribute("id").Value;
					list3.Add(value11);
				}
				AccountEx item3 = new AccountEx(value6, value7, p_strPassword, value8, value9, value10, list3, p_strDescription2);
				list2.Add(item3);
			}
			AccountEx item4 = new AccountEx("-1", "admin", "admin", "", "", "", new List<string> { "-1" })
			{
				IsSuper = true
			};
			list2.Add(item4);
			m_accountlist = list2;
		}

		public bool UpdateRole(Role p_newRole)
		{
			Role role = m_rolelist.Find((Role item) => item.RoleID == p_newRole.RoleID);
			if (role == null)
			{
				m_rolelist.Add(p_newRole);
			}
			else
			{
				m_rolelist[m_rolelist.IndexOf(role)] = p_newRole;
			}
			XDocument xdoc = m_xdoc;
			List<XElement> list = (from m_xRole in xdoc.Descendants("roleItem")
				where m_xRole.Attribute("id").Value == p_newRole.RoleID
				select m_xRole).ToList();
			if (list.Count > 0)
			{
				list[0].Attribute("name").Value = p_newRole.RoleName;
				list[0].Attribute("autologout").Value = (p_newRole.IsAutoLogout ? "1" : "0");
				list[0].Attribute("logouttime").Value = p_newRole.LogoutTime.ToString();
				if (list[0].Attribute("description") != null)
				{
					list[0].Attribute("description").Value = p_newRole.Description.ToString();
				}
				list[0].Value = p_newRole.MenuPermission;
			}
			else
			{
				XElement content = new XElement("roleItem", new XAttribute("id", p_newRole.RoleID), new XAttribute("name", p_newRole.RoleName), new XAttribute("autologout", p_newRole.IsAutoLogout ? "1" : "0"), new XAttribute("logouttime", p_newRole.LogoutTime), new XAttribute("description", p_newRole.Description))
				{
					Value = p_newRole.MenuPermission
				};
				xdoc.Root.Element("roles").Add(content);
			}
			xdoc.Save(m_strPath);
			return true;
		}

		public bool DeleteRole(string p_strRoleID)
		{
			Load();
			Role m_role = m_rolelist.Find((Role item) => item.RoleID == p_strRoleID);
			if (m_role != null)
			{
				m_rolelist.Remove(m_role);
				XDocument xdoc = m_xdoc;
				List<XElement> list = (from m_xRole in xdoc.Descendants("roleItem")
					where m_xRole.Attribute("id").Value == p_strRoleID
					select m_xRole).ToList();
				if (list.Count > 0)
				{
					list[0].Remove();
					foreach (AccountEx item in m_accountlist)
					{
						if (item.RoleIDs.Contains(m_role.RoleID))
						{
							item.RoleIDs.Remove(m_role.RoleID);
						}
					}
					list = (from m_xRole in xdoc.Descendants("role")
						where m_xRole.Attribute("id").Value == m_role.RoleID
						select m_xRole).ToList();
					if (list.Count > 0)
					{
						list.Remove();
					}
					xdoc.Save(m_strPath);
					return true;
				}
				return false;
			}
			return false;
		}

		private List<string> GetRolePermission(string roleid)
		{
			List<string> result = new List<string>();
			foreach (Role item in m_rolelist)
			{
				if (item.RoleID == roleid)
				{
					result = item.MenuPermission.Split(';').ToList();
					break;
				}
			}
			return result;
		}

		private int GetMenuPermission(List<string> rolePermissions, string menuid)
		{
			foreach (string rolePermission in rolePermissions)
			{
				if (rolePermission.IndexOf(menuid) >= 0)
				{
					string[] array = rolePermission.Split(',');
					if (array.Length > 1 && array[0].Trim() == menuid)
					{
						return int.Parse(array[1].Trim());
					}
				}
			}
			return 3;
		}

		public List<AppMenu> GetMenusByRole(string roleid, List<AppMenu> menulist)
		{
			List<AppMenu> list = new List<AppMenu>();
			List<string> rolePermission = GetRolePermission(roleid);
			foreach (AppMenu item in menulist)
			{
				List<AppMenu> list2 = new List<AppMenu>();
				foreach (AppMenu menuItem in item.MenuItems)
				{
					AppMenu appMenu = new AppMenu(menuItem.MenuID, menuItem.ViewModel, menuItem.ResKey, null);
					appMenu.System = menuItem.System;
					appMenu.AlarmModule = menuItem.AlarmModule;
					appMenu.Permission = GetMenuPermission(rolePermission, menuItem.MenuID);
					if (appMenu.Permission > 1)
					{
						list2.Add(appMenu);
					}
				}
				if (list2.Count > 0)
				{
					AppMenu appMenu2 = new AppMenu(item.MenuID, item.ViewModel, item.ResKey, list2);
					appMenu2.System = item.System;
					appMenu2.AlarmModule = item.AlarmModule;
					list.Add(appMenu2);
				}
			}
			return list;
		}

		public int GetMenuPermission(string roleid, string menuName)
		{
			List<string> rolePermission = GetRolePermission(roleid);
			return GetMenuPermission(rolePermission, menuName);
		}

		public bool UpdateAccount(AccountEx p_newAccount)
		{
			AccountEx accountEx = m_accountlist.Find((AccountEx item) => item.UserID == p_newAccount.UserID);
			if (accountEx == null)
			{
				m_accountlist.Add(p_newAccount);
			}
			else
			{
				m_accountlist[m_accountlist.IndexOf(accountEx)] = p_newAccount;
			}
			XDocument xdoc = m_xdoc;
			List<XElement> list = (from xAccount in xdoc.Descendants("userItem")
				where xAccount.Attribute("id").Value == p_newAccount.UserID
				select xAccount).ToList();
			if (list.Count > 0)
			{
				list[0].SetAttributeValue("loginname", p_newAccount.LoginName);
				list[0].SetAttributeValue("password", Encrypt(p_newAccount.Password));
				list[0].SetAttributeValue("firstname", p_newAccount.FirstName);
				list[0].SetAttributeValue("lastname", p_newAccount.LastName);
				list[0].SetAttributeValue("email", p_newAccount.Email);
				list[0].SetAttributeValue("description", p_newAccount.Description);

                list[0].Element("rolegroup").RemoveAll();
				foreach (string roleID in p_newAccount.RoleIDs)
				{
					list[0].Element("rolegroup").Add(new XElement("role", new XAttribute("id", roleID)));
				}
			}
			else
			{
				XElement xElement = new XElement("userItem", new XAttribute("id", p_newAccount.UserID), new XAttribute("loginname", p_newAccount.LoginName), new XAttribute("password", Encrypt(p_newAccount.Password)), new XAttribute("firstname", p_newAccount.FirstName), new XAttribute("lastname", p_newAccount.LastName), new XAttribute("email", p_newAccount.Email), new XAttribute("description", p_newAccount.Description), new XElement("rolegroup"));
				foreach (string roleID2 in p_newAccount.RoleIDs)
				{
					xElement.Element("rolegroup").Add(new XElement("role", new XAttribute("id", roleID2)));
				}
				xdoc.Root.Element("users").Add(xElement);
			}
			xdoc.Save(m_strPath);
			return true;
		}

		public bool DeleteAccount(string p_strUserID)
		{
			AccountEx accountEx = m_accountlist.Find((AccountEx item) => item.UserID == p_strUserID);
			if (accountEx != null)
			{
				m_accountlist.Remove(accountEx);
				XDocument xdoc = m_xdoc;
				List<XElement> list = (from xAccount in xdoc.Descendants("userItem")
					where xAccount.Attribute("id").Value == p_strUserID
					select xAccount).ToList();
				if (list.Count > 0)
				{
					list[0].Remove();
					xdoc.Save(m_strPath);
					return true;
				}
				return false;
			}
			return false;
		}

		public string Encrypt(string encrytStr)
		{
			if (string.IsNullOrWhiteSpace(encrytStr))
			{
				return string.Empty;
			}
			try
			{
				byte[] bytes = Encoding.UTF8.GetBytes(encrytStr);
				return Convert.ToBase64String(bytes);
			}
			catch
			{
				return encrytStr;
			}
		}

		public string Decrypt(string decryptStr)
		{
			if (string.IsNullOrWhiteSpace(decryptStr))
			{
				return string.Empty;
			}
			try
			{
				byte[] bytes = Convert.FromBase64String(decryptStr);
				return Encoding.UTF8.GetString(bytes);
			}
			catch
			{
				return decryptStr;
			}
		}
	}
}

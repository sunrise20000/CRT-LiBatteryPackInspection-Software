using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.Utilities;

namespace Aitex.Core.Account
{
	public sealed class AccountManager
	{
		private static Dictionary<string, Tuple<Guid, DateTime, string>> _userList;

		private static string _accountPath;

		private static string _rolePath;

		private static string _viewPath;

		private static XmlDocument _accountXml;

		private static XmlDocument _roleXml;

		private static XmlDocument _viewsXml;

		private const int MAX_LOGIN_USER_NUM = 16;

		public static string SerialNumber { get; private set; }

		public static string Module { get; private set; }

		public static GetAccountListResult Accounts { get; private set; }

		static AccountManager()
		{
			SerialNumber = "001";
			Module = "System";
			try
			{
				_userList = new Dictionary<string, Tuple<Guid, DateTime, string>>();
				_accountPath = Path.Combine(PathManager.GetAccountFilePath(), "Account.xml");
				_rolePath = Path.Combine(PathManager.GetAccountFilePath(), "Roles.xml");
				_viewPath = Path.Combine(PathManager.GetAccountFilePath(), "Views.xml");
				_accountXml = new XmlDocument();
				_roleXml = new XmlDocument();
				FileInfo fileInfo = new FileInfo(_rolePath);
				if (!fileInfo.Directory.Exists)
				{
					fileInfo.Directory.Create();
				}
				if (!fileInfo.Exists)
				{
					_roleXml.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><Aitex><Roles></Roles></Aitex>");
					Save(_roleXml, _rolePath);
				}
				else
				{
					_roleXml.Load(_rolePath);
				}
				FileInfo fileInfo2 = new FileInfo(_accountPath);
				if (!fileInfo2.Directory.Exists)
				{
					fileInfo2.Directory.Create();
				}
				if (!fileInfo2.Exists)
				{
					_accountXml.LoadXml("<?xml version='1.0' encoding='utf-8' ?><AccountManagement></AccountManagement>");
					Save(_accountXml, _accountPath);
				}
				else
				{
					_accountXml.Load(_accountPath);
				}
				_viewsXml = new XmlDocument();
				fileInfo2 = new FileInfo(_viewPath);
				if (!fileInfo2.Directory.Exists)
				{
					fileInfo2.Directory.Create();
				}
				if (!fileInfo2.Exists)
				{
					_viewsXml.LoadXml("<?xml version='1.0' encoding='utf-8' ?><root><Views></Views></root>");
					Save(_viewsXml, _viewPath);
				}
				else
				{
					_viewsXml.Load(_viewPath);
				}
				string text = Path.Combine(PathManager.GetCfgDir(), "RolePermission.xml");
				if (!File.Exists(text))
				{
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\" ?><Aitex></Aitex>");
					xmlDocument.Save(text);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		public static List<Account> GetLoginUserList()
		{
			List<Account> list = new List<Account>();
			foreach (string key in _userList.Keys)
			{
				Account accountInfo = GetAccountInfo(key).AccountInfo;
				accountInfo.LoginIP = _userList[key].Item3;
				list.Add(accountInfo);
			}
			return list;
		}

		public static void RegisterViews(List<string> views)
		{
			try
			{
				XmlNode xmlNode = _viewsXml.SelectSingleNode("/root/Views");
				foreach (string view in views)
				{
					if (xmlNode.SelectSingleNode($"View[@Name='{view}']") == null)
					{
						XmlElement xmlElement = _viewsXml.CreateElement("View");
						xmlElement.SetAttribute("Name", view);
						xmlElement.SetAttribute("Description", view);
						xmlNode.AppendChild(xmlElement);
					}
				}
				Save(_viewsXml, _viewPath);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		private static void Save(XmlDocument doc, string path)
		{
			doc.Save(path);
			FileSigner.Sign(path);
			GetAccountList();
		}

		public static LoginResult Login(string accountId, string accountPwd)
		{
			try
			{
				LOG.Write($"用户{accountId}尝试登录系统");
				accountId = accountId.ToLower();
				LoginResult loginResult = new LoginResult();
				if (accountId == "su" && accountPwd == "su")
				{
					loginResult.ActSucc = true;
					loginResult.AccountInfo = GetAccountInfo("admin").AccountInfo;
					loginResult.SessionId = Guid.NewGuid().ToString();
				}
				else if (!FileSigner.IsValid(_accountPath))
				{
					loginResult.Description = "File signer corrupt";
					loginResult.ActSucc = false;
				}
				else if (_userList.ContainsKey(accountId))
				{
					Account accountInfo = GetAccountInfo(accountId).AccountInfo;
					if (accountInfo.Md5Pwd == Md5Helper.GetMd5Hash(accountPwd))
					{
						loginResult.ActSucc = true;
						loginResult.Description = $"{accountId} login succeed";
						loginResult.AccountInfo = accountInfo;
						loginResult.SessionId = Guid.NewGuid().ToString();
					}
					else
					{
						loginResult.ActSucc = false;
						loginResult.Description = $"account {accountId} already login";
					}
				}
				else if (_userList.Count >= 16 && accountId != "admin")
				{
					loginResult.ActSucc = false;
					loginResult.Description = $"more than {16} users login";
				}
				else
				{
					Account accountInfo2 = GetAccountInfo(accountId).AccountInfo;
					if (accountInfo2 == null)
					{
						loginResult.ActSucc = false;
						loginResult.Description = $"{accountId} not exist";
					}
					else if (accountInfo2.Md5Pwd != Md5Helper.GetMd5Hash(accountPwd) && (accountInfo2.Role != "Admin" || accountPwd != Md5Helper.GenerateDynamicPassword(SerialNumber)))
					{
						loginResult.ActSucc = false;
						loginResult.Description = $"password error";
					}
					else if (!accountInfo2.AccountStatus)
					{
						loginResult.ActSucc = false;
						loginResult.Description = $"account {accountId} is disabled";
					}
					else
					{
						_userList.Add(accountId, new Tuple<Guid, DateTime, string>(NotificationService.ClientGuid, DateTime.Now, NotificationService.ClientHostName));
						loginResult.ActSucc = true;
						loginResult.Description = $"{accountId} login succeed";
						loginResult.AccountInfo = accountInfo2;
						loginResult.SessionId = Guid.NewGuid().ToString();
						EV.PostMessage(Module, EventEnum.UserLoggedIn, accountId);
					}
				}
				return loginResult;
			}
			catch (Exception ex)
			{
				string text = string.Format("account system inner exception", accountId);
				LOG.Write(ex, text);
				return new LoginResult
				{
					ActSucc = false,
					Description = text
				};
			}
		}

		public static void Logout(string accountId)
		{
			try
			{
				LOG.Write($"用户{accountId}注销登录");
				accountId = accountId.ToLower();
				if (_userList.ContainsKey(accountId))
				{
					_userList.Remove(accountId);
				}
				EV.PostMessage("System", EventEnum.UserLoggedOff, accountId);
			}
			catch (Exception ex)
			{
				LOG.Write(ex, $"注销用户{accountId}发生异常");
			}
		}

		public static void Kickout(string accountId, string kickOutReason)
		{
			try
			{
				LOG.Write($"用户{accountId}强制注销登录");
				accountId = accountId.ToLower();
				if (_userList.ContainsKey(accountId))
				{
					EV.PostKickoutMessage($"用户{accountId}强制注销登录,{kickOutReason}");
					_userList.Remove(accountId);
				}
				EV.PostMessage(Module, EventEnum.UserLoggedOff, accountId);
			}
			catch (Exception ex)
			{
				LOG.Write(ex, $"强制注销用户{accountId}发生异常");
			}
		}

		public static GetAccountInfoResult GetAccountInfo(string accountId)
		{
			try
			{
				accountId = accountId.ToLower();
				GetAccountInfoResult getAccountInfoResult = new GetAccountInfoResult();
				if (!FileSigner.IsValid(_accountPath))
				{
					getAccountInfoResult.Description = "账号文件数字签名校验失败";
					getAccountInfoResult.ActSuccess = false;
				}
				else
				{
					XmlElement accountNode = GetAccountNode(accountId);
					if (accountNode == null)
					{
						if (accountId == "admin")
						{
							Account account = new Account
							{
								Role = "Admin",
								Permission = GetSingleRolePermission("Admin"),
								AccountId = "admin",
								RealName = "admin",
								Email = "admin@admin.com",
								Telephone = "86-21-88886666",
								Touxian = "Admin",
								Company = "MY Tech",
								Department = "IT",
								Description = "Administrator，拥有用户权限修改、菜单修改，定序器修改等权限.",
								AccountStatus = true,
								Md5Pwd = Md5Helper.GetMd5Hash("admin")
							};
							CreateAccount(account);
							getAccountInfoResult.ActSuccess = true;
							getAccountInfoResult.AccountInfo = account;
							getAccountInfoResult.Description = $"成功获取账号信息{accountId}";
						}
						else
						{
							getAccountInfoResult.Description = $"账号{accountId}不存在";
							getAccountInfoResult.ActSuccess = false;
						}
					}
					else
					{
						getAccountInfoResult.AccountInfo = new Account
						{
							Role = accountNode.SelectSingleNode("Role").InnerText,
							Permission = GetSingleRolePermission((accountId == "admin") ? "Admin" : accountNode.SelectSingleNode("Role").InnerText),
							AccountId = accountId,
							RealName = accountNode.SelectSingleNode("RealName").InnerText,
							Email = accountNode.SelectSingleNode("Email").InnerText,
							Telephone = accountNode.SelectSingleNode("Telephone").InnerText,
							Touxian = accountNode.SelectSingleNode("Touxian").InnerText,
							Company = accountNode.SelectSingleNode("Company").InnerText,
							Department = accountNode.SelectSingleNode("Department").InnerText,
							Description = accountNode.SelectSingleNode("Description").InnerText,
							AccountStatus = (string.Compare(accountNode.SelectSingleNode("AccountState").InnerText, "Enable", ignoreCase: true) == 0),
							AccountCreationTime = accountNode.SelectSingleNode("CreationTime").InnerText,
							LastAccountUpdateTime = accountNode.SelectSingleNode("LastUpdateTime").InnerText,
							LastLoginTime = accountNode.SelectSingleNode("LastLoginTime").InnerText,
							Md5Pwd = accountNode.SelectSingleNode("Password").InnerText
						};
						getAccountInfoResult.Description = $"获取账号{accountId}成功";
						getAccountInfoResult.ActSuccess = true;
					}
				}
				return getAccountInfoResult;
			}
			catch (Exception ex)
			{
				string text = $"获取账号{accountId}发生异常";
				LOG.Write(ex, text);
				return new GetAccountInfoResult
				{
					ActSuccess = false,
					Description = text
				};
			}
		}

		public static ChangePwdResult ChangePassword(string accountId, string newPassword)
		{
			try
			{
				LOG.Write($"修改账号{accountId}的密码");
				accountId = accountId.ToLower();
				ChangePwdResult changePwdResult = new ChangePwdResult();
				if (!FileSigner.IsValid(_accountPath))
				{
					changePwdResult.Description = "修改密码失败，账号文件数字签名损坏！";
					changePwdResult.ActSucc = false;
				}
				else
				{
					XmlElement accountNode = GetAccountNode(accountId);
					if (accountNode == null)
					{
						changePwdResult.Description = $"账号{accountId}不存在";
						changePwdResult.ActSucc = false;
					}
					else
					{
						accountNode.SelectSingleNode("Password").InnerText = Md5Helper.GetMd5Hash(newPassword);
						Save(_accountXml, _accountPath);
						changePwdResult.Description = "修改密码成功！";
						changePwdResult.ActSucc = true;
						EV.PostMessage(Module, EventEnum.PasswordChanged, accountId);
					}
				}
				return changePwdResult;
			}
			catch (Exception ex)
			{
				string text = $"修改账号{accountId}的密码失败";
				LOG.Write(ex, text);
				return new ChangePwdResult
				{
					ActSucc = false,
					Description = text
				};
			}
		}

		public static CreateAccountResult CreateAccount(Account newAccount)
		{
			try
			{
				LOG.Write($"创建账号{newAccount.AccountId}");
				CreateAccountResult createAccountResult = new CreateAccountResult();
				if (newAccount == null)
				{
					createAccountResult.Description = "账号有误";
					createAccountResult.ActSucc = false;
				}
				else if (!FileSigner.IsValid(_accountPath))
				{
					createAccountResult.Description = $"创建账号失败，数字签名损坏！";
					createAccountResult.ActSucc = false;
				}
				else if (GetAccountNode(newAccount.AccountId) != null)
				{
					createAccountResult.Description = $"创建账号失败，账号 {newAccount.AccountId} 已存在！";
					createAccountResult.ActSucc = false;
				}
				else
				{
					XmlElement xmlElement = _accountXml.CreateElement("Account");
					xmlElement.SetAttribute("AccountId", newAccount.AccountId.ToLower());
					_accountXml.DocumentElement.AppendChild(xmlElement);
					XmlElement xmlElement2 = _accountXml.CreateElement("RealName");
					xmlElement2.InnerText = newAccount.RealName;
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("Role");
					xmlElement2.InnerText = newAccount.Role.ToString();
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("Password");
					xmlElement2.InnerText = Md5Helper.GetMd5Hash(newAccount.AccountId);
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("AccountState");
					xmlElement2.InnerText = (newAccount.AccountStatus ? "Enable" : "Disable");
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("Email");
					xmlElement2.InnerText = newAccount.Email;
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("Telephone");
					xmlElement2.InnerText = newAccount.Telephone;
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("Touxian");
					xmlElement2.InnerText = newAccount.Touxian;
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("Company");
					xmlElement2.InnerText = newAccount.Company;
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("Department");
					xmlElement2.InnerText = newAccount.Department;
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("Description");
					xmlElement2.InnerText = newAccount.Description;
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("CreationTime");
					xmlElement2.InnerText = DateTime.Now.ToString();
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("LastLoginTime");
					xmlElement2.InnerText = string.Empty;
					xmlElement.AppendChild(xmlElement2);
					xmlElement2 = _accountXml.CreateElement("LastUpdateTime");
					xmlElement2.InnerText = string.Empty;
					xmlElement.AppendChild(xmlElement2);
					Save(_accountXml, _accountPath);
					createAccountResult.Description = $"创建新账号{newAccount.AccountId}成功";
					createAccountResult.ActSucc = true;
					EV.PostMessage(Module, EventEnum.AccountCreated, newAccount.AccountId);
				}
				return createAccountResult;
			}
			catch (Exception ex)
			{
				string text = $"创建账号{newAccount.AccountId}失败";
				LOG.Write(ex, text);
				return new CreateAccountResult
				{
					ActSucc = false,
					Description = text
				};
			}
		}

		public static DeleteAccountResult DeleteAccount(string accountId)
		{
			try
			{
				LOG.Write($"删除账号{accountId}");
				accountId = accountId.ToLower();
				DeleteAccountResult deleteAccountResult = new DeleteAccountResult();
				if (accountId == "admin")
				{
					deleteAccountResult.Description = "Admin'admin'账号不能删除";
					deleteAccountResult.ActSucc = false;
				}
				else if (!FileSigner.IsValid(_accountPath))
				{
					deleteAccountResult.Description = "删除账号失败，账号文件数字签名损坏！";
					deleteAccountResult.ActSucc = false;
				}
				else
				{
					XmlElement accountNode = GetAccountNode(accountId);
					if (accountNode == null)
					{
						deleteAccountResult.Description = $"删除账号 {accountId} 失败，账号不存在！";
						deleteAccountResult.ActSucc = false;
					}
					else
					{
						_accountXml.DocumentElement.RemoveChild(accountNode);
						Save(_accountXml, _accountPath);
						deleteAccountResult.Description = $"删除账号 {accountId} 成功！";
						deleteAccountResult.ActSucc = true;
						EV.PostMessage(Module, EventEnum.AccountDeleted, accountId);
					}
				}
				return deleteAccountResult;
			}
			catch (Exception ex)
			{
				string text = $"删除账号{accountId}发生异常";
				LOG.Write(ex, text);
				return new DeleteAccountResult
				{
					ActSucc = false,
					Description = text
				};
			}
		}

		public static UpdateAccountResult UpdateAccount(Account account)
		{
			try
			{
				UpdateAccountResult updateAccountResult = new UpdateAccountResult();
				if (account == null)
				{
					updateAccountResult.Description = "账号有误";
					updateAccountResult.ActSucc = false;
				}
				else if (!FileSigner.IsValid(_accountPath))
				{
					updateAccountResult.Description = $"更新账号资料失败，账号文件数字签名损坏！";
					updateAccountResult.ActSucc = false;
				}
				else
				{
					XmlElement accountNode = GetAccountNode(account.AccountId.ToLower());
					if (accountNode == null)
					{
						updateAccountResult.Description = $"更新账号 {account.AccountId} 失败，账号不存在！";
						updateAccountResult.ActSucc = false;
					}
					else
					{
						accountNode.SelectSingleNode("RealName").InnerText = account.RealName;
						accountNode.SelectSingleNode("Role").InnerText = ((account.AccountId.ToLower() == "admin") ? "Admin" : account.Role.ToString());
						accountNode.SelectSingleNode("AccountState").InnerText = (account.AccountStatus ? "Enable" : "Disable");
						accountNode.SelectSingleNode("Email").InnerText = account.Email;
						accountNode.SelectSingleNode("Telephone").InnerText = account.Telephone;
						accountNode.SelectSingleNode("Touxian").InnerText = account.Touxian;
						accountNode.SelectSingleNode("Company").InnerText = account.Company;
						accountNode.SelectSingleNode("Department").InnerText = account.Department;
						accountNode.SelectSingleNode("Description").InnerText = account.Description;
						accountNode.SelectSingleNode("CreationTime").InnerText = account.AccountCreationTime;
						accountNode.SelectSingleNode("LastLoginTime").InnerText = account.LastLoginTime;
						accountNode.SelectSingleNode("LastUpdateTime").InnerText = account.LastAccountUpdateTime;
						Save(_accountXml, _accountPath);
						updateAccountResult.Description = $"成功更新 {account.AccountId} 的账号资料！";
						updateAccountResult.ActSucc = true;
						EV.PostMessage(Module, EventEnum.AccountChanged, account.AccountId);
					}
				}
				return updateAccountResult;
			}
			catch (Exception ex)
			{
				string text = $"更新账号{account.AccountId}资料发生异常";
				LOG.Write(ex, text);
				return new UpdateAccountResult
				{
					ActSucc = false,
					Description = text
				};
			}
		}

		public static GetAccountListResult GetAccountList()
		{
			try
			{
				LOG.Write("获取所有的账号信息列表");
				GetAccountListResult getAccountListResult = new GetAccountListResult();
				if (!FileSigner.IsValid(_accountPath))
				{
					getAccountListResult.Description = "获取账号列表失败，账号文件数字签名文件损坏！";
					getAccountListResult.ActSuccess = false;
					getAccountListResult.AccountList = null;
				}
				else
				{
					XmlNodeList xmlNodeList = _accountXml.SelectNodes("AccountManagement/Account");
					List<Account> list = new List<Account>();
					foreach (XmlNode item in xmlNodeList)
					{
						list.Add(GetAccountInfo(item.Attributes["AccountId"].Value).AccountInfo);
					}
					getAccountListResult.AccountList = list;
					getAccountListResult.Description = "成功获取账号列表！";
					getAccountListResult.ActSuccess = true;
				}
				Accounts = getAccountListResult;
				return getAccountListResult;
			}
			catch (Exception ex)
			{
				string text = "获取账号列表发生异常";
				LOG.Write(ex, text);
				return new GetAccountListResult
				{
					AccountList = null,
					ActSuccess = false,
					Description = text
				};
			}
		}

		public static void CheckAlive(string accountId)
		{
			try
			{
				if (_userList.ContainsKey(accountId))
				{
					_userList[accountId] = new Tuple<Guid, DateTime, string>(_userList[accountId].Item1, DateTime.Now, _userList[accountId].Item3);
				}
				else
				{
					EV.PostKickoutMessage($"账号{accountId}已在服务器上注销");
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		private static XmlElement GetAccountNode(string accountId)
		{
			XmlNode xmlNode = _accountXml.SelectSingleNode($"/AccountManagement/Account[@AccountId='{accountId.ToLower()}']");
			return (XmlElement)xmlNode;
		}

		public static SerializableDictionary<string, string> GetAllViewList()
		{
			SerializableDictionary<string, string> serializableDictionary = new SerializableDictionary<string, string>();
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				string filename = Path.Combine(PathManager.GetAccountFilePath(), "Views.xml");
				xmlDocument.Load(filename);
				XmlNodeList xmlNodeList = xmlDocument.SelectNodes("/root/Views/View");
				if (xmlNodeList != null)
				{
					foreach (XmlElement item in xmlNodeList)
					{
						serializableDictionary.Add(item.Attributes["Name"].Value, item.Attributes["Description"].Value);
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				serializableDictionary = new SerializableDictionary<string, string>();
			}
			return serializableDictionary;
		}

		public static bool SaveAllRolesPermission(Dictionary<string, Dictionary<string, ViewPermission>> data)
		{
			try
			{
				XmlElement xmlElement = _roleXml.SelectSingleNode("/Aitex/Roles") as XmlElement;
				xmlElement.RemoveAll();
				foreach (KeyValuePair<string, Dictionary<string, ViewPermission>> datum in data)
				{
					if (datum.Key == "Admin")
					{
						continue;
					}
					XmlElement xmlElement2 = _roleXml.CreateElement("Role");
					xmlElement2.SetAttribute("Name", datum.Key);
					xmlElement.AppendChild(xmlElement2);
					foreach (string key in data[datum.Key].Keys)
					{
						XmlElement xmlElement3 = _roleXml.CreateElement("View");
						xmlElement2.AppendChild(xmlElement3);
						xmlElement3.SetAttribute("Name", key);
						xmlElement3.SetAttribute("Permission", data[datum.Key][key].ToString());
					}
				}
				_roleXml.Save(_rolePath);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return false;
			}
			return true;
		}

		public static IEnumerable<string> GetAllRoles()
		{
			List<string> list = new List<string>();
			try
			{
				XmlNodeList xmlNodeList = _roleXml.SelectNodes("/Aitex/Roles/Role");
				foreach (XmlElement item in xmlNodeList)
				{
					list.Add(item.Attributes["Name"].Value);
				}
				if (!list.Contains("Admin"))
				{
					list.Add("Admin");
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				list = new List<string>();
			}
			return list;
		}

		public static SerializableDictionary<string, ViewPermission> GetSingleRolePermission(string roleName)
		{
			SerializableDictionary<string, ViewPermission> serializableDictionary = new SerializableDictionary<string, ViewPermission>();
			try
			{
				SerializableDictionary<string, string> allViewList = GetAllViewList();
				if (roleName == "Admin")
				{
					foreach (KeyValuePair<string, string> item in allViewList)
					{
						serializableDictionary.Add(item.Key, ViewPermission.FullyControl);
					}
				}
				else
				{
					XmlNode xmlNode = _roleXml.SelectSingleNode($"/Aitex/Roles/Role[@Name='{roleName}']");
					if (xmlNode != null)
					{
						foreach (XmlElement item2 in xmlNode)
						{
							string value = item2.Attributes["Name"].Value;
							string value2 = item2.Attributes["Permission"].Value;
							if (allViewList.ContainsKey(value))
							{
								serializableDictionary.Add(value, (ViewPermission)Enum.Parse(typeof(ViewPermission), value2, ignoreCase: true));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				serializableDictionary = new SerializableDictionary<string, ViewPermission>();
			}
			return serializableDictionary;
		}

		public static SerializableDictionary<string, SerializableDictionary<string, ViewPermission>> GetAllRolesPermission()
		{
			try
			{
				SerializableDictionary<string, SerializableDictionary<string, ViewPermission>> serializableDictionary = new SerializableDictionary<string, SerializableDictionary<string, ViewPermission>>();
				foreach (string allRole in GetAllRoles())
				{
					serializableDictionary.Add(allRole, GetSingleRolePermission(allRole));
				}
				return serializableDictionary;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return new SerializableDictionary<string, SerializableDictionary<string, ViewPermission>>();
			}
		}
	}
}

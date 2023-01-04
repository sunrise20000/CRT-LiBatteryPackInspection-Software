using System;
using System.Collections.Generic;
using System.Linq;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Microsoft.Win32;

namespace Aitex.Core.RT.Key
{
	public class KeyManager : Singleton<KeyManager>
	{
		public const string KeyTag = "Jet";

		public const string RegistryKey = "SOFTWARE\\Jet\\Keys";

		private PeriodicJob _thread;

		private Dictionary<string, string> _historyKeys = new Dictionary<string, string>();

		private object _locker = new object();

		private DeviceTimer _timer0Days = new DeviceTimer();

		private R_TRIG _trig0Days = new R_TRIG();

		private DeviceTimer _timer3Days = new DeviceTimer();

		private R_TRIG _trig3Days = new R_TRIG();

		private DeviceTimer _timer7Days = new DeviceTimer();

		private R_TRIG _trig7Days = new R_TRIG();

		private DeviceTimer _timer15Days = new DeviceTimer();

		private R_TRIG _trig15Days = new R_TRIG();

		public string LocalMachineCode { get; private set; }

		public bool IsExpired => false;

		public int LeftDays
		{
			get
			{
				if (IsExpired)
				{
					return 0;
				}
				return (ExpireDateTime - CurrentDateTime).Days;
			}
		}

		public DateTime CurrentDateTime => DateTime.Now;

		public DateTime ExpireDateTime => LastRegisterDateTime + new TimeSpan(LastRegisterDays, 0, 0, 0);

		public DateTime LastRegisterDateTime { get; set; }

		public int LastRegisterDays { get; set; }

		public void Initialize()
		{
			LocalMachineCode = new MachineCoder().CreateCode();
			UpdateLicenseInformation();
			_thread = new PeriodicJob(1000, OnTimer, "Register Key Thread", isStartNow: true);
		}

		private void UpdateLicenseInformation()
		{
			try
			{
				RegistryKey currentUser = Registry.CurrentUser;
				RegistryKey registryKey = currentUser.OpenSubKey("SOFTWARE\\Jet\\Keys", writable: true);
				if (registryKey == null)
				{
					registryKey = currentUser.CreateSubKey("SOFTWARE\\Jet\\Keys");
				}
				if (registryKey == null)
				{
					throw new ApplicationException("注册表操作失败，无法进行软件授权操作.\r\n请确保用管理员权限运行程序.");
				}
				string[] subKeyNames = registryKey.GetSubKeyNames();
				foreach (string text in subKeyNames)
				{
					RegistryKey registryKey2 = registryKey.OpenSubKey(text);
					_historyKeys.Add(text, (string)registryKey2.GetValue("Key"));
				}
				if (_historyKeys.Count == 0)
				{
					if (!Register(7, "---", out var reason))
					{
						throw new ApplicationException(reason);
					}
					return;
				}
				List<string> list = _historyKeys.Keys.ToList();
				list.Sort();
				string text2 = list.Last();
				RegistryKey registryKey3 = registryKey.OpenSubKey(text2);
				LastRegisterDateTime = new DateTime(int.Parse(text2.Substring(0, 4)), int.Parse(text2.Substring(4, 2)), int.Parse(text2.Substring(6, 2)), int.Parse(text2.Substring(8, 2)), int.Parse(text2.Substring(10, 2)), int.Parse(text2.Substring(12, 2)));
				LastRegisterDays = int.Parse(registryKey3.GetValue("Days").ToString());
			}
			catch (Exception ex)
			{
				throw new ApplicationException(ex.Message);
			}
		}

		private bool OnTimer()
		{
			try
			{
				_trig0Days.CLK = IsExpired;
				_trig3Days.CLK = LeftDays <= 3 && !IsExpired;
				_trig7Days.CLK = LeftDays <= 7 && LeftDays > 3;
				_trig15Days.CLK = LeftDays <= 15 && LeftDays > 7;
				if (_trig0Days.M)
				{
					if (_trig0Days.Q)
					{
						EV.PostMessage("System", EventEnum.DefaultWarning, $"Software expired at {ExpireDateTime.ToString()} ");
						_timer0Days.Start(3600000.0);
					}
					if (_timer0Days.IsTimeout())
					{
						EV.PostMessage("System", EventEnum.DefaultWarning, $"Software expired at {ExpireDateTime.ToString()} ");
						_timer0Days.Start(3600000.0);
					}
				}
				if (_trig3Days.M)
				{
					if (_trig3Days.Q)
					{
						EV.PostMessage("System", EventEnum.DefaultWarning, $"Software will be expired in {LeftDays} days, at {ExpireDateTime.ToString()} ");
						_timer3Days.Start(10800000.0);
					}
					if (_timer3Days.IsTimeout())
					{
						EV.PostMessage("System", EventEnum.DefaultWarning, $"Software will be expired in {LeftDays} days, at {ExpireDateTime.ToString()}");
						_timer3Days.Start(10800000.0);
					}
				}
				if (_trig7Days.M)
				{
					if (_trig7Days.Q)
					{
						EV.PostMessage("System", EventEnum.DefaultWarning, $"Software will be expired in {LeftDays} days, at {ExpireDateTime.ToString()} ");
						_timer7Days.Start(28800000.0);
					}
					if (_timer7Days.IsTimeout())
					{
						EV.PostMessage("System", EventEnum.DefaultWarning, $"Software will be expired in {LeftDays} days, at {ExpireDateTime.ToString()}");
						_timer7Days.Start(28800000.0);
					}
				}
				if (_trig15Days.M)
				{
					if (_trig15Days.Q)
					{
						EV.PostMessage("System", EventEnum.GeneralInfo, $"Software will be expired in {LeftDays} days, at {ExpireDateTime.ToString()} ");
						_timer15Days.Start(28800000.0);
					}
					if (_timer15Days.IsTimeout())
					{
						EV.PostMessage("System", EventEnum.GeneralInfo, $"Software will be expired in {LeftDays} days, at {ExpireDateTime.ToString()}");
						_timer15Days.Start(28800000.0);
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return true;
		}

		private bool Register(int days, string key, out string reason)
		{
			try
			{
				if (_historyKeys.ContainsValue(key))
				{
					reason = "注册码已经使用，" + key;
					LOG.Write(reason);
					return false;
				}
				if (days < 0)
				{
					reason = "授权天数无效，" + days;
					LOG.Write(reason);
					return false;
				}
				RegistryKey currentUser = Registry.CurrentUser;
				RegistryKey registryKey = currentUser.OpenSubKey("SOFTWARE\\Jet\\Keys", writable: true);
				if (registryKey == null)
				{
					registryKey = currentUser.CreateSubKey("SOFTWARE\\Jet\\Keys");
				}
				if (registryKey == null)
				{
					reason = "注册表操作失败，无法进行软件授权操作.\r\n请确保用管理员权限运行程序.";
					LOG.Write(reason);
					return false;
				}
				DateTime currentDateTime = CurrentDateTime;
				string text = currentDateTime.ToString("yyyyMMddHHmmss");
				RegistryKey registryKey2 = registryKey.CreateSubKey(text);
				registryKey2.SetValue("Date", text);
				registryKey2.SetValue("Key", key);
				registryKey2.SetValue("Days", days.ToString());
				registryKey.Close();
				_historyKeys.Add(text, key);
				LastRegisterDateTime = currentDateTime;
				LastRegisterDays = days;
			}
			catch (Exception ex)
			{
				reason = "注册码无效，" + ex;
				LOG.Write(reason);
				return false;
			}
			reason = "注册成功";
			return true;
		}

		public bool Register(string key, out string reason)
		{
			reason = string.Empty;
			RsaCryption rsaCryption = new RsaCryption();
			string empty = string.Empty;
			try
			{
				empty = rsaCryption.RSADecrypt("<RSAKeyValue><Modulus>1grdIZdLFrqRjlFbuk+wXWQeJaKOXsAlKOyFJYNo8wFUHDtOJMaKpdeYtmwtn/ZhvEP+dDWgKGYx+oIAFhF0A8BMrtTkhSeJXJvYbrWa1ObYayvjNTx49bC7xX/c+woWOJvwsllb1s04m2dvTpZUsC6aI3hDvRWPjT8GJglMJjU=</Modulus><Exponent>AQAB</Exponent><P>+5fBfLc7nfej2BoQH/sKMBxfu6K+dtTdIB1vBo8OTtxfqKoxNgzv0KtcZBoWZzBlG1yv8/Z+sAMV0Xh94O4Qow==</P><Q>2cq3iRRkJEnxE9NdZ+FyUXuFdbBAj8CTdy+xETRq3YrJBxm4SY7JRwbADyxXJ3zTzH+0wC+apaji9SbuTrfjRw==</Q><DP>ARkScRbjnbbc8i5674jK7JbTHCCDsEURifhW6bJqH5H6oOPNPy0jRsfYqV8rxduCNXJcGjZzKxV4XOentPmU/Q==</DP><DQ>d1ozaIjnNEfdOJstZf9TkbnacptrVhwX9EoLhD0wj0Y+UojSyGTagvT9DZOkE3zB6SDXIjc0TbKW5fg2wqbdgw==</DQ><InverseQ>k/kPJ2Yo5nItIDvCjOIK2q0XulYDYqJmCHO/PopslpQ2nMBhO7f5SL9j9FAEpotvpmXH3RVQ1txk1SO3EncDkg==</InverseQ><D>OYsUJhq5ijPAPAWtZmpUHNd0r2ODaP+5PcZQiWRJy8LYrPpYqNRjo/BRUwHERlQDtIyHFRDxrMrEtvdKNKSejoFsU+kIZWuUH6AwytFUcglOjnZtXNWiBD9xGrDg4qQJk3tIEvtPhe0uamUTtGPNZuiyzF2HJmXJNjokS+SypKU=</D></RSAKeyValue>", key);
			}
			catch (Exception ex)
			{
				reason = "注册码无效";
				LOG.Write(ex);
				return false;
			}
			string[] array = empty.Split(',');
			int result = 0;
			if (array.Length == 3 && array[0] == "Jet" && array[1] == LocalMachineCode && int.TryParse(array[2], out result))
			{
				return Register(result, key, out reason);
			}
			LOG.Error("注册码无效，" + empty);
			reason = "注册码无效";
			return false;
		}
	}
}

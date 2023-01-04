using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MECF.Framework.Common.Account.Extends
{
	[Serializable]
	public class AppMenu : INotifyPropertyChanged
	{
		private string m_strMenuID;

		private string m_strViewModel;

		private string m_strResKey;

		private string _System;

		private int permission;

		private AppMenu parent;

		private List<AppMenu> menuItems;

		private bool selected;

		private bool _isAlarm;

		private string _alarmModule;

		public string MenuID
		{
			get
			{
				return m_strMenuID;
			}
			set
			{
				m_strMenuID = value;
			}
		}

		public string ResKey
		{
			get
			{
				return m_strResKey;
			}
			set
			{
				m_strResKey = value;
			}
		}

		public List<AppMenu> MenuItems
		{
			get
			{
				return menuItems;
			}
			set
			{
				menuItems = value;
			}
		}

		public string ViewModel
		{
			get
			{
				return m_strViewModel;
			}
			set
			{
				m_strViewModel = value;
			}
		}

		public int Permission
		{
			get
			{
				return permission;
			}
			set
			{
				permission = value;
			}
		}

		public string System
		{
			get
			{
				return _System;
			}
			set
			{
				_System = value;
			}
		}

		public AppMenu Parent
		{
			get
			{
				return parent;
			}
			set
			{
				parent = value;
			}
		}

		public bool Selected
		{
			get
			{
				return selected;
			}
			set
			{
				selected = value;
				OnPropertyChanged("Selected");
			}
		}

		public bool IsAlarm
		{
			get
			{
				return _isAlarm;
			}
			set
			{
				_isAlarm = value;
				OnPropertyChanged("IsAlarm");
			}
		}

		public string AlarmModule
		{
			get
			{
				return _alarmModule;
			}
			set
			{
				_alarmModule = value;
			}
		}

		public object Model { get; set; }

		public AppMenu LastSelectedSubMenu { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		public AppMenu()
		{
		}

		public AppMenu(string p_strMenuID, string p_strMenuViewModel, string p_strResKey, List<AppMenu> p_menuItems)
		{
			m_strMenuID = p_strMenuID;
			m_strViewModel = p_strMenuViewModel;
			m_strResKey = p_strResKey;
			menuItems = p_menuItems;
		}

		public object Clone(AppMenu parent)
		{
			AppMenu appMenu = new AppMenu();
			appMenu.MenuID = MenuID;
			appMenu.ResKey = ResKey;
			appMenu.ViewModel = ViewModel;
			appMenu.Permission = Permission;
			appMenu.Selected = Selected;
			appMenu.System = System;
			appMenu.Parent = parent;
			appMenu.IsAlarm = IsAlarm;
			appMenu.AlarmModule = AlarmModule;
			if (MenuItems != null)
			{
				appMenu.MenuItems = new List<AppMenu>();
				foreach (AppMenu menuItem in MenuItems)
				{
					appMenu.MenuItems.Add((AppMenu)menuItem.Clone(appMenu));
				}
			}
			return appMenu;
		}

		protected virtual void OnPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

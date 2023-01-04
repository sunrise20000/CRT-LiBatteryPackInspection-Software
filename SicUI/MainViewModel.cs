using Aitex.Core.Account;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using Caliburn.Micro;
using MECF.Framework.Common.Account.Extends;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.LogOnOff;
using MECF.Framework.UI.Client.ClientBase;
using MECF.Framework.UI.Core.Accounts;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;
using OpenSEMI.ClientBase.Utility;
using SciChart.Charting.ChartModifiers;
using SciChart.Charting.Visuals;
using SciChart.Charting.Visuals.Annotations;
using SciChart.Charting.Visuals.Axes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro.Core;
using Sicentury.Core.Collections;
using Cali = Caliburn.Micro.Core;
using System.Diagnostics;
using MECF.Framework.Common.Equipment;
using SciChart.Core.Extensions;

namespace SicUI.Client
{
    public class TimeredMainViewModel : Cali.Conductor<Cali.Screen>.Collection.OneActive
    {
        PeriodicJob _timer;
        ConcurrentBag<string> _subscribedKeys = new ConcurrentBag<string>();

        Func<object, bool> _isSubscriptionAttribute;
        Func<MemberInfo, bool> _hasSubscriptionAttribute;

        private IProgress<string> _progOfflineTimeout;

        public TimeredMainViewModel()
        {
            _timer = new PeriodicJob(1000, this.OnTimer, "UIUpdaterThread - " + GetType().Name);

            _isSubscriptionAttribute = attribute => attribute is SubscriptionAttribute;
            _hasSubscriptionAttribute = mi => mi.GetCustomAttributes(false).Any(_isSubscriptionAttribute);

            _progOfflineTimeout = new Progress<string>(msg =>
            {
                DialogBox.ShowWarning(msg);
            });

            SubscribeKeys(this);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LASTINPUTINFO
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public int dwTime;
        }

        [DllImport("user32.dll")]
        internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        /// <summary>
        /// 获取鼠标键盘不活动的时间
        /// </summary>
        /// <returns>结果</returns>
        public static int GetLastInputTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            int idleTime = 0;
            if (GetLastInputInfo(ref lastInputInfo))
            {
                idleTime = Environment.TickCount - lastInputInfo.dwTime;
            }

            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }

        protected virtual bool OnTimer()
        {
            try
            {
                Poll();
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }

            return true;
        }


        public virtual void EnableTimer(bool enable)
        {
            if (enable) _timer.Start();
            else _timer.Pause();
        }


        protected virtual void Poll()
        {
            if (_subscribedKeys.Count > 0)
            {
                Dictionary<string, object> result = QueryDataClient.Instance.Service.PollData(_subscribedKeys);

                if (result == null)
                {
                    LOG.Error("获取RT数据失败");
                    return;
                }

                if (result.Count != _subscribedKeys.Count)
                {
                    string unknowKeys = string.Empty;
                    foreach (string key in _subscribedKeys)
                    {
                        if (!result.ContainsKey(key))
                        {
                            unknowKeys += key + "\r\n";
                        }
                    }
                    //System.Diagnostics.Debug.Assert(false, unknowKeys);
                }

                InvokeBeforeUpdateProperty(result);

                UpdateValue(result);

                Application.Current.Dispatcher.Invoke(new System.Action(() =>
                {
                    InvokePropertyChanged();

                    InvokeAfterUpdateProperty(result);
                }));


            }
        }

        private void InvokePropertyChanged()
        {
            Refresh();
        }

        protected virtual void InvokeBeforeUpdateProperty(Dictionary<string, object> data)
        {

        }

        protected virtual void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {

        }

        void UpdateValue(Dictionary<string, object> data)
        {
            if (data == null)
                return;

            UpdateSubscribe(data, this);

            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttribute<SubscriptionModuleAttribute>() != null);
            foreach (var property in properties)
            {
                var moduleAttr = property.GetCustomAttribute<SubscriptionModuleAttribute>();
                UpdateSubscribe(data, property.GetValue(this), moduleAttr.Module);
            }
        }

        protected void Subscribe(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _subscribedKeys.Add(key);
            }
        }
        
        private ConcurrentDictionary<string, R_TRIG> _dictOfflineTimeoutTrigger = new ConcurrentDictionary<string, R_TRIG>();
        
        public void SubscribeKeys(TimeredMainViewModel target)
        {
            Parallel.ForEach(target.GetType().GetProperties().Where(_hasSubscriptionAttribute),
                property =>
                {
                    SubscriptionAttribute subscription = property.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
                    string key = subscription.ModuleKey;
                    if (!_subscribedKeys.Contains(key))
                    {
                        _subscribedKeys.Add(key);

                        // 如果属性订阅模块的IsOfflineTimeout属性，则为其添加一个R_Trig用于检测超时跳变。
                        if (key.Contains("IsOfflineTimeout"))
                            _dictOfflineTimeoutTrigger.TryAdd(key, new R_TRIG());
                    }
                });

            Parallel.ForEach(target.GetType().GetFields().Where(_hasSubscriptionAttribute),
                method =>
                {
                    SubscriptionAttribute subscription = method.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
                    string key = subscription.ModuleKey;
                    if (!_subscribedKeys.Contains(key))
                        _subscribedKeys.Add(key);
                });
        }

        public void UpdateSubscribe(Dictionary<string, object> data, object target, string module = null)
        {
            ConcurrentBag<string> offlineTimeoutModules = new ConcurrentBag<string>();
            var ret = Parallel.ForEach(target.GetType().GetProperties().Where(_hasSubscriptionAttribute),
                property =>
                {
                    PropertyInfo pi = (PropertyInfo)property;
                    SubscriptionAttribute subscription =
                        property.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
                    string key = subscription.ModuleKey;
                    key = module == null ? key : string.Format("{0}.{1}", module, key);

                    if (_subscribedKeys.Contains(key) && data.ContainsKey(key))
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(data[key], pi.PropertyType);
                            var originValue = Convert.ChangeType(pi.GetValue(target, null), pi.PropertyType);
                            if (originValue != convertedValue)
                            {
                                if (pi.Name == "PumpLimitSetPoint")
                                    pi.SetValue(target, convertedValue, null);
                                else
                                    pi.SetValue(target, convertedValue, null);
                            }

                            if (_dictOfflineTimeoutTrigger.TryGetValue(key, out var rTrig))
                            {
                                rTrig.CLK = (bool)convertedValue;
                                if (rTrig.Q)
                                    offlineTimeoutModules.Add(key.Replace(".IsOfflineTimeout", ""));

                            }
                        }
                        catch (Exception ex)
                        {
                            LOG.Error("由RT返回的数据更新失败" + key, ex);

                        }

                    }
                });

            Parallel.ForEach(target.GetType().GetFields().Where(_hasSubscriptionAttribute),
                property =>
                {
                    FieldInfo pi = (FieldInfo)property;
                    SubscriptionAttribute subscription =
                        property.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
                    string key = subscription.ModuleKey;

                    if (_subscribedKeys.Contains(key) && data.ContainsKey(key))
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(data[key], pi.FieldType);
                            pi.SetValue(target, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            LOG.Error("由RT返回的数据更新失败" + key, ex);

                        }

                    }
                });

            if (offlineTimeoutModules.IsEmpty)
                return;

            // 弹警告框提示Modules离线超时。
            _progOfflineTimeout.Report(
                $"The following module(s) are offline timeout: {string.Join(",", offlineTimeoutModules.ToArray())}");
            
        }

    }

    public class MainViewModel : TimeredMainViewModel
    {
        #region Menus

        public string NowDateTime { get; set; }

        private bool _IsLogin = false;
        public bool IsLogin
        {
            get { return _IsLogin; }
            set { _IsLogin = value; NotifyOfPropertyChange("IsLogin"); }
        }

        private List<Role> roles;
        public List<Role> Roles
        {
            get { return this.roles; }
            set { this.roles = value; this.RaisePropertyChangedEventImmediately("Roles"); }
        }

        private ICommand menuItemClickCommand;
        public ICommand MenuItemClickCommand
        {
            get
            {
                if (this.menuItemClickCommand == null)
                    this.menuItemClickCommand = new BaseCommand<AppMenu>((AppMenu menuViewItem) => this.SwitchMenuItem(menuViewItem));
                return this.menuItemClickCommand;
            }
        }

        private ICommand mainmenuItemClickCommand;
        public ICommand MainMenuItemClickCommand
        {
            get
            {
                if (this.mainmenuItemClickCommand == null)
                    this.mainmenuItemClickCommand = new BaseCommand<AppMenu>((AppMenu menuViewItem) => this.MainSwitchMenuItem(menuViewItem));
                return this.mainmenuItemClickCommand;
            }
        }

        public List<AppMenu> MenuItems
        {
            get { return this.menuItems; }
            set { this.menuItems = value; this.NotifyOfPropertyChange("MenuItems"); }
        }

        public List<AppMenu> SubMenuItems
        {
            get { return this.subMenuItems; }
            set { this.subMenuItems = value; this.NotifyOfPropertyChange("SubMenuItems"); }
        }

        public ObservableCollection<AppMenu> HistoryMenus
        {
            get { return this.historyItems; }
            set { this.historyItems = value; this.NotifyOfPropertyChange("HistoryMenus"); }
        }

        public string Context
        {
            get { return this.context; }
            set { this.context = value; this.NotifyOfPropertyChange("Context"); }
        }

        public BaseModel CurrentViewModel { get; private set; }
        public UserContext User { get { return BaseApp.Instance.UserContext; } }

        private AppMenu _currentMenuItem;
        private List<AppMenu> menuItems;
        private List<AppMenu> subMenuItems;
        private ObservableCollection<AppMenu> historyItems;
        private string context;
        private MainView _view;
        private Dictionary<Type, BaseModel> _models;

        #endregion

        public bool IsPM1Installed { get; set; }
        
        public bool IsPM2Installed { get; set; }
        
        public bool IsBufferInstalled { get; set; }
        
        public bool IsLLInstalled { get; set; }

        public bool IsPermission { get; set; }
        
        public bool IsAutoLogout { get; set; }
        
        public int LogoutTime { get; set; }


        //public ObservableCollection<EventItem> WarnEventLogList { get; set; }
        private DelayedPresentRollingObservableCollection<EventItem> EventLogList { get; }

        /// <summary>
        /// 用于在主界面显示Event Log的视图。
        /// 通过该视图筛选IsAlarm条目。
        /// </summary>
        public ICollectionView EventLogsView { get; }


        private bool _isShowAlarmEventOnly;

        /// <summary>
        /// IsAlarm CheckBox绑定到这里，直接从<see cref="EventLogsView"/>中过滤所需的数据。
        /// </summary>
        public bool IsShowAlarmEventOnly
        {
            get => _isShowAlarmEventOnly;
            set
            {
                _isShowAlarmEventOnly = value;
                if (_isShowAlarmEventOnly)
                {
                    EventLogsView.Filter = item =>
                    {
                        if (item is EventItem ei)
                        {
                            return ei.Level == EventLevel.Alarm;
                        }

                        return false;
                    };
                }
                else
                {
                    EventLogsView.Filter = null;
                }

                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsShowAlarmEventOnly)));
            }
        }

        public Visibility AllEventsVisibility { get; set; }

        public Visibility WarnEventsVisibility { get; set; }

        [Subscription("Rt.Status")]
        public string RtStatus { get; set; }
        
        [Subscription("PM1.IsOfflineTimeout")]
        public bool IsPm1OfflineTimeout { get; set; }
        
        [Subscription("TM.IsOfflineTimeout")]
        public bool IsTmOfflineTimeout { get; set; }
        
        [Subscription("EFEM.IsOfflineTimeout")]
        public bool IsEfemOfflineTimeout { get; set; }
        
        [Subscription("Buffer.IsOfflineTimeout")]
        public bool IsBufferfflineTimeout { get; set; }
        
        [Subscription("LoadLock.IsOfflineTimeout")]
        public bool IsLoadOfflineTimeout { get; set; }
        
        [Subscription("UnLoad.IsOfflineTimeout")]
        public bool IsUnLoadOfflineTimeout { get; set; }
        
        [Subscription("Aligner.IsOfflineTimeout")]
        public bool IsAlignerOfflineTimeout { get; set; }
        
        [Subscription("CassAL.IsOfflineTimeout")]
        public bool IsCassALOfflineTimeout { get; set; }
        
        [Subscription("CassAR.IsOfflineTimeout")]
        public bool IsCassAROfflineTimeout { get; set; }
        
        [Subscription("CassBL.IsOfflineTimeout")]
        public bool IsCassBLOfflineTimeout { get; set; }
        
        [Subscription("WaferRobot.IsOfflineTimeout")]
        public bool IsWaferRobotOfflineTimeout { get; set; }
        
        [Subscription("TrayRobot.IsOfflineTimeout")]
        public bool IsTrayRobotOfflineTimeout { get; set; }

        public void MonitorIsOfflineTimeout()
        {
            var props = this.GetType().GetProperties()
                .Where(pi => pi.GetCustomAttributes(typeof(SubscriptionAttribute), false).Any())
                .ToList();

            foreach (var pi in props)
            {
                if (pi.GetCustomAttributes().Any(attribute => attribute is SubscriptionAttribute sub && sub.Key.Contains("IsOfflineTimeout")))
                {
                    var isTimeout = pi.GetValue(this);
                    var rTrig = new R_TRIG();
                    
                }
            }
        }
        
        public string RtStatusBackground => ModuleStatusBackground.GetStatusBackground(RtStatus);

        [Subscription("System.ControlStatus")]
        public string ControlStatus { get; set; }

        [Subscription("System.CommunicationStatus")]
        public string HostStatus { get; set; }
        public string HostStatusBackground
        {
            get
            {
                switch (HostStatus)
                {
                    case "Disabled":
                        return "Yellow";
                    case "Enabled":
                    case "EnabledNotCommunicating":
                    case "WaitCRA":
                    case "WaitDelay":
                    case "WaitCRFromHost":
                        return "Transparent";
                    case "EnabledCommunicating":
                        return "LawnGreen";
                    default:
                        return "Yellow";
                }
            }
        }

        [Subscription("PM1.Status")]
        public string PM1Status { get; set; }
        public string PM1StatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(PM1Status); }
        }

        //[Subscription("PM2.Status")]
        private string _pm2Status;
        public string PM2Status
        {
            get
            {
                return "NotInstall";
            }
            set { _pm2Status = value; }
        } 
        public string PM2StatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(PM2Status); }
        }
        [Subscription("Aligner.Status")]
        public string AlignerStatus { get; set; }
        public string AlignerStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(AlignerStatus); }
        }
        [Subscription("CassAL.Status")]
        public string CassALStatus { get; set; }
        public string CassALStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(CassALStatus); }
        }
        [Subscription("CassAR.Status")]
        public string CassARStatus { get; set; }
        public string CassARStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(CassARStatus); }
        }
        [Subscription("CassBL.Status")]
        public string CassBLStatus { get; set; }
        public string CassBLStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(CassBLStatus); }
        }

        [Subscription("UnLoad.Status")]
        public string UnLoadStatus { get; set; }
        public string UnLoadStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(UnLoadStatus); }
        }
        [Subscription("EFEM.Status")]
        public string EFEMStatus { get; set; }
        public string EFEMStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(EFEMStatus); }
        }
        [Subscription("Buffer.Status")]
        public string BufferStatus { get; set; }
        public string BufferStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(BufferStatus); }
        }
        [Subscription("LoadLock.Status")]
        public string LLStatus { get; set; }
        public string LLStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(LLStatus); }
        }
        [Subscription("TM.Status")]
        public string TMStatus { get; set; }

        public string TMStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(TMStatus); }
        }


        [Subscription("WaferRobot.Status")]
        public string WaferRobotStatus { get; set; }
        public string WaferRobotStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(WaferRobotStatus); }
        }

        [Subscription("TrayRobot.Status")]
        public string TrayRobotStatus { get; set; }
        public string TrayRobotStatusBackground
        {
            get { return ModuleStatusBackground.GetStatusBackground(TrayRobotStatus); }
        }



        [Subscription("System.IsOnline")]
        public bool IsOnlineSystem { get; set; }

        [Subscription("PM1.IsOnline")]
        public bool IsOnlinePM1 { get; set; }

        [Subscription("PM2.IsOnline")]
        public bool IsOnlinePM2 { get; set; }
        [Subscription("EFEM.IsOnline")]
        public bool IsOnlineEFEM { get; set; }
        [Subscription("Aligner.IsOnline")]
        public bool IsOnlineAligner { get; set; }
        [Subscription("CassAL.IsOnline")]
        public bool IsOnlineCassAL { get; set; }
        [Subscription("CassBL.IsOnline")]
        public bool IsOnlineCassBL { get; set; }
        [Subscription("CassAR.IsOnline")]
        public bool IsOnlineCassAR { get; set; }

        [Subscription("UnLoad.IsOnline")]
        public bool IsOnlineUnLoad { get; set; }
        [Subscription("LoadLock.IsOnline")]
        public bool IsOnlineLL { get; set; }

        [Subscription("TM.IsOnline")]
        public bool IsOnlineTM { get; set; }


        [Subscription("Buffer.IsOnline")]
        public bool IsOnlineBuffer { get; set; }

        [Subscription("WaferRobot.IsOnline")]
        public bool IsOnlineWaferRobot { get; set; }

        [Subscription("TrayRobot.IsOnline")]
        public bool IsOnlineTrayRobot { get; set; }


        [Subscription("PM1.SignalTower.DeviceData")]
        public AITSignalTowerData SignalTowerData { get; set; }

        public string SoftwareVersion
        {
            get;
            set;
        }

        public string RunTime
        {
            get
            {
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }



        [Subscription("System.HasActiveAlarm")]
        public bool SystemHasAlarm { get; set; }

        private AppMenu _alarmMenu;
        private readonly Cali.IEventAggregator _eventAggregator;

        public MainViewModel(Cali.IEventAggregator eventAggregator)
        {
            BaseApp.Instance.Initialize();
            ((SicUI.Client.ClientApp)BaseApp.Instance).ViewModelSwitcher = this;
            this._models = new Dictionary<Type, BaseModel>();
            _eventAggregator = eventAggregator;

            //for login part
            Roles = RoleAccountProvider.Instance.GetRoles();

            EventLogList = new DelayedPresentRollingObservableCollection<EventItem>(1000);
            EventLogsView = CollectionViewSource.GetDefaultView(EventLogList);


            //WarnEventLogList = new ObservableCollection<EventItem>();

            SoftwareVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            EventClient.Instance.OnEvent += Instance_OnEvent;
            EventClient.Instance.OnDisconnectedWithRT += Instance_OnDisconnectedWithRT;
            EventClient.Instance.Start();

            IsPM1Installed = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsPM1Installed");
            IsPM2Installed = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsPM2Installed");
            IsBufferInstalled = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsBufferInstalled");
            IsLLInstalled = (bool)QueryDataClient.Instance.Service.GetConfig("System.SetUp.IsLoadLockInstalled");
            
            Reset();
        }

        private void Instance_OnDisconnectedWithRT()
        {
            MessageBox.Show("Disconnected with RT, UI will exit", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(0);
        }

        public void ShowAlarmEvents()
        {
            AllEventsVisibility = Visibility.Hidden;
            WarnEventsVisibility = Visibility.Visible;
            this.NotifyOfPropertyChange("AllEventsVisibility");
            this.NotifyOfPropertyChange("WarnEventsVisibility");
        }

        public void ShowAllEvents()
        {
            AllEventsVisibility = Visibility.Visible;
            WarnEventsVisibility = Visibility.Hidden;
            this.NotifyOfPropertyChange("AllEventsVisibility");
            this.NotifyOfPropertyChange("WarnEventsVisibility");
        }

        private void Instance_OnEvent(EventItem obj)
        {
            switch (obj.Type)
            {
                case EventType.EventUI_Notify:
                    LogEvent(obj);
                    break;
                case EventType.Dialog_Nofity:
                    //PopDialog(obj);
                    break;
                case EventType.KickOut_Notify:
                    if (obj.Description == "ShutDown")
                    {
                        AccountClient.Instance.Service.LogoutEx(BaseApp.Instance.UserContext.LoginName, BaseApp.Instance.UserContext.LoginId);
                        ShutdownThread = ShutdownExecute;
                        ShutdownThread.BeginInvoke(ShutdownCallBack, ShutdownThread);
                    }
                    break;
                case EventType.Sound_Notify:
                    break;
                case EventType.UIMessage_Notify:
                    //PopUIMessage(obj);
                    break;
            }
        }

        private void LogEvent(EventItem obj)
        {
            if (obj.Type != EventType.EventUI_Notify)
                return;

            EventLogList.Add(obj);

        }

        #region login part
        public void Enter(KeyEventArgs args, string loginName, PasswordBox password, Role role)
        {
            if (args.Key == Key.Enter)
                this.Login(loginName, password, role);
        }

        public void Login(string loginName, PasswordBox password, Role role)
        {
            try
            {

                LoginResult result = AccountClient.Instance.Service.LoginEx(loginName, password.Password, role.RoleID);
                if (result.ActSucc)
                {
                    ClientApp.Instance.UserContext.LoginId = result.SessionId;

                    ClientApp.Instance.UserMode = UserMode.Normal;
                    ClientApp.Instance.UserContext.LoginName = loginName;
                    ClientApp.Instance.UserContext.Role = role;
                    ClientApp.Instance.UserContext.RoleID = role.RoleID;
                    ClientApp.Instance.UserContext.RoleName = role.RoleName;
                    ClientApp.Instance.UserContext.LoginTime = DateTime.Now;
                    //ClientApp.Instance.UserContext.Token = token;
                    ClientApp.Instance.UserContext.LastAccessTime = DateTime.Now;
                    ClientApp.Instance.UserContext.IsLogin = true;

                    //Load menu by role
                    //filer menu if necessary... 
                    ClientApp.Instance.MenuManager.LoadMenu(RoleAccountProvider.Instance.GetMenusByRole(role.RoleID, ClientApp.Instance.MenuLoader.MenuList));

                    IsAutoLogout = role.IsAutoLogout;
                    LogoutTime = role.LogoutTime;

                    IsPermission = RoleAccountProvider.Instance.GetMenuPermission(role.RoleID, "Header") == 3;

                    InitMenu(); //bind menu to main view

                    IsLogin = true; //control the display logic of main view 


                    LOG.Info(string.Format("{0} login as {1}", loginName, role.RoleName));
                }
                else
                {

                    Enum.TryParse(result.Description, out AuthorizeResult errCode);

                    switch (errCode)
                    {
                        case AuthorizeResult.None:
                            DialogBox.ShowError("Not connected with RT.");
                            break;
                        case AuthorizeResult.WrongPwd:
                            DialogBox.ShowError("Invalid password.");
                            break;
                        case AuthorizeResult.HasLogin:
                            DialogBox.ShowError("{0} has already logged in.", loginName);
                            break;
                        case AuthorizeResult.NoMatchRole:
                            DialogBox.ShowError("{0} does not match {1} role.", loginName, role.RoleName);
                            break;
                        case AuthorizeResult.NoMatchUser:
                            DialogBox.ShowError("{0} does not exists.", loginName);
                            break;
                        case AuthorizeResult.NoSession:
                            DialogBox.ShowError("The current session is invalid.");
                            break;
                    }
                }
                password.Clear();
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message, ex);
            }
        }
        #endregion

        public void SetModuleOnline(string module)
        {
            if (MessageBoxResult.Yes == MessageBox.Show($"Set {module} Online ?", "", MessageBoxButton.YesNo, MessageBoxImage.Warning))
            {
                InvokeClient.Instance.Service.DoOperation($"{module}.SetOnline");
            }

        }

        public void SetModuleOffline(string module)
        {
            if (MessageBoxResult.Yes == MessageBox.Show($"Set {module} Offline ?", "", MessageBoxButton.YesNo, MessageBoxImage.Warning))
            {
                InvokeClient.Instance.Service.DoOperation($"{module}.SetOffline");
            }
        }

        public void Logout()
        {
            this.OnLogoutCommand();
        }

        public void OnLogoutCommand()
        {
            WindowManager windowmanager = new WindowManager();
            var logoffViewmodel = new LogoffViewModel();
            windowmanager.ShowDialog(logoffViewmodel);

            BaseApp.Instance.UserMode = logoffViewmodel.DialogResult;

            switch (logoffViewmodel.DialogResult)
            {
                case UserMode.Logoff:
                    Logoff();
                    break;
                case UserMode.Exit:
                    AccountClient.Instance.Service.LogoutEx(BaseApp.Instance.UserContext.LoginName, BaseApp.Instance.UserContext.LoginId);

                    BaseApp.Instance.UserMode = UserMode.Exit;
                    LOG.Info(string.Format("{0} exit as {1}", BaseApp.Instance.UserContext.LoginName, BaseApp.Instance.UserContext.RoleName));

                    this.TryClose();
                    break;
                case UserMode.Shutdown:
                    InvokeClient.Instance.Service.DoOperation("System.ShutDown");
                    break;
            }

            _eventAggregator.PublishOnUIThread(logoffViewmodel.DialogResult);
        }

        public void Logoff()
        {
            BaseApp.Instance.UserMode = UserMode.Logoff;
            if (BaseApp.Instance.UserContext.IsLogin)
            {
                try
                {
                    AccountClient.Instance.Service.LogoutEx(BaseApp.Instance.UserContext.LoginName, BaseApp.Instance.UserContext.LoginId);

                    BaseApp.Instance.UserContext.IsLogin = false;
                    LOG.Info(string.Format("{0} logoff as {1}", BaseApp.Instance.UserContext.LoginName, BaseApp.Instance.UserContext.RoleName));
                }
                catch (Exception exp)
                {
                    LOG.Write(exp);
                }
            }
            IsLogin = false;    //no independent login page
            Roles = RoleAccountProvider.Instance.GetRoles();


            // 如果 ProcessMonitor 打开，则关闭
            if (_eventAggregator?.HandlerExistsFor(typeof(ShowCloseMonitorWinEvent)) == true)
                _eventAggregator?.PublishOnUIThread(new ShowCloseMonitorWinEvent(false));
        }

        public void Reset()
        {
            InvokeClient.Instance.Service.DoOperation("System.Reset");
        }

        public void BuzzerOff()
        {
            InvokeClient.Instance.Service.DoOperation($"PM1.SignalTower.{AITSignalTowerOperation.SwitchOffBuzzer}");
        }

        #region override functions
        public override void CanClose(Action<bool> callback)
        {
            if (BaseApp.Instance.UserMode == UserMode.Normal)
            {
                callback(false);
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate
                {
                    this.OnLogoutCommand();
                });
            }
            else
                callback(true);
        }

        protected override void OnInitialize()
        {
            //display system version or other info...
            this.DisplayName = "Sic";

            base.OnInitialize();
            this.StartTimer();

            DrawSciChart();

            if(Debugger.IsAttached)
            {
                Login("admin", new PasswordBox() { Password = "admin" }, new Role("0", "Manager", false, 1000, ""));
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            this.ShowAllEvents();
            EnableTimer(true);

        }

        void DrawSciChart()
        {
            // Create the chart surface
            var sciChartSurface = new SciChartSurface();

            // Create the X and Y Axis
            var xAxis = new NumericAxis() { AxisTitle = "Number of Samples (per series)" };
            var yAxis = new NumericAxis() { AxisTitle = "Value" };

            sciChartSurface.XAxis = xAxis;
            sciChartSurface.YAxis = yAxis;

            // Specify Interactivity Modifiers
            sciChartSurface.ChartModifier = new ModifierGroup(new RubberBandXyZoomModifier(), new ZoomExtentsModifier());
            // Add annotation hints to the user
            var textAnnotation = new TextAnnotation()
            {
                Text = "Hello World!",
                X1 = 5.0,
                Y1 = 5.0
            };
            sciChartSurface.Annotations.Add(textAnnotation);
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            this._view = view as MainView;
            this._view.tbLoginName.Focus();
            _view.SplashScreen?.Complete();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            EnableTimer(false);
        }
        #endregion

        #region
        #region Sync ShutDown Thread
        public delegate void ShutDownSysncThread();
        ShutDownSysncThread ShutdownThread = null;
        ShutdownViewModel ShutdownWindow = null;

        private void ShutdownExecute()
        {
            BaseApp.Instance.UserMode = UserMode.Shutdown;
            BaseApp.Instance.UserContext.IsLogin = false;
            LOG.Info(string.Format("{0} shutdown as {1}", BaseApp.Instance.UserContext.LoginName, BaseApp.Instance.UserContext.RoleName));

            this.TryClose();
        }

        private void ShutdownCallBack(IAsyncResult result)
        {
            if (ShutdownWindow != null)
            {
                ShutdownWindow.TryClose();
            }
            ShutdownThread.EndInvoke(result);
        }
        #endregion

        #region Menu Control and page switch
        private void InitMenu()
        {
            this.MenuItems = BaseApp.Instance.MenuManager.MenuItems;
            this.SubMenuItems = new List<AppMenu>();
            this.HistoryMenus = new ObservableCollection<AppMenu>();

            if (this.MenuItems.Count > 0)
            {
                AppMenu _default = null;
                foreach (AppMenu menuitem in this.MenuItems)
                {
                    if (menuitem.MenuItems.Count > 0)
                    {
                        if (menuitem.AlarmModule == "System")
                        {
                            _alarmMenu = menuitem;
                            break;
                        }

                        if (_default == null)
                            _default = menuitem.MenuItems[0];
                    }
                }

                this.SwitchMenuItem(_default);
            }
        }

        public void MainSwitchMenuItem(AppMenu menuViewItem)
        {
            if (menuViewItem.MenuItems.Count > 0)
            {
                if (menuViewItem.LastSelectedSubMenu != null)
                    SwitchMenuItem(menuViewItem.LastSelectedSubMenu);
                else
                    SwitchMenuItem(menuViewItem.MenuItems[0]);
            }
        }

        public void SwitchMenuItem(AppMenu menuViewItem)
        {
            if (menuViewItem.ViewModel != null && menuViewItem.ViewModel != string.Empty)
            {
                if (menuViewItem.Model == null)
                {
                    menuViewItem.Model = (BaseModel)AssemblyUtil.CreateInstance(AssemblyUtil.GetType(menuViewItem.ViewModel));
                    ((BaseModel)menuViewItem.Model).Permission = menuViewItem.Permission;
                    ((BaseModel)menuViewItem.Model).Token = BaseApp.Instance.UserContext.Token;

                    if (menuViewItem.Model is ISupportMultipleSystem)
                        (menuViewItem.Model as ISupportMultipleSystem).SystemName = menuViewItem.System;
                }

                this.ActivateItem(((BaseModel)menuViewItem.Model));
                CurrentViewModel = ((BaseModel)menuViewItem.Model);

                //if (((BaseModel)menuViewItem.Model).Page != PageID.MAX_PAGE)
                //    BaseApp.Instance.SetCurrentPage(((BaseModel)menuViewItem.Model).Page);

                this.HandleSubAndHistoryMenu(menuViewItem);

                if (this._currentMenuItem != null)
                {
                    this._currentMenuItem.Selected = false;
                    this._currentMenuItem.Parent.Selected = false;
                }

                menuViewItem.Selected = true;
                menuViewItem.Parent.Selected = true;
                menuViewItem.Parent.LastSelectedSubMenu = menuViewItem;

                this._currentMenuItem = menuViewItem;
            }
        }

        private void HandleSubAndHistoryMenu(AppMenu menuitem)
        {
            this.SubMenuItems = menuitem.Parent.MenuItems;

            if (!this.HistoryMenus.Contains(menuitem))
            {
                if (this.HistoryMenus.Count >= 8)
                    this.HistoryMenus.RemoveAt(7);
                this.HistoryMenus.Insert(0, menuitem);
            }
            else
            {
                this.HistoryMenus.Remove(menuitem);
                this.HistoryMenus.Insert(0, menuitem);
            }
        }

        public bool SwitchPage(string firstLevelMenuID, string secondLevelMenuID)
        {
            foreach (AppMenu menuitem in BaseApp.Instance.MenuManager.MenuItems)
            {
                if (menuitem.MenuID == firstLevelMenuID)
                {
                    foreach (AppMenu menu in menuitem.MenuItems)
                    {
                        if (menu.MenuID == secondLevelMenuID)
                        {
                            SwitchMenuItem(menu);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        #endregion

        #region Refresh Date Time on page

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            if (_alarmMenu != null)
                _alarmMenu.IsAlarm = SystemHasAlarm;
        }
        protected override bool OnTimer()
        {
            try
            {
                base.Poll();
                List<Role> roles = RoleAccountProvider.Instance.GetRoles();
                if (!string.IsNullOrEmpty(ClientApp.Instance.UserContext.RoleName))
                {
                    Role role = roles.Find(x => x.RoleName == ClientApp.Instance.UserContext.RoleName);
                    LogoutTime = role.LogoutTime;
                    IsAutoLogout = role.IsAutoLogout;
                    int intervaltime = GetLastInputTime();
                    //if (System.DateTime.Now >= ClientApp.Instance.UserContext.LoginTime.AddMinutes(LogoutTime) && IsLogin && IsAutoLogout)
                    if (intervaltime >= LogoutTime * 60 && IsLogin && IsAutoLogout)
                        Logoff();
                }

            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }

            return true;
        }


        private void StartTimer()
        {
            System.Windows.Threading.DispatcherTimer myDispatcherTimer =
                new System.Windows.Threading.DispatcherTimer();
            myDispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            myDispatcherTimer.Tick += new EventHandler(Each_Tick);
            myDispatcherTimer.Start();
        }

        public void Each_Tick(object o, EventArgs sender)
        {
            this.NowDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.NotifyOfPropertyChange("NowDateTime");
        }
        #endregion
        #endregion

    }
}

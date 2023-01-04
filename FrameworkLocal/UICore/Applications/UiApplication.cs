using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Aitex.Core.Account;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.UI.MVVM;
using Aitex.Core.UI.View.Common;
using Aitex.Core.UI.View.Frame;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Aitex.Core.WCF;
using Autofac;
using MECF.Framework.UI.Core.Accounts;

namespace MECF.Framework.UI.Core.Applications
{
    public class UiApplication : Singleton<UiApplication>
    {
        public IUiInstance Current
        {
            get { return _instance; }
        }

        public event EventHandler<int> OnSimulationSpeedChanged; 

        public event Action OnWindowsLoaded;

        private IUiInstance _instance;
        private ViewManager _views;
        static ThreadExceptionEventHandler ThreadHandler = new ThreadExceptionEventHandler(Application_ThreadException);
        //private Guid _clientId = new Guid();

        private IContainer container;
        private ContainerBuilder containerBuilder;

        public ContainerBuilder ContainerBuilder
        {
            get => containerBuilder;
        }

        public IContainer Container
        {
            get => container;
        }

        protected System.Windows.Application application { get; set; }

        public UiApplication()
        {
            containerBuilder = new ContainerBuilder();
        }

        public void Initialize(IUiInstance instance)
        {
            application = System.Windows.Application.Current;
            application.DispatcherUnhandledException += OnUnhandledException;

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            //Because this is a static event, you must detach your event handlers when your application is disposed, or memory leaks will result.
            Application.ThreadException += ThreadHandler;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            _instance = instance;

            var asms = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.GlobalAssemblyCache).ToArray();
            containerBuilder.RegisterAssemblyTypes(asms).Where(x =>
            {
                return typeof(IBaseView).IsAssignableFrom(x);
            })
            .PublicOnly()
            .AsSelf();

            containerBuilder.RegisterAssemblyTypes(asms).Where(x =>
            {
                return typeof(IBaseModel).IsAssignableFrom(x);
            })
            .PublicOnly()
            .As(t => t.GetInterfaces().First(x => x != typeof(IBaseModel) && typeof(IBaseModel).IsAssignableFrom(x)));

            Init();
        }

        protected void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                LOG.Write(e.Exception);
                if (e.Exception.InnerException != null)
                    LOG.Write(e.Exception.InnerException);
            }

            e.Handled = true;
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Application.ThreadException -= ThreadHandler;
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowMessageDialog(e.Exception);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowMessageDialog((Exception)e.ExceptionObject);
        }

        static void ShowMessageDialog(Exception ex)
        {
            try
            {
                MessageBox.Show(string.Format("{0} UI Inner Exception, {1}", UiApplication.Instance.Current.SystemName, ex.Message));
            }
            finally
            {
                //Environment.Exit(0);
            }
        }

        public void Terminate()
        {
            EventClient.Instance.Stop();

            Singleton<LogManager>.Instance.Terminate();
        }

        public bool Init()
        {

            try
            {
                Singleton<LogManager>.Instance.Initialize();

                //EventClient.Instance.Init();

                //_event = new UiEvent();

                //SystemConfigManager.Instance.Initialize();

                //

                //SelectCulture(CultureSupported.English);

                //WcfClient.Instance.Initialize();

                //object language;
                //int i = 0;
                //do
                //{
                //    language = WCF.Query.GetConfig(SCName.System_Language);
                //    i++;
                //    if (i == 100)
                //        break;
                //    Thread.Sleep(500);
                //} while (language == null);

                container = containerBuilder.Build();

                _views = new ViewManager()
                {
                    SystemName = _instance.SystemName,
                    SystemLogo = _instance.MainIcon,
                    UILayoutFile = _instance.LayoutFile,
                    MaxSizeShow = _instance.MaxSizeShow,
                };

                _views.OnSimulationSpeedChanged += (sender, speed) =>
                {
                    OnSimulationSpeedChanged?.Invoke(this, speed);
                };

                //SetCulture((language != null && (int)(language) == 2) ? "zh-CN" : "en-US");


                //try
                {
                    _views.OnMainWindowLoaded += views_OnMainWindowLoaded;
                    _views.ShowMainWindow(!_instance.EnableAccountModule);



                    if (_instance.EnableAccountModule)
                    {
                        AccountClient.Instance.Service.RegisterViews(_views.GetAllViewList);
                        if (_views.SystemName == "GonaSorterUI")
                        {
                            GonaMainLogin mainLogin = new GonaMainLogin();

                            if (mainLogin.ShowDialog() == true)
                            {
                                //Account account = SorterUiSystem.Instance.WCF.Account.GetAccountInfo(mainLogin.UserName).AccountInfo;
                                //_views.SetViewPermission(account);
                                Account account = AccountClient.Instance.Service.GetAccountInfo(mainLogin.textBoxUserName.Text).AccountInfo;
                                _views.SetViewPermission(account);
                                _views.MainWindow.Show();
                            }
                        }
                        else
                        {
                            MainLogin mainLogin = new MainLogin();

                            if (mainLogin.ShowDialog() == true)
                            {
                                //Account account = SorterUiSystem.Instance.WCF.Account.GetAccountInfo(mainLogin.UserName).AccountInfo;
                                //_views.SetViewPermission(account);
                                Account account = AccountClient.Instance.Service.GetAccountInfo(mainLogin.textBoxUserName.Text).AccountInfo;
                                _views.SetViewPermission(account);
                                _views.MainWindow.Show();
                            }
                        }
                    }
                }



            }
            catch (Exception ex)
            {
                MessageBox.Show(_instance.SystemName + "Initialize failed, " + ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }


        void views_OnMainWindowLoaded()
        {
            if (OnWindowsLoaded != null)
                OnWindowsLoaded();

        }

        public void Logoff()
        {
            if (Current.EnableAccountModule)
            {
                AccountClient.Instance.Service.Logout(AccountClient.Instance.CurrentUser.AccountId);
            }
            _views.Logoff();
        }

    }
}

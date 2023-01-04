using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.NotifyTrayIcons;
using MECF.Framework.RT.Core.Backend;
using MenuItem = System.Windows.Controls.MenuItem;

namespace MECF.Framework.RT.Core.Applications
{
    public class RtApplication: Singleton<RtApplication>
    {
 
        static ThreadExceptionEventHandler ThreadHandler = new ThreadExceptionEventHandler(Application_ThreadException);

        private ShowWindowNotifyIcon _trayIcon;

        private IRtInstance _instance;

        private static bool _ignoreError;

        public static BackendMainView MainView { get; set; }

        private PassWordView _loginWindow = new PassWordView();

        private static Mutex _mutex;

        protected System.Windows.Application application { get; set; }

        public void Initialize(IRtInstance instance)
        {
            application = System.Windows.Application.Current;
            application.DispatcherUnhandledException += OnUnhandledException;

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            //Because this is a static event, you must detach your event handlers when your application is disposed, or memory leaks will result.
            System.Windows.Forms.Application.ThreadException += ThreadHandler;
            System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            _instance = instance;

            CheckAnotherInstanceRunning(instance.SystemName);

            MainView = new BackendMainView();
            MainView.Icon = _instance.TrayIcon;
			MainView.Title = _instance.SystemName + " Console";

            _loginWindow.Title = _instance.SystemName + " RT Backend Login";
            _loginWindow.Icon = _instance.TrayIcon;
            _ignoreError = _instance.KeepRunningAfterUnknownException;

            if (_instance.EnableNotifyIcon)
            {
                _trayIcon = new ShowWindowNotifyIcon(_instance.SystemName, _instance.TrayIcon);
                var menuItem = new MenuItem();
                menuItem.Header = "Open Logs";
                menuItem.Click += (sender, args) =>
                {
                    // 打开YALV日志管理器。
                    try
                    {
                        // 检查YALV是否已经打开
                        var yalv = Process.GetProcesses().FirstOrDefault(p => p.ProcessName.ToLower().Contains("yalv"));
                        if (yalv != null)
                            throw new InvalidOperationException("日志管理器已经打开。");

                        const string PATH_YALV = @"yalv\yalv.exe";
                        var yalvPath = Path.Combine(Environment.CurrentDirectory, PATH_YALV);

                        // check the existence of the YALV
                        if (File.Exists(yalvPath) == false)
                        {
                            throw new FileNotFoundException("日志管理器可执行文件不存在。");
                        }

                        var logFile = Path.Combine(Environment.CurrentDirectory,
                            $"logs\\log{DateTime.Now:yyyyMMdd}.xlog");

                        var proc = new Process()
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = yalvPath,
                                Arguments = $"\"{logFile}\""
                            }
                        };

                        proc.Start();

                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex);

                        // 此处弹对话框会自动关闭，具体原因参考：
                        // https://stackoverflow.com/questions/576503/how-to-set-wpf-messagebox-owner-to-desktop-window-because-splashscreen-closes-me
                        var tempWindow = new Window()
                        {
                            Visibility = Visibility.Hidden, ShowInTaskbar = false, WindowState = WindowState.Minimized
                        };
                        tempWindow.Show();
                        System.Windows.MessageBox.Show(tempWindow,$"打开日志管理器失败，{ex.Message}", "错误", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        tempWindow.Close();

                    }
                };

                _trayIcon.ContextMenu.Items.Add(menuItem);
				_trayIcon.ExitWindow += TrayIconExitWindow;
				_trayIcon.ShowMainWindow += TrayIconShowMainWindow;
				_trayIcon.ShowBallon(_instance.SystemName, "Start running");
            }

            InitSystem();

            if (_instance.DefaultShowBackendWindow)
            {
                MainView.Show();
            }
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

        void CheckAnotherInstanceRunning(string appName)
        {
            _mutex = new Mutex(true, appName, out bool createNew);

            if (!createNew)
            {
                if (!_mutex.WaitOne(1000))
                {
                    string msg = $"{appName} is already running，can not start again";

                    System.Windows.MessageBox.Show(msg, $"{appName} Launch Failed", System.Windows.MessageBoxButton.OK,  MessageBoxImage.Warning, System.Windows.MessageBoxResult.No,
                        System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                    Environment.Exit(0);
                }
            }
        }

 

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.ThreadException -= ThreadHandler;
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
            LOG.Write(ex);
            if (!_ignoreError)
            {
                string message = string.Format(" RT unknown exception：{0},\n", DateTime.Now);
                System.Windows.MessageBox.Show(message + ex.Message, "Unexpected exception", System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Exclamation,
                    System.Windows.MessageBoxResult.No,
                    System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                //Environment.Exit(0);
            }

        }

        private void TrayIconExitWindow()
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to exit system?", _instance.SystemName, System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Exclamation,
                    System.Windows.MessageBoxResult.No,
                    System.Windows.MessageBoxOptions.DefaultDesktopOnly) == System.Windows.MessageBoxResult.Yes)
            {
                if (_trayIcon != null)
                {
                    _trayIcon.ShowBallon(_instance.SystemName, "Stop running");
                }

                Terminate();
            }
        }

        private void TrayIconShowMainWindow(bool show)
        {
            if (MainView == null)
                return;

            if (show )
            {
                if (!MainView.IsVisible)
                {
                    if (!_loginWindow.IsVisible)
                    {
                        _loginWindow.Reset();

                        _loginWindow.ShowDialog();

                        if (_loginWindow.VerificationResult) 
                            MainView.Show();
                    }
                }
                else
                {
                    MainView.Show();
                }
            }
            else
            {
                MainView.Hide();
            }
        }


        private void InitSystem()
        {
            if (_instance.Loader != null)
            {
                _instance.Loader.Initialize();
            }
        }

        public void Terminate()
        {
            if (_instance.Loader != null)
            {
                _instance.Loader.Terminate();
            }
 
            if (_trayIcon != null)
            {
                _trayIcon.Dispose();
            }
 
            if (MainView != null)
            {
                MainView.Close();
            }

            System.Windows.Application.Current.Shutdown(0);

            Environment.Exit(0);
        }
    }
}

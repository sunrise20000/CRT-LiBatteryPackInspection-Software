using Caliburn.Micro;
using Caliburn.Micro.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using CommandLine;
using MECF.Framework.UI.Client.ClientBase;
using SciChart.Charting.Visuals;
using SicUI.Models.Operations.Overviews;

namespace SicUI.Client
{
    public class Bootstrapper : BootstrapperBase
    {
        #region Variables

        private Splash _splashScreen;

        #endregion

        //static Mutex mutex;
        //bool isNewMutex;
        protected override void OnStartup(object sender, StartupEventArgs e)
        {

            // 解析启动参数
            CommandLine.Parser.Default.ParseArguments<StartupOptions>(e.Args)
                .WithParsed<StartupOptions>(o =>
                {
                    if (o.RtHostIpAddress)
                    {
                        // 启动RT远程主机地址配置窗口

                        _splashScreen?.Complete();

                        try
                        {

                            var clientSection =
                                ConfigurationManager.GetSection("system.serviceModel/client") as ClientSection;
                            var ip =
                                clientSection?.Endpoints.Cast<ChannelEndpointElement>().First().Address.Host;

                            var ipConfigWin = new RtIpAddressInput(ip);
                            var dlg = ipConfigWin.ShowDialog();
                            if (dlg != true)
                            {
                                Environment.Exit(-2);
                            }

                            // 修改app.config中的WCF Host地址。
                            var path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
                                .FilePath; //path to app.Config

                            // 备份文件
                            File.Copy(path, $"{path}.{DateTime.Now.ToString("yyyyMMddHHmmss")}.old");

                            //then:s
                            var doc = XDocument.Load(path);
                            var query = doc.Descendants("endpoint").ToList();

                            const string PATTERN = "(net.tcp:\\/\\/)(.+)(:[0-9]+\\/.+)";

                            foreach (var item in query)
                            {
                                var attr = item.Attribute("address");
                                if (attr == null)
                                    continue;

                                var uri = attr.Value;

                                var isMatch = Regex.IsMatch(uri, PATTERN);
                                if (isMatch == false)
                                    continue;

                                uri = Regex.Replace(uri, PATTERN,
                                    m => m.Groups[1] + ipConfigWin.RtHostAddress + m.Groups[3]);
                                attr.SetValue(uri);
                            }

                            doc.Save(path);

                            var info = $"配置成功，RT主机地址更改为{ipConfigWin.RtHostAddress}。";
                            LOG.Info(info);
                            MessageBox.Show(info, "成功",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                        }
                        catch (Exception ex)
                        {
                            var err = $"无法更改RT主机地址，{ex.Message}";
                            LOG.Error(err, ex);

                            MessageBox.Show(err, "成功",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            Environment.Exit(0);
                        }
                        
                    }
                    else
                    {
                        // 正常启动

#if SHOW_SPLASH_SCREEN

                        #region Show Splash Screen

                        var resetSplashCreated = new ManualResetEvent(false);

                        var splashThread = new Thread(() =>
                        {
                            _splashScreen = new Splash();
                            _splashScreen?.Show();

                            resetSplashCreated.Set();
                            System.Windows.Threading.Dispatcher.Run();
                        });

                        splashThread.SetApartmentState(ApartmentState.STA);
                        splashThread.IsBackground = true;
                        splashThread.Name = "Splash Screen";
                        splashThread.Start();
                        resetSplashCreated.WaitOne();

                        #endregion

#endif

                        try
                        {

                            #region 检查配置，如果连接到远程RT，则不要启动本地RT

                            // 是否连接到本地RT。
                            var isConnectToLocalRt = true;

                            var clientSection =
                                ConfigurationManager.GetSection("system.serviceModel/client") as ClientSection;
                            var hosts =
                                clientSection?.Endpoints.Cast<ChannelEndpointElement>()
                                    .GroupBy(x => x.Address.IsLoopback)
                                    .ToList();

                            // 获取 WCF Host IP地址失败。
                            if (hosts == null)
                            {
                                _splashScreen?.ShowErrorMessageBox(
                                    "Unable to get the ip address of host which is running SicRT, please check the settings in app.config.");
                                Environment.Exit(-1);
                                return;
                            }

                            // 连接到多个WCF Host
                            if (hosts.Count != 1)
                            {
                                _splashScreen?.ShowErrorMessageBox(
                                    "It seems that you are trying to connect multiple SicRT hosts, please check the settings in app.config.");
                                Environment.Exit(-1);
                                return;
                            }

                            var isLoopback = hosts[0].Key;
                            if (!isLoopback)
                                isConnectToLocalRt = false;

                            var hostEndpoint = clientSection.Endpoints.Cast<ChannelEndpointElement>().First();

                            #endregion

                            _splashScreen?.SetMessage1(
                                $"Connecting to SicRT ({hostEndpoint.Address.Host}), please wait ...");

                            if (isConnectToLocalRt)
                            {
                                // 如果WCF的Client IP中存在本机地址，则启动本地RT
                                var processRt = Process.GetProcesses()
                                    .FirstOrDefault(x => x.ProcessName.Contains("SicRT"));
                                if (processRt == null)
                                {
                                    // 如果SicRT没有启动，则启动之。。
                                    var processModule = Process.GetCurrentProcess().MainModule;
                                    if (processModule != null)
                                    {
                                        var exeFilePath = processModule.FileName;

                                        if (!Debugger.IsAttached)
                                        {
                                            exeFilePath = exeFilePath.Replace("SicUI", "SicRT");
                                            Process.Start(exeFilePath);
                                            Thread.Sleep(1000);
                                        }
                                    }
                                }
                            }


                            //if (!EventClient.Instance.ConnectRT())
                            //{
                            //    _splashScreen?.Complete();
                            //    MessageBox.Show("Can not connect with RT, launch RT first", "Error",
                            //        MessageBoxButton.OK, MessageBoxImage.Error);
                            //    Environment.Exit(0);
                            //    return;
                            //}
                        }
                        catch (Exception ex)
                        {
                            _splashScreen?.Complete();
                            MessageBox.Show($"Unable to start SicUI, {ex.Message}", "Error", MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            Environment.Exit(0);
                            return;
                        }

                        _splashScreen?.SetMessage1("Initialize logging system ...");
                        Singleton<Aitex.Core.RT.Log.LogManager>.Instance.Initialize();

                        _splashScreen?.SetMessage1("Initialize sub modules ...");
                        BaseApp.Instance = new ClientApp();

                        _splashScreen?.SetMessage1("Loading the window ...");

                        var dictArguments = new Dictionary<string, object>
                        {
                            { nameof(MainView.SplashScreen), _splashScreen }
                        };

                        DisplayRootViewFor<MainViewModel>(dictArguments);

                        base.OnStartup(sender, e);
                    }
                })
                .WithNotParsed(errs =>
                {

                });
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            base.OnExit(sender, e);

 
                Singleton<Aitex.Core.RT.Log.LogManager>.Instance.Terminate();
 
                Application.Current.Shutdown();
 
        }

        protected override void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                LOG.Write(e.Exception);
                if (e.Exception.InnerException != null)
                    LOG.Write(e.Exception.InnerException);
            }

            e.Handled = true;
        }

        private SimpleContainer _container;

        public Bootstrapper()
        {
            SciChartSurface.SetRuntimeLicenseKey("vwAIyu6FysOMMV/v7pag3WEENpK0hDMQx5zbMKYtlb3PMTsV9R+v2toHl/Z2YJ7t/MDVkKCBsZjBxOUs1djpkyLqgCFMlX5DMrKFdP82QRqZRCOht0hQjW5Omy5z9ZRbalxSx4mlgnL/YXWr2JQrD1dWoTeoDP7xm8JB3+KZ4M5pqxeUCib6VvRpfq3O7HVIyFfcDk0JVByjV+vtgGpOo5RP630lKr9VLS3CPk1aUeul4XQAnJX+IafLnsgSKiDWlZMYU9qJehqA1EdwOnEvvOwhcwckJ5/BoeQ0qDvDaYZ1Jfzkcv5sqOYKd749TJ8wsoTDubT/bLv+BwBiXura1mBZlOIE9zB5XwJVedWWzi6dGDG8LRRKh1XjuyD6V92G596xYsb5b8EJJ8AkgsC/R+fLeN/FIZBlq/Vepg+1dwkLlCCtp8nZWBjsRDQWNrG6Vyk5TF2RzI62WrwKfWfNWXtC5wjkJE5IJzUFlj/B/UhCzB8=");
            //SciChartSurface.SetRuntimeLicenseKey("iNdmdnTAr7p4GuRZoPMZICrdj6PpsIWXoGi7ZLAYWOCkuNBA46oNU8KcfUyWnmYo2XPAKXlp8bFIXuZEDPPMaS/9p6ZMX8CHrSLfoRsWY9wYfr/ZSBgi8Tl+XJyaGcNRLqT1RSP52K7KrJZ/3LEZsLoq36w7A1n+T1sndyl6pmhcv5wyDR0sFmVH1VgO+9Ksj++8s+nzm1br7vXziPyiF7K3RlDI51eY94ffu2X1DhKhgc3zMZCYr0AJ09xLl5ksf3qMAo2/vbjuWB85AugU3XtJ4807dr/I9eDJows8le6E7rw5hIZ3tOs11ShpJOe0VToJi/ILKvq6ZyAFmzhjt+MeEbxFJ1gM4ycTQKt1cN/BKuOO8E/W8mQRx/CQaWtuOfie6TNqcxVxyzFYsfC0eFtvkGMk8xFnnkJVDAshQriXnM3RMVhQOkSbEw4Vsgk/3trJgwPWG05HcEhX9MHaDxJoQe2W2RbyswUktkgvBjQsS06ufQUioaiAlntIg9HXpqUBdia7eD7pUep6wsSgEPz7jStJo6h8+iY/INzf5qvB/0giwf31P+fHdXtwfSWVTbjrbRNWJawpPOI7mZbW/tD/6QGxLA==");

            Initialize();
        }

        protected override void Configure()
        {
            _container = new SimpleContainer();

            _container.Singleton<IEventAggregator, EventAggregator>();
            _container.Singleton<IWindowManager, WindowManager>();
            //container.Singleton<IPublisher, Publisher>();

            _container.PerRequest<MainViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            return _container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }
    }
}

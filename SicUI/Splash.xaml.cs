using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SicUI
{
    /// <summary>
    /// Interaction logic for Splash.xaml
    /// </summary>
    public partial class Splash : Window
    {
        #region Variables

        private readonly Stopwatch _sw = new Stopwatch();
        private readonly CancellationTokenSource _cts;

        #endregion

        public Splash()
        {
            InitializeComponent();

            #region The code use to count the load time of the application

            _sw.Start();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            Task.Run(async () =>
            {
                while (true)
                {
                    Dispatcher.Invoke(() =>
                    {
                        //txtStartupTimeMeasure.Text = $"{_sw.Elapsed.TotalSeconds:F0}s";
                    });

                    await Task.Delay(500);

                    if (ct.IsCancellationRequested)
                        break;

                    if (_sw.Elapsed.TotalMinutes > 2) // be sure the thread can be exited if error occurs.
                        break;
                }
            });

            #endregion
        }

        #region Methods

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            txtVersion.Text = $"v{version}";
        }

        public void SetMessage1(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtMessage1.Text = message;
            });
        }

        public void SetMessage2(string message)
        {
            Dispatcher.Invoke(() =>
            {
                
            });
        }

        public void ShowErrorMessageBox(string message, string title = "Error")
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        public void HideMe()
        {
            Dispatcher.Invoke(() =>
            {
                Visibility = Visibility.Hidden;
            });
        }

        public void ShowMe()
        {
            Dispatcher.Invoke(() =>
            {
                Visibility = Visibility.Visible;
            });
        }

        public void Complete()
        {
            _cts.Cancel();
            Dispatcher.InvokeShutdown();
        }

        #endregion
    }
}

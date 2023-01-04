using System.Windows;
using MECF.Framework.RT.Core.Applications;
using SorterRT.Modules;

namespace SicRT.Instances
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            RtApplication.Instance.Initialize(new RtInstance());
        }

        protected override void OnExit(ExitEventArgs e)
        {
            RtApplication.Instance.Terminate();

            base.OnExit(e);
        }
    }
}

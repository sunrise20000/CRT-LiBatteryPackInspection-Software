using System.Windows;
using SicSimulator.Instances;
using MECF.Framework.UI.Core.Applications;

namespace SicSimulator
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            UiApplication.Instance.Initialize(new UiInstance());
            UiApplication.Instance.OnSimulationSpeedChanged += delegate(object sender, int speed)
            {
                SimulatorSystem.Instance.SetSimulationSpeed(speed);
            };

            SimulatorSystem.Instance.Initialize();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            UiApplication.Instance.Terminate();

            SimulatorSystem.Instance.Terminate();

            base.OnExit(e);
        }
    }
}

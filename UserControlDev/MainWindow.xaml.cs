using Crt.UiCore.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace UserControlDev
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Yamaha机械手动画测试
            Task.Run(async () =>
            {
                await Task.Delay(1000);

                while (true)
                {
                    try
                    {
                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Standby}");
                        Invoke(() =>
                        {
                            RobotYamaha.CurrentPosition = (int)YamahaRobot600.Positions.Standby;
                            RobotYamaha.HasBattery = false;
                        });

                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.FeederA}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot600.Positions.FeederA);
                        await Task.Delay(500);
                        Invoke(() => RobotYamaha.HasBattery = true);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Standby}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot600.Positions.Standby);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Station1A}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot600.Positions.Station1A);
                        await Task.Delay(500);
                        Invoke(() => RobotYamaha.HasBattery = false);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Standby}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot600.Positions.Standby);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.FeederB}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot600.Positions.FeederB);
                        await Task.Delay(500);
                        Invoke(() => RobotYamaha.HasBattery = true);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Standby}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot600.Positions.Standby);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Station1B}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot600.Positions.Station1B);
                        await Task.Delay(500);
                        Invoke(() => RobotYamaha.HasBattery = false);
                        await Task.Delay(500);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            });

            // 直线电机机械手动画测试
            Task.Run(async () =>
            {
                await Task.Delay(1000);

                while (true)
                {
                    try
                    {
                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Standby}");
                        Invoke(() =>
                        {
                            LinearMotor.CurrentPosition = (int)YamahaRobot600.Positions.Standby;
                            LinearMotor.HasBattery = false;
                        });

                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {LiBattMoverTopView.Positions.Standby}");
                        Invoke(() => LinearMotor.CurrentPosition = (int)YamahaRobot600.Positions.Standby);
                        await Task.Delay(500);
                        Invoke(() => LinearMotor.HasBattery = true);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {LiBattMoverTopView.Positions.End}");
                        Invoke(() => LinearMotor.CurrentPosition = (int)LiBattMoverTopView.Positions.End);
                        await Task.Delay(500);
                        Invoke(() => LinearMotor.HasBattery = false);
                        await Task.Delay(500);


                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            });
        }

        private void Invoke(Action action)
        {
            Dispatcher.Invoke(action);
        }
    }
}

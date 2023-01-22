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

            Task.Run(async () =>
            {
                await Task.Delay(1000);

                while (true)
                {
                    try
                    {
                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Standby}");
                        Invoke(()=>
                        {
                            robotYamaha.CurrentPosition = YamahaRobot600.Positions.Standby;
                            robotYamaha.HasBattery = false;
                        });
                        
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.FeederA}");
                        Invoke(() => robotYamaha.CurrentPosition = YamahaRobot600.Positions.FeederA);
                        await Task.Delay(500);
                        Invoke(()=> robotYamaha.HasBattery = true);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Standby}");
                        Invoke(() => robotYamaha.CurrentPosition = YamahaRobot600.Positions.Standby);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Station1A}");
                        Invoke(() => robotYamaha.CurrentPosition = YamahaRobot600.Positions.Station1A);
                        await Task.Delay(500);
                        Invoke(() => robotYamaha.HasBattery = false);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Standby}");
                        Invoke(() => robotYamaha.CurrentPosition = YamahaRobot600.Positions.Standby);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.FeederB}");
                        Invoke(() => robotYamaha.CurrentPosition = YamahaRobot600.Positions.FeederB);
                        await Task.Delay(500);
                        Invoke(() => robotYamaha.HasBattery = true);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Standby}");
                        Invoke(() => robotYamaha.CurrentPosition = YamahaRobot600.Positions.Standby);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot600.Positions.Station1B}");
                        Invoke(() => robotYamaha.CurrentPosition = YamahaRobot600.Positions.Station1B);
                        await Task.Delay(500);
                        Invoke(() => robotYamaha.HasBattery = false);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Move to {YamahaRobot600.Positions.Standby}");
            robotYamaha.CurrentPosition = YamahaRobot600.Positions.Standby;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Move to {YamahaRobot600.Positions.FeederA}");
            robotYamaha.CurrentPosition = YamahaRobot600.Positions.FeederA;
        }
    }
}

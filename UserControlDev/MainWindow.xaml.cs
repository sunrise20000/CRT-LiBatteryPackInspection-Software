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
                await Task.Delay(3000);

                while (true)
                {
                    try
                    {
                        Debug.WriteLine($"Move to {YamahaRobot.Positions.System}");
                        Invoke(()=> robotYamaha.CurrentPosition = YamahaRobot.Positions.System);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot.Positions.FeederA}");
                        Invoke(() => robotYamaha.CurrentPosition = YamahaRobot.Positions.FeederA);
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
            Debug.WriteLine($"Move to {YamahaRobot.Positions.System}");
            robotYamaha.CurrentPosition = YamahaRobot.Positions.System;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Move to {YamahaRobot.Positions.FeederA}");
            robotYamaha.CurrentPosition = YamahaRobot.Positions.FeederA;
        }
    }
}

﻿using Crt.UiCore.Controls;
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

               // return Task.CompletedTask;
                await Task.Delay(1000);

                while (true)
                {
                    try
                    {
                        Debug.WriteLine($"Move to {YamahaRobot800.Positions.Standby}");
                        Invoke(() =>
                        {
                            RobotYamaha.CurrentPosition = (int)YamahaRobot800.Positions.Standby;
                            RobotYamaha.HasBattery = false;
                        });

                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot800.Positions.FeederA}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot800.Positions.FeederA);
                        await Task.Delay(500);
                        Invoke(() => RobotYamaha.HasBattery = true);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot800.Positions.Standby}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot800.Positions.Standby);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot800.Positions.Station1A}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot800.Positions.Station1A);
                        await Task.Delay(500);
                        Invoke(() => RobotYamaha.HasBattery = false);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot800.Positions.Standby}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot800.Positions.Standby);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot800.Positions.FeederB}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot800.Positions.FeederB);
                        await Task.Delay(500);
                        Invoke(() => RobotYamaha.HasBattery = true);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot800.Positions.Standby}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot800.Positions.Standby);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {YamahaRobot800.Positions.Station1B}");
                        Invoke(() => RobotYamaha.CurrentPosition = (int)YamahaRobot800.Positions.Station1B);
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
                //return Task.CompletedTask;
                await Task.Delay(1000);

                while (true)
                {
                    try
                    {
                        Debug.WriteLine($"Move to {LinearMotor.Positions.Standby}");
                        Invoke(() =>
                        {
                            LinearMotor.BatteryView = LinearMotor.BatteryViewTypes.Top;
                            LinearMotor.CurrentPosition = (int)LinearMotor.Positions.Standby;
                            LinearMotor.HasBattery = false;
                        });

                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {LinearMotor.Positions.Standby}");
                        Invoke(() => LinearMotor.CurrentPosition = (int)LinearMotor.Positions.Standby);
                        await Task.Delay(500);
                        Invoke(() => LinearMotor.HasBattery = true);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {LinearMotor.Positions.End}");
                        Invoke(() => LinearMotor.CurrentPosition = (int)LinearMotor.Positions.End);
                        await Task.Delay(500);
                        Invoke(() => LinearMotor.HasBattery = false);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {LinearMotor.Positions.Standby}");
                        Invoke(() => LinearMotor.CurrentPosition = (int)LinearMotor.Positions.Standby);
                        Invoke(() => LinearMotor.BatteryView = LinearMotor.BatteryViewTypes.Front);
                        await Task.Delay(500);
                        Invoke(() => LinearMotor.HasBattery = true);
                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {LinearMotor.Positions.EndWithRotation}");
                        Invoke(() => LinearMotor.CurrentPosition = (int)LinearMotor.Positions.EndWithRotation);
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

            // Feeder动画测试
            Task.Run(async () =>
            {
                await Task.Delay(1000);

                while (true)
                {
                    try
                    {
                        Debug.WriteLine($"Move to {Feeder.Positions.Standby}");
                        Invoke(() =>
                        {
                            Feeder.CurrentPosition = (int)Feeder.Positions.Standby;
                            Feeder.HasBattery = false;
                        });

                        await Task.Delay(500);

                        Debug.WriteLine($"Move to {Feeder.Positions.End}");
                        Invoke(() => Feeder.CurrentPosition = (int)Feeder.Positions.End);
                        await Task.Delay(500);
                        Invoke(() => Feeder.HasBattery = true);
                        await Task.Delay(500);


                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            });
            
            // Flip Stage测试
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        Invoke(()=> FlippingStage.HasBattery = true);
                        await Task.Delay(500);
                        Invoke(() => FlippingStage.BatteryView = BatteryFlipStage.BatteryViewTypes.Top);
                        await Task.Delay(1000);
                        Invoke(() => FlippingStage.BatteryView = BatteryFlipStage.BatteryViewTypes.Front);
                        await Task.Delay(500);
                        Invoke(() => FlippingStage.HasBattery = false);
                        await Task.Delay(1000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    
                }
            });

            // NG Conveyor测试
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        Invoke(() => ngConveyor.AddBattery());
                        await Task.Delay(1000);
                        Invoke(() => ngConveyor.AddBattery());
                        await Task.Delay(1000);
                        Invoke(() => ngConveyor.AddBattery());
                        await Task.Delay(1000);
                        Invoke(() => ngConveyor.AddBattery());
                        await Task.Delay(1000);
                        Invoke(() => ngConveyor.AddBattery());
                        await Task.Delay(1000);

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

﻿using MECF.Framework.Common.CommonData;
using SicUI.Controls.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SicUI.Controls.M2C4Parts
{
    /// <summary>
    /// AtmRobotMultiLP.xaml 的交互逻辑
    /// </summary>
    public partial class AtmRobotMultiLP : UserControl, INotifyPropertyChanged
    {
        private int moveTime = 300;
        private const int AnimationTimeout = 3000; // ms

        private string CurrentPosition
        {
            get; set;
        }

        private RobotAction CurrentAction
        {
            get; set;
        }

        public int MoveTime
        {
            get => moveTime;
            set
            {
                moveTime = value;
            }
        }

        public bool ShowDock
        {
            get { return (bool)GetValue(ShowDockProperty); }
            set { SetValue(ShowDockProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowDock.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowDockProperty =
            DependencyProperty.Register("ShowDock", typeof(bool), typeof(AtmRobotMultiLP), new PropertyMetadata(false));

        // Using a DependencyProperty as the backing store for RotateAngel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RotateAngleProperty =
            DependencyProperty.Register("RotateAngel", typeof(int), typeof(AtmRobotMultiLP), new PropertyMetadata(0));

        public int TranslateX
        {
            get { return (int)GetValue(TranslateXProperty); }
            set { SetValue(TranslateXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TranslateX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TranslateXProperty =
            DependencyProperty.Register("TranslateX", typeof(int), typeof(AtmRobotMultiLP), new PropertyMetadata(0));

        public MECF.Framework.UI.Client.ClientBase.WaferInfo Wafer1
        {
            get { return (MECF.Framework.UI.Client.ClientBase.WaferInfo)GetValue(Wafer1Property); }
            set { SetValue(Wafer1Property, value); }
        }

        // Using a DependencyProperty as the backing store for Wafer1.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Wafer1Property =
            DependencyProperty.Register("Wafer1", typeof(MECF.Framework.UI.Client.ClientBase.WaferInfo), typeof(AtmRobotMultiLP), new PropertyMetadata(null));
        
        //public Visibility IsArmWater => Wafer1 == null ? Visibility.Hidden : Wafer1.IsVisibility;
       
        public MECF.Framework.UI.Client.ClientBase.WaferInfo Wafer2
        {
            get { return (MECF.Framework.UI.Client.ClientBase.WaferInfo)GetValue(Wafer2Property); }
            set { SetValue(Wafer2Property, value); }
        }

        // Using a DependencyProperty as the backing store for Wafer2.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Wafer2Property =
            DependencyProperty.Register("Wafer2", typeof(MECF.Framework.UI.Client.ClientBase.WaferInfo), typeof(AtmRobotMultiLP), new PropertyMetadata(null));

        public string Station
        {
            get { return (string)GetValue(StationProperty); }
            set { SetValue(StationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StationProperty =
            DependencyProperty.Register("Station", typeof(string), typeof(AtmRobotMultiLP), new PropertyMetadata("Robot"));

        public RobotMoveInfo RobotMoveInfo
        {
            get { return (RobotMoveInfo)GetValue(RobotMoveInfoProperty); }
            set { SetValue(RobotMoveInfoProperty, value); }
        }

        public static readonly DependencyProperty RobotMoveInfoProperty =
            DependencyProperty.Register("RobotMoveInfo", typeof(RobotMoveInfo), typeof(AtmRobotMultiLP), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool WaferPresentA
        {
            get { return (bool)GetValue(WaferPresentAProperty); }
            set { SetValue(WaferPresentAProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WaferPresent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WaferPresentAProperty =
            DependencyProperty.Register("WaferPresentA", typeof(bool), typeof(AtmRobotMultiLP), new PropertyMetadata(false));

        public bool WaferPresentB
        {
            get { return (bool)GetValue(WaferPresentBProperty); }
            set { SetValue(WaferPresentBProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WaferPresent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WaferPresentBProperty =
            DependencyProperty.Register("WaferPresentB", typeof(bool), typeof(AtmRobotMultiLP), new PropertyMetadata(false));

        public ICommand CreateDeleteWaferCommand
        {
            get { return (ICommand)GetValue(CreateDeleteWaferCommandProperty); }
            set { SetValue(CreateDeleteWaferCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CreateDeleteWaferCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CreateDeleteWaferCommandProperty =
            DependencyProperty.Register("CreateDeleteWaferCommand", typeof(ICommand), typeof(AtmRobotMultiLP), new PropertyMetadata(null));

        public ICommand MoveWaferCommand
        {
            get { return (ICommand)GetValue(MoveWaferCommandProperty); }
            set { SetValue(MoveWaferCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MoveWaferCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MoveWaferCommandProperty =
            DependencyProperty.Register("MoveWaferCommand", typeof(ICommand), typeof(AtmRobotMultiLP), new PropertyMetadata(null));

        public Dictionary<string, StationPosition> StationPosition
        {
            get { return (Dictionary<string, StationPosition>)GetValue(StationPositionProperty); }
            set { SetValue(StationPositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StationPosition.  This enables animation, styling, binding, etc... new PropertyMetadata(null, StationPositionChangedCallback)
        public static readonly DependencyProperty StationPositionProperty =
            DependencyProperty.Register("StationPosition", typeof(Dictionary<string, StationPosition>), typeof(AtmRobotMultiLP), new PropertyMetadata(null, StationPositionChangedCallback));

        public Visibility HasWafer 
        {
            get { return (Visibility)GetValue(HasWaferProperty); }
            set { SetValue(HasWaferProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasWaferProperty =
            DependencyProperty.Register("HasWafer", typeof(Visibility), typeof(AtmRobotMultiLP), new PropertyMetadata(Visibility.Hidden));

        public Visibility HasTray
        {
            get { return (Visibility)GetValue(HasTrayProperty); }
            set { SetValue(HasTrayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasTrayProperty =
            DependencyProperty.Register("HasTray", typeof(Visibility), typeof(AtmRobotMultiLP), new PropertyMetadata(Visibility.Hidden));

        private List<MenuItem> menu;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<MenuItem> Menu
        {
            get
            {
                return menu;
            }
            set
            {
                menu = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Menu"));
            }
        }

        private ICommand MoveCommand
        {
            get; set;
        }

        private void MoveTo(object target)
        {
            MoveRobot((RobotMoveInfo)target);
        }

        public AtmRobotMultiLP()
        {
            InitializeComponent();
            root.DataContext = this;

            MoveCommand = new RelayCommand(MoveTo);

            CurrentPosition = "ArmA.System";

            canvas1.Rotate(45);
            canvas2.Rotate(120);
            canvas3.Rotate(120);

            StationPosition = new Dictionary<string, StationPosition>
                {
                   { "ArmA.System",new StationPosition()
                        {
                             StartPosition= new RobotPosition()
                             {
                                Z=0,
                                Root=45,
                                Arm=120,
                                Hand=120
                             },
                             EndPosition= new RobotPosition()
                             {
                                Root=45,
                                Arm=120,
                                Hand=120
                             }
                        }
                    },
                    { "ArmA.PM1",new StationPosition()      //ArmA.ReactorA
                        {
                          StartPosition= new RobotPosition()
                             {
                                Z=0,
                                Root = 99,
                                Arm = 240,
                                Hand = 240
                             },
                             EndPosition= new RobotPosition()
                             {
                                Root = 239,
                                Arm = 0,
                                Hand = 360
                             }

                        }
                    },
                    { "ArmA.PM2",new StationPosition()     //ArmA.ReactorB
                        {
                              StartPosition= new RobotPosition()
                             {
                                Z=0,
                                Root = 10,
                                Arm = 194,
                                Hand = 99
                             },
                             EndPosition= new RobotPosition()
                             {
                                Root = -58,
                                Arm = 360,
                                Hand = 0
                             }
                        }
                    },
                      { "ArmA.Buffer",new StationPosition()     //ArmA.Buffer
                        {
                              StartPosition= new RobotPosition()
                             {
                                Z=0,
                                Root = 80,
                                Arm = 190,
                                Hand = 100
                             },
                             EndPosition= new RobotPosition()
                             {
                                Root = 13,
                                Arm = 360,
                                Hand = 0
                             }
                        }
                    },
                      { "ArmA.UnLoad",new StationPosition()     //ArmA.PerHeat
                        {
                              StartPosition= new RobotPosition()
                             {
                                Z=0,
                               Root =27,
                                Arm = 240,
                                Hand = 240
                             },
                             EndPosition= new RobotPosition()
                             {
                                Root = 168,
                                Arm = 0,
                                Hand = 360
                             }
                        }
                    },
                      { "ArmA.LoadLock",new StationPosition()
                        {
                            StartPosition= new RobotPosition()
                             {
                                Z=0,
                                Root =-45,
                                Arm = 240,
                                Hand = 240
                             },
                             EndPosition= new RobotPosition()
                             {
                               Root = 90,
                                 Arm = 0,
                                 Hand = 360
                             }
                        }
                    },
                };
        }

        static void StationPositionChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AtmRobotMultiLP)d;
            var positions = (Dictionary<string, StationPosition>)e.NewValue;
            var menus = new List<MenuItem>();
            foreach (var position in positions)
            {
                var m = new MenuItem() { Header = position.Key };
                Enum.TryParse<RobotArm>(position.Key.Split('.')[0], out RobotArm arm);
                m.Items.Add(new MenuItem() { Header = "Pick", Command = self.MoveCommand, CommandParameter = new RobotMoveInfo() { BladeTarget = position.Key, Action = RobotAction.Picking, ArmTarget = arm } });
                m.Items.Add(new MenuItem() { Header = "Place", Command = self.MoveCommand, CommandParameter = new RobotMoveInfo() { BladeTarget = position.Key, Action = RobotAction.Placing, ArmTarget = arm } });
                m.Items.Add(new MenuItem() { Header = "Move", Command = self.MoveCommand, CommandParameter = new RobotMoveInfo() { BladeTarget = position.Key, Action = RobotAction.Moving, ArmTarget = arm } });
                menus.Add(m);
            }
            self.Menu = menus;
            self.MoveTo(new RobotMoveInfo() { BladeTarget = positions.First().Key, Action = RobotAction.None });
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            if (RobotMoveInfo != null)
            {
                var needMove = CurrentPosition != RobotMoveInfo.BladeTarget || CurrentAction != RobotMoveInfo.Action;

                if (needMove)
                {
                    LogMsg($" RobotMoveInfo, action:{RobotMoveInfo.Action}  armTarget:{RobotMoveInfo.ArmTarget} bladeTarget:{RobotMoveInfo.BladeTarget}");

                    Invoke(() => MoveRobot(RobotMoveInfo));

                    CurrentAction = RobotMoveInfo.Action;
                    CurrentPosition = RobotMoveInfo.BladeTarget;
                }
            }
        }

        private void Invoke(Action action)
        {
            Dispatcher.Invoke(action);
        }

        private void LogMsg(string msg)
        {
            var source = "ATM Robot";
            Console.WriteLine("{0} {1}", source, msg);
        }

        private void MoveRobot(RobotMoveInfo moveInfo)
        {
            canvas1.Stop();
            canvas2.Stop();
            canvas3.Stop();

            //canvas21.Stop();
            //canvas22.Stop();
            //canvas23.Stop();

            var target = moveInfo.BladeTarget;
            var arm = moveInfo.ArmTarget;

            MoveToStart(arm, CurrentPosition
            ,
            () => TranslateTo(target, () => MoveToStart(arm, target, () =>
            {
                if (moveInfo.Action != RobotAction.Moving)
                {
                    MoveToEnd(arm, target, () => UpdateWafer(moveInfo));
                }
            })));
        }

        private void RotateTo(string station, Action onComplete = null)
        {
            var position = StationPosition[station];
            LogMsg($"Rotate to {position.StartPosition.X}");
        }

        private void TranslateTo(string station, Action onComplete = null)
        {
            var position = StationPosition[station];
            LogMsg($"Translate to {position.StartPosition.Z}");
            Translate(StationPosition[CurrentPosition].StartPosition.Z, position.StartPosition.Z, onComplete);
        }

        private void MoveToStart(RobotArm arm, string station, Action onComplete = null)
        {
            LogMsg($"{arm} Move to start {station}");

            var position = StationPosition[station];

            var storyboard = new Storyboard();
            storyboard.Completed += (s, e) => onComplete?.Invoke();

            var needRotate = new List<bool>();

            //if (arm == RobotArm.ArmA)
            {
                needRotate.Add(canvas1.Rotate(storyboard, position.StartPosition.Root, true, MoveTime));
                needRotate.Add(canvas2.Rotate(storyboard, position.StartPosition.Arm, true, MoveTime));
                needRotate.Add(canvas3.Rotate(storyboard, position.StartPosition.Hand, true, MoveTime));
            }
            //else if (arm == RobotArm.ArmB)
            {
                //needRotate.Add(canvas21.Rotate(storyboard, position.StartPosition.Root, true, MoveTime));
                //needRotate.Add(canvas22.Rotate(storyboard, position.StartPosition.Arm, true, MoveTime));
                //needRotate.Add(canvas23.Rotate(storyboard, position.StartPosition.Hand, true, MoveTime));
            }           

            if (needRotate.Any(x => x))
            {
                storyboard.Begin();
            }
            else
            {
                onComplete?.Invoke();
            }

            CurrentPosition = station;
        }

        private void MoveToEnd(RobotArm arm, string station, Action onComplete = null)
        {
            LogMsg($"{arm} move to end {station}");
            
            var position = StationPosition[station];

            var storyboard = new Storyboard();
            storyboard.Completed += (s, e) => onComplete?.Invoke();

            var needRotate = new List<bool>();

            if (arm == RobotArm.ArmA || arm == RobotArm.Both)
            {
                needRotate.Add(canvas1.Rotate(storyboard, position.EndPosition.Root, true, MoveTime));
                needRotate.Add(canvas2.Rotate(storyboard, position.EndPosition.Arm, true, MoveTime));
                needRotate.Add(canvas3.Rotate(storyboard, position.EndPosition.Hand, true, MoveTime));
            }

            if (arm == RobotArm.ArmB || arm == RobotArm.Both)
            {
                //needRotate.Add(canvas21.Rotate(storyboard, position.EndPosition.Root, true, MoveTime));
                //needRotate.Add(canvas22.Rotate(storyboard, position.EndPosition.Arm, true, MoveTime));
                //needRotate.Add(canvas23.Rotate(storyboard, position.EndPosition.Hand, true, MoveTime));
            }

            if (needRotate.Any(x => x))
            {
                storyboard.Begin();
            }
            else
            {
                onComplete?.Invoke();
            }

            CurrentPosition = station;
        }

        private void UpdateWafer(RobotMoveInfo moveInfo)
        {
            var waferPresent = false;
            switch (moveInfo.Action)
            {
                case RobotAction.None:
                case RobotAction.Moving:
                    return;
                case RobotAction.Picking:
                    waferPresent = true;
                    break;
                case RobotAction.Placing:
                    waferPresent = false;
                    break;
                default:
                    break;
            }

            switch (moveInfo.ArmTarget)
            {
                case RobotArm.ArmA:
                    WaferPresentA = waferPresent;
                    break;
                case RobotArm.ArmB:
                    WaferPresentB = waferPresent;
                    break;
                case RobotArm.Both:
                    WaferPresentA = waferPresent;
                    WaferPresentB = waferPresent;
                    break;
                default:
                    break;
            }
        }

        private void Translate(int start, int target, Action onComplete = null)
        {
            AnimationHelper.TranslateX(root, start, target, MoveTime, onComplete);
        }
    }


}

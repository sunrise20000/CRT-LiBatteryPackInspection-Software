using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.UI.Control;
using MECF.Framework.Common.OperationCenter;

namespace Aitex.Core.UI.DeviceControl
{
    /// <summary>
    /// AITThrottleValve.xaml 的交互逻辑
    /// </summary>
    public partial class AITThrottleValve2 : UserControl
    {
        public AITThrottleValve2()
        {
            InitializeComponent();
        }

        public event Action<string, string> clickAct;

        public static readonly DependencyProperty OnOffProperty = DependencyProperty.Register(
       "OnOff", typeof(object), typeof(AITThrottleValve2),
       new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsManualModeProperty = DependencyProperty.Register(
          "IsManualModeValve", typeof(bool), typeof(AITThrottleValve2),
          new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValveNameProperty = DependencyProperty.Register(
          "ValveName", typeof(string), typeof(AITThrottleValve2),
           new FrameworkPropertyMetadata("TV", FrameworkPropertyMetadataOptions.AffectsRender));

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(AITThrottleValve2),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CommandCloseTVProperty = DependencyProperty.Register(
                        "CommandCloseTV", typeof(ICommand), typeof(AITThrottleValve2),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITThrottleValveData), typeof(AITThrottleValve2),
                        new FrameworkPropertyMetadata(new AITThrottleValveData(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HasPermissionProperty = DependencyProperty.Register(
            nameof(HasPermission), typeof(bool), typeof(AITThrottleValve2), new PropertyMetadata(default(bool)));

        public bool HasPermission
        {
            get => (bool)GetValue(HasPermissionProperty);
            set => SetValue(HasPermissionProperty, value);
        }

        public ICommand CommandCloseTV
        {
            get => (ICommand)this.GetValue(CommandCloseTVProperty);
            set => this.SetValue(CommandCloseTVProperty, value);
        }


        public ICommand Command
        {
            get => (ICommand)this.GetValue(CommandProperty);
            set => this.SetValue(CommandProperty, value);
        }

        public bool IsManualModeValve
        {
            get => (bool)GetValue(IsManualModeProperty);
            set => SetValue(IsManualModeProperty, value);
        }


        public string ValveName
        {
            get => (string)GetValue(ValveNameProperty);
            set => SetValue(ValveNameProperty, value);
        }

        /// <summary>
        /// set, get current progress value AnalogDeviceData
        /// </summary>
        public AITThrottleValveData DeviceData
        {
            get => (AITThrottleValveData)this.GetValue(DeviceDataProperty);
            set => this.SetValue(DeviceDataProperty, value);
        }

        public static readonly DependencyProperty PressureStringFormatProperty = DependencyProperty.Register(
           "PressureStringFormat", typeof(string), typeof(AITThrottleValve2),
           new FrameworkPropertyMetadata("F1", FrameworkPropertyMetadataOptions.AffectsRender));

        public string PressureStringFormat
        {
            get => (string)this.GetValue(PressureStringFormatProperty);
            set => this.SetValue(PressureStringFormatProperty, value);
        }

        public static readonly DependencyProperty PositionStringFormatProperty = DependencyProperty.Register(
            "PositionStringFormat", typeof(string), typeof(AITThrottleValve2),
            new FrameworkPropertyMetadata("F1", FrameworkPropertyMetadataOptions.AffectsRender));

        public string PositionStringFormat
        {
            get => (string)this.GetValue(PositionStringFormatProperty);
            set => this.SetValue(PositionStringFormatProperty, value);
        }

        private AITThrottleValveInputDialogBox _dialogBox;

        public Window AnalogOwner { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData != null)
            {
                //if (DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl)
                //{
                //    rotateTransform.Angle = DeviceData.PressureFeedback * 90.0 / DeviceData.MaxValuePressure;
                //}
                //else if (DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl)
                //{
                    rotateTransform.Angle = DeviceData.PositionFeedback * 90.0 / DeviceData.MaxValuePosition;
                //}

                PositionValue.Content = DeviceData.PositionFeedback.ToString(PositionStringFormat);
                PositionUnit.Content = !string.IsNullOrEmpty(DeviceData.UnitPosition) ? DeviceData.UnitPosition : "%"; 
                PressureValue.Content = DeviceData.PressureFeedback.ToString(PressureStringFormat);
                PressureUnit.Content = !string.IsNullOrEmpty(DeviceData.UnitPressure) ? DeviceData.UnitPressure : "mTorr";

                PositionSetValue.Content = DeviceData.PositionSetPointCurrent.ToString(PositionStringFormat);
                PressureSetValue.Content = DeviceData.PressureSetPointCurrent.ToString(PositionStringFormat);

                if (_dialogBox != null)
                {
                    _dialogBox.IsPositionMode = DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl;
                    _dialogBox.IsPressureMode = DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl;

                    //_dialogBox.SetPointPosition = DeviceData.PositionSetPoint;
                    //_dialogBox.SetPointPressure = DeviceData.PressureSetPoint;
                }
            }
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (HasPermission == false)
            {
                ToolTip = null;
                return;
            }

            if (DeviceData != null)
            {
                //string tooltipValue =
                //    string.Format("Mode：{3}\r\n PositionFeedback：{4}% \r\n PositionTargetSetPoint：{5}% \r\n PositionCurrentSetPoint：{6}%\r\n\r\n PressureFeedback：{7} \r\n PressureTargetSetPoint：{8} \r\n PressureCurrentSetPoint：{9}",
                //        DeviceData.DisplayName,
                //        DeviceData.MaxValuePressure,
                //        DeviceData.UnitPressure,
                //        DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl ? "Pressure" : (DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl ? "Position" : ""),

                //        DeviceData.PositionFeedback.ToString("F1"),
                //        DeviceData.PositionSetPoint.ToString("F1"),
                //        DeviceData.PositionSetPointCurrent.ToString("F1"),

                //        DeviceData.PressureFeedback.ToString("F1"),
                //        DeviceData.PressureSetPoint.ToString("F1"),
                //        DeviceData.PressureSetPointCurrent.ToString("F1"));

                string tooltipValue =
                    string.Format("Mode：{0}\r\n PositionTargetSetPoint：{1}% \r\n PressureTargetSetPoint：{2} mbar",
                        DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl ? "Pressure" : (DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl ? "Position" : ""),                       
                        DeviceData.PositionSetPoint.ToString("F1"),
                        DeviceData.PressureSetPoint.ToString("F1"));

                ToolTip = tooltipValue;
            }
        }



        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (HasPermission == false)
                return;

            if (DeviceData == null)
            {
                return;
            }
            else
            {
                if (DeviceData.TVEnable!=true)
                {
                    MessageBox.Show("TV is disable,Click mouse right buttom to turn on!");
                    return;
                }


                _dialogBox = new AITThrottleValveInputDialogBox
                {
                    SetThrottleModeCommandDelegate = SetThrottleModeExecute,
                    SetPressureCommandDelegate = SetPressureExecute,
                    SetPositionCommandDelegate = SetPositionExecute,
                    SetPressureCloseCommandDelegate = SetPressureImmediately,

                    DeviceName = string.Format("{0}: {1}", DeviceData.Type, DeviceData.DisplayName),
                    DeviceId = DeviceData.DeviceSchematicId,

                    SetPointPosition = Math.Round(DeviceData.PositionSetPointCurrent, 1),
                    SetPointPressure = Math.Round(DeviceData.PressureSetPointCurrent, 1),

                    MaxValuePressure = DeviceData.MaxValuePressure,
                    MaxValuePosition = DeviceData.MaxValuePosition,

                    UnitPosition = DeviceData.UnitPosition,
                    UnitPressure = DeviceData.UnitPressure,

                    FeedbackPosition = DeviceData.PositionFeedback,
                    FeedbackPressure = DeviceData.PressureFeedback,

                    IsPositionMode = DeviceData.Mode == (int)PressureCtrlMode.TVPositionCtrl,
                    IsPressureMode = DeviceData.Mode == (int)PressureCtrlMode.TVPressureCtrl,

                };

                if (AnalogOwner != null)
                    _dialogBox.Owner = AnalogOwner;
                _dialogBox.Topmost = true;
                _dialogBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                _dialogBox.FocasAll();
                _dialogBox.ShowDialog();

                _dialogBox = null;
            }
        }

        private void SetThrottleModeExecute(PressureCtrlMode value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetMode}", value.ToString());
        }

        private void SetPressureExecute(double value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetPressure}", (float)value);
        }

        private void SetPositionExecute(double value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITThrottleValveOperation.SetPosition}", (float)value);
        }

        private void SetPressureImmediately(double value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetPositionToZero");
        }


        private void SetPositionToZero()
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetPositionToZero");
        }



        public object OnOff
        {
            get => GetValue(OnOffProperty);
            set => SetValue(OnOffProperty, value);
        }


        private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (HasPermission == false)
                return;

            if (DeviceData.TVEnable == true)
            {
                TvEnable(null,null);
            }
            else
            {
                TvDisEnable(null, null);
            }
        }



        private void TvEnable(object sender, MouseButtonEventArgs e)
        {
            if (!IsManualModeValve)
                return;

            ContextMenu mouseClickMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "_" + ValveName;
            item.IsEnabled = false;
            mouseClickMenu.Items.Add(item);

            addCloseMenu(mouseClickMenu, item);
            //addSetPositionToZeroMenu(mouseClickMenu, item);

            mouseClickMenu.IsOpen = true;

        }

        private void TvDisEnable(object sender, MouseButtonEventArgs e)
        {
            if (!IsManualModeValve)
                return;

            ContextMenu mouseClickMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "_" + ValveName;
            item.IsEnabled = false;
            mouseClickMenu.Items.Add(item);
            addOpenMenu(mouseClickMenu, item);
            //addSetPositionToZeroMenu(mouseClickMenu, item);
            mouseClickMenu.IsOpen = true;

        }

        void addSetPositionToZeroMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "CloseTV";
            item.Click += TurnPositionToZero;
            item.Tag = this.Tag;
            mouseClickMenu.Items.Add(item);
        }

        void addOpenMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "Enable";
            item.Click += TurnOnValve;
            item.Tag = this.Tag;
            mouseClickMenu.Items.Add(item);
        }

        void addCloseMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "Disable";
            item.Tag = this.Tag;
            item.Click += TurnOffValve;
            mouseClickMenu.Items.Add(item);
        }

        private void TurnOnValve(object sender, RoutedEventArgs e)
        {
            if (Command != null || clickAct != null)
            {
                AITValveData deviceData = OnOff as AITValveData;
                if (deviceData != null)
                {
                    deviceData.SetPoint = true;
                }
            }
            if (Command != null)
            {
                KeyValuePair<string, string> pair = new KeyValuePair<string, string>(((MenuItem)e.Source).Tag + "", "true");

                Command.Execute(pair);
            }
            else if (clickAct != null)
            {
                clickAct(((MenuItem)e.Source).Tag + "", "true");
            }
        }

        private void TurnOffValve(object sender, RoutedEventArgs e)
        {
            if (Command != null || clickAct != null)
            {
                AITValveData deviceData = OnOff as AITValveData;
                if (deviceData != null)
                {
                    deviceData.SetPoint = false;
                }
            }
            if (Command != null)
            {
                KeyValuePair<string, string> pair = new KeyValuePair<string, string>(((MenuItem)e.Source).Tag + "", "false");

                Command.Execute(pair);
            }
            else if (clickAct != null)
            {
                clickAct(((MenuItem)e.Source).Tag + "", "false");
            }
        }

        private void TurnPositionToZero(object sender, RoutedEventArgs e)
        {
            SetPositionToZero();
        }
    }
}

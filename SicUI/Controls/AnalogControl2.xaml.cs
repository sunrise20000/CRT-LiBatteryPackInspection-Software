using Aitex.Core.Common.DeviceData;
using Aitex.Core.UI.ControlDataContext;
using Caliburn.Micro;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.Ctrlib.UnitControls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SicUI.Controls
{
    /// <summary>
    /// AnalogControl2.xaml 的交互逻辑
    /// </summary>
    public partial class AnalogControl2 : UserControl
    {

        public AnalogControl2()
        {
            InitializeComponent();

            // LabelSet = "0.0";
        }

        public event Action<string, string> clickAct;

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(AnalogControl2),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
            "DeviceData", typeof(AITMfcData), typeof(AnalogControl2),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender,
                OnDeviceDataChanged));

        public static readonly DependencyProperty OperationNameProperty = DependencyProperty.Register(
            "OperationName", typeof(string), typeof(AnalogControl2),
            new FrameworkPropertyMetadata(AnalogDeviceOperation.Ramp, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackColorProperty = DependencyProperty.Register(
            "BackColor", typeof(Brush), typeof(AnalogControl2),
            new FrameworkPropertyMetadata(Brushes.Green, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HideDialogProperty = DependencyProperty.Register(
            "HideDialog", typeof(bool), typeof(AnalogControl2),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LabelSetProperty = DependencyProperty.Register(
            "LabelSet", typeof(object), typeof(AnalogControl2),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        private static void OnDeviceDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }


        public static readonly DependencyProperty HasPermissionProperty = DependencyProperty.Register(
            nameof(HasPermission), typeof(bool), typeof(AnalogControl2), new PropertyMetadata(default(bool)));

        /// <summary>
        /// 设置或返回是否有权限查看实际值。
        /// <para>该属性通常根据权限进行配置。</para>
        /// </summary>
        public bool HasPermission
        {
            get => (bool)GetValue(HasPermissionProperty);
            set => SetValue(HasPermissionProperty, value);
        }

        public static readonly DependencyProperty DisturbanceProperty = DependencyProperty.Register(
            nameof(Disturbance), typeof(double), typeof(AnalogControl2), new PropertyMetadata(default(double)));

        /// <summary>
        /// 显示值加入扰动，单位%
        /// </summary>
        public double Disturbance
        {
            get { return (double)GetValue(DisturbanceProperty); }
            set { SetValue(DisturbanceProperty, value); }
        }

        /// <summary>
        /// 输入值是否百分比，默认否
        /// </summary>
        public bool IsPercent { get; set; }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// set, get current progress value AnalogDeviceData
        /// </summary>
        public AITMfcData DeviceData
        {
            get => (AITMfcData)GetValue(DeviceDataProperty);
            set => SetValue(DeviceDataProperty, value);
        }

        public Brush BackColor
        {
            get => (Brush)GetValue(BackColorProperty);
            set => SetValue(BackColorProperty, value);
        }

        public string OperationName
        {
            get => (string)GetValue(OperationNameProperty);
            set => SetValue(OperationNameProperty, value);
        }

        public bool HideDialog
        {
            get => (bool)GetValue(HideDialogProperty);
            set => SetValue(HideDialogProperty, value);
        }

        public object LabelSet
        {
            get => (object)GetValue(LabelSetProperty);
            set => SetValue(LabelSetProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            //draw background color
            rectBkground.Fill = BackColor;

            if (DeviceData != null)
            {
                var format = "0.0";

                #region FeedBack

                //draw red board if mfc meets a warning
                rectBkground.Stroke = DeviceData.IsWarning ? Brushes.Red : Brushes.Gray;

                //draw reading value
                var dispFeedback = IsPercent ? DeviceData.FeedBack * 100 : DeviceData.FeedBack;

                // 加入指定的扰动。
                dispFeedback += dispFeedback * (Disturbance / 100);

                if (HasPermission == false)
                    labelValue.Text = dispFeedback <= 0.1 ? "-" : "***"; // 没流量时显示为“-”
                else
                    labelValue.Text = dispFeedback.ToString(format);

                labelValue.Foreground = DeviceData.IsWarning ? Brushes.Pink : Brushes.LightYellow;
                rectBkground.StrokeThickness = DeviceData.IsWarning ? 2 : 1;


                if (dialogBox != null)
                {
                    dialogBox.DeviceData = DeviceData;
                    dialogBox.DeviceData.InvokePropertyChanged();
                }

                #endregion

                #region SetPoint

                //draw red board if mfc meets a warning
                rectSetPoint.Stroke = DeviceData.IsWarning ? Brushes.Red : Brushes.Gray;

                //draw reading value
                var sp = IsPercent ? DeviceData.SetPoint * 100 : DeviceData.SetPoint;
                if (HasPermission == false)
                    labelSetPoint.Text = "***";
                else
                    labelSetPoint.Text = dispFeedback.ToString(format);

                //VerticalContentAlignment = "Center" HorizontalContentAlignment = "Center"
                labelSetPoint.Foreground = DeviceData.IsWarning ? Brushes.Pink : Brushes.LightYellow;
                rectSetPoint.StrokeThickness = DeviceData.IsWarning ? 2 : 1;

                #endregion
            }
        }

        private MfcSettingDialogViewModel dialogBox;

        public Window AnalogOwner { get; set; }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            if (HasPermission == false)
                return;

            if (DeviceData == null)
                return;

            if (HideDialog)
                return;

            if (DeviceData.ActMode != (double)MfcCtrlMode.Normal)
            {
                MessageBox.Show("Not normal mode,Click mouse right buttom to change!");
                return;
            }
            //dialogBox = new InputDialogBox
            //{
            //    CommandDelegate = Execute,
            //    DeviceName = string.Format("{0}: {1}", DeviceData.Type, DeviceData.DisplayName),
            //    DeviceId = DeviceData.DeviceSchematicId,
            //    DefaultValue = DeviceData.DefaultValue,
            //    RealValue = DeviceData.FeedBack.ToString("F1"),
            //    SetPoint = Math.Round(DeviceData.SetPoint, 1),
            //    MaxValue = DeviceData.Scale,
            //    Unit = DeviceData.Unit,
            //};

            //dialogBox.IsPercent = IsPercent;
            //if (IsPercent)
            //    dialogBox.SetPoint = Math.Round(DeviceData.SetPoint * 100.0, 1);
            //if (AnalogOwner != null)
            //    dialogBox.Owner = AnalogOwner;
            //dialogBox.Topmost = true;
            //dialogBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //dialogBox.FocasAll();
            //dialogBox.ShowDialog();

            //dialogBox = null;

            dialogBox = new MfcSettingDialogViewModel($"MFC {DeviceData.DisplayName} Setting");
            dialogBox.DeviceData = DeviceData;
            dialogBox.InputSetPoint = DeviceData.SetPoint.ToString("F1");

            var wm = new WindowManager();

            var owner = Application.Current.MainWindow;
            if (owner != null)
            {
                Mouse.Capture(owner);
                var pointToWindow = Mouse.GetPosition(owner);
                var pointToScreen = owner.PointToScreen(pointToWindow);
                pointToScreen.X = pointToScreen.X + 50;
                pointToScreen.Y = pointToScreen.Y - 150;
                Mouse.Capture(null);

                //wm.ShowDialog(dialogBox, pointToScreen);
                wm.ShowDialog(dialogBox);
            }
            else
            {
                wm.ShowDialog(dialogBox);
            }

        }

        #region MouseRightButton

        private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (HasPermission == false) 
                return;

            var mouseClickMenu = new ContextMenu();
            var item = new MenuItem
            {
                Header = "_" + DeviceData.DeviceName,
                IsEnabled = false
            };
            mouseClickMenu.Items.Add(item);
            addNormalMenu(mouseClickMenu, item);
            addCloseMenu(mouseClickMenu, item);
            addOpenMenu(mouseClickMenu, item);
            addHoldMenu(mouseClickMenu, item);

            mouseClickMenu.IsOpen = true;
        }

        void addNormalMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "Normal";
            item.Click += NormalMode;
            item.Tag = Tag;
            mouseClickMenu.Items.Add(item);
        }

        private void NormalMode(object sender, RoutedEventArgs e)
        {
            SetNormalMode(MfcCtrlMode.Normal);
        }

        void addCloseMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "Close";
            item.Click += CloseMode;
            item.Tag = Tag;
            mouseClickMenu.Items.Add(item);
        }

        private void CloseMode(object sender, RoutedEventArgs e)
        {
            SetCloseMode(MfcCtrlMode.Close);
        }

        void addOpenMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "Open";
            item.Click += OpenMode;
            item.Tag = Tag;
            mouseClickMenu.Items.Add(item);
        }

        private void OpenMode(object sender, RoutedEventArgs e)
        {
            SetOpenMode(MfcCtrlMode.Open);
        }

        void addHoldMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "Hold";
            item.Click += HoldMode;
            item.Tag = Tag;
            mouseClickMenu.Items.Add(item);
        }

        private void HoldMode(object sender, RoutedEventArgs e)
        {
            SetHoldMode(MfcCtrlMode.Hold);
        }

        private void SetNormalMode(MfcCtrlMode value)
        {

            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetMode",
                value.ToString());

        }

        private void SetCloseMode(MfcCtrlMode value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetMode",
                value.ToString());

        }

        private void SetOpenMode(MfcCtrlMode value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetMode",
                value.ToString());

        }

        private void SetHoldMode(MfcCtrlMode value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetMode",
                value.ToString());

        }

        #endregion


        private void Execute(double value)
        {
            Command.Execute(new object[] { DeviceData.UniqueName, OperationName, value });
        }


        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (HasPermission == false)
            {
                ToolTip = null;
                return;
            }

            var format = "0.0";
            if (DeviceData != null)
            {
                var tooltipValue =
                    string.Format(
                        "{0}：{1}\r\n\r\nID：{2}\r\nScale：{3} {4}\r\nSetPoint：{5} {4} \r\nFeedback：{6} {4}\r\nTolerance：{7}%\r\nStatus：{8}",
                        DeviceData.Type,
                        DeviceData.DisplayName,
                        DeviceData.DeviceSchematicId,
                        DeviceData.Scale,
                        DeviceData.Unit,
                        IsPercent
                            ? (DeviceData.SetPoint * 100).ToString(format) + "%"
                            : DeviceData.SetPoint.ToString(format),
                        IsPercent
                            ? (DeviceData.FeedBack * 100).ToString(format) + "%"
                            : DeviceData.FeedBack.ToString(format),
                        DeviceData.Scale > 0
                            ? ((DeviceData.FeedBack - DeviceData.SetPoint) / DeviceData.Scale * 100).ToString(format)
                            : "0",
                        DeviceData.IsWarning ? "Tolerance Warning" : "Normal");

                ToolTip = tooltipValue;
            }

            if (DeviceData != null)
            {
                var tooltipValue =
                    $"{DeviceData.Type}：{DeviceData.DisplayName}\r\n\r\nID：{DeviceData.DeviceSchematicId}\r\nScale：{DeviceData.Scale} {DeviceData.Unit}\r\nSetPoint：{DeviceData.SetPoint.ToString(format)} {DeviceData.Unit} \r\nFeedback：{DeviceData.FeedBack.ToString(format)} {DeviceData.Unit}";

                ToolTip = tooltipValue;
            }
        }
    }
}

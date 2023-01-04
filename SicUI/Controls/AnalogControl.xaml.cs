using Aitex.Core.Common.DeviceData;
using Aitex.Core.UI.ControlDataContext;
using MECF.Framework.Common.OperationCenter;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SicUI.Controls
{
    /// <summary>
    /// AnalogControl.xaml 的交互逻辑
    /// </summary>
    public partial class AnalogControl : UserControl
    {
        public AnalogControl()
        {
            InitializeComponent();
        }

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(AnalogControl),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITPressureMeterData), typeof(AnalogControl),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender,
                        new PropertyChangedCallback(OnDeviceDataChanged)));

        public static readonly DependencyProperty OperationNameProperty = DependencyProperty.Register(
                "OperationName", typeof(string), typeof(AnalogControl),
                new FrameworkPropertyMetadata(AnalogDeviceOperation.Ramp, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackColorProperty = DependencyProperty.Register(
                        "BackColor", typeof(Brush), typeof(AnalogControl),
                         new FrameworkPropertyMetadata(Brushes.Green, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HideDialogProperty = DependencyProperty.Register(
                    "HideDialog", typeof(bool), typeof(AnalogControl),
                    new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        private static void OnDeviceDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// 输入值是否百分比，默认否
        /// </summary>
        public bool IsPercent { get; set; }


        public static readonly DependencyProperty HasPermissionProperty = DependencyProperty.Register(
            nameof(HasPermission), typeof(bool), typeof(AnalogControl), new PropertyMetadata(default(bool)));

        /// <summary>
        /// 设置或返回是否有权限查看实际值。
        /// <para>该属性通常根据权限进行配置。</para>
        /// </summary>
        public bool HasPermission
        {
            get => (bool)GetValue(HasPermissionProperty);
            set => SetValue(HasPermissionProperty, value);
        }


        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue(CommandProperty);
            }
            set
            {
                this.SetValue(CommandProperty, value);
            }
        }

        /// <summary>
        /// set, get current progress value AnalogDeviceData
        /// </summary>
        public AITPressureMeterData DeviceData
        {
            get
            {
                return (AITPressureMeterData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        public Brush BackColor
        {
            get
            {
                return (Brush)this.GetValue(BackColorProperty);
            }
            set
            {
                this.SetValue(BackColorProperty, value);
            }
        }

        public string OperationName
        {
            get
            {
                return (string)this.GetValue(OperationNameProperty);
            }
            set
            {
                this.SetValue(OperationNameProperty, value);
            }
        }

        public bool HideDialog
        {
            get
            {
                return (bool)this.GetValue(HideDialogProperty);
            }
            set
            {
                this.SetValue(HideDialogProperty, value);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            //draw background color
            rectBkground.Fill = BackColor;

            if (DeviceData != null)
            {
                //draw red board if mfc meets a warning
                rectBkground.Stroke = DeviceData.IsWarning ? Brushes.Red : Brushes.Gray;

                //draw reading value
                var feedback = IsPercent ? DeviceData.FeedBack * 100 : DeviceData.FeedBack;
                if (HasPermission == false)
                    labelValue.Text = feedback <= 0.1 ? "-" : "***"; // 没流量时显示为“-”
                else
                    labelValue.Text = feedback.ToString(DeviceData.FormatString);

                labelValue.Foreground = DeviceData.IsWarning ? Brushes.Pink : Brushes.LightYellow;
                rectBkground.StrokeThickness = DeviceData.IsWarning ? 2 : 1;

                if (dialogBox != null)
                {
                    if (IsPercent)
                        dialogBox.RealValue = (DeviceData.FeedBack * 100).ToString(DeviceData.FormatString) + "%";
                    else
                        dialogBox.RealValue = DeviceData.FeedBack.ToString(DeviceData.FormatString);
                }
            }
        }

        private InputDialogBox dialogBox;

        public Window AnalogOwner { get; set; }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (HasPermission == false)
                return;


            if (DeviceData == null)
                return;

            if (HideDialog)
                return;

            if (DeviceData.ActMode != (double)PcCtrlMode.Normal)
            {
                MessageBox.Show("Not normal mode,Click mouse right buttom to change!");
                return;
            }

            dialogBox = new InputDialogBox
            {
                CommandDelegate = Execute,
                DeviceName = string.Format("{0}: {1}", DeviceData.Type, DeviceData.DisplayName),
                DeviceId = DeviceData.DeviceSchematicId,
                DefaultValue = DeviceData.DefaultValue,
                RealValue = DeviceData.FeedBack.ToString(DeviceData.FormatString),
                SetPoint = Math.Round(DeviceData.SetPoint, 1),
                MaxValue = DeviceData.Scale,
                Unit = DeviceData.Unit,
            };

            dialogBox.IsPercent = IsPercent;
            if (IsPercent)
                dialogBox.SetPoint = Math.Round(DeviceData.SetPoint * 100.0, 1);
            if (AnalogOwner != null)
                dialogBox.Owner = AnalogOwner;
            dialogBox.Topmost = true;
            dialogBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialogBox.FocasAll();
            dialogBox.ShowDialog();

            dialogBox = null;
        }

        #region MouseRightButton
        private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (HasPermission == false)
                return;

            if (HideDialog)
                return;
            ContextMenu mouseClickMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "_" + DeviceData.DeviceName;
            item.IsEnabled = false;
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
            item.Tag = this.Tag;
            mouseClickMenu.Items.Add(item);
        }

        private void NormalMode(object sender, RoutedEventArgs e)
        {
            SetNormalMode(PcCtrlMode.Normal);
        }

        void addCloseMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "Close";
            item.Click += CloseMode;
            item.Tag = this.Tag;
            mouseClickMenu.Items.Add(item);
        }

        private void CloseMode(object sender, RoutedEventArgs e)
        {
            SetCloseMode(PcCtrlMode.Close);
        }

        void addOpenMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "Open";
            item.Click += OpenMode;
            item.Tag = this.Tag;
            mouseClickMenu.Items.Add(item);
        }

        private void OpenMode(object sender, RoutedEventArgs e)
        {
            SetOpenMode(PcCtrlMode.Open);
        }

        void addHoldMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "Hold";
            item.Click += HoldMode;
            item.Tag = this.Tag;
            mouseClickMenu.Items.Add(item);
        }

        private void HoldMode(object sender, RoutedEventArgs e)
        {
            SetHoldMode(PcCtrlMode.Hold);
        }

        private void SetNormalMode(PcCtrlMode value)
        {

            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetMode", value.ToString());

        }

        private void SetCloseMode(PcCtrlMode value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetMode", value.ToString());

        }

        private void SetOpenMode(PcCtrlMode value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetMode", value.ToString());

        }

        private void SetHoldMode(PcCtrlMode value)
        {
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.SetMode", value.ToString());

        }
        #endregion


        private void Execute(double value)
        {
            //Command.Execute(new object[] { $"{DeviceData.Module}.{DeviceData.DeviceName}", OperationName, value });
            InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.Ramp", value);
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
                //    string.Format("{0}：{1}\r\n\r\nID：{2}\r\nScale：{3} {4}\r\nSetPoint：{5} {4} \r\nFeedback：{6} {4}\r\nTolerance：{7}%\r\nStatus：{8}",
                //        DeviceData.Type,
                //        DeviceData.DisplayName,
                //        DeviceData.DeviceSchematicId,
                //        DeviceData.Scale,
                //        DeviceData.Unit,
                //        IsPercent ? (DeviceData.SetPoint * 100).ToString(DeviceData.FormatString) + "%" : DeviceData.SetPoint.ToString(DeviceData.FormatString),
                //        IsPercent ? (DeviceData.FeedBack * 100).ToString(DeviceData.FormatString) + "%" : DeviceData.FeedBack.ToString(DeviceData.FormatString),
                //        DeviceData.Scale > 0 ? ((DeviceData.FeedBack - DeviceData.SetPoint) / DeviceData.Scale * 100).ToString(DeviceData.FormatString) : "0",
                //        DeviceData.IsWarning ? "Tolerance Warning" : "Normal");

                //ToolTip = tooltipValue;

                string tooltipValue =
                    string.Format("Scale：{3} {4}\r\nSetPoint：{5} {4} \r\nFeedback：{6} {4} \r\nOpenDegree:  {9}%",
                        DeviceData.Type,
                        DeviceData.DisplayName,
                        DeviceData.DeviceSchematicId,
                        DeviceData.Scale,
                        DeviceData.Unit,
                        IsPercent ? (DeviceData.SetPoint * 100).ToString(DeviceData.FormatString) + "%" : DeviceData.SetPoint.ToString(DeviceData.FormatString),
                        IsPercent ? (DeviceData.FeedBack * 100).ToString(DeviceData.FormatString) + "%" : DeviceData.FeedBack.ToString(DeviceData.FormatString),
                        DeviceData.Scale > 0 ? ((DeviceData.FeedBack - DeviceData.SetPoint) / DeviceData.Scale * 100).ToString(DeviceData.FormatString) : "0",
                        DeviceData.IsWarning ? "Tolerance Warning" : "Normal",
                        DeviceData.OpenDegree.ToString("0.0"));

                ToolTip = tooltipValue;
            }
        }
    }
}

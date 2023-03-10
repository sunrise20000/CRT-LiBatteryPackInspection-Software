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
using Aitex.Core.UI.ControlDataContext;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// RFGenerator.xaml 的交互逻辑
    /// </summary>
    public partial class RFGenerator : UserControl
    {
        public RFGenerator()
        {
            InitializeComponent();
        }

        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(RFGenerator),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(RfItem), typeof(RFGenerator),
                        new FrameworkPropertyMetadata(new RfItem(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OperationNameProperty = DependencyProperty.Register(
                "OperationName", typeof(string), typeof(RFGenerator),
                new FrameworkPropertyMetadata(AnalogDeviceOperation.Ramp, FrameworkPropertyMetadataOptions.AffectsRender));

        //public static readonly DependencyProperty BackColorProperty = DependencyProperty.Register(
        //                "BackColor", typeof(Brush), typeof(RFGenerator),
        //                 new FrameworkPropertyMetadata(Brushes.Green, FrameworkPropertyMetadataOptions.AffectsRender));


        /// <summary>
        /// 输入值是否百分比，默认否
        /// </summary>
        public bool IsPercent { get; set; }

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
        public RfItem DeviceData
        {
            get
            {
                return (RfItem)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        //public Brush BackColor
        //{
        //    get
        //    {
        //        return (Brush)this.GetValue(BackColorProperty);
        //    }
        //    set
        //    {
        //        this.SetValue(BackColorProperty, value);
        //    }
        //}

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

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData != null)
            {
                //draw red board if mfc meets a warning
                rectBkground.Stroke = DeviceData.IsWarning ? Brushes.Red : Brushes.Gray;

                labelValue.Foreground = DeviceData.IsWarning ? Brushes.Pink : Brushes.MidnightBlue;
                rectBkground.StrokeThickness = DeviceData.IsWarning ? 2 : 1;

                if (dialogBox != null)
                {
                    if (IsPercent)
                        dialogBox.RealValue = (DeviceData.FeedBack * 100).ToString("F1") + "%";
                    else
                        dialogBox.RealValue = DeviceData.FeedBack.ToString("F1");
                }
            }
        }

        private InputDialogBox dialogBox;

        public Window AnalogOwner { get; set; }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DeviceData == null)
                return;

            dialogBox = new InputDialogBox
            {
                CommandDelegate = Execute,
                DeviceName = string.Format("{0}: {1}", DeviceData.Type, DeviceData.DisplayName),
                DeviceId = DeviceData.DeviceId,
                DefaultValue = DeviceData.DefaultValue,
                RealValue = DeviceData.FeedBack.ToString("F1"),
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

        private void Execute(double value)
        {
            Command.Execute(new object[] { DeviceData.DeviceName, OperationName, value });
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DeviceData != null)
            {
                string tooltipValue =
                    string.Format(@"{0}：{1}\r\n\r\nID：{2}\r\nScale：{3} {4}\r\nSetpoint：{5} {4} \r\nFeedback：{6} {4}\r\nTolerance：{7}%\r\nStatus：{8}",
                        DeviceData.Type,
                        DeviceData.DisplayName,
                        DeviceData.DeviceId,
                        DeviceData.Scale, 
                        DeviceData.Unit,
                        IsPercent ? (DeviceData.SetPoint * 100).ToString("F1") + "%" : DeviceData.SetPoint.ToString("F1"),
                        IsPercent ? (DeviceData.FeedBack * 100).ToString("F1") + "%" : DeviceData.FeedBack.ToString("F1"),
                        DeviceData.Scale > 0 ? ((DeviceData.FeedBack - DeviceData.SetPoint) / DeviceData.Scale * 100).ToString("F1") : "0",
                        DeviceData.IsWarning ? "Tolerance Warning" : "Normal");

                ToolTip = tooltipValue;
            }
        }
    }
}

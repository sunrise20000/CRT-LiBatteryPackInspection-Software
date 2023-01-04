using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.OperationCenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SicUI.Controls
{
    /// <summary>
    /// AITPumpDouble.xaml 的交互逻辑
    /// </summary>
    public partial class AITPumpDouble : UserControl
    {
        public AITPumpDouble()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DeviceDataMpProperty = DependencyProperty.Register(
               "DeviceDataMp", typeof(AITPumpData), typeof(AITPumpDouble),
               new FrameworkPropertyMetadata(new AITPumpData(), FrameworkPropertyMetadataOptions.AffectsRender));
        public AITPumpData DeviceDataMp
        {
            get
            {
                return (AITPumpData)this.GetValue(DeviceDataMpProperty);
            }
            set
            {
                this.SetValue(DeviceDataMpProperty, value);
            }
        }


        public static readonly DependencyProperty DeviceDataBpProperty = DependencyProperty.Register(
               "DeviceDataBp", typeof(AITPumpData), typeof(AITPumpDouble),
               new FrameworkPropertyMetadata(new AITPumpData(), FrameworkPropertyMetadataOptions.AffectsRender));
        public AITPumpData DeviceDataBp
        {
            get
            {
                return (AITPumpData)this.GetValue(DeviceDataBpProperty);
            }
            set
            {
                this.SetValue(DeviceDataBpProperty, value);
            }
        }

        bool EnableControl = true;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceDataMp != null)
            {
                if (DeviceDataMp.IsError || DeviceDataMp.IsOverLoad)
                {
                    imageMP.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/p3.png"));
                    lableMp.Content = "Error";
                }
                else if (DeviceDataMp.IsWarning)
                {
                    imageMP.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/p5.png"));
                    lableMp.Content = "Warning";
                }
                else if (DeviceDataMp.IsOn)
                {
                    imageMP.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/p4.png"));
                    lableMp.Content = "On";
                }
                else
                {
                    imageMP.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/p2.png"));
                    lableMp.Content = "Off";
                }

                this.MuneMpOn.IsEnabled = !DeviceDataMp.IsOn;
                this.MuneMpOff.IsEnabled = DeviceDataMp.IsOn;
            }
            else
            {
                this.MuneMpOn.Visibility = Visibility.Collapsed;
                this.MuneMpOff.Visibility = Visibility.Collapsed;
                this.lableMp.Visibility = Visibility.Collapsed;
            }

            if (DeviceDataBp != null)
            {

                if (DeviceDataBp.IsError || DeviceDataBp.IsOverLoad)
                {
                    imageBP.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/p3.png"));
                    lableBp.Content = "Error";
                }
                else if (DeviceDataBp.IsWarning)
                {
                    imageBP.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/p5.png"));
                    lableBp.Content = "Warning";
                }
                else if (DeviceDataBp.IsOn)
                {
                    imageBP.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/p4.png"));
                    lableBp.Content = "On";
                }
                else
                {
                    imageBP.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/p2.png"));
                    lableBp.Content = "Off";
                }

                this.MuneBpOn.IsEnabled = !DeviceDataBp.IsOn;
                this.MuneBpOff.IsEnabled = DeviceDataBp.IsOn;
            }
            else
            {
                this.MuneBpOn.Visibility = Visibility.Collapsed;
                this.MuneBpOff.Visibility = Visibility.Collapsed;
                this.lableBp.Visibility = Visibility.Collapsed;
            }
        }


        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            string tooltipValue = "";
            if (DeviceDataMp != null)
            {
                tooltipValue +=
                    string.Format("{0}：{1} ",
                        "MPPump",
                        DeviceDataMp.IsOn?"On" :"Off");
            }
            if (DeviceDataBp != null)
            {
                tooltipValue +=
                    string.Format("\r\n{0}：{1} ",
                        "BPPump",
                        DeviceDataBp.IsOn ? "On" : "Off");
            }

            if (tooltipValue != "")
            {
                ToolTip = tooltipValue;
            }
        }


        private void MPPumpOn(object sender, RoutedEventArgs e)
        {
            if (EnableControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceDataMp.DeviceModule}.{DeviceDataMp.DeviceName}.{AITPumpOperation.PumpOn}");
            }
        }

        private void MPPumpOff(object sender, RoutedEventArgs e)
        {
            if (EnableControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceDataMp.DeviceModule}.{DeviceDataMp.DeviceName}.{AITPumpOperation.PumpOff}");
            }
        }

        private void BPPumpOn(object sender, RoutedEventArgs e)
        {
            if (EnableControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceDataBp.DeviceModule}.{DeviceDataBp.DeviceName}.{AITPumpOperation.PumpOn}");
            }
        }

        private void BPPumpOff(object sender, RoutedEventArgs e)
        {
            if (EnableControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceDataBp.DeviceModule}.{DeviceDataBp.DeviceName}.{AITPumpOperation.PumpOff}");
            }
        }
    }
}

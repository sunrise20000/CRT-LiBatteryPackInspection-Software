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
    /// Tray.xaml 的交互逻辑
    /// </summary>
    public partial class Tray : UserControl
    {
        public Tray()
        {
            InitializeComponent();
        }

        private void MovoToTransferPos(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"PM1.Servo1.SetSvPosGo", 1, true);
        }

        private void MovoToProcessPos(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"PM1.Servo1.SetSvPosGo", 2, true);
        }

        private void MovoToHomePos(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"PM1.Servo1.SetSvBool", 12, true);
        }
    }
}

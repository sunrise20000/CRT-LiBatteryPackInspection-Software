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
    /// SlitDoor.xaml 的交互逻辑
    /// </summary>
    public partial class SlitDoor : UserControl
    {
        public SlitDoor()
        {
            InitializeComponent();
        }

        private void Open(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"PM1.V601.{AITValveOperation.GVTurnValve}", true);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"PM1.V601.{AITValveOperation.GVTurnValve}", false);
        }
    }
}

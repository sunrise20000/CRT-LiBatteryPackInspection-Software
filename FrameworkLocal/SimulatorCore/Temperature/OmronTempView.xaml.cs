using Aitex.Core.UI.MVVM;
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

namespace MECF.Framework.Simulator.Core.Temperature
{
    /// <summary>
    /// OmronTemp.xaml 的交互逻辑
    /// </summary>
    public partial class OmronTempView : UserControl
    {
        public static int CurrentTick = 0;
        public OmronTempView()
        {
            InitializeComponent();
            DataContext = new OmronTempViewModel((CurrentTick++ == 0) ? "COM44" : "COM46");

            (DataContext as TimerViewModelBase).Start();

            this.Loaded += OnViewLoaded;
        }
        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {

            }
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            InitSerialPort();
        }
        //GetPortNames()方法:获取当前计算机的串行端口名的数组
        private void InitSerialPort()
        {
            PortName.Items.Clear();
            foreach (string com in System.IO.Ports.SerialPort.GetPortNames())
            {
                PortName.Items.Add(com);
            }
            PortName.SelectedIndex = 0;

        }
    }
}

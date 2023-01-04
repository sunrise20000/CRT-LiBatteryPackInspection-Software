using Aitex.Common.Util;
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
using System.Windows.Shapes;

namespace SicSimulator.Views
{
    /// <summary>
    /// SimulatorIOTMView.xaml 的交互逻辑
    /// </summary>
    public partial class SimulatorIOTMView : UserControl
    {
        public SimulatorIOTMView()
        {
            InitializeComponent();

            DataContext = new IoViewModel(6833, "TM.PLC", PathManager.GetCfgDir() + "IODefinePlatform.xml", "TM");

            this.IsVisibleChanged += IOView_IsVisibleChanged;
        }
        private void IOView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext == null)
            {
            }
           (DataContext as TimerViewModelBase).EnableTimer(IsVisible);
        }
    }
}

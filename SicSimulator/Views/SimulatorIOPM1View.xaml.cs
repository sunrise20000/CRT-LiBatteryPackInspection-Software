using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aitex.Common.Util;
using Aitex.Core.UI.MVVM;
using SicSimulator.Instances;
using MECF.Framework.Common.IOCore;

namespace SicSimulator.Views
{
    /// <summary>
    /// SimulatorPMIOView.xaml 的交互逻辑
    /// </summary>
    public partial class SimulatorIOPM1View : UserControl
    {
        public SimulatorIOPM1View()
        {
            InitializeComponent();
            DataContext = new IoViewModel(6831, "PM1.PLC", PathManager.GetCfgDir() + "_ioDefinePM.xml", "PM1");

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

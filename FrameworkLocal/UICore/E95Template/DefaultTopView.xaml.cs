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
using Aitex.Core.UI.View.Frame;

namespace MECF.Framework.UI.Core.E95Template
{
    /// <summary>
    /// DefaultTopView.xaml 的交互逻辑
    /// </summary>
    public partial class DefaultTopView : UserControl, ITopView
    {
        public event EventHandler<int> OnSimulationSpeedChanged; 

        public DefaultTopView()
        {
            InitializeComponent();
        }

        public void SetTitle(string title)
        {
            
        }

        private void RbtnSimSpeed1x_OnChecked(object sender, RoutedEventArgs e)
        {
            OnSimulationSpeedChanged?.Invoke(this, 1);
        }

        private void RbtnSimSpeed2x_OnChecked(object sender, RoutedEventArgs e)
        {
            OnSimulationSpeedChanged?.Invoke(this, 5);
        }

        private void RbtnSimSpeed4x_OnChecked(object sender, RoutedEventArgs e)
        {
            OnSimulationSpeedChanged?.Invoke(this, 10);
        }
    }
}

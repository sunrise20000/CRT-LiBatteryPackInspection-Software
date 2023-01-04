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

namespace SicUI.Models.PMs.Charting
{
    /// <summary>
    /// PMCharting.xaml 的交互逻辑
    /// </summary>
    public partial class PMChartingView : UserControl
    {
        public PMChartingView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.TabMain.SelectedIndex = 1;
        }

        private void ShowDataPanel_Click(object sender, RoutedEventArgs e)
        {
            if (this.MainGrid.RowDefinitions[2].Height.Value < 300)
            {
                this.MainGrid.RowDefinitions[2].Height = new GridLength(300, GridUnitType.Pixel);
            }
            else
            {
                this.MainGrid.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Pixel);
            }
        }
    }
}

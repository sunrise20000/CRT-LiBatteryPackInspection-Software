using Aitex.Core.UI.View.Common;
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

namespace SicUI.Models.PMs
{
    /// <summary>
    /// PMOperationView.xaml 的交互逻辑
    /// </summary>
    public partial class PMAlarmView : UserControl
    {
        public PMAlarmView()
        {
            InitializeComponent();
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                var item = e.AddedItems[0] as AlarmItem;
                AnalysisText.Text = string.Format("Event Type：{0}\r\n\r\nEvent Name：{1}\r\n\r\nEvent Number：{2}\r\n\r\nTime：{3}\r\n\r\nDescription：{4}\r\n\r\nSolution：{5}",
                    item.Type,
                    item.EventEnum,
                    item.EventId,
                    item.OccuringTime,
                    item.Description,
                    item.Solution);
            }
            else
            {
                AnalysisText.Text = string.Empty;
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.Event
{
    /// <summary>
    /// EventView.xaml 的交互逻辑
    /// </summary>
    public delegate void ItemSelectionChange(bool changeFalg);

    public class ItemSelectionData
    {
        public bool _IsSelcet;
        public string _SelectItem;

        public bool IsSelect
        {
            get => _IsSelcet;
            set => _IsSelcet = value;
        }

        public string SelectItem
        {
            get => _SelectItem;
            set => _SelectItem = value;
        }

        public ItemSelectionData(bool IsSelcet, string SelectItem)
        {
            this.IsSelect = IsSelcet;
            this.SelectItem = SelectItem;
        }
    }

    public partial class EventView : UserControl
    {
        public EventView()
        {
            InitializeComponent();
            tbLoadPort1ToolTipList = new List<string>();
        }

        public List<string> tbLoadPort1ToolTipList { get; set; }

        public static readonly DependencyProperty tbLoadPort1ToolTipValueProperty = DependencyProperty.Register(
            "tbLoadPort1ToolTipValueData", typeof(ItemSelectionData), typeof(EventView));

        public ItemSelectionData tbLoadPort1ToolTipValueData
        {
            get => (ItemSelectionData)this.GetValue(tbLoadPort1ToolTipValueProperty);
            set => SetValue(tbLoadPort1ToolTipValueProperty, value);
        }

        private void ccbxFilterEventSource_ItemSelectionChanged(object sender,
            Xceed.Wpf.Toolkit.Primitives.ItemSelectionChangedEventArgs e)
        {
            if (e.Item != null)
            {
                tbLoadPort1ToolTipValueData = new ItemSelectionData(e.IsSelected, e.Item.ToString());
            }
            else
            {
                tbLoadPort1ToolTipValueData = null;
            }
        }
    }
}
using MECF.Framework.Common.CommonData;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace SicUI.Controls.Parts
{
    public class StatsDataListItem : NotifiableItem
    {
        public string Gas { get; set; }
        public string GasName { get; set; }
        public string Flow { get; set; }
        public string ActualFlow { get; set; }
        public string Error { get; set; }
        public string InvertedVolume { get; set; }
        public string DataTime { get; set; }
        public string ElapseTime { get; set; }
    }

    /// <summary>
    /// Interaction logic for IOView.xaml
    /// </summary>
    public partial class MFCView : UserControl
    {
        public ObservableCollection<StatsDataListItem> StatData
        {
            get { return (ObservableCollection<StatsDataListItem>)GetValue(StatDataProperty); }
            set { SetValue(StatDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StatData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StatDataProperty =
            DependencyProperty.Register("StatData", typeof(ObservableCollection<StatsDataListItem>), typeof(MFCView), new PropertyMetadata(null));
        
        public MFCView()
        {
            InitializeComponent();
        }
    }
}

using MECF.Framework.UI.Client.CenterViews.Operations.WaferAssociation;
using System.Windows;
using System.Windows.Controls;

namespace SicUI.Controls
{
    /// <summary>
    /// Interaction logic for WaferAssociationUnit.xaml
    /// </summary>
    public partial class WaferAssociationUnit : UserControl
    {
        public WaferAssociationUnit()
        {
            InitializeComponent();
        }

        public WaferAssociationInfo WAInfo
        {
            get { return (WaferAssociationInfo)GetValue(WAInfoProperty); }
            set { SetValue(WAInfoProperty, value); }
        }
        public static readonly DependencyProperty WAInfoProperty = DependencyProperty.Register("WAInfo", typeof(WaferAssociationInfo), typeof(WaferAssociationUnit), new UIPropertyMetadata(null));
    }
}

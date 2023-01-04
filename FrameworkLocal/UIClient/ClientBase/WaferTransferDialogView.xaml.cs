using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Client.ClientBase
{
    /// <summary>
    /// WaferTransferDialogView.xaml 的交互逻辑
    /// </summary>
    public partial class WaferTransferDialogView : UserControl
    {
        public WaferTransferDialogView()
        {
            InitializeComponent();
        }

        private void ChkAuto_Checked(object sender, RoutedEventArgs e)
        {
            //PassWordView passWordView = new PassWordView();
            //passWordView.ShowDialog();
            //if (!passWordView.VerificationResult)
            //{
            //    ChkVirtualTransfer.IsChecked = false;
            //}
        }

    }
}

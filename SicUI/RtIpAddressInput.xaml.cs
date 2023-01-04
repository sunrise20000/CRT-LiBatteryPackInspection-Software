using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

namespace SicUI
{
    /// <summary>
    /// Interaction logic for RtIpAddressInput.xaml
    /// </summary>
    public partial class RtIpAddressInput : Window
    {
        public RtIpAddressInput(string ipAddress)
        {
            InitializeComponent();

            if (ipAddress.ToLower() == "localhost")
                txtRtIpAddress.Text = ipAddress;
            else
            {
                if (IPAddress.TryParse(ipAddress, out var ip))
                {
                    txtRtIpAddress.Text = ip.ToString();
                }
                else
                {
                    txtRtIpAddress.Text = "";
                }
            }
        }

        public string RtHostAddress { get; private set; }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            txtRtIpAddress.SelectAll();
            txtRtIpAddress.Focus();
        }

        private void BtnLocalhost_OnClick(object sender, RoutedEventArgs e)
        {
            txtRtIpAddress.Text = "localhost";
            txtRtIpAddress.SelectAll();
            txtRtIpAddress.Focus();
        }

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            var isMatch = Regex.IsMatch(txtRtIpAddress.Text, @"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.|$)){4}\b");
            if (txtRtIpAddress.Text.ToLower() == "localhost" || isMatch)
            {
                RtHostAddress = txtRtIpAddress.Text;
                DialogResult = true;
                return;
            }

            MessageBox.Show("IP地址输入错误，请重新输入。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            txtRtIpAddress.SelectAll();
            txtRtIpAddress.Focus();
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

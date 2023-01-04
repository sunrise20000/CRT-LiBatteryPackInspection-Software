using Aitex.Core.Common;
using MECF.Framework.Common.Equipment;
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

namespace SicUI.Controls.Parts
{
    /// <summary>
    /// WaferTransferDialog.xaml 的交互逻辑
    /// </summary>
    public partial class WaferTransferDialog : Window
    {
		public WaferTransferDialog(WaferInfo waferInfo, ModuleName sourceStation, int sourceSlot, ModuleName destinationStation, int destinationSlot, WaferTransferOption option = null)
		{
			InitializeComponent();

			if (waferInfo.Status == WaferStatus.Dummy)
			{
				grid.RowDefinitions[7].Height = new GridLength(0);
			}
			else
			{
				grid.RowDefinitions[8].Height = new GridLength(0);
				if (destinationStation == ModuleName.Robot)
				{
					if (destinationSlot == 0)
					{
						chkBlade1.IsChecked = true;
					}
					else if (destinationSlot == 1)
					{
						chkBlade2.IsChecked = true;
					}
					chkBlade1.IsEnabled = false;
					chkBlade2.IsEnabled = false;
				}
			}

			if (option != null)
			{
				if (!option.Setting.ShowAlign)
				{
					grid.RowDefinitions[4].Height = new GridLength(0);
					grid.RowDefinitions[8].Height = new GridLength(0);
				}

				if (!option.Setting.ShowLaserMarker)
				{
					grid.RowDefinitions[5].Height = new GridLength(0);
				}

				if (!option.Setting.ShowT7Code)
				{
					grid.RowDefinitions[6].Height = new GridLength(0);
				}

				chkAligner.IsChecked = option.Align;
				chkReadID.IsChecked = option.ReadLaserMarker;
				chkReadID2.IsChecked = option.ReadT7Code;
				if (option.Blade == 0)
				{
					chkBlade1.IsChecked = true;
				}
				else
				{
					chkBlade2.IsChecked = true;
				}
				tbAngle.Text = option.AlignerAngle.ToString();
			}

			lbSource.Content = sourceStation;
			lbSourceSlot.Content = sourceSlot + 1;
			lbDest.Content = destinationStation;
			lblDestSlot.Content = destinationSlot + 1;
		}

		public bool Aligner
		{
			get
			{
				return chkAligner.IsChecked.Value;
			}
		}

		public bool ReadLaserMarker
		{
			get
			{
				return chkReadID.IsChecked.Value;
			}
		}

		public bool ReadT7Code
		{
			get
			{
				return chkReadID2.IsChecked.Value;
			}
		}

		public int Blade
		{
			get
			{
				return chkBlade1.IsChecked.Value ? 0 : 1;
			}
		}

		public double AlignerAngle
		{
			get
			{
				double value = 0d;
				double.TryParse(tbAngle.Text, out value);
				return value;
			}
		}

		private void btnOK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}

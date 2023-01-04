using Aitex.Sorter.Common;
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

namespace SicUI.Controls.Parts
{
	/// <summary>
	/// Door.xaml 的交互逻辑
	/// </summary>
	public partial class Door : UserControl
	{
		public Door()
		{
			InitializeComponent();

			root.DataContext = this;
		}

		public FoupDoorState State
		{
			get { return (FoupDoorState)GetValue(StateProperty); }
			set { SetValue(StateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for State.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StateProperty =
			DependencyProperty.Register("State", typeof(FoupDoorState), typeof(Door), new FrameworkPropertyMetadata(FoupDoorState.Unknown));

	}
}

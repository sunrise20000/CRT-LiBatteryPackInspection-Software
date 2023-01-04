using Aitex.Core.Common;
using Aitex.Sorter.Common;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

using CB = MECF.Framework.UI.Client.ClientBase;

namespace SicUI.Controls.Parts
{
    /// <summary>
    /// Chamber.xaml 的交互逻辑
    /// </summary>
    public partial class Chamber : UserControl
    {
        private double PM1 = 127;
        private double PM2 = 75;
        private double PM3 = -25;
        private double PM4 = -129;
        public Chamber()
		{
			InitializeComponent();
		}

        public CB.WaferInfo WaferData
        {
            get { return (CB.WaferInfo)GetValue(WaferDataProperty); }
            set { SetValue(WaferDataProperty, value); }
        }

        public static readonly DependencyProperty WaferDataProperty =
            DependencyProperty.Register("WaferData", typeof(CB.WaferInfo), typeof(Chamber), new PropertyMetadata(null));

      //  public WaferInfo Wafer
      //  {
      //      get { return (WaferInfo)GetValue(WaferProperty); }
      //      set { SetValue(WaferProperty, value); }
      //  }

      //  // Using a DependencyProperty as the backing store for WaferItem.  This enables animation, styling, binding, etc...
      //  public static readonly DependencyProperty WaferProperty =
      //      DependencyProperty.Register("Wafer", typeof(WaferInfo), typeof(Chamber), new PropertyMetadata(null));

      //  public ModuleName Station
      //  {
      //      get { return (ModuleName)GetValue(StationProperty); }
      //      set { SetValue(StationProperty, value); }
      //  }

      //  // Using a DependencyProperty as the backing store for WaferItem.  This enables animation, styling, binding, etc...
      //  public static readonly DependencyProperty SlotProperty =
      //      DependencyProperty.Register("Slot", typeof(int), typeof(Chamber), new PropertyMetadata(0));

      //  public int Slot
      //  {
      //      get { return (int)GetValue(SlotProperty); }
      //      set { SetValue(SlotProperty, value); }
      //  }

      //  // Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
      //  public static readonly DependencyProperty StationProperty =
      //DependencyProperty.Register("Station", typeof(ModuleName), typeof(Chamber), new PropertyMetadata(ModuleName.System));

        public FoupDoorState DoorState
		{
			get { return (FoupDoorState)GetValue(DoorStateProperty); }
			set { SetValue(DoorStateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for DoorState.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty DoorStateProperty =
			DependencyProperty.Register("DoorState", typeof(FoupDoorState), typeof(Chamber), new PropertyMetadata(FoupDoorState.Unknown));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(Chamber), new PropertyMetadata(null));

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var cmd = ((MenuItem)sender).Tag;

            Command.Execute(new[] { WaferData.ModuleID, cmd });
        }

        #region wafer 角度
        public static readonly DependencyProperty WaferAngleProperty =
            DependencyProperty.Register("WaferAngleChanged", typeof(string), typeof(Chamber), new PropertyMetadata(WaferAngleCurrentStsChanged));

        public static void WaferAngleCurrentStsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Chamber control = (Chamber)d;
            if (e.Property == WaferAngleProperty)
            {
                control.WaferAngleChanged = (string)e.NewValue;
            }
        }
        public string WaferAngleChanged
        {
            get { return (string)GetValue(WaferAngleProperty); }
            set
            {
                SetValue(WaferAngleProperty, value);
                RotateTransform rotateTransform = null;
                switch (WaferAngleChanged)
                {
                    case "PM1":
                        rotateTransform = new RotateTransform(PM1);
                        break;
                    case "PM2":
                        rotateTransform = new RotateTransform(PM2);
                        break;
                    case "PM3":
                        rotateTransform = new RotateTransform(PM3);
                        break;
                    case "PM4":
                        rotateTransform = new RotateTransform(PM4);
                        break;
                }
                //local_Wafer.RenderTransform = rotateTransform;
                this.UpdateLayout();
            }
        }
        #endregion
    }
}

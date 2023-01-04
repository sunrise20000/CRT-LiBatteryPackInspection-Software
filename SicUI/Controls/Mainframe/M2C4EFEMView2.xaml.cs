
using System.Windows;
using System.Windows.Controls;

namespace SicUI.Controls.M2C4Parts
{
    /// <summary>
    /// M2C4EFEMView2.xaml 的交互逻辑
    /// </summary>
    public partial class M2C4EFEMView2 : UserControl
    {
        public M2C4EFEMView2()
        {
            InitializeComponent();
        }

        public bool IsPM1DoorOpen
        {
            get { return (bool)GetValue(IsPM1DoorOpenProperty); }
            set { SetValue(IsPM1DoorOpenProperty, value); }
        }

        public static readonly DependencyProperty IsPM1DoorOpenProperty =
            DependencyProperty.Register("IsPM1DoorOpen", typeof(bool), typeof(M2C4EFEMView2), new FrameworkPropertyMetadata(false));

        public bool IsPM2DoorOpen
        {
            get { return (bool)GetValue(IsPM2DoorOpenProperty); }
            set { SetValue(IsPM2DoorOpenProperty, value); }
        }

        public static readonly DependencyProperty IsPM2DoorOpenProperty =
            DependencyProperty.Register("IsPM2DoorOpen", typeof(bool), typeof(M2C4EFEMView2), new FrameworkPropertyMetadata(false));

        public bool IsLLDoorOpen
        {
            get { return (bool)GetValue(IsLLDoorOpenProperty); }
            set { SetValue(IsLLDoorOpenProperty, value); }
        }

        public static readonly DependencyProperty IsLLDoorOpenProperty =
            DependencyProperty.Register("IsLLDoorOpen", typeof(bool), typeof(M2C4EFEMView2), new FrameworkPropertyMetadata(false));
    }
}

using System.Windows;
using System.Windows.Controls;

namespace Crt.UiCore.Controls
{
    public partial class LiBatteryPack : UserControl
    {
        #region Enums

        public enum Views
        {
            Top,
            Front,
            Left
        }

        #endregion

        public LiBatteryPack()
        {
            InitializeComponent();
        }

        #region DependencyProperties

        public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(
            nameof(View), typeof(Views), typeof(LiBatteryPack), new PropertyMetadata(Views.Top, OnViewChangedCallback));

        private static void OnViewChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LiBatteryPack uc)
            {
                uc.cavTopView.Visibility = Visibility.Hidden;
                uc.cavSide1View.Visibility = Visibility.Hidden;
                uc.cavSide2View.Visibility = Visibility.Hidden;

                switch (uc.View)
                {
                    case Views.Top:
                        uc.cavTopView.Visibility = Visibility.Visible;
                        break;

                    case Views.Front:
                        uc.cavSide1View.Visibility = Visibility.Visible;
                        break;

                    case Views.Left:
                        uc.cavSide2View.Visibility = Visibility.Visible;
                        break;

                    default:
                        uc.cavTopView.Visibility = Visibility.Visible;
                        break;

                }
            }
        }

        /// <summary>
        /// 设置或返回电池视角。
        /// </summary>
        public Views View
        {
            get => (Views)GetValue(ViewProperty);
            set => SetValue(ViewProperty, value);
        }


        public static readonly DependencyProperty IsShowBatteryProperty = DependencyProperty.Register(
            nameof(IsShowBattery), typeof(bool), typeof(LiBatteryPack), new PropertyMetadata(default(bool)));

        public bool IsShowBattery
        {
            get => (bool)GetValue(IsShowBatteryProperty);
            set => SetValue(IsShowBatteryProperty, value);
        }
        
        #endregion
    }
}

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
            Front90
        }

        #endregion

        public LiBatteryPack()
        {
            InitializeComponent();

            rootGrid.DataContext = this;
        }

        #region DependencyProperties

        public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(
            nameof(View), typeof(Views), typeof(LiBatteryPack), new PropertyMetadata(Views.Top, OnViewChangedCallback));

        private static void OnViewChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LiBatteryPack uc)
            {
                uc.TopView.Visibility = Visibility.Hidden;
                uc.FrontView.Visibility = Visibility.Hidden;
                uc.Front90View.Visibility = Visibility.Hidden;

                switch (uc.View)
                {
                    case Views.Top:
                        uc.TopView.Visibility = Visibility.Visible;
                        break;

                    case Views.Front:
                        uc.FrontView.Visibility = Visibility.Visible;
                        break;

                    case Views.Front90:
                        uc.Front90View.Visibility = Visibility.Visible;
                        break;

                    default:
                        uc.TopView.Visibility = Visibility.Visible;
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

        /// <summary>
        /// 设置或返回是否显示电池图标。
        /// </summary>
        public bool IsShowBattery
        {
            get => (bool)GetValue(IsShowBatteryProperty);
            set => SetValue(IsShowBatteryProperty, value);
        }

        public static readonly DependencyProperty AllowSelectedProperty = DependencyProperty.Register(
            nameof(AllowSelected), typeof(bool), typeof(LiBatteryPack), new PropertyMetadata(true));

        /// <summary>
        /// 设置或返回是否电池允许被选中。
        /// </summary>
        public bool AllowSelected
        {
            get => (bool)GetValue(AllowSelectedProperty);
            set => SetValue(AllowSelectedProperty, value);
        }
        
        #endregion
    }
}

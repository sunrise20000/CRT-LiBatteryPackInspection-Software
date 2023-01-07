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

namespace MECF.Framework.UI.Core.Control
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

        #region Dps

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

        public Views View
        {
            get => (Views)GetValue(ViewProperty);
            set => SetValue(ViewProperty, value);
        }

        #endregion
    }
}

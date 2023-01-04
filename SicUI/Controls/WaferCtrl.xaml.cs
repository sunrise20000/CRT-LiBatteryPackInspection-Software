using System.Windows;
using System.Windows.Media.Animation;
using MECF.Framework.UI.Client.ClientBase;

namespace SicUI.Controls
{
    /// <summary>
    /// WaferCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class WaferCtrl
    {
        public WaferCtrl()
        {
            InitializeComponent();
        }
        
        public static readonly DependencyProperty WaferDataProperty =
            DependencyProperty.Register(nameof(WaferData), typeof(WaferInfo), typeof(WaferCtrl), new PropertyMetadata(null));

        public WaferInfo WaferData
        {
            get => (WaferInfo)GetValue(WaferDataProperty);
            set => SetValue(WaferDataProperty, value);
        }

        public static readonly DependencyProperty IsRotaryProperty = DependencyProperty.Register(
            nameof(IsRotary), typeof(bool), typeof(WaferCtrl), new PropertyMetadata(default(bool), IsRotaryPropertyChangedCallback));

        private static void IsRotaryPropertyChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is WaferCtrl uc && args.NewValue is bool isRotary)
            {
                var res = uc.TryFindResource("sbRotateWafer");
                if (res is Storyboard sb)
                {
                    if(isRotary)
                        sb.Begin();
                    else
                        sb.Stop();
                }
            }
        }

        /// <summary>
        /// 设置或返回是否正在旋转。
        /// </summary>
        public bool IsRotary
        {
            get => (bool)GetValue(IsRotaryProperty);
            set => SetValue(IsRotaryProperty, value);
        }
        
    }
}

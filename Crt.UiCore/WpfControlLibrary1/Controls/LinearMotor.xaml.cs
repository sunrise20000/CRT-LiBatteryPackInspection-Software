using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Crt.UiCore.Controls
{
    /// <summary>
    /// Interaction logic for LiBattStation1.xaml
    /// </summary>
    public partial class LinearMotor : BatteryCarrierBase
    {

        public enum Positions
        {
            Standby,
            End,
            EndWithRotation
        }

        public enum BatteryViewTypes
        {
            Top,
            Front,
            Front90
        }

        private readonly
            Dictionary<BatteryViewTypes, (double GripperLeft, double BattLeft, double BattTop,  LiBatteryPack.Views BattView, Visibility WideGripperVisibility, Visibility NarrowGripperVisibility)> _dictionaryLocations;

        public LinearMotor()
        {
            InitializeComponent();

            // 创建各个元素的位置清单
            _dictionaryLocations =
                new Dictionary<BatteryViewTypes, (double GripperLeft, double BattLeft, double BattTop, LiBatteryPack.Views BattView, Visibility WideGripperVisibility, Visibility NarrowGripperVisibility)>
                {
                    {
                        BatteryViewTypes.Top,
                        (206, 10, 27, LiBatteryPack.Views.Top, Visibility.Visible, Visibility.Hidden)
                    },
                    {
                        BatteryViewTypes.Front,
                        (206, 8, 0, LiBatteryPack.Views.Front, Visibility.Visible, Visibility.Hidden)
                    },
                    {
                        BatteryViewTypes.Front90,
                        (220, 1, -8, LiBatteryPack.Views.Front90, Visibility.Hidden, Visibility.Visible)
                    }

                };

            SetElementLocation(BatteryViewTypes.Top);
        }

        #region Dp - BatteryViewType

        public static readonly DependencyProperty BatteryViewProperty = DependencyProperty.Register(
            nameof(BatteryView), typeof(BatteryViewTypes), typeof(LinearMotor), new PropertyMetadata(default(BatteryViewTypes), BatteryViewPropertyChangedCallback));

        /// <summary>
        /// 设置或获取电池视角。
        /// <para>参考<see cref="BatteryViewTypes"/></para>
        /// </summary>
        public BatteryViewTypes BatteryView
        {
            get => (BatteryViewTypes)GetValue(BatteryViewProperty);
            set => SetValue(BatteryViewProperty, value);
        }

        #endregion

        #region Methods

        protected override void BuildStoryboard()
        {
            // 创建动画列表
            Storyboards = new Dictionary<int, Storyboard>
            {
                { (int)Positions.Standby, FindResource("ToStandbyStoryboard") as Storyboard },
                { (int)Positions.End, FindResource("ToEndStoryboard") as Storyboard },
                { (int)Positions.EndWithRotation, FindResource("ToEndWithRotationStoryboard") as Storyboard }
            };
        }

        private void SetElementLocation(BatteryViewTypes view)
        {
            if (_dictionaryLocations.ContainsKey(view) == false)
                return;

            var location = _dictionaryLocations[view];
            
            // 配置夹手位置
            Canvas.SetLeft(Mover, location.GripperLeft);
            ImgGripperWide.Visibility = location.WideGripperVisibility;
            ImgGripperNarrow.Visibility = location.NarrowGripperVisibility;

            // 配置电池视图和位置
            BatteryPack.View = location.BattView;
            Canvas.SetLeft(BatteryPack, location.BattLeft);
            Canvas.SetTop(BatteryPack, location.BattTop);
            
        }

        private static void BatteryViewPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LinearMotor motor && e.NewValue is BatteryViewTypes view)
            {
                motor.SetElementLocation(view);
            }
        }

        #endregion
    }
}

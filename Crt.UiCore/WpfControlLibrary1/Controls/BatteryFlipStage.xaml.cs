using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Crt.UiCore.Controls
{
    /// <summary>
    /// Interaction logic for BattreyFlipStage.xaml
    /// </summary>
    public partial class BatteryFlipStage : BatteryCarrierBase
    {
        public enum BatteryViewTypes
        {
            Top,
            Front
        }

        private readonly Dictionary<BatteryViewTypes, (double BattLeft, double BattTop, LiBatteryPack.Views BattView)> _dictBatteryLocations;

        public BatteryFlipStage()
        {
            InitializeComponent();
            
            // 电池位置列表
            _dictBatteryLocations = new Dictionary<BatteryViewTypes, (double BattLeft, double BattTop, LiBatteryPack.Views BattView)>
            {
                { BatteryViewTypes.Top , (10, 27, LiBatteryPack.Views.Top)},
                { BatteryViewTypes.Front , (7, 0, LiBatteryPack.Views.Front)}
            };
            
            // 默认显示Top视图
            SetBatteryView(BatteryViewTypes.Top);
        }

        #region DP - BatteryView

        public static readonly DependencyProperty BatteryViewProperty = DependencyProperty.Register(
            nameof(BatteryView), typeof(BatteryViewTypes), typeof(BatteryFlipStage), new PropertyMetadata(default(BatteryViewTypes), BatteryViewPropertyChangedCallback));

        private static void BatteryViewPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BatteryFlipStage stage && e.NewValue is BatteryViewTypes view)
            {
                stage.SetBatteryView(view);
            }
        }

        public BatteryViewTypes BatteryView
        {
            get => (BatteryViewTypes)GetValue(BatteryViewProperty);
            set => SetValue(BatteryViewProperty, value);
        }

        #endregion

        #region Methods

        protected override void BuildStoryboard()
        {
            Storyboards = new Dictionary<int, Storyboard>();
        }

        private void SetBatteryView(BatteryViewTypes view)
        {

            if (_dictBatteryLocations.ContainsKey(view) == false)
                return;
            
            var (battLeft, battTop, battView) = _dictBatteryLocations[view];
            Canvas.SetLeft(Battery, battLeft);
            Canvas.SetTop(Battery, battTop);
            Battery.View = battView;
        }

        #endregion


    }
}

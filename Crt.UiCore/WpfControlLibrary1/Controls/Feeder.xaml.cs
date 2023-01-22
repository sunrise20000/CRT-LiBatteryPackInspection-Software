using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;

namespace Crt.UiCore.Controls
{
    /// <summary>
    /// Interaction logic for Feeder.xaml
    /// </summary>
    public partial class Feeder : BatteryCarrierBase
    {
        public enum Positions
        {
            Standby,
            End
        }
        
        public Feeder()
        {
            InitializeComponent();
        }

        static Feeder()
        {
            HasBatteryProperty.OverrideMetadata(typeof(Feeder), new PropertyMetadata(default(bool), HasBatteryPropertyChangedCallback));
        }
        

        private static void HasBatteryPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Feeder self && e.NewValue is bool hasBattery)
            {
                self.liBatteryPack.IsShowBattery = !hasBattery;
                self.liBatteryPackInPickPos.IsShowBattery = hasBattery;

                if (self.HasBattery)
                    self.CurrentPosition = (int)Positions.Standby;
            }
        }

        protected override void BuildStoryboard()
        {
            // 每个点位的动画
            Storyboards = new Dictionary<int, Storyboard>
            {
                { (int)Positions.Standby, FindResource("ToStandbyStoryboard") as Storyboard },
                { (int)Positions.End, FindResource("ToEndStoryboard") as Storyboard }
            };
        }
    }
}

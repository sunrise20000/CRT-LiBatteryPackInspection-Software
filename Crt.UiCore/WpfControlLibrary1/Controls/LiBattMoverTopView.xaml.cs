using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Crt.UiCore.Controls
{
    /// <summary>
    /// Interaction logic for LiBattStation1.xaml
    /// </summary>
    public partial class LiBattMoverTopView : BatteryCarrierBase
    {

        public enum Positions
        {
            Standby,
            End
        }

        public LiBattMoverTopView()
        {
            InitializeComponent();
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

using System.Collections.Generic;
using System.Windows.Media.Animation;
using Crt.UiCore.Controls.Base;

namespace Crt.UiCore.Controls
{
    /// <summary>
    /// Interaction logic for TransporterS1ToS2.xaml
    /// </summary>
    public partial class TransporterS1ToS2 : BatteryCarrierBase
    {
        public enum Positions
        {
            Standby,
            End
        }
        
        public TransporterS1ToS2()
        {
            InitializeComponent();
        }

        protected override void BuildStoryboard()
        {
            Storyboards = new Dictionary<int, Storyboard>
            {
                { (int)Positions.Standby, FindResource("ToStandbyStoryboard") as Storyboard },
                { (int)Positions.End, FindResource("ToEndStoryboard") as Storyboard }
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace Crt.UiCore.Controls
{
    
    /// <summary>
    /// Interaction logic for YamahaRobot600.xaml
    /// </summary>
    public partial class YamahaRobot800 : BatteryCarrierBase
    {
        public enum Positions
        {
            Standby,
            FeederA,
            FeederB,
            Station1A,
            Station1B
        }
        

        #region Variables
        

        #endregion

        public YamahaRobot800()
        {
            InitializeComponent();
        }


        #region Methods

        protected override void BuildStoryboard()
        {
            // 每个点位的动画
            Storyboards = new Dictionary<int, Storyboard>
            {
                { (int)Positions.Standby, FindResource("ToStandbyStoryboard") as Storyboard },
                { (int)Positions.FeederA, FindResource("ToFeederAStoryboard") as Storyboard },
                { (int)Positions.FeederB, FindResource("ToFeederBStoryboard") as Storyboard },
                { (int)Positions.Station1A, FindResource("ToStation1AStoryboard") as Storyboard },
                { (int)Positions.Station1B, FindResource("ToStation1BStoryboard") as Storyboard }
            };
        }

        #endregion
    }
}

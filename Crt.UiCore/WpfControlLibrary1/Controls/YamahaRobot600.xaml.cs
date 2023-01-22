using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Crt.UiCore.Controls
{
    /// <summary>
    /// Interaction logic for YamahaRobot600.xaml
    /// </summary>
    public partial class YamahaRobot600 : UserControl
    {

        public enum Positions
        {
            System,
            FeederA,
            FeederB,
            Station1A,
            Station1B
        }

        #region Variables

        private readonly object _animationLock = new object();

        private readonly Dictionary<Positions, Storyboard> _storyboards;

        private Positions _lastPosition;

        #endregion

        public YamahaRobot600()
        {
            InitializeComponent();
        }
    }
}

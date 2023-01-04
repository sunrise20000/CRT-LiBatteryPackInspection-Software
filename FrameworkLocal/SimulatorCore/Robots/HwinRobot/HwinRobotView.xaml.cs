
using System.Windows;
using System.Windows.Controls;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;

namespace MECF.Framework.Simulator.Core.Robots
{
    /// <summary>
    /// HwinRobotView.xaml 的交互逻辑
    /// </summary>
    public partial class HwinRobotView : UserControl
    {
        public HwinRobotView()
        {
            InitializeComponent();
            this.DataContext = new HwinRobotViewModel();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            (DataContext as TimerViewModelBase).Start();
        }
    }

    class HwinRobotViewModel : SocketDeviceViewModel
    {
        public string Title
        {
            get { return "Hwin Robot Simulator"; }
        }

        private HwinRobotSimulator _robot;

        public bool IsFailed
        {
            get
            {
                return _robot.Failed;
            }
            set
            {
                _robot.Failed = value;

            }
        }

        //private string _value;

        [IgnorePropertyChange]
        public string ErrorCode
        {
            get
            {
                return _robot.ErrorCode;
            }
            set
            {
                _robot.ErrorCode = value;

            }
        }
        public bool EventChecked
        {
            get
            {
                return _robot.EventChecked;
            }
            set
            {
                _robot.EventChecked = value;
                if (value) EventCode = "_EVENT ROBOR 02610 0003 B -00028 000028 000028 -00002\r";
                else EventCode = null;
            }
        }

        [IgnorePropertyChange]
        public string EventCode
        {
            get
            {
                return _robot.EventCode;
            }
            set
            {
                _robot.EventCode = value;
            }
        }
        public HwinRobotViewModel() : base("HwinRobotViewModel")
        {
            _robot = new HwinRobotSimulator();
            Init(_robot);
            
        }
    }

}

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;

namespace Crt.UiCore.Controls
{

    /// <summary>
    /// Interaction logic for YamahaRobot600.xaml
    /// </summary>
    public partial class YamahaRobot800WithoutStoryboard : BatteryCarrierBase
    {

        #region Variables
        

        #endregion

        public YamahaRobot800WithoutStoryboard()
        {
            InitializeComponent();
        }
        

        #region Methods

        protected override void BuildStoryboard()
        {
            // 每个点位的动画
            
        }

        #endregion

        #region DP - Arm Angles

        public static readonly DependencyProperty Arm1AngleProperty = DependencyProperty.Register(
            nameof(Arm1Angle), typeof(double), typeof(YamahaRobot800WithoutStoryboard), new PropertyMetadata(default(double)));

        public double Arm1Angle
        {
            get => (double)GetValue(Arm1AngleProperty);
            set => SetValue(Arm1AngleProperty, value);
        }

        public static readonly DependencyProperty Arm2AngleProperty = DependencyProperty.Register(
            nameof(Arm2Angle), typeof(double), typeof(YamahaRobot800WithoutStoryboard), new PropertyMetadata(default(double)));

        public double Arm2Angle
        {
            get => (double)GetValue(Arm2AngleProperty);
            set => SetValue(Arm2AngleProperty, value);
        }

        public static readonly DependencyProperty GripperAngleProperty = DependencyProperty.Register(
            nameof(GripperAngle), typeof(double), typeof(YamahaRobot800WithoutStoryboard), new PropertyMetadata(default(double)));

        public double GripperAngle
        {
            get => (double)GetValue(GripperAngleProperty);
            set => SetValue(GripperAngleProperty, value);
        }

        #endregion

    }
}

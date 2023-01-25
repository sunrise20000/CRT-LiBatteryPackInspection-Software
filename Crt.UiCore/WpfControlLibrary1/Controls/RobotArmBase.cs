using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Crt.UiCore.Controls
{
    public class RobotArmBase : Canvas
    {

        #region Constructors

        public RobotArmBase()
        {
            RenderTransform = new RotateTransform();
        }

        #endregion

        #region DP - ShowRotationCenter

        public static readonly DependencyProperty ShowRotationCenterProperty = DependencyProperty.Register(
            nameof(ShowRotationCenter), typeof(bool), typeof(RobotArmBase), new PropertyMetadata(default(bool)));

        /// <summary>
        /// 设置或返回是否显示旋转中心参考点。
        /// </summary>
        public bool ShowRotationCenter
        {
            get => (bool)GetValue(ShowRotationCenterProperty);
            set => SetValue(ShowRotationCenterProperty, value);
        }

        #endregion

        #region DP - Rotation Angle, CenterX, CenterY

        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
            nameof(Angle), typeof(double), typeof(RobotArmBase), new PropertyMetadata(default(double),RotationCenterPropertyChanged));

        public double Angle
        {
            get => (double)GetValue(AngleProperty);
            set => SetValue(AngleProperty, value);
        }

        /* public static readonly DependencyProperty RotationCenterXProperty = DependencyProperty.Register(
            nameof(RotationCenterX), typeof(double), typeof(RobotArmBase), new PropertyMetadata(default(double), RotationCenterPropertyChanged));
        
       
        public double RotationCenterX
        {
            get => (double)GetValue(RotationCenterXProperty);
            set => SetValue(RotationCenterXProperty, value);
        }

        public static readonly DependencyProperty RotationCenterYProperty = DependencyProperty.Register(
            nameof(RotationCenterY), typeof(double), typeof(RobotArmBase), new PropertyMetadata(default(double), RotationCenterPropertyChanged));

        public double RotationCenterY
        {
            get => (double)GetValue(RotationCenterYProperty);
            set => SetValue(RotationCenterYProperty, value);
        }*/

        private static void RotationCenterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RobotArmBase arm)
            {
                if(!(arm.RenderTransform is RotateTransform transform))
                    arm.RenderTransform = new RotateTransform(arm.Angle);
                else
                {
                    //transform.CenterX = arm.RotationCenterX;
                    //transform.CenterY = arm.RotationCenterY;
                    transform.Angle = arm.Angle;
                }
            }
        }

        #endregion

        #region Methods

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            
            
        }

        #endregion

    }
}

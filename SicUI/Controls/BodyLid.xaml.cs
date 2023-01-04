using SicUI.Controls.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SicUI.Controls
{
    /// <summary>
    /// BodyLid.xaml 的交互逻辑
    /// </summary>
    public partial class BodyLid : UserControl //, INotifyPropertyChanged
    {

        private bool _isEnd = false;
        private bool _isUp = false;
        private bool _isFront = true;
        private bool _isDown = true;
        AxisBorder bd;



        //public event PropertyChangedEventHandler PropertyChanged;

        public BodyLid()
        {
            InitializeComponent();
        }

        public int MovingUpTime
        {
            get { return (int)GetValue(MovingUpTimeProperty); }
            set { SetValue(MovingUpTimeProperty, value); }
        }
        public static readonly DependencyProperty MovingUpTimeProperty = DependencyProperty.Register("MovingUpTime", typeof(int), typeof(BodyLid), new FrameworkPropertyMetadata(5000));

        public int MovingEndTime
        {
            get { return (int)GetValue(MovingEndTimeProperty); }
            set { SetValue(MovingEndTimeProperty, value); }
        }
        public static readonly DependencyProperty MovingEndTimeProperty = DependencyProperty.Register("MovingEndTime", typeof(int), typeof(BodyLid), new FrameworkPropertyMetadata(5000));

        public int MovingSwingTime
        {
            get { return (int)GetValue(MovingSwingTimeProperty); }
            set { SetValue(MovingSwingTimeProperty, value); }
        }
        public static readonly DependencyProperty MovingSwingTimeProperty = DependencyProperty.Register("MovingSwingTime", typeof(int), typeof(BodyLid), new FrameworkPropertyMetadata(5000));



        public string ChamberBodyGroup
        {
            get { return (string)GetValue(ChamberBodyInfoProperty); }
            set { SetValue(ChamberBodyInfoProperty, value); }
        }

        public static readonly DependencyProperty ChamberBodyInfoProperty = DependencyProperty.Register(
            "ChamberBodyGroup", typeof(string), typeof(BodyLid),
            new PropertyMetadata(default(string)));



        public bool MovingUp
        {
            get { return (Boolean)GetValue(MovingUpProperty); }
            set { SetValue(MovingUpProperty, value); }
        }

        public static readonly DependencyProperty MovingUpProperty =
           DependencyProperty.Register("MovingUp", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool MovingDown
        {
            get { return (Boolean)GetValue(MovingDownProperty); }
            set { SetValue(MovingDownProperty, value); }
        }

        public static readonly DependencyProperty MovingDownProperty =
           DependencyProperty.Register("MovingDown", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool MovingEnd
        {
            get { return (Boolean)GetValue(MovingEndProperty); }
            set { SetValue(MovingEndProperty, value); }
        }

        public static readonly DependencyProperty MovingEndProperty =
           DependencyProperty.Register("MovingEnd", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool MovingFront
        {
            get { return (Boolean)GetValue(MovingFrontProperty); }
            set { SetValue(MovingFrontProperty, value); }
        }

        public static readonly DependencyProperty MovingFrontProperty =
           DependencyProperty.Register("MovingFront", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));



        public bool MovingSHSwingLoseen
        {
            get { return (Boolean)GetValue(MovingSHSwingLoseenProperty); }
            set { SetValue(MovingSHSwingLoseenProperty, value); }
        }

        public static readonly DependencyProperty MovingSHSwingLoseenProperty =
           DependencyProperty.Register("MovingSHSwingLoseen", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool MovingSHSwingTighten
        {
            get { return (Boolean)GetValue(MovingSHSwingTightenProperty); }
            set { SetValue(MovingSHSwingTightenProperty, value); }
        }
        public static readonly DependencyProperty MovingSHSwingTightenProperty =
         DependencyProperty.Register("MovingSHSwingTighten", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));


        public bool MovingMiddleSwingLoseen
        {
            get { return (Boolean)GetValue(MovingMiddleSwingLoseenProperty); }
            set { SetValue(MovingMiddleSwingLoseenProperty, value); }
        }

        public static readonly DependencyProperty MovingMiddleSwingLoseenProperty =
           DependencyProperty.Register("MovingMiddleSwingLoseen", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool MovingMiddleSwingTighten
        {
            get { return (Boolean)GetValue(MovingMiddleSwingTightenProperty); }
            set { SetValue(MovingMiddleSwingTightenProperty, value); }
        }
        public static readonly DependencyProperty MovingMiddleSwingTightenProperty =
         DependencyProperty.Register("MovingMiddleSwingTighten", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));



        public bool ChamberIsUp
        {
            get { return (Boolean)GetValue(ChamberIsUpProperty); }
            set { SetValue(ChamberIsUpProperty, value); }
        }

        public static readonly DependencyProperty ChamberIsUpProperty =
           DependencyProperty.Register("ChamberIsUp", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ChamberIsDown
        {
            get { return (Boolean)GetValue(ChamberIsDownProperty); }
            set { SetValue(ChamberIsDownProperty, value); }
        }

        public static readonly DependencyProperty ChamberIsDownProperty =
           DependencyProperty.Register("ChamberIsDown", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));


        public bool ChamberIsEnd
        {
            get { return (Boolean)GetValue(ChamberIsEndProperty); }
            set { SetValue(ChamberIsEndProperty, value); }
        }

        public static readonly DependencyProperty ChamberIsEndProperty =
           DependencyProperty.Register("ChamberIsEnd", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));


        public bool ChamberIsFront
        {
            get { return (Boolean)GetValue(ChamberIsFrontProperty); }
            set { SetValue(ChamberIsFrontProperty, value); }
        }

        public static readonly DependencyProperty ChamberIsFrontProperty =
           DependencyProperty.Register("ChamberIsFront", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));



        public bool ChamberIsUpLeach
        {
            get { return (Boolean)GetValue(ChamberIsUpLeachProperty); }
            set { SetValue(ChamberIsUpLeachProperty, value); }
        }

        public static readonly DependencyProperty ChamberIsUpLeachProperty =
           DependencyProperty.Register("ChamberIsUpLeach", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));



        public bool TightenSH
        {
            get { return (Boolean)GetValue(TightenSHProperty); }
            set { SetValue(TightenSHProperty, value); }
        }

        public static readonly DependencyProperty TightenSHProperty =
           DependencyProperty.Register("TightenSH", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));


        public bool TightenMiddle
        {
            get { return (Boolean)GetValue(TightenMiddleProperty); }
            set { SetValue(TightenMiddleProperty, value); }
        }

        public static readonly DependencyProperty TightenMiddleProperty =
           DependencyProperty.Register("TightenMiddle", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));


        public bool SwingLockSH
        {
            get { return (Boolean)GetValue(SwingLockSHProperty); }
            set { SetValue(SwingLockSHProperty, value); }
        }

        public static readonly DependencyProperty SwingLockSHProperty =
           DependencyProperty.Register("SwingLockSH", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));


        public bool SwingLockMiddle
        {
            get { return (Boolean)GetValue(SwingLockMiddleProperty); }
            set { SetValue(SwingLockMiddleProperty, value); }
        }

        public static readonly DependencyProperty SwingLockMiddleProperty =
           DependencyProperty.Register("SwingLockMiddle", typeof(bool), typeof(BodyLid), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));





        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            bd = null;

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            SetPicVisble();
            SetBodyImage();

            if (bd == null)
            {
                return;
            }

            //初始位置
            if (ChamberIsUp != _isUp || ChamberIsDown != _isDown)
            {
                _isUp = ChamberIsUp;
                _isDown = ChamberIsDown;
                Dispatcher.Invoke(() => MoveBodyUp(ChamberIsUp, 0));
            }
            if (ChamberIsEnd != _isEnd || ChamberIsFront != _isFront)
            {
                _isEnd = ChamberIsEnd;
                _isFront = ChamberIsFront;
                Dispatcher.Invoke(() => MoveBodyEnd(ChamberIsEnd, 0));
            }

            //按钮移动
            if (MovingUp && !ChamberIsEnd && ChamberIsDown)
            {
                Dispatcher.Invoke(() => MoveBodyUp(true, MovingUpTime));
            }
            else if (MovingDown && !ChamberIsEnd && ChamberIsUp)
            {
                Dispatcher.Invoke(() => MoveBodyUp(false, MovingUpTime));
            }
            else if (MovingEnd && ChamberIsUp && !ChamberIsEnd)
            {
                Dispatcher.Invoke(() => MoveBodyEnd(true, MovingEndTime));
            }
            else if (MovingFront && ChamberIsUp && ChamberIsEnd)
            {
                Dispatcher.Invoke(() => MoveBodyEnd(false, MovingEndTime));
            }
        }

        private void SetPicVisble()
        {
            if (TightenSH)
            {
                PicTL.Visibility = Visibility.Visible;
                PicTR.Visibility = Visibility.Visible;
            }
            else
            {
                PicTL.Visibility = Visibility.Collapsed;
                PicTR.Visibility = Visibility.Collapsed;
            }


            if (TightenMiddle)
            {
                PicML.Visibility = Visibility.Visible;
                PicMR.Visibility = Visibility.Visible;
            }
            else
            {
                PicML.Visibility = Visibility.Collapsed;
                PicMR.Visibility = Visibility.Collapsed;
            }
        }

        private void SetBodyImage()
        {
            this.PicTop.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/Showerhead2.png"));
            this.picMiddle.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/Middle.png"));
            this.PicTR.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/khr.png"));
            this.PicTL.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/khl.png"));
            this.PicTR.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/khr.png"));
            this.PicTL.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/khl.png"));

            if (!TightenSH && !SwingLockSH)
            {
                this.PicTop.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/Showerhead.png"));

                bd = this.bdht;
            }
            else if (TightenSH && SwingLockSH && !TightenMiddle && !SwingLockMiddle)
            {
                this.PicTop.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/Showerhead.png"));
                this.picMiddle.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/Middle2.png"));
                this.PicTR.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/khr2.png"));
                this.PicTL.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/khl2.png"));

                bd = this.bdhtm;
            }
        }


        private void MoveBodyUp(bool up, int time, Action onComplete = null)
        {
            bd.Stop();
            bd.MoveToUp(up, time, onComplete);
        }

        private void MoveBodyEnd(bool end, int time, Action onComplete = null)
        {
            bd.Stop();
            bd.MoveToEnd(end, time, onComplete);
        }

        private void Stop()
        {
            this.bdht.Stop();
            this.bdht.Stop();
            this.bdhtm.Stop();
        }

        private void Invoke(Action action)
        {
            Dispatcher.Invoke(action);
        }
    }
}

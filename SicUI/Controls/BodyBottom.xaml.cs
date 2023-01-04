using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SicUI.Controls
{
    /// <summary>
    /// BodyBottom.xaml 的交互逻辑
    /// </summary>
    public partial class BodyBottom : UserControl
    {
        //private bool _isSectionDown = false;
        //private bool _isSectionUp = false;
        private bool _isRingDown = false;
        //private bool _canMoveButton = false;

        public BodyBottom()
        {
            InitializeComponent();
        }

        //public int SectionUpTime
        //{
        //    get { return (int)GetValue(SectionUpTimeProperty); }
        //    set { SetValue(SectionUpTimeProperty, value); }
        //}
        //public static readonly DependencyProperty SectionUpTimeProperty = DependencyProperty.Register("SectionUpTime", typeof(int), typeof(BodyBottom), new FrameworkPropertyMetadata(5000));

        //public int SwingTime
        //{
        //    get { return (int)GetValue(SwingTimeProperty); }
        //    set { SetValue(SwingTimeProperty, value); }
        //}
        //public static readonly DependencyProperty SwingTimeProperty = DependencyProperty.Register("SwingTime", typeof(int), typeof(BodyBottom), new FrameworkPropertyMetadata(5000));

        public int RingTime
        {
            get { return (int)GetValue(RingTimeProperty); }
            set { SetValue(RingTimeProperty, value); }
        }
        public static readonly DependencyProperty RingTimeProperty = DependencyProperty.Register("RingTime", typeof(int), typeof(BodyBottom), new FrameworkPropertyMetadata(5000));



        //public bool MovingSectionUp
        //{
        //    get { return (Boolean)GetValue(MovingSectionUpProperty); }
        //    set { SetValue(MovingSectionUpProperty, value); }
        //}

        //public static readonly DependencyProperty MovingSectionUpProperty =
        //   DependencyProperty.Register("MovingSectionUp", typeof(bool), typeof(BodyBottom), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        //public bool MovingSectionDown
        //{
        //    get { return (Boolean)GetValue(MovingSectionDownProperty); }
        //    set { SetValue(MovingSectionDownProperty, value); }
        //}

        //public static readonly DependencyProperty MovingSectionDownProperty =
        //   DependencyProperty.Register("MovingSectionDown", typeof(bool), typeof(BodyBottom), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        //public bool SectionUpFeedBack
        //{
        //    get { return (Boolean)GetValue(SectionUpFeedBackProperty); }
        //    set { SetValue(SectionUpFeedBackProperty, value); }
        //}

        //public static readonly DependencyProperty SectionUpFeedBackProperty =
        //   DependencyProperty.Register("SectionUpFeedBack", typeof(bool), typeof(BodyBottom), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        //public bool SectionDownFeedBack
        //{
        //    get { return (Boolean)GetValue(SectionDownFeedBackProperty); }
        //    set { SetValue(SectionDownFeedBackProperty, value); }
        //}

        //public static readonly DependencyProperty SectionDownFeedBackProperty =
        //   DependencyProperty.Register("SectionDownFeedBack", typeof(bool), typeof(BodyBottom), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));





        public bool MovingRingUp
        {
            get { return (Boolean)GetValue(MovingRingUpProperty); }
            set { SetValue(MovingRingUpProperty, value); }
        }

        public static readonly DependencyProperty MovingRingUpProperty =
           DependencyProperty.Register("MovingRingUp", typeof(bool), typeof(BodyBottom), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool MovingRingDown
        {
            get { return (Boolean)GetValue(MovingRingDownProperty); }
            set { SetValue(MovingRingDownProperty, value); }
        }

        public static readonly DependencyProperty MovingRingDownProperty =
           DependencyProperty.Register("MovingRingDown", typeof(bool), typeof(BodyBottom), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));


        public bool RingUpFeedBack
        {
            get { return (Boolean)GetValue(RingUpFeedBackProperty); }
            set { SetValue(RingUpFeedBackProperty, value); }
        }

        public static readonly DependencyProperty RingUpFeedBackProperty =
           DependencyProperty.Register("RingUpFeedBack", typeof(bool), typeof(BodyBottom), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool RingDownFeedBack
        {
            get { return (Boolean)GetValue(RingDownFeedBackProperty); }
            set { SetValue(RingDownFeedBackProperty, value); }
        }

        public static readonly DependencyProperty RingDownFeedBackProperty =
           DependencyProperty.Register("RingDownFeedBack", typeof(bool), typeof(BodyBottom), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        //public bool TightenBotton
        //{
        //    get { return (Boolean)GetValue(TightenBottonProperty); }
        //    set { SetValue(TightenBottonProperty, value); }
        //}

        //public static readonly DependencyProperty TightenBottonProperty =
        //   DependencyProperty.Register("TightenBotton", typeof(bool), typeof(BodyBottom), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));


        //public bool SwingLockBotton
        //{
        //    get { return (Boolean)GetValue(SwingLockBottonProperty); }
        //    set { SetValue(SwingLockBottonProperty, value); }
        //}

        //public static readonly DependencyProperty SwingLockBottonProperty =
        //   DependencyProperty.Register("SwingLockBotton", typeof(bool), typeof(BodyBottom), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            //初始位置
            //if (_isSectionDown != SectionDownFeedBack || _isSectionUp != SectionUpFeedBack)
            //{
            //    _isSectionUp = SectionUpFeedBack;
            //    _isSectionDown = SectionDownFeedBack;
            //    Dispatcher.Invoke(() => MoveSectionDown(SectionDownFeedBack, 0));
            //}
            if (_isRingDown != MovingRingDown)
            {
                Dispatcher.Invoke(() => MoveRingDown(true, 0));
            }

            //SetPicVisble();
            //SetBodyImage();

            //按钮移动
            //if (MovingSectionUp && _canMoveButton && SectionDownFeedBack)
            //{
            //    Dispatcher.Invoke(() => MoveSectionDown(false, SectionUpTime));
            //}
            //else if (MovingSectionDown && _canMoveButton && SectionUpFeedBack)
            //{
            //    Dispatcher.Invoke(() => MoveSectionDown(true, SectionUpTime));
            //}

            if (MovingRingDown && RingUpFeedBack)
            {
                Dispatcher.Invoke(() => MoveRingDown(true, RingTime));
            }
            else if (MovingRingUp && RingDownFeedBack)
            {
                Dispatcher.Invoke(() => MoveRingDown(false, RingTime));
            }
        }

        //private void SetPicVisble()
        //{
        //    if (TightenBotton)
        //    {
        //        PicBL.Visibility = Visibility.Visible;
        //        PicBR.Visibility = Visibility.Visible;
        //    }
        //    else
        //    {
        //        PicBL.Visibility = Visibility.Collapsed;
        //        PicBR.Visibility = Visibility.Collapsed;
        //    }

        //}

        //private void SetBodyImage()
        //{
        //    if (!TightenBotton && !SwingLockBotton)
        //    {
        //        this.PicBottom.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/Bottom2.png"));
        //        this.PicB21.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/bottom4f.png"));
        //        this.PicB22.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/bottom4f.png"));

        //        _canMoveButton = true;
        //    }
        //    else
        //    {
        //        this.PicBottom.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/Bottom.png"));
        //        this.PicB21.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/bottom4.png"));
        //        this.PicB22.Source = new BitmapImage(new Uri("pack://application:,,,/SicUI;component/Themes/Images/parts/bodymove/bottom4.png"));

        //        _canMoveButton = false;
        //    }
        //}


        //private void MoveSectionDown(bool down,int time, Action onComplete = null)
        //{
        //    this.canvasButtom.Stop();
        //    int orgYPoint = 0;
        //    int desYPoint = 0;
        //    if (down)
        //    {
        //        orgYPoint = 0;
        //        desYPoint = 30;
        //    }
        //    else
        //    {
        //        orgYPoint = 30;
        //        desYPoint = 0;
        //    }

        //    this.canvasButtom.MoveToUpDown(orgYPoint, desYPoint, time,onComplete);
        //}

        private void MoveRingDown(bool down, int time, Action onComplete = null)
        {
            this.canvasRing.Stop();
            this.canvasRing2.Stop();
            int orgYPoint = 0;
            int desYPoint = 0;
            if (down)
            {
                orgYPoint = 0;
                desYPoint = 18;
            }
            else
            {
                orgYPoint = 18;
                desYPoint = 0;
            }

            this.canvasRing.MoveToUpDown(orgYPoint, desYPoint, time, onComplete);
            this.canvasRing2.MoveToUpDown(orgYPoint, desYPoint, time, onComplete);
        }
    }
}

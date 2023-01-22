using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Crt.UiCore.Controls
{
    /// <summary>
    /// Interaction logic for YamahaRobot.xaml
    /// </summary>
    public partial class YamahaRobot : UserControl
    {
        public enum Positions
        {
            System,
            FeederA
        }
        
        #region Variables
        
        private readonly object _animationLock = new object();

        private readonly Dictionary<Positions, Storyboard> _storyboards;

        private Positions _lastPosition;

        #endregion

        public YamahaRobot()
        {
            InitializeComponent();

           /* var isDesignMode = DesignerProperties.GetIsInDesignMode(this);
            if (isDesignMode)
                return;

            // 每个点位的动画
            _storyboards = new Dictionary<Positions, Storyboard>
            {
                { Positions.System, FindResource("ToStandbyStoryboard") as Storyboard },
                { Positions.FeederA, FindResource("ToFeederAStoryboard") as Storyboard }
            };

            foreach (var storyboard in _storyboards.Values)
            {
                storyboard.Completed += StoryboardOnCompleted;
            }

            _lastPosition = Positions.System;

            AnimationBusy = false;
            
            StoryboardMonitor();*/
        }

        #region Properties

        private bool _animationBusy = false;

        private bool AnimationBusy
        {
            get
            {
                lock (_animationLock)
                {
                    return _animationBusy;
                }
            }
            set
            {
                lock (_animationLock)
                {
                    _animationBusy = value;
                }
            }
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty CurrentPositionProperty = DependencyProperty.Register(
            nameof(CurrentPosition), typeof(Positions), typeof(YamahaRobot), new PropertyMetadata(default(Positions)));

        public Positions CurrentPosition
        {
            get => (Positions)GetValue(CurrentPositionProperty);
            set => SetValue(CurrentPositionProperty, value);
        }

        #endregion

        #region Methods

        private void StoryboardMonitor()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var currPos = Dispatcher.Invoke(()=> (Positions)GetValue(CurrentPositionProperty));
                        
                        if (_lastPosition != currPos && !AnimationBusy)
                        {
                            Debug.WriteLine($"_lastPosition: {_lastPosition}, CurrentPosition: {currPos}");
                            var sb = _storyboards.ContainsKey(currPos) ? _storyboards[currPos] : null;

                            Debug.WriteLine(_storyboards[currPos]);
                            AnimationBusy = true;
                            Dispatcher.Invoke(() => sb?.Begin());
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Write(ex);
                    }
                   

                    await Task.Delay(10);
                }
            });
        }

        #endregion

        #region Events

        private void StoryboardOnCompleted(object sender, EventArgs e)
        {
            _lastPosition = CurrentPosition;
            AnimationBusy = false;
        }

        #endregion
    }
}

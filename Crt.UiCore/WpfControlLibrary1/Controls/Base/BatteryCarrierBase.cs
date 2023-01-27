using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Crt.UiCore.Controls.Base
{
    public abstract class BatteryCarrierBase: ContentControl
    {

        #region Variables

        protected readonly object AnimationLock = new object();
        protected Dictionary<int, Storyboard> Storyboards;
        protected int? LastPosition;

        private Action _invokeOnAnimationComplete;

        private readonly ConcurrentQueue<(int position, Action doOnAnimationBegin, Action doOnAnimationEnd)>
            _qPositions;

        #endregion

        #region Constructors

        protected BatteryCarrierBase()
        {
            _qPositions = new ConcurrentQueue<(int position, Action doOnAnimationBegin, Action doOnAnimationEnd)>();
        }
        

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

           

            /*var isDesignMode = DesignerProperties.GetIsInDesignMode(this);
            if (isDesignMode)
                return;*/

            // 创建动画故事板
            BuildStoryboard();

            if (Storyboards == null || Storyboards.Count == 0)
                return;

            foreach (var storyboard in Storyboards.Values)
            {
                storyboard.Completed += OnStoryboardComplete;
            }

            LastPosition = null;
            AnimationBusy = false;

            StoryboardMonitor();
        }

        #endregion

        #region Properties

        private bool _animationBusy = false;

        public bool AnimationBusy
        {
            get
            {
                lock (AnimationLock)
                {
                    return _animationBusy;
                }
            }
            private set
            {
                lock (AnimationLock)
                {
                    _animationBusy = value;
                }
            }
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty CurrentPositionProperty = DependencyProperty.Register(
            nameof(CurrentPosition), typeof(int), typeof(BatteryCarrierBase), new PropertyMetadata(default(int)));

        /// <summary>
        /// 设置或返回机械手工具末端目前所在的位置。
        /// </summary>
        public int CurrentPosition
        {
            get => (int)GetValue(CurrentPositionProperty);
            private set => SetValue(CurrentPositionProperty, value);
        }


        public static readonly DependencyProperty HasBatteryProperty = DependencyProperty.Register(
            nameof(HasBattery), typeof(bool), typeof(BatteryCarrierBase), new PropertyMetadata(default(bool)));

        /// <summary>
        /// 设置或返回机械手上是否有电池。
        /// </summary>
        public bool HasBattery
        {
            get => (bool)GetValue(HasBatteryProperty);
            set => SetValue(HasBatteryProperty, value);
        }

        #endregion

        #region Methods

        protected abstract void BuildStoryboard();

        private void StoryboardMonitor()
        {
            if(Storyboards == null || Storyboards.Count == 0) 
                return;

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (_qPositions.TryDequeue(out var token))
                        {
                            var sb = Storyboards.ContainsKey(token.position) ? Storyboards[token.position] : null;
                            _invokeOnAnimationComplete = token.doOnAnimationEnd;

                            Dispatcher?.Invoke(() =>
                            {
                                LastPosition = CurrentPosition;
                                CurrentPosition = token.position;
                                AnimationBusy = true; 
                                token.doOnAnimationBegin?.Invoke();
                                sb?.Begin();
                            });

                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Write(ex);
                    }


                    await Task.Delay(5);
                }
            });
        }

        /// <summary>
        /// 设置下一个位置。
        /// </summary>
        /// /// <param name="nextPosition">控件的位置。</param>
        /// <param name="doOnAnimationBegin">启动动画前执行的动作</param>
        /// <param name="doOnAnimationEnd">动画结束后的动作</param>
        public void SetPosition(int nextPosition, Action doOnAnimationBegin = null, Action doOnAnimationEnd = null)
        {
            _qPositions.Enqueue((nextPosition, doOnAnimationBegin, doOnAnimationEnd));
        }

        #endregion

        #region Events

        protected virtual void OnStoryboardComplete(object sender, EventArgs e)
        {
            AnimationBusy = false;

            if (_invokeOnAnimationComplete != null)
            {
                Dispatcher?.Invoke(_invokeOnAnimationComplete);
                _invokeOnAnimationComplete = null;
            }
        }

        #endregion


    }
}

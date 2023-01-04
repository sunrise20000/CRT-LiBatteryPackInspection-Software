using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SicUI.Controls.Common
{
    public class AxisBorder:Border
    {
        private Storyboard currentStoryboard;

        public void Stop()
        {
            if (currentStoryboard != null)
            {
                currentStoryboard.Stop(this);
                currentStoryboard = null;
            }
        }

        public void MoveToUp(bool isUp, double times, Action onComplete = null)
        {
            Storyboard storyboard = new Storyboard();   //创建Storyboard对象

            int desYPoint = 0;
            int orgYPoint = 0;
            if (isUp)
            {
                orgYPoint = 0;
                desYPoint = -10;
            }
            else
            {
                orgYPoint = -10;
                desYPoint = 0;
            }

            DoubleAnimation doubleAnimation = new DoubleAnimation(orgYPoint, desYPoint, new Duration(TimeSpan.FromMilliseconds(times)));
            Storyboard.SetTarget(doubleAnimation, this);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(Canvas.Top)"));
            storyboard.Children.Add(doubleAnimation);
            storyboard.Completed += (s, e) =>
            {
                InvalidateVisual();
                currentStoryboard = null;
                onComplete?.Invoke();
            };

            currentStoryboard = storyboard;
            storyboard.Begin();
        }

        public void MoveToEnd(bool frontToEnd, int times, Action onComplete = null)
        {
            Path path1 = new Path();

            if (frontToEnd)
            {
                path1.Data = Geometry.Parse("M 0,0 A 300,160 0 0 0 -250,13");
            }
            else
            {
                path1.Data = Geometry.Parse("M -250,13 A 300,160 0 0 1 0,0");
            }

            TranslateTransform translate = new TranslateTransform();
            this.RenderTransform = translate;

            NameScope.SetNameScope(this, new NameScope());
            this.RegisterName("translate", translate);

            DoubleAnimationUsingPath animationX = new DoubleAnimationUsingPath();
            animationX.PathGeometry = path1.Data.GetFlattenedPathGeometry();
            animationX.Source = PathAnimationSource.X;
            animationX.Duration = new Duration(TimeSpan.FromMilliseconds(times));
            animationX.FillBehavior = FillBehavior.HoldEnd;

            DoubleAnimationUsingPath animationY = new DoubleAnimationUsingPath();
            animationY.PathGeometry = path1.Data.GetFlattenedPathGeometry();
            animationY.Source = PathAnimationSource.Y;
            animationY.Duration = animationX.Duration;
            animationY.FillBehavior = FillBehavior.HoldEnd;

            Storyboard storyboard = new Storyboard();
            storyboard.AutoReverse = false;
            storyboard.Children.Add(animationX);
            storyboard.Children.Add(animationY);
            Storyboard.SetTargetName(animationX, "translate");
            Storyboard.SetTargetName(animationY, "translate");
            Storyboard.SetTargetProperty(animationX, new PropertyPath(TranslateTransform.XProperty));
            Storyboard.SetTargetProperty(animationY, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Completed += (s, e) =>
            {
                InvalidateVisual();
                currentStoryboard = null;
                onComplete?.Invoke();
            };

            currentStoryboard = storyboard;
            storyboard.Begin(this);
        }

        public void MoveToUpDown(bool toUp,int orgYPoint, int desYPoint, double times, Action onComplete = null)
        {
            Storyboard storyboard = new Storyboard();   //创建Storyboard对象          

            DoubleAnimation doubleAnimation = new DoubleAnimation(orgYPoint, desYPoint, new Duration(TimeSpan.FromMilliseconds(times)));
            Storyboard.SetTarget(doubleAnimation, this);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(Canvas.Top)"));
            storyboard.Children.Add(doubleAnimation);
            storyboard.Completed += (s, e) =>
            {
                InvalidateVisual();
                currentStoryboard = null;
                onComplete?.Invoke();
            };

            currentStoryboard = storyboard;
            storyboard.Begin();
        }
    }
}

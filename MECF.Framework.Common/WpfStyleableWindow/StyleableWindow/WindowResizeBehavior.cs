using System.Windows;
using System.Windows.Controls.Primitives;

namespace WpfStyleableWindow.StyleableWindow
{
	public static class WindowResizeBehavior
	{
		public static readonly DependencyProperty TopLeftResize = DependencyProperty.RegisterAttached("TopLeftResize", typeof(Window), typeof(WindowResizeBehavior), new UIPropertyMetadata(null, OnTopLeftResizeChanged));

		public static readonly DependencyProperty TopRightResize = DependencyProperty.RegisterAttached("TopRightResize", typeof(Window), typeof(WindowResizeBehavior), new UIPropertyMetadata(null, OnTopRightResizeChanged));

		public static readonly DependencyProperty BottomRightResize = DependencyProperty.RegisterAttached("BottomRightResize", typeof(Window), typeof(WindowResizeBehavior), new UIPropertyMetadata(null, OnBottomRightResizeChanged));

		public static readonly DependencyProperty BottomLeftResize = DependencyProperty.RegisterAttached("BottomLeftResize", typeof(Window), typeof(WindowResizeBehavior), new UIPropertyMetadata(null, OnBottomLeftResizeChanged));

		public static readonly DependencyProperty LeftResize = DependencyProperty.RegisterAttached("LeftResize", typeof(Window), typeof(WindowResizeBehavior), new UIPropertyMetadata(null, OnLeftResizeChanged));

		public static readonly DependencyProperty RightResize = DependencyProperty.RegisterAttached("RightResize", typeof(Window), typeof(WindowResizeBehavior), new UIPropertyMetadata(null, OnRightResizeChanged));

		public static readonly DependencyProperty TopResize = DependencyProperty.RegisterAttached("TopResize", typeof(Window), typeof(WindowResizeBehavior), new UIPropertyMetadata(null, OnTopResizeChanged));

		public static readonly DependencyProperty BottomResize = DependencyProperty.RegisterAttached("BottomResize", typeof(Window), typeof(WindowResizeBehavior), new UIPropertyMetadata(null, OnBottomResizeChanged));

		public static Window GetTopLeftResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(TopLeftResize);
		}

		public static void SetTopLeftResize(DependencyObject obj, Window window)
		{
			obj.SetValue(TopLeftResize, window);
		}

		private static void OnTopLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is Thumb thumb)
			{
				thumb.DragDelta += DragTopLeft;
			}
		}

		public static Window GetTopRightResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(TopRightResize);
		}

		public static void SetTopRightResize(DependencyObject obj, Window window)
		{
			obj.SetValue(TopRightResize, window);
		}

		private static void OnTopRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is Thumb thumb)
			{
				thumb.DragDelta += DragTopRight;
			}
		}

		public static Window GetBottomRightResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(BottomRightResize);
		}

		public static void SetBottomRightResize(DependencyObject obj, Window window)
		{
			obj.SetValue(BottomRightResize, window);
		}

		private static void OnBottomRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is Thumb thumb)
			{
				thumb.DragDelta += DragBottomRight;
			}
		}

		public static Window GetBottomLeftResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(BottomLeftResize);
		}

		public static void SetBottomLeftResize(DependencyObject obj, Window window)
		{
			obj.SetValue(BottomLeftResize, window);
		}

		private static void OnBottomLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is Thumb thumb)
			{
				thumb.DragDelta += DragBottomLeft;
			}
		}

		public static Window GetLeftResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(LeftResize);
		}

		public static void SetLeftResize(DependencyObject obj, Window window)
		{
			obj.SetValue(LeftResize, window);
		}

		private static void OnLeftResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is Thumb thumb)
			{
				thumb.DragDelta += DragLeft;
			}
		}

		public static Window GetRightResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(RightResize);
		}

		public static void SetRightResize(DependencyObject obj, Window window)
		{
			obj.SetValue(RightResize, window);
		}

		private static void OnRightResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is Thumb thumb)
			{
				thumb.DragDelta += DragRight;
			}
		}

		public static Window GetTopResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(TopResize);
		}

		public static void SetTopResize(DependencyObject obj, Window window)
		{
			obj.SetValue(TopResize, window);
		}

		private static void OnTopResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is Thumb thumb)
			{
				thumb.DragDelta += DragTop;
			}
		}

		public static Window GetBottomResize(DependencyObject obj)
		{
			return (Window)obj.GetValue(BottomResize);
		}

		public static void SetBottomResize(DependencyObject obj, Window window)
		{
			obj.SetValue(BottomResize, window);
		}

		private static void OnBottomResizeChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is Thumb thumb)
			{
				thumb.DragDelta += DragBottom;
			}
		}

		private static void DragLeft(object sender, DragDeltaEventArgs e)
		{
			Thumb thumb = sender as Thumb;
			if (thumb.GetValue(LeftResize) is Window window)
			{
				double num = window.SafeWidthChange(e.HorizontalChange, positive: false);
				window.Width -= num;
				window.Left += num;
			}
		}

		private static void DragRight(object sender, DragDeltaEventArgs e)
		{
			Thumb thumb = sender as Thumb;
			if (thumb.GetValue(RightResize) is Window window)
			{
				double num = window.SafeWidthChange(e.HorizontalChange);
				window.Width += num;
			}
		}

		private static void DragTop(object sender, DragDeltaEventArgs e)
		{
			Thumb thumb = sender as Thumb;
			if (thumb.GetValue(TopResize) is Window window)
			{
				double num = window.SafeHeightChange(e.VerticalChange, positive: false);
				window.Height -= num;
				window.Top += num;
			}
		}

		private static void DragBottom(object sender, DragDeltaEventArgs e)
		{
			Thumb thumb = sender as Thumb;
			if (thumb.GetValue(BottomResize) is Window window)
			{
				double num = window.SafeHeightChange(e.VerticalChange);
				window.Height += num;
			}
		}

		private static void DragTopLeft(object sender, DragDeltaEventArgs e)
		{
			Thumb thumb = sender as Thumb;
			if (thumb.GetValue(TopLeftResize) is Window window)
			{
				double num = window.SafeHeightChange(e.VerticalChange, positive: false);
				double num2 = window.SafeWidthChange(e.HorizontalChange, positive: false);
				window.Width -= num2;
				window.Left += num2;
				window.Height -= num;
				window.Top += num;
			}
		}

		private static void DragTopRight(object sender, DragDeltaEventArgs e)
		{
			Thumb thumb = sender as Thumb;
			if (thumb.GetValue(TopRightResize) is Window window)
			{
				double num = window.SafeHeightChange(e.VerticalChange, positive: false);
				double num2 = window.SafeWidthChange(e.HorizontalChange);
				window.Width += num2;
				window.Height -= num;
				window.Top += num;
			}
		}

		private static void DragBottomRight(object sender, DragDeltaEventArgs e)
		{
			Thumb thumb = sender as Thumb;
			if (thumb.GetValue(BottomRightResize) is Window window)
			{
				double num = window.SafeHeightChange(e.VerticalChange);
				double num2 = window.SafeWidthChange(e.HorizontalChange);
				window.Width += num2;
				window.Height += num;
			}
		}

		private static void DragBottomLeft(object sender, DragDeltaEventArgs e)
		{
			Thumb thumb = sender as Thumb;
			if (thumb.GetValue(BottomLeftResize) is Window window)
			{
				double num = window.SafeHeightChange(e.VerticalChange);
				double num2 = window.SafeWidthChange(e.HorizontalChange, positive: false);
				window.Width -= num2;
				window.Left += num2;
				window.Height += num;
			}
		}

		private static double SafeWidthChange(this Window window, double change, bool positive = true)
		{
			double num = (positive ? (window.Width + change) : (window.Width - change));
			if (num <= window.MinWidth)
			{
				return 0.0;
			}
			if (num >= window.MaxWidth)
			{
				return 0.0;
			}
			if (num < 0.0)
			{
				return 0.0;
			}
			return change;
		}

		private static double SafeHeightChange(this Window window, double change, bool positive = true)
		{
			double num = (positive ? (window.Height + change) : (window.Height - change));
			if (num <= window.MinHeight)
			{
				return 0.0;
			}
			if (num >= window.MaxHeight)
			{
				return 0.0;
			}
			if (num < 0.0)
			{
				return 0.0;
			}
			return change;
		}
	}
}

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfStyleableWindow.StyleableWindow
{
	public static class ShowSystemMenuBehavior
	{
		public static readonly DependencyProperty TargetWindow = DependencyProperty.RegisterAttached("TargetWindow", typeof(Window), typeof(ShowSystemMenuBehavior));

		public static readonly DependencyProperty LeftButtonShowAt = DependencyProperty.RegisterAttached("LeftButtonShowAt", typeof(UIElement), typeof(ShowSystemMenuBehavior), new UIPropertyMetadata(null, LeftButtonShowAtChanged));

		public static readonly DependencyProperty RightButtonShow = DependencyProperty.RegisterAttached("RightButtonShow", typeof(bool), typeof(ShowSystemMenuBehavior), new UIPropertyMetadata(false, RightButtonShowChanged));

		private static bool leftButtonToggle = true;

		public static Window GetTargetWindow(DependencyObject obj)
		{
			return (Window)obj.GetValue(TargetWindow);
		}

		public static void SetTargetWindow(DependencyObject obj, Window window)
		{
			obj.SetValue(TargetWindow, window);
		}

		public static UIElement GetLeftButtonShowAt(DependencyObject obj)
		{
			return (UIElement)obj.GetValue(LeftButtonShowAt);
		}

		public static void SetLeftButtonShowAt(DependencyObject obj, UIElement element)
		{
			obj.SetValue(LeftButtonShowAt, element);
		}

		public static bool GetRightButtonShow(DependencyObject obj)
		{
			return (bool)obj.GetValue(RightButtonShow);
		}

		public static void SetRightButtonShow(DependencyObject obj, bool arg)
		{
			obj.SetValue(RightButtonShow, arg);
		}

		private static void LeftButtonShowAtChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is UIElement uIElement)
			{
				uIElement.MouseLeftButtonDown += LeftButtonDownShow;
			}
		}

		private static void LeftButtonDownShow(object sender, MouseButtonEventArgs e)
		{
			if (leftButtonToggle)
			{
				object value = ((UIElement)sender).GetValue(LeftButtonShowAt);
				Point menuLocation = ((Visual)value).PointToScreen(new Point(0.0, 0.0));
				Window targetWindow = ((UIElement)sender).GetValue(TargetWindow) as Window;
				SystemMenuManager.ShowMenu(targetWindow, menuLocation);
				leftButtonToggle = !leftButtonToggle;
			}
			else
			{
				leftButtonToggle = !leftButtonToggle;
			}
		}

		private static void RightButtonShowChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is UIElement uIElement)
			{
				uIElement.MouseRightButtonDown += RightButtonDownShow;
			}
		}

		private static void RightButtonDownShow(object sender, MouseButtonEventArgs e)
		{
			UIElement uIElement = (UIElement)sender;
			Window window = uIElement.GetValue(TargetWindow) as Window;
			Point menuLocation = window.PointToScreen(Mouse.GetPosition(window));
			SystemMenuManager.ShowMenu(window, menuLocation);
		}
	}
}

using System.Windows;
using System.Windows.Input;

namespace WpfStyleableWindow.StyleableWindow
{
	public static class WindowDragBehavior
	{
		public static readonly DependencyProperty LeftMouseButtonDrag = DependencyProperty.RegisterAttached("LeftMouseButtonDrag", typeof(Window), typeof(WindowDragBehavior), new UIPropertyMetadata(null, OnLeftMouseButtonDragChanged));

		public static Window GetLeftMouseButtonDrag(DependencyObject obj)
		{
			return (Window)obj.GetValue(LeftMouseButtonDrag);
		}

		public static void SetLeftMouseButtonDrag(DependencyObject obj, Window window)
		{
			obj.SetValue(LeftMouseButtonDrag, window);
		}

		private static void OnLeftMouseButtonDragChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is UIElement uIElement)
			{
				uIElement.MouseLeftButtonDown += buttonDown;
			}
		}

		private static void buttonDown(object sender, MouseButtonEventArgs e)
		{
			UIElement uIElement = sender as UIElement;
			if (uIElement.GetValue(LeftMouseButtonDrag) is Window window)
			{
				window.DragMove();
			}
		}
	}
}

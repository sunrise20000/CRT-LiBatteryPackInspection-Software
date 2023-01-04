using System;
using System.Windows;

namespace Hardcodet.Wpf.TaskbarNotification
{
	internal static class RoutedEventHelper
	{
		internal static void RaiseEvent(DependencyObject target, RoutedEventArgs args)
		{
			if (target is UIElement uIElement)
			{
				uIElement.RaiseEvent(args);
			}
			else if (target is ContentElement contentElement)
			{
				contentElement.RaiseEvent(args);
			}
		}

		internal static void AddHandler(DependencyObject element, RoutedEvent routedEvent, Delegate handler)
		{
			UIElement uIElement = element as UIElement;
			if (element != null && uIElement != null)
			{
				uIElement.AddHandler(routedEvent, handler);
				return;
			}
			ContentElement contentElement = element as ContentElement;
			if (element != null)
			{
				contentElement?.AddHandler(routedEvent, handler);
			}
		}

		internal static void RemoveHandler(DependencyObject element, RoutedEvent routedEvent, Delegate handler)
		{
			if (element is UIElement uIElement)
			{
				uIElement.RemoveHandler(routedEvent, handler);
			}
			else if (element is ContentElement contentElement)
			{
				contentElement.RemoveHandler(routedEvent, handler);
			}
		}
	}
}

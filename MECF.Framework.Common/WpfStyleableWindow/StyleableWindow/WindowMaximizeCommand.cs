using System;
using System.Windows;
using System.Windows.Input;

namespace WpfStyleableWindow.StyleableWindow
{
	public class WindowMaximizeCommand : ICommand
	{
		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			if (parameter is Window window)
			{
				if (window.WindowState == WindowState.Maximized)
				{
					window.WindowState = WindowState.Normal;
				}
				else
				{
					window.WindowState = WindowState.Maximized;
				}
			}
		}
	}
}

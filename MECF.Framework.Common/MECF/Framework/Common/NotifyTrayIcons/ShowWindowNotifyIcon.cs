using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;

namespace MECF.Framework.Common.NotifyTrayIcons
{
	public class ShowWindowNotifyIcon : TaskbarIcon
	{
		public event Action<bool> ShowMainWindow;

		public event Action ExitWindow;

		public ShowWindowNotifyIcon(string windowName, ImageSource icon)
		{
			ContextMenu contextMenu = new ContextMenu();
			MenuItem menuItem = new MenuItem();
			menuItem.Header = "Show " + windowName;
			menuItem.Click += ClickShow;
			contextMenu.Items.Add(menuItem);
			menuItem = new MenuItem();
			menuItem.Header = "Hide " + windowName;
			menuItem.Click += ClickHide;
			contextMenu.Items.Add(menuItem);
			menuItem = new MenuItem
			{
				Header = "-"
			};
			contextMenu.Items.Add(menuItem);
			menuItem = new MenuItem();
			menuItem.Header = "Exit ";
			menuItem.Click += ClickExit;
			contextMenu.Items.Add(menuItem);
			base.ContextMenu = contextMenu;
			base.TrayMouseDoubleClick += ShowWindowNotifyIcon_TrayMouseDoubleClick;
			base.ToolTipText = "Double-click for window, right-click for menu";
			base.IconSource = icon;
		}

		private void ShowWindowNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
		{
			if (this.ShowMainWindow != null)
			{
				this.ShowMainWindow(obj: true);
			}
		}

		public void ShowBallon(string title, string text)
		{
			ShowBalloonTip(title, text, BalloonIcon.Info);
		}

		private void ClickHide(object sender, RoutedEventArgs e)
		{
			if (this.ShowMainWindow != null)
			{
				this.ShowMainWindow(obj: false);
			}
		}

		private void ClickShow(object sender, RoutedEventArgs e)
		{
			if (this.ShowMainWindow != null)
			{
				this.ShowMainWindow(obj: true);
			}
		}

		private void ClickExit(object sender, RoutedEventArgs e)
		{
			if (this.ExitWindow != null)
			{
				Application.Current.Dispatcher.Invoke(delegate
				{
					this.ExitWindow();
				});
			}
		}
	}
}

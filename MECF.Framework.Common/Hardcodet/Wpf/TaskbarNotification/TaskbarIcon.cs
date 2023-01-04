#define DEBUG
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification.Interop;

namespace Hardcodet.Wpf.TaskbarNotification
{
	public class TaskbarIcon : FrameworkElement, IDisposable
	{
		public delegate Hardcodet.Wpf.TaskbarNotification.Interop.Point GetCustomPopupPosition();

		private readonly object lockObject = new object();

		private NotifyIconData iconData;

		private readonly WindowMessageSink messageSink;

		private Action singleClickTimerAction;

		private readonly Timer singleClickTimer;

		private readonly Timer balloonCloseTimer;

		public const string CategoryName = "NotifyIcon";

		private static readonly DependencyPropertyKey TrayPopupResolvedPropertyKey;

		public static readonly DependencyProperty TrayPopupResolvedProperty;

		private static readonly DependencyPropertyKey TrayToolTipResolvedPropertyKey;

		public static readonly DependencyProperty TrayToolTipResolvedProperty;

		private static readonly DependencyPropertyKey CustomBalloonPropertyKey;

		public static readonly DependencyProperty CustomBalloonProperty;

		private Icon icon;

		public static readonly DependencyProperty IconSourceProperty;

		public static readonly DependencyProperty ToolTipTextProperty;

		public static readonly DependencyProperty TrayToolTipProperty;

		public static readonly DependencyProperty TrayPopupProperty;

		public static readonly DependencyProperty MenuActivationProperty;

		public static readonly DependencyProperty PopupActivationProperty;

		public static readonly DependencyProperty DoubleClickCommandProperty;

		public static readonly DependencyProperty DoubleClickCommandParameterProperty;

		public static readonly DependencyProperty DoubleClickCommandTargetProperty;

		public static readonly DependencyProperty LeftClickCommandProperty;

		public static readonly DependencyProperty LeftClickCommandParameterProperty;

		public static readonly DependencyProperty LeftClickCommandTargetProperty;

		public static readonly DependencyProperty NoLeftClickDelayProperty;

		public static readonly RoutedEvent TrayLeftMouseDownEvent;

		public static readonly RoutedEvent TrayRightMouseDownEvent;

		public static readonly RoutedEvent TrayMiddleMouseDownEvent;

		public static readonly RoutedEvent TrayLeftMouseUpEvent;

		public static readonly RoutedEvent TrayRightMouseUpEvent;

		public static readonly RoutedEvent TrayMiddleMouseUpEvent;

		public static readonly RoutedEvent TrayMouseDoubleClickEvent;

		public static readonly RoutedEvent TrayMouseMoveEvent;

		public static readonly RoutedEvent TrayBalloonTipShownEvent;

		public static readonly RoutedEvent TrayBalloonTipClosedEvent;

		public static readonly RoutedEvent TrayBalloonTipClickedEvent;

		public static readonly RoutedEvent TrayContextMenuOpenEvent;

		public static readonly RoutedEvent PreviewTrayContextMenuOpenEvent;

		public static readonly RoutedEvent TrayPopupOpenEvent;

		public static readonly RoutedEvent PreviewTrayPopupOpenEvent;

		public static readonly RoutedEvent TrayToolTipOpenEvent;

		public static readonly RoutedEvent PreviewTrayToolTipOpenEvent;

		public static readonly RoutedEvent TrayToolTipCloseEvent;

		public static readonly RoutedEvent PreviewTrayToolTipCloseEvent;

		public static readonly RoutedEvent PopupOpenedEvent;

		public static readonly RoutedEvent ToolTipOpenedEvent;

		public static readonly RoutedEvent ToolTipCloseEvent;

		public static readonly RoutedEvent BalloonShowingEvent;

		public static readonly RoutedEvent BalloonClosingEvent;

		public static readonly DependencyProperty ParentTaskbarIconProperty;

		private int DoubleClickWaitTime => (!NoLeftClickDelay) ? WinApi.GetDoubleClickTime() : 0;

		public bool IsTaskbarIconCreated { get; private set; }

		public bool SupportsCustomToolTips => messageSink.Version == NotifyIconVersion.Vista;

		private bool IsPopupOpen
		{
			get
			{
				Popup trayPopupResolved = TrayPopupResolved;
				ContextMenu contextMenu = base.ContextMenu;
				Popup customBalloon = CustomBalloon;
				return (trayPopupResolved != null && trayPopupResolved.IsOpen) || (contextMenu != null && contextMenu.IsOpen) || (customBalloon?.IsOpen ?? false);
			}
		}

		public GetCustomPopupPosition CustomPopupPosition { get; set; }

		public bool IsDisposed { get; private set; }

		[Category("NotifyIcon")]
		public Popup TrayPopupResolved => (Popup)GetValue(TrayPopupResolvedProperty);

		[Category("NotifyIcon")]
		[Browsable(true)]
		[Bindable(true)]
		public ToolTip TrayToolTipResolved => (ToolTip)GetValue(TrayToolTipResolvedProperty);

		public Popup CustomBalloon => (Popup)GetValue(CustomBalloonProperty);

		[Browsable(false)]
		public Icon Icon
		{
			get
			{
				return icon;
			}
			set
			{
				icon = value;
				iconData.IconHandle = ((value == null) ? IntPtr.Zero : icon.Handle);
				Util.WriteIconData(ref iconData, NotifyCommand.Modify, IconDataMembers.Icon);
			}
		}

		[Category("NotifyIcon")]
		[Description("Sets the displayed taskbar icon.")]
		public ImageSource IconSource
		{
			get
			{
				return (ImageSource)GetValue(IconSourceProperty);
			}
			set
			{
				SetValue(IconSourceProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("Alternative to a fully blown ToolTip, which is only displayed on Vista and above.")]
		public string ToolTipText
		{
			get
			{
				return (string)GetValue(ToolTipTextProperty);
			}
			set
			{
				SetValue(ToolTipTextProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("Custom UI element that is displayed as a tooltip. Only on Vista and above")]
		public UIElement TrayToolTip
		{
			get
			{
				return (UIElement)GetValue(TrayToolTipProperty);
			}
			set
			{
				SetValue(TrayToolTipProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("Displayed as a Popup if the user clicks on the taskbar icon.")]
		public UIElement TrayPopup
		{
			get
			{
				return (UIElement)GetValue(TrayPopupProperty);
			}
			set
			{
				SetValue(TrayPopupProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("Defines what mouse events display the context menu.")]
		public PopupActivationMode MenuActivation
		{
			get
			{
				return (PopupActivationMode)GetValue(MenuActivationProperty);
			}
			set
			{
				SetValue(MenuActivationProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("Defines what mouse events display the TaskbarIconPopup.")]
		public PopupActivationMode PopupActivation
		{
			get
			{
				return (PopupActivationMode)GetValue(PopupActivationProperty);
			}
			set
			{
				SetValue(PopupActivationProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("A command that is being executed if the tray icon is being double-clicked.")]
		public ICommand DoubleClickCommand
		{
			get
			{
				return (ICommand)GetValue(DoubleClickCommandProperty);
			}
			set
			{
				SetValue(DoubleClickCommandProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("Parameter to submit to the DoubleClickCommand when the user double clicks on the NotifyIcon.")]
		public object DoubleClickCommandParameter
		{
			get
			{
				return GetValue(DoubleClickCommandParameterProperty);
			}
			set
			{
				SetValue(DoubleClickCommandParameterProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("The target of the command that is fired if the notify icon is double clicked.")]
		public IInputElement DoubleClickCommandTarget
		{
			get
			{
				return (IInputElement)GetValue(DoubleClickCommandTargetProperty);
			}
			set
			{
				SetValue(DoubleClickCommandTargetProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("A command that is being executed if the tray icon is being left-clicked.")]
		public ICommand LeftClickCommand
		{
			get
			{
				return (ICommand)GetValue(LeftClickCommandProperty);
			}
			set
			{
				SetValue(LeftClickCommandProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("The target of the command that is fired if the notify icon is clicked with the left mouse button.")]
		public object LeftClickCommandParameter
		{
			get
			{
				return GetValue(LeftClickCommandParameterProperty);
			}
			set
			{
				SetValue(LeftClickCommandParameterProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("The target of the command that is fired if the notify icon is clicked with the left mouse button.")]
		public IInputElement LeftClickCommandTarget
		{
			get
			{
				return (IInputElement)GetValue(LeftClickCommandTargetProperty);
			}
			set
			{
				SetValue(LeftClickCommandTargetProperty, value);
			}
		}

		[Category("NotifyIcon")]
		[Description("Set to true to make left clicks work without delay.")]
		public bool NoLeftClickDelay
		{
			get
			{
				return (bool)GetValue(NoLeftClickDelayProperty);
			}
			set
			{
				SetValue(NoLeftClickDelayProperty, value);
			}
		}

		[Category("NotifyIcon")]
		public event RoutedEventHandler TrayLeftMouseDown
		{
			add
			{
				AddHandler(TrayLeftMouseDownEvent, value);
			}
			remove
			{
				RemoveHandler(TrayLeftMouseDownEvent, value);
			}
		}

		public event RoutedEventHandler TrayRightMouseDown
		{
			add
			{
				AddHandler(TrayRightMouseDownEvent, value);
			}
			remove
			{
				RemoveHandler(TrayRightMouseDownEvent, value);
			}
		}

		public event RoutedEventHandler TrayMiddleMouseDown
		{
			add
			{
				AddHandler(TrayMiddleMouseDownEvent, value);
			}
			remove
			{
				RemoveHandler(TrayMiddleMouseDownEvent, value);
			}
		}

		public event RoutedEventHandler TrayLeftMouseUp
		{
			add
			{
				AddHandler(TrayLeftMouseUpEvent, value);
			}
			remove
			{
				RemoveHandler(TrayLeftMouseUpEvent, value);
			}
		}

		public event RoutedEventHandler TrayRightMouseUp
		{
			add
			{
				AddHandler(TrayRightMouseUpEvent, value);
			}
			remove
			{
				RemoveHandler(TrayRightMouseUpEvent, value);
			}
		}

		public event RoutedEventHandler TrayMiddleMouseUp
		{
			add
			{
				AddHandler(TrayMiddleMouseUpEvent, value);
			}
			remove
			{
				RemoveHandler(TrayMiddleMouseUpEvent, value);
			}
		}

		public event RoutedEventHandler TrayMouseDoubleClick
		{
			add
			{
				AddHandler(TrayMouseDoubleClickEvent, value);
			}
			remove
			{
				RemoveHandler(TrayMouseDoubleClickEvent, value);
			}
		}

		public event RoutedEventHandler TrayMouseMove
		{
			add
			{
				AddHandler(TrayMouseMoveEvent, value);
			}
			remove
			{
				RemoveHandler(TrayMouseMoveEvent, value);
			}
		}

		public event RoutedEventHandler TrayBalloonTipShown
		{
			add
			{
				AddHandler(TrayBalloonTipShownEvent, value);
			}
			remove
			{
				RemoveHandler(TrayBalloonTipShownEvent, value);
			}
		}

		public event RoutedEventHandler TrayBalloonTipClosed
		{
			add
			{
				AddHandler(TrayBalloonTipClosedEvent, value);
			}
			remove
			{
				RemoveHandler(TrayBalloonTipClosedEvent, value);
			}
		}

		public event RoutedEventHandler TrayBalloonTipClicked
		{
			add
			{
				AddHandler(TrayBalloonTipClickedEvent, value);
			}
			remove
			{
				RemoveHandler(TrayBalloonTipClickedEvent, value);
			}
		}

		public event RoutedEventHandler TrayContextMenuOpen
		{
			add
			{
				AddHandler(TrayContextMenuOpenEvent, value);
			}
			remove
			{
				RemoveHandler(TrayContextMenuOpenEvent, value);
			}
		}

		public event RoutedEventHandler PreviewTrayContextMenuOpen
		{
			add
			{
				AddHandler(PreviewTrayContextMenuOpenEvent, value);
			}
			remove
			{
				RemoveHandler(PreviewTrayContextMenuOpenEvent, value);
			}
		}

		public event RoutedEventHandler TrayPopupOpen
		{
			add
			{
				AddHandler(TrayPopupOpenEvent, value);
			}
			remove
			{
				RemoveHandler(TrayPopupOpenEvent, value);
			}
		}

		public event RoutedEventHandler PreviewTrayPopupOpen
		{
			add
			{
				AddHandler(PreviewTrayPopupOpenEvent, value);
			}
			remove
			{
				RemoveHandler(PreviewTrayPopupOpenEvent, value);
			}
		}

		public event RoutedEventHandler TrayToolTipOpen
		{
			add
			{
				AddHandler(TrayToolTipOpenEvent, value);
			}
			remove
			{
				RemoveHandler(TrayToolTipOpenEvent, value);
			}
		}

		public event RoutedEventHandler PreviewTrayToolTipOpen
		{
			add
			{
				AddHandler(PreviewTrayToolTipOpenEvent, value);
			}
			remove
			{
				RemoveHandler(PreviewTrayToolTipOpenEvent, value);
			}
		}

		public event RoutedEventHandler TrayToolTipClose
		{
			add
			{
				AddHandler(TrayToolTipCloseEvent, value);
			}
			remove
			{
				RemoveHandler(TrayToolTipCloseEvent, value);
			}
		}

		public event RoutedEventHandler PreviewTrayToolTipClose
		{
			add
			{
				AddHandler(PreviewTrayToolTipCloseEvent, value);
			}
			remove
			{
				RemoveHandler(PreviewTrayToolTipCloseEvent, value);
			}
		}

		public TaskbarIcon()
		{
			messageSink = (Util.IsDesignMode ? WindowMessageSink.CreateEmpty() : new WindowMessageSink(NotifyIconVersion.Win95));
			iconData = NotifyIconData.CreateDefault(messageSink.MessageWindowHandle);
			CreateTaskbarIcon();
			messageSink.MouseEventReceived += OnMouseEvent;
			messageSink.TaskbarCreated += OnTaskbarCreated;
			messageSink.ChangeToolTipStateRequest += OnToolTipChange;
			messageSink.BalloonToolTipChanged += OnBalloonToolTipChanged;
			singleClickTimer = new Timer(DoSingleClickAction);
			balloonCloseTimer = new Timer(CloseBalloonCallback);
			if (Application.Current != null)
			{
				Application.Current.Exit += OnExit;
			}
		}

		public Hardcodet.Wpf.TaskbarNotification.Interop.Point GetPopupTrayPosition()
		{
			return TrayInfo.GetTrayLocation();
		}

		public void ShowCustomBalloon(UIElement balloon, PopupAnimation animation, int? timeout)
		{
			Dispatcher dispatcher = this.GetDispatcher();
			if (!dispatcher.CheckAccess())
			{
				Action method = delegate
				{
					ShowCustomBalloon(balloon, animation, timeout);
				};
				dispatcher.Invoke(DispatcherPriority.Normal, method);
				return;
			}
			if (balloon == null)
			{
				throw new ArgumentNullException("balloon");
			}
			if (timeout.HasValue && timeout < 500)
			{
				string format = "Invalid timeout of {0} milliseconds. Timeout must be at least 500 ms";
				format = string.Format(format, timeout);
				throw new ArgumentOutOfRangeException("timeout", format);
			}
			EnsureNotDisposed();
			lock (lockObject)
			{
				CloseBalloon();
			}
			Popup popup = new Popup
			{
				AllowsTransparency = true
			};
			UpdateDataContext(popup, null, base.DataContext);
			popup.PopupAnimation = animation;
			Popup popup2 = LogicalTreeHelper.GetParent(balloon) as Popup;
			if (popup2 != null)
			{
				popup2.Child = null;
			}
			if (popup2 != null)
			{
				string format2 = "Cannot display control [{0}] in a new balloon popup - that control already has a parent. You may consider creating new balloons every time you want to show one.";
				format2 = string.Format(format2, balloon);
				throw new InvalidOperationException(format2);
			}
			popup.Child = balloon;
			popup.Placement = PlacementMode.AbsolutePoint;
			popup.StaysOpen = true;
			Hardcodet.Wpf.TaskbarNotification.Interop.Point point = ((CustomPopupPosition != null) ? CustomPopupPosition() : GetPopupTrayPosition());
			popup.HorizontalOffset = point.X - 1;
			popup.VerticalOffset = point.Y - 1;
			lock (lockObject)
			{
				SetCustomBalloon(popup);
			}
			SetParentTaskbarIcon(balloon, this);
			RaiseBalloonShowingEvent(balloon, this);
			popup.IsOpen = true;
			if (timeout.HasValue)
			{
				balloonCloseTimer.Change(timeout.Value, -1);
			}
		}

		public void ResetBalloonCloseTimer()
		{
			if (IsDisposed)
			{
				return;
			}
			lock (lockObject)
			{
				balloonCloseTimer.Change(-1, -1);
			}
		}

		public void CloseBalloon()
		{
			if (IsDisposed)
			{
				return;
			}
			Dispatcher dispatcher = this.GetDispatcher();
			if (!dispatcher.CheckAccess())
			{
				Action method = CloseBalloon;
				dispatcher.Invoke(DispatcherPriority.Normal, method);
				return;
			}
			lock (lockObject)
			{
				balloonCloseTimer.Change(-1, -1);
				Popup customBalloon = CustomBalloon;
				if (customBalloon == null)
				{
					return;
				}
				UIElement child = customBalloon.Child;
				RoutedEventArgs routedEventArgs = RaiseBalloonClosingEvent(child, this);
				if (!routedEventArgs.Handled)
				{
					customBalloon.IsOpen = false;
					customBalloon.Child = null;
					if (child != null)
					{
						SetParentTaskbarIcon(child, null);
					}
				}
				SetCustomBalloon(null);
			}
		}

		private void CloseBalloonCallback(object state)
		{
			if (!IsDisposed)
			{
				Action callback = CloseBalloon;
				this.GetDispatcher().Invoke(callback);
			}
		}

		private void OnMouseEvent(MouseEvent me)
		{
			if (IsDisposed)
			{
				return;
			}
			switch (me)
			{
			case MouseEvent.MouseMove:
				RaiseTrayMouseMoveEvent();
				return;
			case MouseEvent.IconRightMouseDown:
				RaiseTrayRightMouseDownEvent();
				break;
			case MouseEvent.IconLeftMouseDown:
				RaiseTrayLeftMouseDownEvent();
				break;
			case MouseEvent.IconRightMouseUp:
				RaiseTrayRightMouseUpEvent();
				break;
			case MouseEvent.IconLeftMouseUp:
				RaiseTrayLeftMouseUpEvent();
				break;
			case MouseEvent.IconMiddleMouseDown:
				RaiseTrayMiddleMouseDownEvent();
				break;
			case MouseEvent.IconMiddleMouseUp:
				RaiseTrayMiddleMouseUpEvent();
				break;
			case MouseEvent.IconDoubleClick:
				singleClickTimer.Change(-1, -1);
				RaiseTrayMouseDoubleClickEvent();
				break;
			case MouseEvent.BalloonToolTipClicked:
				RaiseTrayBalloonTipClickedEvent();
				break;
			default:
				throw new ArgumentOutOfRangeException("me", "Missing handler for mouse event flag: " + me);
			}
			Hardcodet.Wpf.TaskbarNotification.Interop.Point cursorPosition = default(Hardcodet.Wpf.TaskbarNotification.Interop.Point);
			if (messageSink.Version == NotifyIconVersion.Vista)
			{
				WinApi.GetPhysicalCursorPos(ref cursorPosition);
			}
			else
			{
				WinApi.GetCursorPos(ref cursorPosition);
			}
			cursorPosition = TrayInfo.GetDeviceCoordinates(cursorPosition);
			bool flag = false;
			if (me.IsMatch(PopupActivation))
			{
				if (me == MouseEvent.IconLeftMouseUp)
				{
					singleClickTimerAction = delegate
					{
						LeftClickCommand.ExecuteIfEnabled(LeftClickCommandParameter, LeftClickCommandTarget ?? this);
						ShowTrayPopup(cursorPosition);
					};
					singleClickTimer.Change(DoubleClickWaitTime, -1);
					flag = true;
				}
				else
				{
					ShowTrayPopup(cursorPosition);
				}
			}
			if (me.IsMatch(MenuActivation))
			{
				if (me == MouseEvent.IconLeftMouseUp)
				{
					singleClickTimerAction = delegate
					{
						LeftClickCommand.ExecuteIfEnabled(LeftClickCommandParameter, LeftClickCommandTarget ?? this);
						ShowContextMenu(cursorPosition);
					};
					singleClickTimer.Change(DoubleClickWaitTime, -1);
					flag = true;
				}
				else
				{
					ShowContextMenu(cursorPosition);
				}
			}
			if (me == MouseEvent.IconLeftMouseUp && !flag)
			{
				singleClickTimerAction = delegate
				{
					LeftClickCommand.ExecuteIfEnabled(LeftClickCommandParameter, LeftClickCommandTarget ?? this);
				};
				singleClickTimer.Change(DoubleClickWaitTime, -1);
			}
		}

		private void OnToolTipChange(bool visible)
		{
			if (TrayToolTipResolved == null)
			{
				return;
			}
			if (visible)
			{
				if (IsPopupOpen)
				{
					return;
				}
				RoutedEventArgs routedEventArgs = RaisePreviewTrayToolTipOpenEvent();
				if (!routedEventArgs.Handled)
				{
					TrayToolTipResolved.IsOpen = true;
					if (TrayToolTip != null)
					{
						RaiseToolTipOpenedEvent(TrayToolTip);
					}
					RaiseTrayToolTipOpenEvent();
				}
				return;
			}
			RoutedEventArgs routedEventArgs2 = RaisePreviewTrayToolTipCloseEvent();
			if (!routedEventArgs2.Handled)
			{
				if (TrayToolTip != null)
				{
					RaiseToolTipCloseEvent(TrayToolTip);
				}
				TrayToolTipResolved.IsOpen = false;
				RaiseTrayToolTipCloseEvent();
			}
		}

		private void CreateCustomToolTip()
		{
			ToolTip toolTip = TrayToolTip as ToolTip;
			if (toolTip == null && TrayToolTip != null)
			{
				toolTip = new ToolTip
				{
					Placement = PlacementMode.Mouse,
					HasDropShadow = false,
					BorderThickness = new Thickness(0.0),
					Background = System.Windows.Media.Brushes.Transparent,
					StaysOpen = true,
					Content = TrayToolTip
				};
			}
			else if (toolTip == null && !string.IsNullOrEmpty(ToolTipText))
			{
				toolTip = new ToolTip
				{
					Content = ToolTipText
				};
			}
			if (toolTip != null)
			{
				UpdateDataContext(toolTip, null, base.DataContext);
			}
			SetTrayToolTipResolved(toolTip);
		}

		private void WriteToolTipSettings()
		{
			iconData.ToolTipText = ToolTipText;
			if (messageSink.Version == NotifyIconVersion.Vista && string.IsNullOrEmpty(iconData.ToolTipText) && TrayToolTipResolved != null)
			{
				iconData.ToolTipText = "ToolTip";
			}
			Util.WriteIconData(ref iconData, NotifyCommand.Modify, IconDataMembers.Tip);
		}

		private void CreatePopup()
		{
			Popup popup = TrayPopup as Popup;
			if (popup == null && TrayPopup != null)
			{
				popup = new Popup
				{
					AllowsTransparency = true,
					PopupAnimation = PopupAnimation.None,
					Child = TrayPopup,
					Placement = PlacementMode.AbsolutePoint,
					StaysOpen = false
				};
			}
			if (popup != null)
			{
				UpdateDataContext(popup, null, base.DataContext);
			}
			SetTrayPopupResolved(popup);
		}

		private void ShowTrayPopup(Hardcodet.Wpf.TaskbarNotification.Interop.Point cursorPosition)
		{
			if (IsDisposed)
			{
				return;
			}
			RoutedEventArgs routedEventArgs = RaisePreviewTrayPopupOpenEvent();
			if (routedEventArgs.Handled || TrayPopup == null)
			{
				return;
			}
			TrayPopupResolved.Placement = PlacementMode.AbsolutePoint;
			TrayPopupResolved.HorizontalOffset = cursorPosition.X;
			TrayPopupResolved.VerticalOffset = cursorPosition.Y;
			TrayPopupResolved.IsOpen = true;
			IntPtr intPtr = IntPtr.Zero;
			if (TrayPopupResolved.Child != null)
			{
				HwndSource hwndSource = (HwndSource)PresentationSource.FromVisual(TrayPopupResolved.Child);
				if (hwndSource != null)
				{
					intPtr = hwndSource.Handle;
				}
			}
			if (intPtr == IntPtr.Zero)
			{
				intPtr = messageSink.MessageWindowHandle;
			}
			WinApi.SetForegroundWindow(intPtr);
			if (TrayPopup != null)
			{
				RaisePopupOpenedEvent(TrayPopup);
			}
			RaiseTrayPopupOpenEvent();
		}

		private void ShowContextMenu(Hardcodet.Wpf.TaskbarNotification.Interop.Point cursorPosition)
		{
			if (IsDisposed)
			{
				return;
			}
			RoutedEventArgs routedEventArgs = RaisePreviewTrayContextMenuOpenEvent();
			if (!routedEventArgs.Handled && base.ContextMenu != null)
			{
				base.ContextMenu.Placement = PlacementMode.AbsolutePoint;
				base.ContextMenu.HorizontalOffset = cursorPosition.X;
				base.ContextMenu.VerticalOffset = cursorPosition.Y;
				base.ContextMenu.IsOpen = true;
				IntPtr intPtr = IntPtr.Zero;
				HwndSource hwndSource = (HwndSource)PresentationSource.FromVisual(base.ContextMenu);
				if (hwndSource != null)
				{
					intPtr = hwndSource.Handle;
				}
				if (intPtr == IntPtr.Zero)
				{
					intPtr = messageSink.MessageWindowHandle;
				}
				WinApi.SetForegroundWindow(intPtr);
				RaiseTrayContextMenuOpenEvent();
			}
		}

		private void OnBalloonToolTipChanged(bool visible)
		{
			if (visible)
			{
				RaiseTrayBalloonTipShownEvent();
			}
			else
			{
				RaiseTrayBalloonTipClosedEvent();
			}
		}

		public void ShowBalloonTip(string title, string message, BalloonIcon symbol)
		{
			lock (lockObject)
			{
				ShowBalloonTip(title, message, symbol.GetBalloonFlag(), IntPtr.Zero);
			}
		}

		public void ShowBalloonTip(string title, string message, Icon customIcon, bool largeIcon = false)
		{
			if (customIcon == null)
			{
				throw new ArgumentNullException("customIcon");
			}
			lock (lockObject)
			{
				BalloonFlags balloonFlags = BalloonFlags.User;
				if (largeIcon)
				{
					balloonFlags |= BalloonFlags.LargeIcon;
				}
				ShowBalloonTip(title, message, balloonFlags, customIcon.Handle);
			}
		}

		private void ShowBalloonTip(string title, string message, BalloonFlags flags, IntPtr balloonIconHandle)
		{
			EnsureNotDisposed();
			iconData.BalloonText = message ?? string.Empty;
			iconData.BalloonTitle = title ?? string.Empty;
			iconData.BalloonFlags = flags;
			iconData.CustomBalloonIconHandle = balloonIconHandle;
			Util.WriteIconData(ref iconData, NotifyCommand.Modify, IconDataMembers.Icon | IconDataMembers.Info);
		}

		public void HideBalloonTip()
		{
			EnsureNotDisposed();
			iconData.BalloonText = (iconData.BalloonTitle = string.Empty);
			Util.WriteIconData(ref iconData, NotifyCommand.Modify, IconDataMembers.Info);
		}

		private void DoSingleClickAction(object state)
		{
			if (!IsDisposed)
			{
				Action action = singleClickTimerAction;
				if (action != null)
				{
					singleClickTimerAction = null;
					this.GetDispatcher().Invoke(action);
				}
			}
		}

		private void SetVersion()
		{
			iconData.VersionOrTimeout = 4u;
			bool flag = WinApi.Shell_NotifyIcon(NotifyCommand.SetVersion, ref iconData);
			if (!flag)
			{
				iconData.VersionOrTimeout = 3u;
				flag = Util.WriteIconData(ref iconData, NotifyCommand.SetVersion);
			}
			if (!flag)
			{
				iconData.VersionOrTimeout = 0u;
				flag = Util.WriteIconData(ref iconData, NotifyCommand.SetVersion);
			}
			if (!flag)
			{
				Debug.Fail("Could not set version");
			}
		}

		private void OnTaskbarCreated()
		{
			IsTaskbarIconCreated = false;
			CreateTaskbarIcon();
		}

		private void CreateTaskbarIcon()
		{
			lock (lockObject)
			{
				if (!IsTaskbarIconCreated && Util.WriteIconData(ref iconData, NotifyCommand.Add, IconDataMembers.Message | IconDataMembers.Icon | IconDataMembers.Tip))
				{
					SetVersion();
					messageSink.Version = (NotifyIconVersion)iconData.VersionOrTimeout;
					IsTaskbarIconCreated = true;
				}
			}
		}

		private void RemoveTaskbarIcon()
		{
			lock (lockObject)
			{
				if (IsTaskbarIconCreated)
				{
					Util.WriteIconData(ref iconData, NotifyCommand.Delete, IconDataMembers.Message);
					IsTaskbarIconCreated = false;
				}
			}
		}

		private void EnsureNotDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(base.Name ?? GetType().FullName);
			}
		}

		private void OnExit(object sender, EventArgs e)
		{
			Dispose();
		}

		~TaskbarIcon()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (IsDisposed || !disposing)
			{
				return;
			}
			lock (lockObject)
			{
				IsDisposed = true;
				if (Application.Current != null)
				{
					Application.Current.Exit -= OnExit;
				}
				singleClickTimer.Dispose();
				balloonCloseTimer.Dispose();
				messageSink.Dispose();
				RemoveTaskbarIcon();
			}
		}

		protected void SetTrayPopupResolved(Popup value)
		{
			SetValue(TrayPopupResolvedPropertyKey, value);
		}

		protected void SetTrayToolTipResolved(ToolTip value)
		{
			SetValue(TrayToolTipResolvedPropertyKey, value);
		}

		protected void SetCustomBalloon(Popup value)
		{
			SetValue(CustomBalloonPropertyKey, value);
		}

		private static void IconSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskbarIcon taskbarIcon = (TaskbarIcon)d;
			taskbarIcon.OnIconSourcePropertyChanged(e);
		}

		private void OnIconSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			ImageSource imageSource = (ImageSource)e.NewValue;
			if (!Util.IsDesignMode)
			{
				Icon = imageSource.ToIcon();
			}
		}

		private static void ToolTipTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskbarIcon taskbarIcon = (TaskbarIcon)d;
			taskbarIcon.OnToolTipTextPropertyChanged(e);
		}

		private void OnToolTipTextPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (TrayToolTip == null)
			{
				ToolTip trayToolTipResolved = TrayToolTipResolved;
				if (trayToolTipResolved == null)
				{
					CreateCustomToolTip();
				}
				else
				{
					trayToolTipResolved.Content = e.NewValue;
				}
			}
			WriteToolTipSettings();
		}

		private static void TrayToolTipPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskbarIcon taskbarIcon = (TaskbarIcon)d;
			taskbarIcon.OnTrayToolTipPropertyChanged(e);
		}

		private void OnTrayToolTipPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			CreateCustomToolTip();
			if (e.OldValue != null)
			{
				SetParentTaskbarIcon((DependencyObject)e.OldValue, null);
			}
			if (e.NewValue != null)
			{
				SetParentTaskbarIcon((DependencyObject)e.NewValue, this);
			}
			WriteToolTipSettings();
		}

		private static void TrayPopupPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskbarIcon taskbarIcon = (TaskbarIcon)d;
			taskbarIcon.OnTrayPopupPropertyChanged(e);
		}

		private void OnTrayPopupPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue != null)
			{
				SetParentTaskbarIcon((DependencyObject)e.OldValue, null);
			}
			if (e.NewValue != null)
			{
				SetParentTaskbarIcon((DependencyObject)e.NewValue, this);
			}
			CreatePopup();
		}

		private static void VisibilityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskbarIcon taskbarIcon = (TaskbarIcon)d;
			taskbarIcon.OnVisibilityPropertyChanged(e);
		}

		private void OnVisibilityPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if ((Visibility)e.NewValue == Visibility.Visible)
			{
				CreateTaskbarIcon();
			}
			else
			{
				RemoveTaskbarIcon();
			}
		}

		private void UpdateDataContext(FrameworkElement target, object oldDataContextValue, object newDataContextValue)
		{
			if (target != null && !target.IsDataContextDataBound() && (this == target.DataContext || object.Equals(oldDataContextValue, target.DataContext)))
			{
				target.DataContext = newDataContextValue ?? this;
			}
		}

		private static void DataContextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskbarIcon taskbarIcon = (TaskbarIcon)d;
			taskbarIcon.OnDataContextPropertyChanged(e);
		}

		private void OnDataContextPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			object newValue = e.NewValue;
			object oldValue = e.OldValue;
			UpdateDataContext(TrayPopupResolved, oldValue, newValue);
			UpdateDataContext(TrayToolTipResolved, oldValue, newValue);
			UpdateDataContext(base.ContextMenu, oldValue, newValue);
		}

		private static void ContextMenuPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskbarIcon taskbarIcon = (TaskbarIcon)d;
			taskbarIcon.OnContextMenuPropertyChanged(e);
		}

		private void OnContextMenuPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue != null)
			{
				SetParentTaskbarIcon((DependencyObject)e.OldValue, null);
			}
			if (e.NewValue != null)
			{
				SetParentTaskbarIcon((DependencyObject)e.NewValue, this);
			}
			UpdateDataContext((ContextMenu)e.NewValue, null, base.DataContext);
		}

		protected RoutedEventArgs RaiseTrayLeftMouseDownEvent()
		{
			return RaiseTrayLeftMouseDownEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayLeftMouseDownEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayLeftMouseDownEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayRightMouseDownEvent()
		{
			return RaiseTrayRightMouseDownEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayRightMouseDownEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayRightMouseDownEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayMiddleMouseDownEvent()
		{
			return RaiseTrayMiddleMouseDownEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayMiddleMouseDownEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayMiddleMouseDownEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayLeftMouseUpEvent()
		{
			return RaiseTrayLeftMouseUpEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayLeftMouseUpEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayLeftMouseUpEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayRightMouseUpEvent()
		{
			return RaiseTrayRightMouseUpEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayRightMouseUpEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayRightMouseUpEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayMiddleMouseUpEvent()
		{
			return RaiseTrayMiddleMouseUpEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayMiddleMouseUpEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayMiddleMouseUpEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayMouseDoubleClickEvent()
		{
			RoutedEventArgs result = RaiseTrayMouseDoubleClickEvent(this);
			DoubleClickCommand.ExecuteIfEnabled(DoubleClickCommandParameter, DoubleClickCommandTarget ?? this);
			return result;
		}

		internal static RoutedEventArgs RaiseTrayMouseDoubleClickEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayMouseDoubleClickEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayMouseMoveEvent()
		{
			return RaiseTrayMouseMoveEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayMouseMoveEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayMouseMoveEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayBalloonTipShownEvent()
		{
			return RaiseTrayBalloonTipShownEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayBalloonTipShownEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayBalloonTipShownEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayBalloonTipClosedEvent()
		{
			return RaiseTrayBalloonTipClosedEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayBalloonTipClosedEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayBalloonTipClosedEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayBalloonTipClickedEvent()
		{
			return RaiseTrayBalloonTipClickedEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayBalloonTipClickedEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayBalloonTipClickedEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayContextMenuOpenEvent()
		{
			return RaiseTrayContextMenuOpenEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayContextMenuOpenEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayContextMenuOpenEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaisePreviewTrayContextMenuOpenEvent()
		{
			return RaisePreviewTrayContextMenuOpenEvent(this);
		}

		internal static RoutedEventArgs RaisePreviewTrayContextMenuOpenEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(PreviewTrayContextMenuOpenEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayPopupOpenEvent()
		{
			return RaiseTrayPopupOpenEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayPopupOpenEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayPopupOpenEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaisePreviewTrayPopupOpenEvent()
		{
			return RaisePreviewTrayPopupOpenEvent(this);
		}

		internal static RoutedEventArgs RaisePreviewTrayPopupOpenEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(PreviewTrayPopupOpenEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayToolTipOpenEvent()
		{
			return RaiseTrayToolTipOpenEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayToolTipOpenEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayToolTipOpenEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaisePreviewTrayToolTipOpenEvent()
		{
			return RaisePreviewTrayToolTipOpenEvent(this);
		}

		internal static RoutedEventArgs RaisePreviewTrayToolTipOpenEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(PreviewTrayToolTipOpenEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaiseTrayToolTipCloseEvent()
		{
			return RaiseTrayToolTipCloseEvent(this);
		}

		internal static RoutedEventArgs RaiseTrayToolTipCloseEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(TrayToolTipCloseEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		protected RoutedEventArgs RaisePreviewTrayToolTipCloseEvent()
		{
			return RaisePreviewTrayToolTipCloseEvent(this);
		}

		internal static RoutedEventArgs RaisePreviewTrayToolTipCloseEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(PreviewTrayToolTipCloseEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		public static void AddPopupOpenedHandler(DependencyObject element, RoutedEventHandler handler)
		{
			RoutedEventHelper.AddHandler(element, PopupOpenedEvent, handler);
		}

		public static void RemovePopupOpenedHandler(DependencyObject element, RoutedEventHandler handler)
		{
			RoutedEventHelper.RemoveHandler(element, PopupOpenedEvent, handler);
		}

		internal static RoutedEventArgs RaisePopupOpenedEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(PopupOpenedEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		public static void AddToolTipOpenedHandler(DependencyObject element, RoutedEventHandler handler)
		{
			RoutedEventHelper.AddHandler(element, ToolTipOpenedEvent, handler);
		}

		public static void RemoveToolTipOpenedHandler(DependencyObject element, RoutedEventHandler handler)
		{
			RoutedEventHelper.RemoveHandler(element, ToolTipOpenedEvent, handler);
		}

		internal static RoutedEventArgs RaiseToolTipOpenedEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(ToolTipOpenedEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		public static void AddToolTipCloseHandler(DependencyObject element, RoutedEventHandler handler)
		{
			RoutedEventHelper.AddHandler(element, ToolTipCloseEvent, handler);
		}

		public static void RemoveToolTipCloseHandler(DependencyObject element, RoutedEventHandler handler)
		{
			RoutedEventHelper.RemoveHandler(element, ToolTipCloseEvent, handler);
		}

		internal static RoutedEventArgs RaiseToolTipCloseEvent(DependencyObject target)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(ToolTipCloseEvent);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		public static void AddBalloonShowingHandler(DependencyObject element, RoutedEventHandler handler)
		{
			RoutedEventHelper.AddHandler(element, BalloonShowingEvent, handler);
		}

		public static void RemoveBalloonShowingHandler(DependencyObject element, RoutedEventHandler handler)
		{
			RoutedEventHelper.RemoveHandler(element, BalloonShowingEvent, handler);
		}

		internal static RoutedEventArgs RaiseBalloonShowingEvent(DependencyObject target, TaskbarIcon source)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(BalloonShowingEvent, source);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		public static void AddBalloonClosingHandler(DependencyObject element, RoutedEventHandler handler)
		{
			RoutedEventHelper.AddHandler(element, BalloonClosingEvent, handler);
		}

		public static void RemoveBalloonClosingHandler(DependencyObject element, RoutedEventHandler handler)
		{
			RoutedEventHelper.RemoveHandler(element, BalloonClosingEvent, handler);
		}

		internal static RoutedEventArgs RaiseBalloonClosingEvent(DependencyObject target, TaskbarIcon source)
		{
			if (target == null)
			{
				return null;
			}
			RoutedEventArgs routedEventArgs = new RoutedEventArgs(BalloonClosingEvent, source);
			RoutedEventHelper.RaiseEvent(target, routedEventArgs);
			return routedEventArgs;
		}

		public static TaskbarIcon GetParentTaskbarIcon(DependencyObject d)
		{
			return (TaskbarIcon)d.GetValue(ParentTaskbarIconProperty);
		}

		public static void SetParentTaskbarIcon(DependencyObject d, TaskbarIcon value)
		{
			d.SetValue(ParentTaskbarIconProperty, value);
		}

		static TaskbarIcon()
		{
			TrayPopupResolvedPropertyKey = DependencyProperty.RegisterReadOnly("TrayPopupResolved", typeof(Popup), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null));
			TrayPopupResolvedProperty = TrayPopupResolvedPropertyKey.DependencyProperty;
			TrayToolTipResolvedPropertyKey = DependencyProperty.RegisterReadOnly("TrayToolTipResolved", typeof(ToolTip), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null));
			TrayToolTipResolvedProperty = TrayToolTipResolvedPropertyKey.DependencyProperty;
			CustomBalloonPropertyKey = DependencyProperty.RegisterReadOnly("CustomBalloon", typeof(Popup), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null));
			CustomBalloonProperty = CustomBalloonPropertyKey.DependencyProperty;
			IconSourceProperty = DependencyProperty.Register("IconSource", typeof(ImageSource), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null, IconSourcePropertyChanged));
			ToolTipTextProperty = DependencyProperty.Register("ToolTipText", typeof(string), typeof(TaskbarIcon), new FrameworkPropertyMetadata(string.Empty, ToolTipTextPropertyChanged));
			TrayToolTipProperty = DependencyProperty.Register("TrayToolTip", typeof(UIElement), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null, TrayToolTipPropertyChanged));
			TrayPopupProperty = DependencyProperty.Register("TrayPopup", typeof(UIElement), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null, TrayPopupPropertyChanged));
			MenuActivationProperty = DependencyProperty.Register("MenuActivation", typeof(PopupActivationMode), typeof(TaskbarIcon), new FrameworkPropertyMetadata(PopupActivationMode.RightClick));
			PopupActivationProperty = DependencyProperty.Register("PopupActivation", typeof(PopupActivationMode), typeof(TaskbarIcon), new FrameworkPropertyMetadata(PopupActivationMode.LeftClick));
			DoubleClickCommandProperty = DependencyProperty.Register("DoubleClickCommand", typeof(ICommand), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null));
			DoubleClickCommandParameterProperty = DependencyProperty.Register("DoubleClickCommandParameter", typeof(object), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null));
			DoubleClickCommandTargetProperty = DependencyProperty.Register("DoubleClickCommandTarget", typeof(IInputElement), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null));
			LeftClickCommandProperty = DependencyProperty.Register("LeftClickCommand", typeof(ICommand), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null));
			LeftClickCommandParameterProperty = DependencyProperty.Register("LeftClickCommandParameter", typeof(object), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null));
			LeftClickCommandTargetProperty = DependencyProperty.Register("LeftClickCommandTarget", typeof(IInputElement), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null));
			NoLeftClickDelayProperty = DependencyProperty.Register("NoLeftClickDelay", typeof(bool), typeof(TaskbarIcon), new FrameworkPropertyMetadata(false));
			TrayLeftMouseDownEvent = EventManager.RegisterRoutedEvent("TrayLeftMouseDown", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayRightMouseDownEvent = EventManager.RegisterRoutedEvent("TrayRightMouseDown", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayMiddleMouseDownEvent = EventManager.RegisterRoutedEvent("TrayMiddleMouseDown", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayLeftMouseUpEvent = EventManager.RegisterRoutedEvent("TrayLeftMouseUp", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayRightMouseUpEvent = EventManager.RegisterRoutedEvent("TrayRightMouseUp", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayMiddleMouseUpEvent = EventManager.RegisterRoutedEvent("TrayMiddleMouseUp", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayMouseDoubleClickEvent = EventManager.RegisterRoutedEvent("TrayMouseDoubleClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayMouseMoveEvent = EventManager.RegisterRoutedEvent("TrayMouseMove", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayBalloonTipShownEvent = EventManager.RegisterRoutedEvent("TrayBalloonTipShown", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayBalloonTipClosedEvent = EventManager.RegisterRoutedEvent("TrayBalloonTipClosed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayBalloonTipClickedEvent = EventManager.RegisterRoutedEvent("TrayBalloonTipClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayContextMenuOpenEvent = EventManager.RegisterRoutedEvent("TrayContextMenuOpen", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			PreviewTrayContextMenuOpenEvent = EventManager.RegisterRoutedEvent("PreviewTrayContextMenuOpen", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayPopupOpenEvent = EventManager.RegisterRoutedEvent("TrayPopupOpen", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			PreviewTrayPopupOpenEvent = EventManager.RegisterRoutedEvent("PreviewTrayPopupOpen", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayToolTipOpenEvent = EventManager.RegisterRoutedEvent("TrayToolTipOpen", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			PreviewTrayToolTipOpenEvent = EventManager.RegisterRoutedEvent("PreviewTrayToolTipOpen", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			TrayToolTipCloseEvent = EventManager.RegisterRoutedEvent("TrayToolTipClose", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			PreviewTrayToolTipCloseEvent = EventManager.RegisterRoutedEvent("PreviewTrayToolTipClose", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			PopupOpenedEvent = EventManager.RegisterRoutedEvent("PopupOpened", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			ToolTipOpenedEvent = EventManager.RegisterRoutedEvent("ToolTipOpened", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			ToolTipCloseEvent = EventManager.RegisterRoutedEvent("ToolTipClose", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			BalloonShowingEvent = EventManager.RegisterRoutedEvent("BalloonShowing", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			BalloonClosingEvent = EventManager.RegisterRoutedEvent("BalloonClosing", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TaskbarIcon));
			ParentTaskbarIconProperty = DependencyProperty.RegisterAttached("ParentTaskbarIcon", typeof(TaskbarIcon), typeof(TaskbarIcon), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
			PropertyMetadata typeMetadata = new PropertyMetadata(Visibility.Visible, VisibilityPropertyChanged);
			UIElement.VisibilityProperty.OverrideMetadata(typeof(TaskbarIcon), typeMetadata);
			typeMetadata = new FrameworkPropertyMetadata(DataContextPropertyChanged);
			FrameworkElement.DataContextProperty.OverrideMetadata(typeof(TaskbarIcon), typeMetadata);
			typeMetadata = new FrameworkPropertyMetadata(ContextMenuPropertyChanged);
			FrameworkElement.ContextMenuProperty.OverrideMetadata(typeof(TaskbarIcon), typeMetadata);
		}
	}
}

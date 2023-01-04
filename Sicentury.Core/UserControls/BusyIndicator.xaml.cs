/************************************************************************
*@file FrameworkLocal\UIClient\CenterViews\Core\UserControls\BusyIndicator.cs
* @author Su Liang
* @Date 2022-08-03
*
* @copyright &copy Sicentury Inc.
*
* @brief Reconstructed to support rich functions.
*
* @details
* *****************************************************************************/


using System.Windows;

namespace Sicentury.Core.UserControls
{
    /// <summary>
    /// Interaction logic for BusyIndicator.xaml
    /// </summary>
    public partial class BusyIndicator
    {
        public BusyIndicator()
        {
            InitializeComponent();
        }

        #region Properties

        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
            "Message", typeof(string), typeof(BusyIndicator), new PropertyMetadata(default(string)));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        #endregion

        #region Routed Events

        // Register a custom routed event using the Bubble routing strategy.
        public static readonly RoutedEvent CanceledEvent = EventManager.RegisterRoutedEvent(
            name: nameof(Canceled),
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(RoutedEventHandler),
            ownerType: typeof(ParameterNodeTreeViewControl));

        // Provide CLR accessors for assigning an event handler.
        public event RoutedEventHandler Canceled
        {
            add => AddHandler(CanceledEvent, value);
            remove => RemoveHandler(CanceledEvent, value);
        }


        #endregion

        #region Events

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CanceledEvent));
        }

        #endregion

    }
}

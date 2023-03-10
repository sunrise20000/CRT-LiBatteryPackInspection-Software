//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;
using MS.Internal;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    ///     A control for displaying a row of the DataGrid.
    ///     A row represents a data item in the DataGrid.
    ///     A row displays a cell for each column of the DataGrid.
    /// 
    ///     The data item for the row is added n times to the row's Items collection, 
    ///     where n is the number of columns in the DataGrid.
    /// </summary>
    public class DataGridRow : Control
    {
        #region Constructors

        /// <summary>
        ///     Instantiates global information.
        /// </summary>
        static DataGridRow()
        {
            VisibilityProperty.OverrideMetadata(typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(null, OnCoerceVisibility));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(typeof(EGC.DataGridRow)));
            ItemsPanelProperty.OverrideMetadata(typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(new ItemsPanelTemplate(new FrameworkElementFactory(typeof(EGC.DataGridCellsPanel)))));
            FocusableProperty.OverrideMetadata(typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(false));
            BackgroundProperty.OverrideMetadata(typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowPropertyChanged, OnCoerceBackground));
            BindingGroupProperty.OverrideMetadata(typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(OnNotifyRowPropertyChanged));

            // Set SnapsToDevicePixels to true so that this element can draw grid lines.  The metadata options are so that the property value doesn't inherit down the tree from here.
            SnapsToDevicePixelsProperty.OverrideMetadata(typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange));
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public DataGridRow()
        {
            _tracker = new EGC.ContainerTracking<EGC.DataGridRow>(this);
        }

        #endregion

        #region Data Item

        /// <summary>
        ///     The item that the row represents. This item is an entry in the list of items from the DataGrid.
        ///     From this item, cells are generated for each column in the DataGrid.
        /// </summary>
        public object Item
        {
            get { return GetValue(ItemProperty); }
            set { SetValue(ItemProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the Item property.
        /// </summary>
        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register("Item", typeof(object), typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyRowPropertyChanged)));

        /// <summary>
        ///     Called when the value of the Item property changes.
        /// </summary>
        /// <param name="oldItem">The old value of Item.</param>
        /// <param name="newItem">The new value of Item.</param>
        protected virtual void OnItemChanged(object oldItem, object newItem)
        {
            EGC.DataGridCellsPresenter cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                cellsPresenter.Item = newItem;
            }

            // Update the event source if AutomationPeer exist
            EGC.DataGridRowAutomationPeer peer = UIElementAutomationPeer.FromElement(this) as EGC.DataGridRowAutomationPeer;
            if (peer != null)
            {
                peer.UpdateEventSource();
            }
        }

        #endregion

        #region Template

        /// <summary>
        ///     A template that will generate the panel that arranges the cells in this row.
        /// </summary>
        /// <remarks>
        ///     The template for the row should contain an ItemsControl that template binds to this property.
        /// </remarks>
        public ItemsPanelTemplate ItemsPanel
        {
            get { return (ItemsPanelTemplate)GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the ItemsPanel property.
        /// </summary>
        public static readonly DependencyProperty ItemsPanelProperty = ItemsControl.ItemsPanelProperty.AddOwner(typeof(EGC.DataGridRow));

        /// <summary>
        ///     Clears the CellsPresenter and DetailsPresenter references on Template change.
        /// </summary>
        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);
            CellsPresenter = null;
            DetailsPresenter = null;
        }

        #endregion

        #region Row Header

        /// <summary>
        ///     The object representing the Row Header.  
        /// </summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the Header property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(object), typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyRowPropertyChanged)));

        /// <summary>
        ///     Called when the value of the Header property changes.
        /// </summary>
        /// <param name="oldHeader">The old value of Header</param>
        /// <param name="newHeader">The new value of Header</param>
        protected virtual void OnHeaderChanged(object oldHeader, object newHeader)
        {   
        }

        /// <summary>
        ///     The object representing the Row Header style.  
        /// </summary>
        public Style HeaderStyle
        {
            get { return (Style)GetValue(HeaderStyleProperty); }
            set { SetValue(HeaderStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderStyle property.
        /// </summary>
        public static readonly DependencyProperty HeaderStyleProperty =
            DependencyProperty.Register("HeaderStyle", typeof(Style), typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowAndRowHeaderPropertyChanged, OnCoerceHeaderStyle));

        /// <summary>
        ///     The object representing the Row Header template.  
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderTemplate property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowAndRowHeaderPropertyChanged, OnCoerceHeaderTemplate));

        /// <summary>
        ///     The object representing the Row Header template selector.  
        /// </summary>
        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(HeaderTemplateSelectorProperty); }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateSelectorProperty =
            DependencyProperty.Register("HeaderTemplateSelector", typeof(DataTemplateSelector), typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowAndRowHeaderPropertyChanged, OnCoerceHeaderTemplateSelector));

        /// <summary>
        /// Template used to visually indicate an error in row Validation.
        /// </summary>
        public ControlTemplate ValidationErrorTemplate
        {
            get { return (ControlTemplate)GetValue(ValidationErrorTemplateProperty); }
            set { SetValue(ValidationErrorTemplateProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for the ValidationErrorTemplate property.
        /// </summary>
        public static readonly DependencyProperty ValidationErrorTemplateProperty =
            DependencyProperty.Register("ValidationErrorTemplate", typeof(ControlTemplate), typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowPropertyChanged, OnCoerceValidationErrorTemplate));

        #endregion

        #region Row Details

        /// <summary>
        ///     The object representing the Row Details template.  
        /// </summary>
        public DataTemplate DetailsTemplate
        {
            get { return (DataTemplate)GetValue(DetailsTemplateProperty); }
            set { SetValue(DetailsTemplateProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the DetailsTemplate property.
        /// </summary>
        public static readonly DependencyProperty DetailsTemplateProperty =
            DependencyProperty.Register("DetailsTemplate", typeof(DataTemplate), typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowAndDetailsPropertyChanged, OnCoerceDetailsTemplate));

        /// <summary>
        ///     The object representing the Row Details template selector.  
        /// </summary>
        public DataTemplateSelector DetailsTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(DetailsTemplateSelectorProperty); }
            set { SetValue(DetailsTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the DetailsTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty DetailsTemplateSelectorProperty =
            DependencyProperty.Register("DetailsTemplateSelector", typeof(DataTemplateSelector), typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowAndDetailsPropertyChanged, OnCoerceDetailsTemplateSelector));

        /// <summary>
        ///     The Visibility of the Details presenter
        /// </summary>
        public Visibility DetailsVisibility
        {
            get { return (Visibility)GetValue(DetailsVisibilityProperty); }
            set { SetValue(DetailsVisibilityProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the DetailsVisibility property.
        /// </summary>
        public static readonly DependencyProperty DetailsVisibilityProperty =
            DependencyProperty.Register("DetailsVisibility", typeof(Visibility), typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(Visibility.Collapsed, OnNotifyDetailsVisibilityChanged, OnCoerceDetailsVisibility));

        internal enum RowDetailsEventStatus : byte
        {
            Disabled, // LoadingRowDetails should not be fired in response to changes to DetailsVisibilty.
            Pending,  // LoadingRowDetails can be called if the row is loaded or if DetailsVisibilty becomes visible.
            Loaded,   // LoadingRowDetails has been called and UnloadingRowDetails can be called.
        }

        internal RowDetailsEventStatus DetailsEventStatus
        {
            get
            {
                return _detailsEventStatus;
            }

            set
            {
                _detailsEventStatus = value;
            }
        }

        #endregion

        #region Row Generation

        /// <summary>
        /// We can't override the metadata for a read only property, so we'll get the property change notification for AlternationIndexProperty this way instead.
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == AlternationIndexProperty)
            {
                NotifyPropertyChanged(this, e, NotificationTarget.Rows);
            }
        }

        /// <summary>
        ///     Prepares a row container for active use.
        /// </summary>
        /// <remarks>
        ///     Instantiates or updates a MultipleCopiesCollection ItemsSource in
        ///     order that cells be generated.
        /// </remarks>
        /// <param name="item">The data item that the row represents.</param>
        /// <param name="owningDataGrid">The DataGrid owner.</param>
        internal void PrepareRow(object item, EGC.DataGrid owningDataGrid)
        {
            bool fireOwnerChanged = (_owner != owningDataGrid);
            Debug.Assert(_owner == null || _owner == owningDataGrid, "_owner should be null before PrepareRow is called or the same as the owningDataGrid.");
            bool forcePrepareCells = false;
            _owner = owningDataGrid;

            if (this != item)
            {
                if (Item != item)
                {
                    Item = item;
                }
                else
                {
                    forcePrepareCells = true;
                }
            }

            if (IsEditing)
            {
                // If IsEditing was left on and this container was recycled, reset it here.
                IsEditing = false;
            }

            // Since we just changed _owner we need to invalidate all child properties that rely on a value supplied by the DataGrid.
            // A common scenario is when a recycled Row was detached from the visual tree and has just been reattached (we always clear out the 
            // owner when recycling a container).
            if (fireOwnerChanged)
            {
                SyncProperties(forcePrepareCells);
            }

            // Re-run validation, but wait until Binding has occured.
            Dispatcher.BeginInvoke(new DispatcherOperationCallback(DelayedValidateWithoutUpdate), DispatcherPriority.DataBind, BindingGroup);
        }

        /// <summary>
        ///     Clears the row of references.
        /// </summary>
        internal void ClearRow(EGC.DataGrid owningDataGrid)
        {
            Debug.Assert(_owner == owningDataGrid, "_owner should be the same as the DataGrid that is clearing the row.");

            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                PersistAttachedItemValue(cellsPresenter, EGC.DataGridCellsPresenter.HeightProperty);
            }

            PersistAttachedItemValue(this, DetailsVisibilityProperty);

            _owner = null;
        }

        private void PersistAttachedItemValue(DependencyObject objectWithProperty, DependencyProperty property)
        {
            ValueSource valueSource = DependencyPropertyHelper.GetValueSource(objectWithProperty, property);
            if (valueSource.BaseValueSource == BaseValueSource.Local)
            {
                // attach the local value to the item so it can be restored later.
                _owner.ItemAttachedStorage.SetValue(Item, property, objectWithProperty.GetValue(property));
                objectWithProperty.ClearValue(property);
            }
        }

        private void RestoreAttachedItemValue(DependencyObject objectWithProperty, DependencyProperty property)
        {
            object value;
            if (_owner.ItemAttachedStorage.TryGetValue(Item, property, out value))
            {
                objectWithProperty.SetValue(property, value);
            }
        }

        /// <summary>
        ///     Used by the DataGrid owner to send notifications to the row container.
        /// </summary>
        internal EGC.ContainerTracking<EGC.DataGridRow> Tracker
        {
            get { return _tracker; }
        }

        #endregion

        #region Row Resizing

        internal void OnRowResizeStarted()
        {
            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                _cellsPresenterResizeHeight = cellsPresenter.Height;
            }
        }

        internal void OnRowResize(double changeAmount)
        {
            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                double newHeight = cellsPresenter.ActualHeight + changeAmount;

                // clamp the CellsPresenter size to the RowHeader size or MinHeight because the header wont shrink any smaller.
                double minHeight = Math.Max(RowHeader.DesiredSize.Height, MinHeight);
                if (DoubleUtil.LessThan(newHeight, minHeight))
                {
                    newHeight = minHeight;
                }

                // clamp the CellsPresenter size to the MaxHeight of Row, because row wouldn't grow any larger
                double maxHeight = MaxHeight;
                if (DoubleUtil.GreaterThan(newHeight, maxHeight))
                {
                    newHeight = maxHeight;
                }

                cellsPresenter.Height = newHeight;
            }
        }

        internal void OnRowResizeCompleted(bool canceled)
        {
            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null && canceled)
            {
                cellsPresenter.Height = _cellsPresenterResizeHeight;
            }
        }

        internal void OnRowResizeReset()
        {
            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                cellsPresenter.ClearValue(EGC.DataGridCellsPresenter.HeightProperty);
                if (_owner != null)
                {
                    _owner.ItemAttachedStorage.ClearValue(Item, EGC.DataGridCellsPresenter.HeightProperty);
                }
            }
        }

        #endregion

        #region Columns Notification

        /// <summary>
        ///     Notification from the DataGrid that the columns collection has changed.
        /// </summary>
        /// <param name="columns">The columns collection.</param>
        /// <param name="e">The event arguments from the collection's change event.</param>
        protected internal virtual void OnColumnsChanged(ObservableCollection<EGC.DataGridColumn> columns, NotifyCollectionChangedEventArgs e)
        {
            EGC.DataGridCellsPresenter cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                cellsPresenter.OnColumnsChanged(columns, e);
            }
        }

        #endregion

        #region Property Coercion

        private static object OnCoerceHeaderStyle(DependencyObject d, object baseValue)
        {
            var row = (EGC.DataGridRow)d;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                row, 
                baseValue, 
                HeaderStyleProperty,
                row.DataGridOwner,
                EGC.DataGrid.RowHeaderStyleProperty);
        }

        private static object OnCoerceHeaderTemplate(DependencyObject d, object baseValue)
        {
            var row = (EGC.DataGridRow)d;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                row, 
                baseValue, 
                HeaderTemplateProperty,
                row.DataGridOwner,
                EGC.DataGrid.RowHeaderTemplateProperty);
        }

        private static object OnCoerceHeaderTemplateSelector(DependencyObject d, object baseValue)
        {
            var row = (EGC.DataGridRow)d;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                row, 
                baseValue, 
                HeaderTemplateSelectorProperty,
                row.DataGridOwner,
                EGC.DataGrid.RowHeaderTemplateSelectorProperty);
        }

        private static object OnCoerceBackground(DependencyObject d, object baseValue)
        {
            var row = (EGC.DataGridRow)d;
            object coercedValue = baseValue;

            switch (row.AlternationIndex)
            {
                case 0:
                    coercedValue = EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                        row, 
                        baseValue, 
                        BackgroundProperty,
                        row.DataGridOwner,
                        EGC.DataGrid.RowBackgroundProperty);

                    break;
                case 1:
                    coercedValue = EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                        row, 
                        baseValue, 
                        BackgroundProperty,
                        row.DataGridOwner,
                        EGC.DataGrid.AlternatingRowBackgroundProperty);

                    break;
            }

            return coercedValue;       
        }

        private static object OnCoerceValidationErrorTemplate(DependencyObject d, object baseValue)
        {
            var row = (EGC.DataGridRow)d;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                row, 
                baseValue, 
                ValidationErrorTemplateProperty,
                row.DataGridOwner,
                EGC.DataGrid.RowValidationErrorTemplateProperty);
        }

        private static object OnCoerceDetailsTemplate(DependencyObject d, object baseValue)
        {
            var row = (EGC.DataGridRow)d;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                row, 
                baseValue, 
                DetailsTemplateProperty,
                row.DataGridOwner,
                EGC.DataGrid.RowDetailsTemplateProperty);
        }

        private static object OnCoerceDetailsTemplateSelector(DependencyObject d, object baseValue)
        {
            var row = (EGC.DataGridRow)d;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                row, 
                baseValue, 
                DetailsTemplateSelectorProperty,
                row.DataGridOwner,
                EGC.DataGrid.RowDetailsTemplateSelectorProperty);
        }

        private static object OnCoerceDetailsVisibility(DependencyObject d, object baseValue)
        {
            var row = (EGC.DataGridRow)d;
            object visibility = EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                row, 
                baseValue, 
                DetailsVisibilityProperty,
                row.DataGridOwner,
                EGC.DataGrid.RowDetailsVisibilityModeProperty);

            if (visibility is EGC.DataGridRowDetailsVisibilityMode)
            {
                var visibilityMode = (EGC.DataGridRowDetailsVisibilityMode)visibility;
                var hasDetailsTemplate = row.DetailsTemplate != null || row.DetailsTemplateSelector != null;
                var isRealItem = row.Item != CollectionView.NewItemPlaceholder;
                switch (visibilityMode)
                {
                    case EGC.DataGridRowDetailsVisibilityMode.Collapsed:
                        visibility = Visibility.Collapsed;
                        break;
                    case EGC.DataGridRowDetailsVisibilityMode.Visible:
                        visibility = hasDetailsTemplate && isRealItem ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case EGC.DataGridRowDetailsVisibilityMode.VisibleWhenSelected:
                        visibility = row.IsSelected && hasDetailsTemplate && isRealItem ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    default:
                        visibility = Visibility.Collapsed;
                        break;
                }
            }

            return visibility;
        }

        /// <summary>
        ///     Coerces Visibility so that the NewItemPlaceholder doesn't show up while you're entering a new Item
        /// </summary>
        private static object OnCoerceVisibility(DependencyObject d, object baseValue)
        {
            var row = (EGC.DataGridRow)d;
            var owningDataGrid = row.DataGridOwner;
            if (row.Item == CollectionView.NewItemPlaceholder && owningDataGrid != null)
            {
                return owningDataGrid.PlaceholderVisibility;
            }
            else
            {
                return baseValue;
            }
        }

        #endregion

        #region Notification Propagation

        private static void OnNotifyRowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as EGC.DataGridRow).NotifyPropertyChanged(d, e, NotificationTarget.Rows);
        }

        private static void OnNotifyRowAndRowHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as EGC.DataGridRow).NotifyPropertyChanged(d, e, NotificationTarget.Rows | NotificationTarget.RowHeaders);
        }

        private static void OnNotifyRowAndDetailsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as EGC.DataGridRow).NotifyPropertyChanged(d, e, NotificationTarget.Rows | NotificationTarget.DetailsPresenter);
        }

        private static void OnNotifyDetailsVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var row = (EGC.DataGridRow)d;

            // Notify the DataGrid at Loaded priority so the template has time to expland.
            Dispatcher.CurrentDispatcher.BeginInvoke(new DispatcherOperationCallback(DelayedRowDetailsVisibilityChanged), DispatcherPriority.Loaded, row);

            row.NotifyPropertyChanged(d, e, NotificationTarget.Rows | NotificationTarget.DetailsPresenter);
        }

        /// <summary>
        ///     Notifies the DataGrid that the visibility is changed.  This is intended to be Invoked at lower than Layout priority to give the template time to expand.
        /// </summary>
        private static object DelayedRowDetailsVisibilityChanged(object arg)
        {
            var row = (EGC.DataGridRow)arg;
            var dataGrid = row.DataGridOwner;
            var detailsElement = row.DetailsPresenter != null ? row.DetailsPresenter.DetailsElement : null;
            if (dataGrid != null)
            {
                var detailsEventArgs = new EGC.DataGridRowDetailsEventArgs(row, detailsElement);
                dataGrid.OnRowDetailsVisibilityChanged(detailsEventArgs);
            }

            return null;
        }

        /// <summary>
        ///     Set by the CellsPresenter when it is created.  Used by the Row to send down property change notifications.
        /// </summary>
        internal EGC.DataGridCellsPresenter CellsPresenter
        {
            get { return _cellsPresenter; }
            set { _cellsPresenter = value; }
        }

        /// <summary>
        ///     Set by the DetailsPresenter when it is created.  Used by the Row to send down property change notifications.
        /// </summary>
        internal EGC.DataGridDetailsPresenter DetailsPresenter
        {
            get { return _detailsPresenter; }
            set { _detailsPresenter = value; }
        }

        /// <summary>
        ///     Set by the RowHeader when it is created.  Used by the Row to send down property change notifications.
        /// </summary>
        internal EGC.DataGridRowHeader RowHeader
        {
            get { return _rowHeader; }
            set { _rowHeader = value; }
        }

        /// <summary>
        ///     General notification for DependencyProperty changes from the grid or from columns.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e, NotificationTarget target)
        {
            NotifyPropertyChanged(d, string.Empty, e, target);
        }

        /// <summary>
        ///     General notification for DependencyProperty changes from the grid or from columns.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, string propertyName, DependencyPropertyChangedEventArgs e, NotificationTarget target)
        {
            if (EGC.DataGridHelper.ShouldNotifyRows(target))
            {
                if (e.Property == EGC.DataGrid.RowBackgroundProperty || e.Property == EGC.DataGrid.AlternatingRowBackgroundProperty ||
                    e.Property == BackgroundProperty || e.Property == AlternationIndexProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, BackgroundProperty);
                }
                else if (e.Property == EGC.DataGrid.RowHeaderStyleProperty || e.Property == HeaderStyleProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, HeaderStyleProperty);
                }
                else if (e.Property == EGC.DataGrid.RowHeaderTemplateProperty || e.Property == HeaderTemplateProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, HeaderTemplateProperty);
                }
                else if (e.Property == EGC.DataGrid.RowHeaderTemplateSelectorProperty || e.Property == HeaderTemplateSelectorProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, HeaderTemplateSelectorProperty);
                }
                else if (e.Property == EGC.DataGrid.RowValidationErrorTemplateProperty || e.Property == ValidationErrorTemplateProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, ValidationErrorTemplateProperty);
                }
                else if (e.Property == EGC.DataGrid.RowDetailsTemplateProperty || e.Property == DetailsTemplateProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, DetailsTemplateProperty);
                    EGC.DataGridHelper.TransferProperty(this, DetailsVisibilityProperty);
                }
                else if (e.Property == EGC.DataGrid.RowDetailsTemplateSelectorProperty || e.Property == DetailsTemplateSelectorProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, DetailsTemplateSelectorProperty);
                    EGC.DataGridHelper.TransferProperty(this, DetailsVisibilityProperty);
                }
                else if (e.Property == EGC.DataGrid.RowDetailsVisibilityModeProperty || e.Property == DetailsVisibilityProperty || e.Property == IsSelectedProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, DetailsVisibilityProperty);
                }
                else if (e.Property == ItemProperty)
                {
                    OnItemChanged(e.OldValue, e.NewValue);
                }
                else if (e.Property == HeaderProperty)
                {
                    OnHeaderChanged(e.OldValue, e.NewValue);
                }
                else if (e.Property == BindingGroupProperty)
                {
                    // Re-run validation, but wait until Binding has occured.
                    Dispatcher.BeginInvoke(new DispatcherOperationCallback(DelayedValidateWithoutUpdate), DispatcherPriority.DataBind, e.NewValue);
                }
            }

            if (EGC.DataGridHelper.ShouldNotifyDetailsPresenter(target))
            {
                if (DetailsPresenter != null)
                {
                    DetailsPresenter.NotifyPropertyChanged(d, e);
                }
            }

            if (EGC.DataGridHelper.ShouldNotifyCellsPresenter(target) ||
                EGC.DataGridHelper.ShouldNotifyCells(target) ||
                EGC.DataGridHelper.ShouldRefreshCellContent(target))
            {
                EGC.DataGridCellsPresenter cellsPresenter = CellsPresenter;
                if (cellsPresenter != null)
                {
                    cellsPresenter.NotifyPropertyChanged(d, propertyName, e, target);
                }
            }

            if (EGC.DataGridHelper.ShouldNotifyRowHeaders(target) && RowHeader != null)
            {
                RowHeader.NotifyPropertyChanged(d, e);
            }
        }

        private object DelayedValidateWithoutUpdate(object arg)
        {
            // Only validate if we have an Item.
            var bindingGroup = (BindingGroup)arg;
            if (bindingGroup != null && bindingGroup.Items.Count > 0)
            {
                bindingGroup.ValidateWithoutUpdate();
            }

            return null;
        }

        /// <summary>
        ///     Fired when the Row is attached to the DataGrid.  The scenario here is if the user is scrolling and
        ///     the Row is a recycled container that was just added back to the visual tree.  Properties that rely on a value from
        ///     the Grid should be reevaluated because they may be stale.  
        /// </summary>
        /// <remarks>
        ///     Properties can obviously be stale if the DataGrid's value changes while the row is disconnected.  They can also
        ///     be stale for unobvious reasons.
        /// 
        ///     For example, the Style property is invalidated when we detect a new Visual parent.  This happens for 
        ///     elements in the row (such as the RowHeader) before Prepare is called on the Row.  The coercion callback
        ///     will thus be unable to find the DataGrid and will return the wrong value.  
        /// 
        ///     There is a potential for perf work here.  If we know a DP isn't invalidated when the visual tree is reconnected
        ///     and we know that the Grid hasn't modified that property then its value is likely fine.  We could also cache whether
        ///     or not the Grid's property is the one that's winning.  If not, no need to redo the coercion.  This notification 
        ///     is pretty fast already and thus not worth the work for now.
        /// </remarks>
        private void SyncProperties(bool forcePrepareCells)
        {
            // Coerce all properties on Row that depend on values from the DataGrid
            // Style is ok since it's equivalent to ItemContainerStyle and has already been invalidated.
            EGC.DataGridHelper.TransferProperty(this, BackgroundProperty);
            EGC.DataGridHelper.TransferProperty(this, HeaderStyleProperty);
            EGC.DataGridHelper.TransferProperty(this, HeaderTemplateProperty);
            EGC.DataGridHelper.TransferProperty(this, HeaderTemplateSelectorProperty);
            EGC.DataGridHelper.TransferProperty(this, ValidationErrorTemplateProperty);
            EGC.DataGridHelper.TransferProperty(this, DetailsTemplateProperty);
            EGC.DataGridHelper.TransferProperty(this, DetailsTemplateSelectorProperty);
            EGC.DataGridHelper.TransferProperty(this, DetailsVisibilityProperty);

            CoerceValue(VisibilityProperty); // Handle NewItemPlaceholder case

            RestoreAttachedItemValue(this, DetailsVisibilityProperty);

            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                cellsPresenter.SyncProperties(forcePrepareCells);
                RestoreAttachedItemValue(cellsPresenter, EGC.DataGridCellsPresenter.HeightProperty);
            }

            if (DetailsPresenter != null)
            {
                DetailsPresenter.SyncProperties();
            }

            if (RowHeader != null)
            {
                RowHeader.SyncProperties();
            }
        }

        #endregion

        #region Alternation

        /// <summary>
        ///     AlternationIndex is set on containers generated for an ItemsControl, when
        ///     the ItemsControl's AlternationCount property is positive.  The AlternationIndex
        ///     lies in the range [0, AlternationCount), and adjacent containers always get
        ///     assigned different values.
        /// </summary>
        /// <remarks>
        ///     Exposes ItemsControl.AlternationIndexProperty attached property as a direct property.
        /// </remarks>
        public int AlternationIndex
        {
            get { return (int)GetValue(AlternationIndexProperty); }
        }

        /// <summary>
        ///     DependencyProperty for AlternationIndex.
        /// </summary>
        /// <remarks>
        ///     Same as ItemsControl.AlternationIndexProperty.
        /// </remarks>
        public static readonly DependencyProperty AlternationIndexProperty = ItemsControl.AlternationIndexProperty.AddOwner(typeof(EGC.DataGridRow));

        #endregion

        #region Selection

        /// <summary>
        ///     Indicates whether this DataGridRow is selected.
        /// </summary>
        /// <remarks>
        ///     When IsSelected is set to true, an InvalidOperationException may be
        ///     thrown if the value of the SelectionUnit property on the parent DataGrid 
        ///     prevents selection or rows.
        /// </remarks>
        [Bindable(true), Category("Appearance")]
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsSelected property.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = Selector.IsSelectedProperty.AddOwner(
            typeof(EGC.DataGridRow), 
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, new PropertyChangedCallback(OnIsSelectedChanged)));

        private static void OnIsSelectedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            EGC.DataGridRow row = (EGC.DataGridRow)sender;
            bool isSelected = (bool)e.NewValue;

            if (isSelected && !row.IsSelectable)
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGridRow_CannotSelectRowWhenCells));
            }

            EGC.DataGrid grid = row.DataGridOwner;
            if (grid != null && row.DataContext != null)
            {
                EGC.DataGridAutomationPeer gridPeer = UIElementAutomationPeer.FromElement(grid) as EGC.DataGridAutomationPeer;
                if (gridPeer != null)
                {
                    EGC.DataGridItemAutomationPeer rowItemPeer = gridPeer.GetOrCreateItemPeer(row.DataContext);
                    if (rowItemPeer != null)
                    {
                        rowItemPeer.RaisePropertyChangedEvent(
                            System.Windows.Automation.SelectionItemPatternIdentifiers.IsSelectedProperty,
                            (bool)e.OldValue, 
                            isSelected);
                    }
                }
            }

            // Update the header's IsRowSelected property
            row.NotifyPropertyChanged(row, e, NotificationTarget.Rows | NotificationTarget.RowHeaders);

            // This will raise the appropriate selection event, which will
            // bubble to the DataGrid. The base class Selector code will listen
            // for these events and will update SelectedItems as necessary.
            row.RaiseSelectionChangedEvent(isSelected);
        }

        private void RaiseSelectionChangedEvent(bool isSelected)
        {
            if (isSelected)
            {
                OnSelected(new RoutedEventArgs(SelectedEvent, this));
            }
            else
            {
                OnUnselected(new RoutedEventArgs(UnselectedEvent, this));
            }
        }

        /// <summary>
        ///     Raised when the item's IsSelected property becomes true.
        /// </summary>
        public static readonly RoutedEvent SelectedEvent = Selector.SelectedEvent.AddOwner(typeof(EGC.DataGridRow));

        /// <summary>
        ///     Raised when the item's IsSelected property becomes true.
        /// </summary>
        public event RoutedEventHandler Selected
        {
            add
            {
                AddHandler(SelectedEvent, value);
            }

            remove
            {
                RemoveHandler(SelectedEvent, value);
            }
        }

        /// <summary>
        ///     Called when IsSelected becomes true. Raises the Selected event.
        /// </summary>
        /// <param name="e">Empty event arguments.</param>
        protected virtual void OnSelected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Raised when the item's IsSelected property becomes false.
        /// </summary>
        public static readonly RoutedEvent UnselectedEvent = Selector.UnselectedEvent.AddOwner(typeof(EGC.DataGridRow));

        /// <summary>
        ///     Raised when the item's IsSelected property becomes false.
        /// </summary>
        public event RoutedEventHandler Unselected
        {
            add
            {
                AddHandler(UnselectedEvent, value);
            }

            remove
            {
                RemoveHandler(UnselectedEvent, value);
            }
        }

        /// <summary>
        ///     Called when IsSelected becomes false. Raises the Unselected event.
        /// </summary>
        /// <param name="e">Empty event arguments.</param>
        protected virtual void OnUnselected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Determines if a row can be selected, based on the DataGrid's SelectionUnit property.
        /// </summary>
        private bool IsSelectable
        {
            get
            {
                EGC.DataGrid dataGrid = DataGridOwner;
                if (dataGrid != null)
                {
                    EGC.DataGridSelectionUnit unit = dataGrid.SelectionUnit;
                    return (unit == EGC.DataGridSelectionUnit.FullRow) ||
                        (unit == EGC.DataGridSelectionUnit.CellOrRowHeader);
                }

                return true;
            }
        }

        #endregion

        #region Editing

        /// <summary>
        ///     Whether the row is in editing mode.
        /// </summary>
        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            internal set { SetValue(IsEditingPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey IsEditingPropertyKey =
            DependencyProperty.RegisterReadOnly("IsEditing", typeof(bool), typeof(EGC.DataGridRow), new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     The DependencyProperty for IsEditing.
        /// </summary>
        public static readonly DependencyProperty IsEditingProperty = IsEditingPropertyKey.DependencyProperty;

        #endregion

        #region Automation

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new EGC.DataGridRowAutomationPeer(this);
        }

        #endregion

        #region Column Virtualization

        /// <summary>
        ///     Method which tries to scroll a cell for given index into the scroll view
        /// </summary>
        /// <param name="index"></param>
        internal void ScrollCellIntoView(int index)
        {
            EGC.DataGridCellsPresenter cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                cellsPresenter.ScrollCellIntoView(index);
            }
        }

        #endregion

        #region Layout

        /// <summary>
        ///     Arrange
        /// </summary>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            EGC.DataGrid dataGrid = DataGridOwner;
            if (dataGrid != null)
            {
                dataGrid.QueueInvalidateCellsPanelHorizontalOffset();
            }

            return base.ArrangeOverride(arrangeBounds);
        }

        #endregion

        #region Helpers

        /// <summary>
        ///     Returns the index of this row within the DataGrid's list of item containers.
        /// </summary>
        /// <remarks>
        ///     This method performs a linear search.
        /// </remarks>
        /// <returns>The index, if found, -1 otherwise.</returns>
        public int GetIndex()
        {
            EGC.DataGrid dataGridOwner = DataGridOwner;
            if (dataGridOwner != null)
            {
                return dataGridOwner.ItemContainerGenerator.IndexFromContainer(this);
            }

            return -1;
        }

        /// <summary>
        ///     Searchs up the visual parent chain from the given element until
        ///     a DataGridRow element is found.
        /// </summary>
        /// <param name="element">The descendent of a DataGridRow.</param>
        /// <returns>
        ///     The first ancestor DataGridRow of the element parameter.
        ///     Returns null of none is found.
        /// </returns>
        public static EGC.DataGridRow GetRowContainingElement(FrameworkElement element)
        {
            return EGC.DataGridHelper.FindVisualParent<EGC.DataGridRow>(element);
        }

        internal EGC.DataGrid DataGridOwner
        {
            get { return _owner; }
        }

        /// <summary>
        /// Returns true if the DetailsPresenter is supposed to draw gridlines for the row.  Only true
        /// if the DetailsPresenter hooked itself up properly to the Row.
        /// </summary>
        internal bool DetailsPresenterDrawsGridLines
        {
            get { return _detailsPresenter != null && _detailsPresenter.Visibility == Visibility.Visible; }
        }

        /// <summary>
        ///     Acceses the CellsPresenter and attempts to get the cell at the given index.
        ///     This is not necessarily the display order.
        /// </summary>
        internal EGC.DataGridCell TryGetCell(int index)
        {
            EGC.DataGridCellsPresenter cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                return cellsPresenter.ItemContainerGenerator.ContainerFromIndex(index) as EGC.DataGridCell;
            }

            return null;
        }

        #endregion

        #region Data

        private EGC.DataGrid _owner;
        private EGC.DataGridCellsPresenter _cellsPresenter;
        private EGC.DataGridDetailsPresenter _detailsPresenter;
        private EGC.DataGridRowHeader _rowHeader;
        private EGC.ContainerTracking<EGC.DataGridRow> _tracker;
        private double _cellsPresenterResizeHeight;
        private RowDetailsEventStatus _detailsEventStatus;

        #endregion
    }
}
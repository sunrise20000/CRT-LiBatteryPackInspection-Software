//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MS.Internal;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    ///     A base class for specifying the column definitions.
    /// </summary>
    public abstract class DataGridColumn : DependencyObject
    {
        #region Header

        /// <summary>
        ///     An object that represents the header of this column.
        /// </summary>
        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the Header property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(object), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyColumnHeaderPropertyChanged)));

        /// <summary>
        ///     The Style for the DataGridColumnHeader
        /// </summary>
        public Style HeaderStyle
        {
            get { return (Style)GetValue(HeaderStyleProperty); }
            set { SetValue(HeaderStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the HeaderStyle property.
        /// </summary>
        public static readonly DependencyProperty HeaderStyleProperty =
            DependencyProperty.Register("HeaderStyle", typeof(Style), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyColumnHeaderPropertyChanged, OnCoerceHeaderStyle));

        private static object OnCoerceHeaderStyle(DependencyObject d, object baseValue)
        {
            var column = d as EGC.DataGridColumn;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                column, 
                baseValue, 
                HeaderStyleProperty,
                column.DataGridOwner,
                EGC.DataGrid.ColumnHeaderStyleProperty);
        }

        /// <summary>
        ///     The string format to apply to the header.
        /// </summary>
        public string HeaderStringFormat
        {
            get { return (string)GetValue(HeaderStringFormatProperty); }
            set { SetValue(HeaderStringFormatProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the HeaderStringFormat property.
        /// </summary>
        public static readonly DependencyProperty HeaderStringFormatProperty =
            DependencyProperty.Register("HeaderStringFormat", typeof(string), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyColumnHeaderPropertyChanged));

        /// <summary>
        ///     The template that defines the visual representation of the header.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the HeaderTemplate property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyColumnHeaderPropertyChanged));

        /// <summary>
        ///     DataTemplateSelector that selects which template to use for the Column Header
        /// </summary>
        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(HeaderTemplateSelectorProperty); }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the HeaderTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateSelectorProperty =
            DependencyProperty.Register("HeaderTemplateSelector", typeof(DataTemplateSelector), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyColumnHeaderPropertyChanged));

        #endregion

        #region Cell Container

        /// <summary>
        ///     A style to apply to the container of cells in this column.
        /// </summary>
        public Style CellStyle
        {
            get { return (Style)GetValue(CellStyleProperty); }
            set { SetValue(CellStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the CellStyle property.
        /// </summary>
        public static readonly DependencyProperty CellStyleProperty =
            DependencyProperty.Register("CellStyle", typeof(Style), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyCellPropertyChanged, OnCoerceCellStyle));

        private static object OnCoerceCellStyle(DependencyObject d, object baseValue)
        {
            var column = d as EGC.DataGridColumn;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                column, 
                baseValue, 
                CellStyleProperty,                
                column.DataGridOwner,
                EGC.DataGrid.CellStyleProperty);
        }

        /// <summary>
        ///     Whether cells in this column can enter edit mode.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the IsReadOnly property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(false, OnNotifyCellPropertyChanged, OnCoerceIsReadOnly));

        private static object OnCoerceIsReadOnly(DependencyObject d, object baseValue)
        {
            var column = d as EGC.DataGridColumn;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                column, 
                baseValue, 
                IsReadOnlyProperty,
                column.DataGridOwner,
                EGC.DataGrid.IsReadOnlyProperty);
        }

        #endregion

        #region Width

        /// <summary>
        ///     Specifies the width of the header and cells within this column.
        /// </summary>
        public EGC.DataGridLength Width
        {
            get { return (EGC.DataGridLength)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the Width property.
        /// </summary>
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                "Width",
                typeof(EGC.DataGridLength),
                typeof(EGC.DataGridColumn),
                new FrameworkPropertyMetadata(EGC.DataGridLength.Auto, new PropertyChangedCallback(OnWidthPropertyChanged), new CoerceValueCallback(OnCoerceWidth)));

        /// <summary>
        /// Internal method which sets the column's width
        /// without actual redistribution of widths among other
        /// columns
        /// </summary>
        /// <param name="width"></param>
        internal void SetWidthInternal(EGC.DataGridLength width)
        {
            bool originalValue = _ignoreRedistributionOnWidthChange;
            _ignoreRedistributionOnWidthChange = true;
            try
            {
                Width = width;
            }
            finally
            {
                _ignoreRedistributionOnWidthChange = originalValue;
            }
        }

        /// <summary>
        /// Property changed call back for Width property which notification propagation
        /// and does the redistribution of widths among other columns if needed
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EGC.DataGridColumn column = (EGC.DataGridColumn)d;
            EGC.DataGridLength oldWidth = (EGC.DataGridLength)e.OldValue;
            EGC.DataGridLength newWidth = (EGC.DataGridLength)e.NewValue;
            EGC.DataGrid dataGrid = column.DataGridOwner;

            if (dataGrid != null &&
                !DoubleUtil.AreClose(oldWidth.DisplayValue, newWidth.DisplayValue))
            {
                dataGrid.InternalColumns.InvalidateAverageColumnWidth();
            }

            if (column._processingWidthChange)
            {
                column.CoerceValue(ActualWidthProperty);
                return;
            }

            column._processingWidthChange = true;
            try
            {
                if (dataGrid != null && (newWidth.IsStar ^ oldWidth.IsStar))
                {
                    dataGrid.InternalColumns.InvalidateHasVisibleStarColumns();
                }

                column.NotifyPropertyChanged(
                    d, 
                    e, 
                    NotificationTarget.ColumnCollection |
                    NotificationTarget.Columns | 
                    NotificationTarget.Cells | 
                    NotificationTarget.ColumnHeaders |
                    NotificationTarget.CellsPresenter |
                    NotificationTarget.ColumnHeadersPresenter);

                if (dataGrid != null)
                {
                    if (!column._ignoreRedistributionOnWidthChange && column.IsVisible)
                    {
                        if (!newWidth.IsStar && !newWidth.IsAbsolute)
                        {
                            EGC.DataGridLength changedWidth = column.Width;
                            double displayValue = EGC.DataGridHelper.CoerceToMinMax(changedWidth.DesiredValue, column.MinWidth, column.MaxWidth);
                            column.SetWidthInternal(new EGC.DataGridLength(changedWidth.Value, changedWidth.UnitType, changedWidth.DesiredValue, displayValue));
                        }

                        dataGrid.InternalColumns.RedistributeColumnWidthsOnWidthChangeOfColumn(column, (EGC.DataGridLength)e.OldValue);
                    }
                }
            }
            finally
            {
                column._processingWidthChange = false;
            }
        }

        /// <summary>
        ///     Specifies the minimum width of the header and cells within this column.
        /// </summary>
        public double MinWidth
        {
            get { return (double)GetValue(MinWidthProperty); }
            set { SetValue(MinWidthProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the MinWidth property.
        /// </summary>
        public static readonly DependencyProperty MinWidthProperty =
            DependencyProperty.Register(
                "MinWidth", 
                typeof(double),
                typeof(EGC.DataGridColumn),
                new FrameworkPropertyMetadata(20d, new PropertyChangedCallback(OnMinWidthPropertyChanged), new CoerceValueCallback(OnCoerceMinWidth)), 
                new ValidateValueCallback(ValidateMinWidth));

        /// <summary>
        /// Property changed call back for MinWidth property which notification propagation
        /// and does the redistribution of widths among other columns if needed
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnMinWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EGC.DataGridColumn column = (EGC.DataGridColumn)d;
            EGC.DataGrid dataGrid = column.DataGridOwner;

            column.NotifyPropertyChanged(d, e, NotificationTarget.Columns);

            if (dataGrid != null && column.IsVisible)
            {
                dataGrid.InternalColumns.RedistributeColumnWidthsOnMinWidthChangeOfColumn(column, (double)e.OldValue);
            }
        }

        /// <summary>
        ///     Specifies the maximum width of the header and cells within this column.
        /// </summary>
        public double MaxWidth
        {
            get { return (double)GetValue(MaxWidthProperty); }
            set { SetValue(MaxWidthProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the MaxWidth property.
        /// </summary>
        public static readonly DependencyProperty MaxWidthProperty =
            DependencyProperty.Register(
                "MaxWidth", 
                typeof(double),
                typeof(EGC.DataGridColumn),
                new FrameworkPropertyMetadata(double.PositiveInfinity, new PropertyChangedCallback(OnMaxWidthPropertyChanged), new CoerceValueCallback(OnCoerceMaxWidth)),
                new ValidateValueCallback(ValidateMaxWidth));

        /// <summary>
        /// Property changed call back for MaxWidth property which notification propagation
        /// and does the redistribution of widths among other columns if needed
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnMaxWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EGC.DataGridColumn column = (EGC.DataGridColumn)d;
            EGC.DataGrid dataGrid = column.DataGridOwner;

            column.NotifyPropertyChanged(d, e, NotificationTarget.Columns);

            if (dataGrid != null && column.IsVisible)
            {
                dataGrid.InternalColumns.RedistributeColumnWidthsOnMaxWidthChangeOfColumn(column, (double)e.OldValue);
            }
        }

        /// <summary>
        ///     Coerces the WidthProperty based on the DataGrid transferred property rules
        /// </summary>
        private static object OnCoerceWidth(DependencyObject d, object baseValue)
        {
            var column = d as EGC.DataGridColumn;
            EGC.DataGridLength width = (EGC.DataGridLength)EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                column, 
                baseValue, 
                WidthProperty,
                column.DataGridOwner,
                EGC.DataGrid.ColumnWidthProperty);

            double newDisplayValue =
                (DoubleUtil.IsNaN(width.DisplayValue) ? width.DisplayValue : EGC.DataGridHelper.CoerceToMinMax(width.DisplayValue, column.MinWidth, column.MaxWidth));
            if (DoubleUtil.IsNaN(newDisplayValue) || DoubleUtil.AreClose(newDisplayValue, width.DisplayValue))
            {
                return width;
            }

            return new EGC.DataGridLength(
                width.Value,
                width.UnitType,
                width.DesiredValue,
                newDisplayValue);
        }

        /// <summary>
        ///     Coerces the MinWidthProperty based on the DataGrid transferred property rules
        /// </summary>
        private static object OnCoerceMinWidth(DependencyObject d, object baseValue)
        {
            var column = d as EGC.DataGridColumn;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                column, 
                baseValue, 
                MinWidthProperty,                
                column.DataGridOwner,
                EGC.DataGrid.MinColumnWidthProperty);
        }

        /// <summary>
        ///     Coerces the MaxWidthProperty based on the DataGrid transferred property rules
        /// </summary>
        private static object OnCoerceMaxWidth(DependencyObject d, object baseValue)
        {
            var column = d as EGC.DataGridColumn;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                column, 
                baseValue, 
                MaxWidthProperty,
                column.DataGridOwner,
                EGC.DataGrid.MaxColumnWidthProperty);
        }

        /// <summary>
        ///     Validates that the minimum width is an acceptable value
        /// </summary>
        private static bool ValidateMinWidth(object v)
        {
            double value = (double)v;
            return !(value < 0d || DoubleUtil.IsNaN(value) || Double.IsPositiveInfinity(value));
        }

        /// <summary>
        ///     Validates that the maximum width is an acceptable value
        /// </summary>
        private static bool ValidateMaxWidth(object v)
        {
            double value = (double)v;
            return !(value < 0d || DoubleUtil.IsNaN(value));
        }

        /// <summary>
        ///      This is the width that cells and headers should use in Arrange.
        /// </summary>
        public double ActualWidth
        {
            get { return (double)GetValue(ActualWidthProperty); }
            private set { SetValue(ActualWidthPropertyKey, value); }
        }

        public static readonly DependencyPropertyKey ActualWidthPropertyKey =
            DependencyProperty.RegisterReadOnly("ActualWidth", typeof(double), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(0.0, null, new CoerceValueCallback(OnCoerceActualWidth)));

        internal static readonly DependencyProperty ActualWidthProperty = ActualWidthPropertyKey.DependencyProperty;

        private static object OnCoerceActualWidth(DependencyObject d, object baseValue)
        {
            EGC.DataGridColumn column = ((EGC.DataGridColumn)d);
            double actualWidth = (double)baseValue;
            double minWidth = column.MinWidth;
            double maxWidth = column.MaxWidth;

            // If the width is an absolute pixel value, then ActualWidth should be that value
            EGC.DataGridLength width = column.Width;
            if (width.IsAbsolute)
            {
                actualWidth = width.DisplayValue;
            }

            if (actualWidth < minWidth)
            {
                actualWidth = minWidth;
            }
            else if (actualWidth > maxWidth)
            {
                actualWidth = maxWidth;
            }

            return actualWidth;
        }

        /// <summary>
        ///     Retrieve the proper measure constraint for cells.
        /// </summary>
        /// <param name="isHeader">Whether a header constraint or a normal cell constraint is requested.</param>
        /// <returns>The value to use as the width when creating a measure constraint.</returns>
        internal double GetConstraintWidth(bool isHeader)
        {
            EGC.DataGridLength width = Width;
            if (!DoubleUtil.IsNaN(width.DisplayValue))
            {
                return width.DisplayValue;
            }

            if (width.IsAbsolute || 
                width.IsStar ||
                (width.IsSizeToCells && isHeader) ||
                (width.IsSizeToHeader && !isHeader))
            {
                // In these cases, the cell's desired size does not matter.
                // Use the column's current width as the constraint.
                return ActualWidth;
            }
            else
            {
                // The element gets to size to content.
                return Double.PositiveInfinity;
            }
        }

        /// <summary>
        ///     Notifies the column of a cell's desired width.
        ///     Updates the actual width if necessary
        /// </summary>
        /// <param name="isHeader">Whether the cell is a header or not.</param>
        /// <param name="pixelWidth">The desired size of the cell.</param>
        internal void UpdateDesiredWidthForAutoColumn(bool isHeader, double pixelWidth)
        {
            EGC.DataGridLength width = Width;
            double minWidth = MinWidth;
            double maxWidth = MaxWidth;
            double displayWidth = EGC.DataGridHelper.CoerceToMinMax(pixelWidth, minWidth, maxWidth);

            if (width.IsAuto || 
                (width.IsSizeToCells && !isHeader) ||
                (width.IsSizeToHeader && isHeader))
            {
                if (DoubleUtil.IsNaN(width.DesiredValue) ||
                    DoubleUtil.LessThan(width.DesiredValue, pixelWidth))
                {
                    if (DoubleUtil.IsNaN(width.DisplayValue))
                    {
                        SetWidthInternal(new EGC.DataGridLength(width.Value, width.UnitType, pixelWidth, displayWidth));
                    }
                    else
                    {
                        double originalDesiredValue = EGC.DataGridHelper.CoerceToMinMax(width.DesiredValue, minWidth, maxWidth);
                        SetWidthInternal(new EGC.DataGridLength(width.Value, width.UnitType, pixelWidth, width.DisplayValue));
                        if (DoubleUtil.AreClose(originalDesiredValue, width.DisplayValue))
                        {
                            DataGridOwner.InternalColumns.RecomputeColumnWidthsOnColumnResize(this, pixelWidth - width.DisplayValue, true);
                        }
                    }

                    width = Width;
                }

                if (DoubleUtil.IsNaN(width.DisplayValue))
                {
                    if (ActualWidth < displayWidth)
                    {
                        ActualWidth = displayWidth;
                    }
                }
                else if (!DoubleUtil.AreClose(ActualWidth, width.DisplayValue))
                {
                    ActualWidth = width.DisplayValue;
                }
            }
        }

        /// <summary>
        ///     Notifies the column that Width="*" columns have a new actual width.
        /// </summary>
        internal void UpdateWidthForStarColumn(double displayWidth, double desiredWidth, double starValue)
        {
            Debug.Assert(Width.IsStar);
            EGC.DataGridLength width = Width;

            if (!DoubleUtil.AreClose(displayWidth, width.DisplayValue) ||
                !DoubleUtil.AreClose(desiredWidth, width.DesiredValue) ||
                !DoubleUtil.AreClose(width.Value, starValue))
            {
                SetWidthInternal(new EGC.DataGridLength(starValue, width.UnitType, desiredWidth, displayWidth));
                ActualWidth = displayWidth;
            }
        }

        #endregion

        #region Visual Tree Generation

        /// <summary>
        ///     Retrieves the visual tree that was generated for a particular row and column.
        /// </summary>
        /// <param name="dataItem">The row that corresponds to the desired cell.</param>
        /// <returns>The element if found, null otherwise.</returns>
        public FrameworkElement GetCellContent(object dataItem)
        {
            if (dataItem == null)
            {
                throw new ArgumentNullException("dataItem");
            }

            if (_dataGridOwner != null)
            {
                EGC.DataGridRow row = _dataGridOwner.ItemContainerGenerator.ContainerFromItem(dataItem) as EGC.DataGridRow;
                if (row != null)
                {
                    return GetCellContent(row);
                }
            }

            return null;
        }

        /// <summary>
        ///     Retrieves the visual tree that was generated for a particular row and column.
        /// </summary>
        /// <param name="dataGridRow">The row that corresponds to the desired cell.</param>
        /// <returns>The element if found, null otherwise.</returns>
        public FrameworkElement GetCellContent(EGC.DataGridRow dataGridRow)
        {
            if (dataGridRow == null)
            {
                throw new ArgumentNullException("dataGridRow");
            }

            if (_dataGridOwner != null)
            {
                int columnIndex = _dataGridOwner.Columns.IndexOf(this);
                if (columnIndex >= 0)
                {
                    EGC.DataGridCell cell = dataGridRow.TryGetCell(columnIndex);
                    if (cell != null)
                    {
                        return cell.Content as FrameworkElement;
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Creates the visual tree that will become the content of a cell.
        /// </summary>
        /// <param name="isEditing">Whether the editing version is being requested.</param>
        /// <param name="dataItem">The data item for the cell.</param>
        /// <param name="cell">The cell container that will receive the tree.</param>
        internal FrameworkElement BuildVisualTree(bool isEditing, object dataItem, EGC.DataGridCell cell)
        {
            if (isEditing)
            {
                return GenerateEditingElement(cell, dataItem);
            }
            else
            {
                return GenerateElement(cell, dataItem);
            }
        }

        /// <summary>
        ///     Creates the visual tree that will become the content of a cell.
        /// </summary>
        protected abstract FrameworkElement GenerateElement(EGC.DataGridCell cell, object dataItem);

        /// <summary>
        ///     Creates the visual tree that will become the content of a cell.
        /// </summary>
        protected abstract FrameworkElement GenerateEditingElement(EGC.DataGridCell cell, object dataItem);

        #endregion

        #region Editing

        /// <summary>
        ///     Called when a cell has just switched to edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <returns>The unedited value of the cell.</returns>
        protected virtual object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            return null;
        }

        /// <summary>
        ///     Called when a cell's value is to be restored to its original value,
        ///     just before it exits edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <param name="uneditedValue">The original, unedited value of the cell.</param>
        protected virtual void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
        }

        /// <summary>
        ///     Called when a cell's value is to be committed, just before it exits edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <returns>false if there is a validation error. true otherwise.</returns>
        protected virtual bool CommitCellEdit(FrameworkElement editingElement)
        {
            return true;
        }

        internal void BeginEdit(FrameworkElement editingElement, RoutedEventArgs e)
        {
            // This call is to ensure that the tree and its bindings have resolved
            // before we proceed to code that relies on the tree being ready.
            if (editingElement != null)
            {
                editingElement.UpdateLayout();

                object originalValue = PrepareCellForEdit(editingElement, e);
                SetOriginalValue(editingElement, originalValue);
            }
        }

        internal void CancelEdit(FrameworkElement editingElement)
        {
            if (editingElement != null)
            {
                CancelCellEdit(editingElement, GetOriginalValue(editingElement));
                ClearOriginalValue(editingElement);
            }
        }

        internal bool CommitEdit(FrameworkElement editingElement)
        {
            if (editingElement != null)
            {
                if (CommitCellEdit(editingElement))
                {
                    // Validation passed
                    ClearOriginalValue(editingElement);
                    return true;
                }
                else
                {
                    // Validation failed. This cell will remain in edit mode.
                    return false;
                }
            }

            return true;
        }

        private static object GetOriginalValue(DependencyObject obj)
        {
            return (object)obj.GetValue(OriginalValueProperty);
        }

        private static void SetOriginalValue(DependencyObject obj, object value)
        {
            obj.SetValue(OriginalValueProperty, value);
        }

        private static void ClearOriginalValue(DependencyObject obj)
        {
            obj.ClearValue(OriginalValueProperty);
        }

        private static readonly DependencyProperty OriginalValueProperty =
            DependencyProperty.RegisterAttached("OriginalValue", typeof(object), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(null));

        #endregion

        #region Owner Communication

        /// <summary>
        ///     Notifies the DataGrid and the Cells about property changes.
        /// </summary>
        internal static void OnNotifyCellPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EGC.DataGridColumn)d).NotifyPropertyChanged(d, e, NotificationTarget.Columns | NotificationTarget.Cells);
        }

        /// <summary>
        ///     Notifies the DataGrid and the Column Headers about property changes.
        /// </summary>
        private static void OnNotifyColumnHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EGC.DataGridColumn)d).NotifyPropertyChanged(d, e, NotificationTarget.Columns | NotificationTarget.ColumnHeaders);
        }

        /// <summary>
        ///     Notifies parts that respond to changes in the column.
        /// </summary>
        private static void OnNotifyColumnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EGC.DataGridColumn)d).NotifyPropertyChanged(d, e, NotificationTarget.Columns);
        }

        /// <summary>
        ///   General notification for DependencyProperty changes from the grid and/or column.  
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e, NotificationTarget target)
        {
            if (EGC.DataGridHelper.ShouldNotifyColumns(target))
            {
                // Remove columns target since we're handling it.  If we're targeting multiple targets it may also need to get
                // sent to the DataGrid.
                target &= ~NotificationTarget.Columns;

                if (e.Property == EGC.DataGrid.MaxColumnWidthProperty || e.Property == MaxWidthProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, MaxWidthProperty);
                }
                else if (e.Property == EGC.DataGrid.MinColumnWidthProperty || e.Property == MinWidthProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, MinWidthProperty);
                }
                else if (e.Property == EGC.DataGrid.ColumnWidthProperty || e.Property == WidthProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, WidthProperty);
                }
                else if (e.Property == EGC.DataGrid.ColumnHeaderStyleProperty || e.Property == HeaderStyleProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, HeaderStyleProperty);
                }
                else if (e.Property == EGC.DataGrid.CellStyleProperty || e.Property == CellStyleProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, CellStyleProperty);
                }
                else if (e.Property == EGC.DataGrid.IsReadOnlyProperty || e.Property == IsReadOnlyProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, IsReadOnlyProperty);
                }
                else if (e.Property == EGC.DataGrid.DragIndicatorStyleProperty || e.Property == DragIndicatorStyleProperty)
                {
                    EGC.DataGridHelper.TransferProperty(this, DragIndicatorStyleProperty);
                }
                else if (e.Property == DisplayIndexProperty)
                {
                    CoerceValue(IsFrozenProperty);
                }
                
                if (e.Property == WidthProperty || e.Property == MinWidthProperty || e.Property == MaxWidthProperty)
                {
                    CoerceValue(ActualWidthProperty);
                }
            }
            
            if (target != NotificationTarget.None)
            {
                // Everything else gets sent to the DataGrid so it can propogate back down 
                // to the targets that need notification.
                EGC.DataGridColumn column = (EGC.DataGridColumn)d;
                EGC.DataGrid dataGridOwner = column.DataGridOwner;
                if (dataGridOwner != null)
                {
                    dataGridOwner.NotifyPropertyChanged(d, e, target);
                }
            }
        }

        /// <summary>
        /// Method which propogates the property changed notification to datagrid
        /// </summary>
        /// <param name="propertyName"></param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (DataGridOwner != null)
            {
                DataGridOwner.NotifyPropertyChanged(this, propertyName, new DependencyPropertyChangedEventArgs(), NotificationTarget.RefreshCellContent);
            }
        }

        /// <summary>
        /// Method used as property changed callback for properties which need RefreshCellContent to be called
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        internal static void NotifyPropertyChangeForRefreshContent(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Debug.Assert(d is EGC.DataGridColumn, "d should be a DataGridColumn");

            ((EGC.DataGridColumn)d).NotifyPropertyChanged(e.Property.Name);
        }

        /// <summary>
        /// Method which updates the cell for property changes
        /// </summary>
        /// <param name="element"></param>
        /// <param name="propertyName"></param>
        protected internal virtual void RefreshCellContent(FrameworkElement element, string propertyName)
        {
        }

        /// <summary>
        ///     Ensures that any properties that may be influenced by a change to the DataGrid are syncronized.
        /// </summary>
        internal void SyncProperties()
        {
            EGC.DataGridHelper.TransferProperty(this, MinWidthProperty);
            EGC.DataGridHelper.TransferProperty(this, MaxWidthProperty);
            EGC.DataGridHelper.TransferProperty(this, WidthProperty);
            EGC.DataGridHelper.TransferProperty(this, HeaderStyleProperty);
            EGC.DataGridHelper.TransferProperty(this, CellStyleProperty);
            EGC.DataGridHelper.TransferProperty(this, IsReadOnlyProperty);
            EGC.DataGridHelper.TransferProperty(this, DragIndicatorStyleProperty);
        }

        /// <summary>
        ///     The owning DataGrid control.
        /// </summary>
        protected internal EGC.DataGrid DataGridOwner
        {
            get { return _dataGridOwner; }
            internal set { _dataGridOwner = value; }
        }

        #endregion

        #region Display Index

        /// <summary>
        ///     Specifies the display index of this column.
        /// </summary>
        /// <remarks>
        ///     A lower display index means a column will appear first (to the left) of columns with a higher display index. 
        ///     Allowable values are from 0 to num columns - 1. (-1 is legal only as the default value and is modified to something else
        ///     when the column is added to a DataGrid's column collection). DataGrid enforces that no two columns have the same display index;
        ///     changing the display index of a column will cause the index of other columns to adjust as well.
        /// </remarks>
        public int DisplayIndex
        {
            get { return (int)GetValue(DisplayIndexProperty); }
            set { SetValue(DisplayIndexProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the Width property.
        /// </summary>
        public static readonly DependencyProperty DisplayIndexProperty =
            DependencyProperty.Register(
                "DisplayIndex", 
                typeof(int),
                typeof(EGC.DataGridColumn), 
                new FrameworkPropertyMetadata(-1, new PropertyChangedCallback(DisplayIndexChanged), new CoerceValueCallback(OnCoerceDisplayIndex)));

        /// <summary>
        ///     We use the coersion callback to validate that the DisplayIndex of a column is between 0 and DataGrid.Columns.Count
        ///     The default value is -1; this value is only legal as the default or when the Column is not attached to a DataGrid.
        /// </summary>
        private static object OnCoerceDisplayIndex(DependencyObject d, object baseValue)
        {
            EGC.DataGridColumn column = (EGC.DataGridColumn)d;

            if (column.DataGridOwner != null)
            {
                column.DataGridOwner.ValidateDisplayIndex(column, (int)baseValue);
            }

            return baseValue;
        }

        /// <summary>
        ///     Notifies the DataGrid that the display index for this column changed.
        /// </summary>
        private static void DisplayIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Cells and ColumnHeaders invalidate Arrange; ColumnCollection handles modifying the DisplayIndex of other columns.
            ((EGC.DataGridColumn)d).NotifyPropertyChanged(
                d, 
                e, 
                NotificationTarget.Columns | 
                NotificationTarget.ColumnCollection | 
                NotificationTarget.Cells | 
                NotificationTarget.ColumnHeaders | 
                NotificationTarget.CellsPresenter | 
                NotificationTarget.ColumnHeadersPresenter);
        }

        #endregion

        #region Auto Sorting

        /// <summary>
        /// Dependency property for SortMemberPath
        /// </summary>
        public static readonly DependencyProperty SortMemberPathProperty =
            DependencyProperty.Register(
                "SortMemberPath",
                typeof(string),
                typeof(EGC.DataGridColumn),
                new FrameworkPropertyMetadata(String.Empty));

        /// <summary>
        /// The property which the determines the member to be sorted upon when sorted on this column
        /// </summary>
        public string SortMemberPath
        {
            get { return (string)GetValue(SortMemberPathProperty); }
            set { SetValue(SortMemberPathProperty, value); }
        }

        /// <summary>
        /// Dependecy property for CanUserSort
        /// </summary>
        public static readonly DependencyProperty CanUserSortProperty =
            DependencyProperty.Register(
                "CanUserSort",
                typeof(bool),
                typeof(EGC.DataGridColumn),
                new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnNotifySortPropertyChanged), new CoerceValueCallback(OnCoerceCanUserSort)));

        /// <summary>
        /// The property which determines whether the datagrid can be sorted upon this column or not
        /// </summary>
        public bool CanUserSort
        {
            get { return (bool)GetValue(CanUserSortProperty); }
            set { SetValue(CanUserSortProperty, value); }
        }

        /// <summary>
        /// Dependency property for SrtDirection
        /// </summary>
        public static readonly DependencyProperty SortDirectionProperty =
            DependencyProperty.Register(
                "SortDirection",
                typeof(Nullable<ListSortDirection>),
                typeof(EGC.DataGridColumn),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifySortPropertyChanged)));

        /// <summary>
        /// The property for current sort direction of the column
        /// </summary>
        public Nullable<ListSortDirection> SortDirection
        {
            get { return (Nullable<ListSortDirection>)GetValue(SortDirectionProperty); }
            set { SetValue(SortDirectionProperty, value); }
        }

        /// <summary>
        /// The Coercion callback for CanUserSort property. Checks if datagrid.Items can sort and
        /// returns the value accordingly.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        internal static object OnCoerceCanUserSort(DependencyObject d, object baseValue)
        {
            EGC.DataGridColumn column = (EGC.DataGridColumn)d;
            EGC.DataGrid dataGrid = column.DataGridOwner;

            if (dataGrid != null)
            {
                if (!dataGrid.Items.CanSort)
                {
                    return false;
                }
            }

            return baseValue;
        }

        /// <summary>
        /// Property changed callback for CanUserSort, SortMemberPath and SortDirection properties
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnNotifySortPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EGC.DataGridColumn)d).NotifyPropertyChanged(d, e, NotificationTarget.ColumnHeaders);
        }

        #endregion

        #region Auto Generation

        private static readonly DependencyPropertyKey IsAutoGeneratedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsAutoGenerated", 
                typeof(bool),
                typeof(EGC.DataGridColumn),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// The DependencyProperty for the IsAutoGenerated Property
        /// </summary>
        public static readonly DependencyProperty IsAutoGeneratedProperty = IsAutoGeneratedPropertyKey.DependencyProperty;

        /// <summary>
        /// This property determines whether the column is autogenerate or not.
        /// </summary>
        public bool IsAutoGenerated
        {
            get { return (bool)GetValue(IsAutoGeneratedProperty); }
            internal set { SetValue(IsAutoGeneratedPropertyKey, value); }
        }

        /// <summary>
        /// Helper Method which creates a default DataGridColumn object for the specified property type.
        /// </summary>
        /// <param name="itemProperty"></param>
        /// <returns></returns>
        internal static EGC.DataGridColumn CreateDefaultColumn(ItemPropertyInfo itemProperty)
        {
            Debug.Assert(itemProperty != null && itemProperty.PropertyType != null, "itemProperty and/or its PropertyType member cannot be null");

            EGC.DataGridColumn dataGridColumn = null;
            EGC.DataGridComboBoxColumn comboBoxColumn = null;
            Type propertyType = itemProperty.PropertyType;
            
            // determine the type of column to be created and create one
            if (propertyType.IsEnum)
            {
                comboBoxColumn = new EGC.DataGridComboBoxColumn();
                comboBoxColumn.ItemsSource = Enum.GetValues(propertyType);
                dataGridColumn = comboBoxColumn;
            }
            else if (typeof(string).IsAssignableFrom(propertyType))
            {
                dataGridColumn = new EGC.DataGridTextColumn();
            }
            else if (typeof(bool).IsAssignableFrom(propertyType))
            {
                dataGridColumn = new EGC.DataGridCheckBoxColumn();
            }
            else if (typeof(Uri).IsAssignableFrom(propertyType))
            {
                dataGridColumn = new EGC.DataGridHyperlinkColumn();
            }           
            else
            {
                dataGridColumn = new EGC.DataGridTextColumn();
            }

            // determine if the datagrid can sort on the column or not
            if (!typeof(IComparable).IsAssignableFrom(propertyType))
            {
                dataGridColumn.CanUserSort = false;
            }

            dataGridColumn.Header = itemProperty.Name;

            // Set the data field binding for such created columns and 
            // choose the BindingMode based on editability of the property.
            EGC.DataGridBoundColumn boundColumn = dataGridColumn as EGC.DataGridBoundColumn;
            if (boundColumn != null || comboBoxColumn != null)
            {
                Binding binding = new Binding(itemProperty.Name);
                if (comboBoxColumn != null)
                {
                    comboBoxColumn.SelectedItemBinding = binding;
                }
                else
                {
                    boundColumn.Binding = binding;
                }

                PropertyDescriptor pd = itemProperty.Descriptor as PropertyDescriptor;
                if (pd != null)
                {
                    if (pd.IsReadOnly)
                    {
                        binding.Mode = BindingMode.OneWay;
                        dataGridColumn.IsReadOnly = true;
                    }
                }
                else
                {
                    PropertyInfo pi = itemProperty.Descriptor as PropertyInfo;
                    if (pi != null)
                    {
                        if (!pi.CanWrite)
                        {
                            binding.Mode = BindingMode.OneWay;
                            dataGridColumn.IsReadOnly = true;
                        }
                    }
                }
            }

            return dataGridColumn;
        }

        #endregion

        #region Frozen Columns

        /// <summary>
        /// Dependency Property Key for IsFrozen property
        /// </summary>
        private static readonly DependencyPropertyKey IsFrozenPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsFrozen", 
                typeof(bool),
                typeof(EGC.DataGridColumn),
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnNotifyFrozenPropertyChanged), new CoerceValueCallback(OnCoerceIsFrozen)));

        /// <summary>
        /// The DependencyProperty for the IsFrozen Property
        /// </summary>
        public static readonly DependencyProperty IsFrozenProperty = IsFrozenPropertyKey.DependencyProperty;

        /// <summary>
        /// This property determines whether the column is frozen or not.
        /// </summary>
        public bool IsFrozen
        {
            get { return (bool)GetValue(IsFrozenProperty); }
            internal set { SetValue(IsFrozenPropertyKey, value); }
        }

        /// <summary>
        /// Property changed callback for IsFrozen property
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnNotifyFrozenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EGC.DataGridColumn)d).NotifyPropertyChanged(d, e, NotificationTarget.ColumnHeaders);
        }

        /// <summary>
        /// Coercion call back for IsFrozenProperty. Ensures that IsFrozen is set as per the 
        /// DataGrid's FrozenColumnCount property.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        private static object OnCoerceIsFrozen(DependencyObject d, object baseValue)
        {
            EGC.DataGridColumn column = (EGC.DataGridColumn)d;
            EGC.DataGrid dataGrid = column.DataGridOwner;
            if (dataGrid != null)
            {
                if (column.DisplayIndex < dataGrid.FrozenColumnCount)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return baseValue;
        }

        #endregion

        #region Column Reordering

        /// <summary>
        ///     The DependencyProperty that represents the CanUserReorder property.
        /// </summary>
        public static readonly DependencyProperty CanUserReorderProperty =
            DependencyProperty.Register("CanUserReorder", typeof(bool), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// The property which determines if column header can be dragged or not
        /// </summary>
        public bool CanUserReorder
        {
            get { return (bool)GetValue(CanUserReorderProperty); }
            set { SetValue(CanUserReorderProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the DragIndicatorStyle property.
        /// </summary>
        public static readonly DependencyProperty DragIndicatorStyleProperty =
            DependencyProperty.Register("DragIndicatorStyle", typeof(Style), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(null, OnNotifyColumnPropertyChanged, OnCoerceDragIndicatorStyle));

        /// <summary>
        /// The style property which would be applied on the column header drag indicator.    
        /// </summary>
        public Style DragIndicatorStyle
        {
            get { return (Style)GetValue(DragIndicatorStyleProperty); }
            set { SetValue(DragIndicatorStyleProperty, value); }
        }

        private static object OnCoerceDragIndicatorStyle(DependencyObject d, object baseValue)
        {
            var column = d as EGC.DataGridColumn;
            return EGC.DataGridHelper.GetCoercedTransferPropertyValue(
                column, 
                baseValue, 
                DragIndicatorStyleProperty,
                column.DataGridOwner,
                EGC.DataGrid.DragIndicatorStyleProperty);
        }

        #endregion

        #region Clipboard Copy/Paste

        /// <summary>
        ///     The binding that will be used to get or set cell content for the clipboard
        /// </summary>
        public virtual BindingBase ClipboardContentBinding
        {
            get
            {
                return _clipboardContentBinding;
            }

            set
            {
                _clipboardContentBinding = value;
            }
        }

        // This property is used together with ClipboardContentBinding to get/set the value from the item
        private static readonly DependencyProperty CellValueProperty = DependencyProperty.RegisterAttached("CellValue", typeof(object), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// This method is called for each selected cell in each selected cell to retrieve the default cell content.
        /// Default cell content is calculated using ClipboardContentBinding.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual object OnCopyingCellClipboardContent(object item)
        {
            object cellValue = null;
            BindingBase binding = ClipboardContentBinding;
            if (binding != null)
            {
                FrameworkElement fe = new FrameworkElement();
                fe.DataContext = item;
                fe.SetBinding(CellValueProperty, binding);
                cellValue = fe.GetValue(CellValueProperty);
            }

            // Raise the event to give a chance for external listeners to modify the cell content
            if (CopyingCellClipboardContent != null)
            {
                EGC.DataGridCellClipboardEventArgs args = new EGC.DataGridCellClipboardEventArgs(item, this, cellValue);
                CopyingCellClipboardContent(this, args);
                cellValue = args.Content;
            }

            return cellValue;
        }

        /// We don't provide default Paste but this public method is exposed to help custom implementation of Paste
        /// <summary>
        /// This method stores the cellContent into the item object using ClipboardContentBinding.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cellContent"></param>
        public virtual void OnPastingCellClipboardContent(object item, object cellContent)
        {
            BindingBase binding = ClipboardContentBinding;
            if (binding != null)
            {
                // Raise the event to give a chance for external listeners to modify the cell content
                // before it gets stored into the cell
                if (PastingCellClipboardContent != null)
                {
                    EGC.DataGridCellClipboardEventArgs args = new EGC.DataGridCellClipboardEventArgs(item, this, cellContent);
                    PastingCellClipboardContent(this, args);
                    cellContent = args.Content;
                }

                // Event handlers can cancel Paste of a cell by setting its content to null
                if (cellContent != null)
                {
                    FrameworkElement fe = new FrameworkElement();
                    fe.DataContext = item;
                    fe.SetBinding(CellValueProperty, binding);
                    fe.SetValue(CellValueProperty, cellContent);
                    BindingExpression be = fe.GetBindingExpression(CellValueProperty);
                    be.UpdateSource();
                }
            }
        }

        /// <summary>
        /// The event is raised for each selected cell after the cell clipboard content is prepared.
        /// Event handlers can modify the cell content before it gets stored into the clipboard.
        /// </summary>
        public event EventHandler<EGC.DataGridCellClipboardEventArgs> CopyingCellClipboardContent;

        /// <summary>
        /// The event is raised for each selected cell before the clipboard content is transfered to the cell.
        /// Event handlers can modify the clipboard content before it gets stored into the cell content.
        /// </summary>
        public event EventHandler<EGC.DataGridCellClipboardEventArgs> PastingCellClipboardContent;

        #endregion

        #region Special Input

        // TODO: Consider making a protected virtual.
        // If made public, look for PUBLIC_ONINPUT (in DataGridCell) and enable.
        internal virtual void OnInput(InputEventArgs e)
        {
        }

        internal void BeginEdit(InputEventArgs e)
        {
            var owner = DataGridOwner;
            if (owner != null)
            {
                if (owner.BeginEdit(e))
                {
                    e.Handled = true;
                }
            }
        }

        #endregion

        #region Column Resizing

        /// <summary>
        /// Dependency property for CanUserResize
        /// </summary>
        public static readonly DependencyProperty CanUserResizeProperty =
            DependencyProperty.Register("CanUserResize", typeof(bool), typeof(EGC.DataGridColumn), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnNotifyCanResizePropertyChanged)));

        /// <summary>
        /// Property which indicates if an end user can resize the column or not
        /// </summary>
        public bool CanUserResize
        {
            get { return (bool)GetValue(CanUserResizeProperty); }
            set { SetValue(CanUserResizeProperty, value); }
        }

        /// <summary>
        /// Property changed callback for CanUserResize property
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnNotifyCanResizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EGC.DataGridColumn)d).NotifyPropertyChanged(d, e, NotificationTarget.ColumnHeaders);
        }

        #endregion

        #region Hidden Columns

        /// <summary>
        ///     Dependency property for Visibility
        /// </summary>
        public static readonly DependencyProperty VisibilityProperty =    
            DependencyProperty.Register(
                "Visibility",
                typeof(Visibility),
                typeof(EGC.DataGridColumn),
                new FrameworkPropertyMetadata(Visibility.Visible, new PropertyChangedCallback(OnVisibilityPropertyChanged)));

        /// <summary>
        ///     The property which determines if the column is visible or not.
        /// </summary>
        public Visibility Visibility
        {
            get { return (Visibility)GetValue(VisibilityProperty); }
            set { SetValue(VisibilityProperty, value); }
        }

        /// <summary>
        ///     Property changed callback for Visibility property
        /// </summary>
        private static void OnVisibilityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs eventArgs)
        {
            Visibility oldVisibility = (Visibility)eventArgs.OldValue;
            Visibility newVisibility = (Visibility)eventArgs.NewValue;

            if (oldVisibility != Visibility.Visible && newVisibility != Visibility.Visible)
            {
                return;
            }

            ((EGC.DataGridColumn)d).NotifyPropertyChanged(
                d, 
                eventArgs, 
                NotificationTarget.CellsPresenter | NotificationTarget.ColumnHeadersPresenter | NotificationTarget.ColumnCollection);
        }

        /// <summary>
        ///     Helper IsVisible property
        /// </summary>
        internal bool IsVisible
        {
            get
            {
                return Visibility == Visibility.Visible;
            }
        }

        #endregion

        #region Data

        private EGC.DataGrid _dataGridOwner = null;                     // This property is updated by DataGrid when the column is added to the DataGrid.Columns collection
        private BindingBase _clipboardContentBinding;               // Storage for ClipboardContentBinding
        private bool _ignoreRedistributionOnWidthChange = false;    // Flag which indicates to ignore recomputation of column widths on width change of column
        private bool _processingWidthChange = false;                // Flag which indicates that execution of width change callback to avoid recursions.

        #endregion
    }
}
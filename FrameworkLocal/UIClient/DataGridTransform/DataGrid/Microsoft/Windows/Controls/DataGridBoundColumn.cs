//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    ///     A base class for specifying column definitions for certain standard
    ///     types that do not allow arbitrary templates.
    /// </summary>
    public abstract class DataGridBoundColumn : EGC.DataGridColumn
    {
        #region Constructors

        static DataGridBoundColumn()
        {
            SortMemberPathProperty.OverrideMetadata(typeof(EGC.DataGridBoundColumn), new FrameworkPropertyMetadata(null, OnCoerceSortMemberPath));
        }

        #endregion

        #region Binding

        private static object OnCoerceSortMemberPath(DependencyObject d, object baseValue)
        {
            var column = (EGC.DataGridBoundColumn)d;
            var sortMemberPath = (string)baseValue;

            if (string.IsNullOrEmpty(sortMemberPath))
            {
                sortMemberPath = EGC.DataGridHelper.GetPathFromBinding(column.Binding as Binding);
            }

            return sortMemberPath;
        }

        /// <summary>
        ///     The binding that will be applied to the generated element.
        /// </summary>
        /// <remarks>
        ///     This isn't a DP because if it were getting the value would evaluate the binding.
        /// </remarks>
        public virtual BindingBase Binding
        {
            get
            {
                if (!_bindingEnsured)
                {
                    if (!IsReadOnly)
                    {
                        EGC.DataGridHelper.EnsureTwoWay(_binding);
                    }

                    _bindingEnsured = true;
                }

                return _binding;
            }

            set
            {
                if (_binding != value)
                {
                    BindingBase oldBinding = _binding;
                    _binding = value;
                    CoerceValue(SortMemberPathProperty);
                    _bindingEnsured = false;
                    OnBindingChanged(oldBinding, _binding);
                }
            }
        }

        /// <summary>
        ///     Called when Binding changes.
        /// </summary>
        /// <remarks>
        ///     Default implementation notifies the DataGrid and its subtree about the change.
        /// </remarks>
        /// <param name="oldBinding">The old binding.</param>
        /// <param name="newBinding">The new binding.</param>
        protected virtual void OnBindingChanged(BindingBase oldBinding, BindingBase newBinding)
        {
            NotifyPropertyChanged("Binding");
        }

        /// <summary>
        ///     Assigns the Binding to the desired property on the target object.
        /// </summary>
        internal void ApplyBinding(DependencyObject target, DependencyProperty property)
        {
            BindingBase binding = Binding;
            if (binding != null)
            {
                BindingOperations.SetBinding(target, property, binding);
            }
            else
            {
                BindingOperations.ClearBinding(target, property);
            }
        }

        #endregion

        #region Styling

        /// <summary>
        ///     A style that is applied to the generated element when not editing.
        ///     The TargetType of the style depends on the derived column class.
        /// </summary>
        public Style ElementStyle
        {
            get { return (Style)GetValue(ElementStyleProperty); }
            set { SetValue(ElementStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the ElementStyle property.
        /// </summary>
        public static readonly DependencyProperty ElementStyleProperty =
            DependencyProperty.Register(
                "ElementStyle", 
                typeof(Style),
                typeof(EGC.DataGridBoundColumn),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(EGC.DataGridColumn.NotifyPropertyChangeForRefreshContent)));

        /// <summary>
        ///     A style that is applied to the generated element when editing.
        ///     The TargetType of the style depends on the derived column class.
        /// </summary>
        public Style EditingElementStyle
        {
            get { return (Style)GetValue(EditingElementStyleProperty); }
            set { SetValue(EditingElementStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the EditingElementStyle property.
        /// </summary>
        public static readonly DependencyProperty EditingElementStyleProperty =
            DependencyProperty.Register(
                "EditingElementStyle", 
                typeof(Style),
                typeof(EGC.DataGridBoundColumn),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(EGC.DataGridColumn.NotifyPropertyChangeForRefreshContent)));

        /// <summary>
        ///     Assigns the ElementStyle to the desired property on the given element.
        /// </summary>
        internal void ApplyStyle(bool isEditing, bool defaultToElementStyle, FrameworkElement element)
        {
            Style style = PickStyle(isEditing, defaultToElementStyle);
            if (style != null)
            {
                element.Style = style;
            }
        }

        private Style PickStyle(bool isEditing, bool defaultToElementStyle)
        {
            Style style = isEditing ? EditingElementStyle : ElementStyle;
            if (isEditing && defaultToElementStyle && (style == null))
            {
                style = ElementStyle;
            }

            return style;
        }

        #endregion

        #region Clipboard Copy/Paste

        /// <summary>
        /// If base ClipboardContentBinding is not set we use Binding.
        /// </summary>
        public override BindingBase ClipboardContentBinding
        {
            get
            {
                return base.ClipboardContentBinding ?? Binding;
            }

            set
            {
                base.ClipboardContentBinding = value;
            }
        }

        #endregion

        #region Property Changed Handler

        /// <summary>
        /// Override which rebuilds the cell's visual tree for Binding change
        /// </summary>
        /// <param name="element"></param>
        /// <param name="propertyName"></param>
        protected internal override void RefreshCellContent(FrameworkElement element, string propertyName)
        {
            EGC.DataGridCell cell = element as EGC.DataGridCell;
            if (cell != null)
            {
                bool isCellEditing = cell.IsEditing;
                if ((string.Compare(propertyName, "Binding", StringComparison.Ordinal) == 0) ||
                    (string.Compare(propertyName, "ElementStyle", StringComparison.Ordinal) == 0 && !isCellEditing) ||
                    (string.Compare(propertyName, "EditingElementStyle", StringComparison.Ordinal) == 0 && isCellEditing))
                {
                    cell.BuildVisualTree();
                    return;
                }
            }

            base.RefreshCellContent(element, propertyName);
        }

        #endregion

        #region Data

        private BindingBase _binding;
        private bool _bindingEnsured;

        #endregion
    }
}
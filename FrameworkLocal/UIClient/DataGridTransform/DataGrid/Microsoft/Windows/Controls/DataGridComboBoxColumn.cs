//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Collections;
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
    ///     A column that displays a drop-down list while in edit mode.
    /// </summary>
    public class DataGridComboBoxColumn : EGC.DataGridColumn
    {
        #region Constructors

        static DataGridComboBoxColumn()
        {
            SortMemberPathProperty.OverrideMetadata(typeof(EGC.DataGridComboBoxColumn), new FrameworkPropertyMetadata(null, OnCoerceSortMemberPath));
        }

        #endregion

        #region Helper Type for Styling

        internal class TextBlockComboBox : ComboBox
        {
            static TextBlockComboBox()
            {
                DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBlockComboBox), new FrameworkPropertyMetadata(typeof(TextBlockComboBox)));
                KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(TextBlockComboBox), new FrameworkPropertyMetadata(false));
                DataContextProperty.OverrideMetadata(typeof(TextBlockComboBox), new FrameworkPropertyMetadata(OnDataContextPropertyChanged));
            }

            /// <summary>
            ///     Property changed callback for DataContext property
            /// </summary>
            private static void OnDataContextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                // Selector has a bug regarding DataContext change and SelectedItem property,
                // where if the SelectedItem due to old DataContext is a valid item in ItemsSource
                // but the SelectedItem due to new DataContext is not a valid item in ItemsSource,
                // the SelectedIndex remains that of old context instead of changing to -1.
                // This method is a workaround to that problem, since it is of high impact to DataGrid.
                TextBlockComboBox combo = (TextBlockComboBox)d;
                bool isLocalValue = (DependencyPropertyHelper.GetValueSource(combo, SelectedItemProperty).BaseValueSource == BaseValueSource.Local);

                if (isLocalValue)
                {
                    // Clear the selection and re-apply the binding.
                    BindingBase binding = BindingOperations.GetBindingBase(combo, SelectedItemProperty);
                    if (binding != null)
                    {
                        combo.ClearValue(SelectedItemProperty);
                        EGC.DataGridComboBoxColumn.ApplyBinding(binding, combo, SelectedItemProperty);
                    }
                }
                else
                {
                    // Clear the selection by setting the local value
                    // and re-evaluate the property by clearing the local value.
                    combo.SelectedItem = null;
                    combo.ClearValue(SelectedItemProperty);
                }
            }
        }

        #endregion

        #region Binding

        private static object OnCoerceSortMemberPath(DependencyObject d, object baseValue)
        {
            var column = (EGC.DataGridComboBoxColumn)d;
            var sortMemberPath = (string)baseValue;

            if (string.IsNullOrEmpty(sortMemberPath))
            {
                sortMemberPath = EGC.DataGridHelper.GetPathFromBinding(column.EffectiveBinding as Binding);
            }

            return sortMemberPath;
        }

        /// <summary>
        ///     Chooses either SelectedItemBinding, TextBinding, SelectedValueBinding or  based which are set.
        /// </summary>
        private BindingBase EffectiveBinding
        {
            get
            {
                if (SelectedItemBinding != null)
                {
                    return SelectedItemBinding;
                }
                else if (SelectedValueBinding != null)
                {
                    return SelectedValueBinding;
                }
                else
                {
                    return TextBinding;
                }
            }
        }

        /// <summary>
        ///     The binding that will be applied to the SelectedValue property of the ComboBox.  This works in conjunction with SelectedValuePath  
        /// </summary>
        /// <remarks>
        ///     This isn't a DP because if it were getting the value would evaluate the binding.
        /// </remarks>
        public virtual BindingBase SelectedValueBinding
        {
            get
            {
                if (!_selectedValueBindingEnsured)
                {
                    if (!IsReadOnly)
                    {
                        EGC.DataGridHelper.EnsureTwoWay(_selectedValueBinding);
                    }

                    _selectedValueBindingEnsured = true;
                }

                return _selectedValueBinding;
            }

            set
            {
                if (_selectedValueBinding != value)
                {
                    BindingBase oldBinding = _selectedValueBinding;
                    _selectedValueBinding = value;
                    CoerceValue(SortMemberPathProperty);
                    _selectedValueBindingEnsured = false;
                    OnSelectedValueBindingChanged(oldBinding, _selectedValueBinding);
                }
            }
        }

        /// <summary>
        ///     The binding that will be applied to the SelectedItem property of the ComboBoxValue.
        /// </summary>
        /// <remarks>
        ///     This isn't a DP because if it were getting the value would evaluate the binding.
        /// </remarks>
        public virtual BindingBase SelectedItemBinding
        {
            get
            {
                if (!_selectedItemBindingEnsured)
                {
                    if (!IsReadOnly)
                    {
                        EGC.DataGridHelper.EnsureTwoWay(_selectedItemBinding);
                    }

                    _selectedItemBindingEnsured = true;
                }

                return _selectedItemBinding;
            }

            set
            {
                if (_selectedItemBinding != value)
                {
                    BindingBase oldBinding = _selectedItemBinding;
                    _selectedItemBinding = value;
                    CoerceValue(SortMemberPathProperty);
                    _selectedItemBindingEnsured = false;
                    OnSelectedItemBindingChanged(oldBinding, _selectedItemBinding);
                }
            }
        }

        /// <summary>
        ///     The binding that will be applied to the Text property of the ComboBoxValue.
        /// </summary>
        /// <remarks>
        ///     This isn't a DP because if it were getting the value would evaluate the binding.
        /// </remarks>
        public virtual BindingBase TextBinding
        {
            get
            {
                if (!_textBindingEnsured)
                {
                    if (!IsReadOnly)
                    {
                        EGC.DataGridHelper.EnsureTwoWay(_textBinding);
                    }

                    _textBindingEnsured = true;
                }

                return _textBinding;
            }

            set
            {
                if (_textBinding != value)
                {
                    BindingBase oldBinding = _textBinding;
                    _textBinding = value;
                    CoerceValue(SortMemberPathProperty);
                    _textBindingEnsured = false;
                    OnTextBindingChanged(oldBinding, _textBinding);
                }
            }
        }

        /// <summary>
        ///     Called when SelectedValueBinding changes.
        /// </summary>
        /// <param name="oldBinding">The old binding.</param>
        /// <param name="newBinding">The new binding.</param>
        protected virtual void OnSelectedValueBindingChanged(BindingBase oldBinding, BindingBase newBinding)
        {
            NotifyPropertyChanged("SelectedValueBinding");
        }

        /// <summary>
        ///     Called when SelectedItemBinding changes.
        /// </summary>
        /// <param name="oldBinding">The old binding.</param>
        /// <param name="newBinding">The new binding.</param>
        protected virtual void OnSelectedItemBindingChanged(BindingBase oldBinding, BindingBase newBinding)
        {
            NotifyPropertyChanged("SelectedItemBinding");
        }

        /// <summary>
        ///     Called when TextBinding changes.
        /// </summary>
        /// <param name="oldBinding">The old binding.</param>
        /// <param name="newBinding">The new binding.</param>
        protected virtual void OnTextBindingChanged(BindingBase oldBinding, BindingBase newBinding)
        {
            NotifyPropertyChanged("TextBinding");
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
            EGC.DataGridBoundColumn.ElementStyleProperty.AddOwner(typeof(EGC.DataGridComboBoxColumn));

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
            EGC.DataGridBoundColumn.EditingElementStyleProperty.AddOwner(typeof(EGC.DataGridComboBoxColumn));

        /// <summary>
        ///     Assigns the ElementStyle to the desired property on the given element.
        /// </summary>
        private void ApplyStyle(bool isEditing, bool defaultToElementStyle, FrameworkElement element)
        {
            Style style = PickStyle(isEditing, defaultToElementStyle);
            if (style != null)
            {
                element.Style = style;
            }
        }

        /// <summary>
        ///     Assigns the ElementStyle to the desired property on the given element.
        /// </summary>
        internal void ApplyStyle(bool isEditing, bool defaultToElementStyle, FrameworkContentElement element)
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

        /// <summary>
        ///     Assigns the Binding to the desired property on the target object.
        /// </summary>
        private static void ApplyBinding(BindingBase binding, DependencyObject target, DependencyProperty property)
        {
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

        #region Clipboard Copy/Paste

        /// <summary>
        /// If base ClipboardContentBinding is not set we use Binding.
        /// </summary>
        public override BindingBase ClipboardContentBinding
        {
            get
            {
                return base.ClipboardContentBinding ?? EffectiveBinding;
            }

            set
            {
                base.ClipboardContentBinding = value;
            }
        }

        #endregion

        #region ComboBox Column Properties

        /// <summary>
        ///     The ComboBox will attach to this ItemsSource.
        /// </summary>
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for ItemsSource.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            ComboBox.ItemsSourceProperty.AddOwner(typeof(EGC.DataGridComboBoxColumn), new FrameworkPropertyMetadata(null, EGC.DataGridColumn.NotifyPropertyChangeForRefreshContent));

        /// <summary>
        ///     DisplayMemberPath is a simple way to define a default template
        ///     that describes how to convert Items into UI elements by using
        ///     the specified path.
        /// </summary>
        public string DisplayMemberPath
        {
            get { return (string)GetValue(DisplayMemberPathProperty); }
            set { SetValue(DisplayMemberPathProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the DisplayMemberPath property.
        /// </summary>
        public static readonly DependencyProperty DisplayMemberPathProperty =
                ComboBox.DisplayMemberPathProperty.AddOwner(typeof(EGC.DataGridComboBoxColumn), new FrameworkPropertyMetadata(string.Empty, EGC.DataGridColumn.NotifyPropertyChangeForRefreshContent));

        /// <summary>
        ///  The path used to retrieve the SelectedValue from the SelectedItem
        /// </summary>
        public string SelectedValuePath
        {
            get { return (string)GetValue(SelectedValuePathProperty); }
            set { SetValue(SelectedValuePathProperty, value); }
        }
        
        /// <summary>
        ///     SelectedValuePath DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SelectedValuePathProperty =
                ComboBox.SelectedValuePathProperty.AddOwner(typeof(EGC.DataGridComboBoxColumn), new FrameworkPropertyMetadata(string.Empty, EGC.DataGridColumn.NotifyPropertyChangeForRefreshContent));

        #endregion

        #region Property Changed Handler

        protected internal override void RefreshCellContent(FrameworkElement element, string propertyName)
        {
            EGC.DataGridCell cell = element as EGC.DataGridCell;
            if (cell != null)
            {
                bool isCellEditing = cell.IsEditing;
                if ((string.Compare(propertyName, "ElementStyle", StringComparison.Ordinal) == 0 && !isCellEditing) ||
                    (string.Compare(propertyName, "EditingElementStyle", StringComparison.Ordinal) == 0 && isCellEditing))
                {
                    cell.BuildVisualTree();
                }
                else
                {
                    ComboBox comboBox = cell.Content as ComboBox;
                    switch (propertyName)
                    {
                        case "SelectedItemBinding":
                            ApplyBinding(SelectedItemBinding, comboBox, ComboBox.SelectedItemProperty);
                            break;
                        case "SelectedValueBinding":
                            ApplyBinding(SelectedValueBinding, comboBox, ComboBox.SelectedValueProperty);
                            break;
                        case "TextBinding":
                            ApplyBinding(TextBinding, comboBox, ComboBox.TextProperty);
                            break;
                        case "SelectedValuePath":
                            EGC.DataGridHelper.SyncColumnProperty(this, comboBox, ComboBox.SelectedValuePathProperty, SelectedValuePathProperty);
                            break;
                        case "DisplayMemberPath":
                            EGC.DataGridHelper.SyncColumnProperty(this, comboBox, ComboBox.DisplayMemberPathProperty, DisplayMemberPathProperty);
                            break;
                        case "ItemsSource":
                            EGC.DataGridHelper.SyncColumnProperty(this, comboBox, ComboBox.ItemsSourceProperty, ItemsSourceProperty);
                            break;
                        default:
                            base.RefreshCellContent(element, propertyName);
                            break;
                    }
                }
            }
            else
            {
                base.RefreshCellContent(element, propertyName);
            }
        }

        #endregion

        #region BindingTarget Helpers

        /// <summary>
        /// Helper method which returns selection value from
        /// combobox based on which Binding's were set.
        /// </summary>
        /// <param name="comboBox"></param>
        /// <returns></returns>
        private object GetComboBoxSelectionValue(ComboBox comboBox)
        {
            if (SelectedItemBinding != null)
            {
                return comboBox.SelectedItem;
            }
            else if (SelectedValueBinding != null)
            {
                return comboBox.SelectedValue;
            }
            else
            {
                return comboBox.Text;
            }
        }

        #endregion

        #region Element Generation

        /// <summary>
        ///     Creates the visual tree for text based cells.
        /// </summary>
        protected override FrameworkElement GenerateElement(EGC.DataGridCell cell, object dataItem)
        {
            TextBlockComboBox comboBox = new TextBlockComboBox();

            ApplyStyle(/* isEditing = */ false, /* defaultToElementStyle = */ false, comboBox);
            ApplyColumnProperties(comboBox);

            return comboBox;
        }

        /// <summary>
        ///     Creates the visual tree for text based cells.
        /// </summary>
        protected override FrameworkElement GenerateEditingElement(EGC.DataGridCell cell, object dataItem)
        {
            ComboBox comboBox = new ComboBox();

            ApplyStyle(/* isEditing = */ true, /* defaultToElementStyle = */ false, comboBox);
            ApplyColumnProperties(comboBox);

            return comboBox;
        }

        private void ApplyColumnProperties(ComboBox comboBox)
        {
            ApplyBinding(SelectedItemBinding, comboBox, ComboBox.SelectedItemProperty);
            ApplyBinding(SelectedValueBinding, comboBox, ComboBox.SelectedValueProperty);
            ApplyBinding(TextBinding, comboBox, ComboBox.TextProperty);

            EGC.DataGridHelper.SyncColumnProperty(this, comboBox, ComboBox.SelectedValuePathProperty, SelectedValuePathProperty);
            EGC.DataGridHelper.SyncColumnProperty(this, comboBox, ComboBox.DisplayMemberPathProperty, DisplayMemberPathProperty);
            EGC.DataGridHelper.SyncColumnProperty(this, comboBox, ComboBox.ItemsSourceProperty, ItemsSourceProperty);
        }

        #endregion

        #region Editing

        /// <summary>
        ///     Called when a cell has just switched to edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <param name="editingEventArgs">The event args of the input event that caused the cell to go into edit mode. May be null.</param>
        /// <returns>The unedited value of the cell.</returns>
        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            ComboBox comboBox = editingElement as ComboBox;
            if (comboBox != null)
            {
                comboBox.Focus();
                object originalValue = GetComboBoxSelectionValue(comboBox);
                
                if (IsComboBoxOpeningInputEvent(editingEventArgs))
                {
                    comboBox.IsDropDownOpen = true;
                }

                return originalValue;
            }

            return null;
        }

        /// <summary>
        ///     Called when a cell's value is to be committed, just before it exits edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <returns>false if there is a validation error. true otherwise.</returns>
        protected override bool CommitCellEdit(FrameworkElement editingElement)
        {
            ComboBox comboBox = editingElement as ComboBox;
            if (comboBox != null)
            {
                EGC.DataGridHelper.UpdateSource(comboBox, ComboBox.SelectedValueProperty);
                EGC.DataGridHelper.UpdateSource(comboBox, ComboBox.SelectedItemProperty);
                EGC.DataGridHelper.UpdateSource(comboBox, ComboBox.TextProperty);
                return !Validation.GetHasError(comboBox);
            }

            return true;
        }

        /// <summary>
        ///     Called when a cell's value is to be cancelled, just before it exits edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <param name="uneditedValue">UneditedValue</param>
        protected override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            ComboBox comboBox = editingElement as ComboBox;
            if (comboBox != null)
            {
                EGC.DataGridHelper.UpdateTarget(comboBox, ComboBox.SelectedValueProperty);
                EGC.DataGridHelper.UpdateTarget(comboBox, ComboBox.SelectedItemProperty);
                EGC.DataGridHelper.UpdateTarget(comboBox, ComboBox.TextProperty);
            }
        }

        internal override void OnInput(InputEventArgs e)
        {
            if (IsComboBoxOpeningInputEvent(e))
            {
                BeginEdit(e);
            }
        }

        private static bool IsComboBoxOpeningInputEvent(RoutedEventArgs e)
        {
            KeyEventArgs keyArgs = e as KeyEventArgs;
            if ((keyArgs != null) && ((keyArgs.KeyStates & KeyStates.Down) == KeyStates.Down))
            {
                bool isAltDown = (keyArgs.KeyboardDevice.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;

                // We want to handle the ALT key. Get the real key if it is Key.System.
                Key key = keyArgs.Key;
                if (key == Key.System)
                {
                    key = keyArgs.SystemKey;
                }

                // F4 alone or ALT+Up or ALT+Down will open the drop-down
                return ((key == Key.F4) && !isAltDown) ||
                       (((key == Key.Up) || (key == Key.Down)) && isAltDown);
            }

            return false;
        }

        #endregion

        #region Data

        private BindingBase _selectedValueBinding;
        private BindingBase _selectedItemBinding;
        private BindingBase _textBinding;
        private bool _selectedValueBindingEnsured = false;
        private bool _selectedItemBindingEnsured = false;
        private bool _textBindingEnsured = false;

        #endregion
    }
}
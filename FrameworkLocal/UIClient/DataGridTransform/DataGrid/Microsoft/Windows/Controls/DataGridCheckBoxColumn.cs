//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    ///     A column that displays a check box.
    /// </summary>
    public class DataGridCheckBoxColumn : EGC.DataGridBoundColumn
    {
        static DataGridCheckBoxColumn()
        {
            ElementStyleProperty.OverrideMetadata(typeof(EGC.DataGridCheckBoxColumn), new FrameworkPropertyMetadata(DefaultElementStyle));
            EditingElementStyleProperty.OverrideMetadata(typeof(EGC.DataGridCheckBoxColumn), new FrameworkPropertyMetadata(DefaultEditingElementStyle));
        }

        #region Styles

        /// <summary>
        ///     The default value of the ElementStyle property.
        ///     This value can be used as the BasedOn for new styles.
        /// </summary>
        public static Style DefaultElementStyle
        {
            get
            {
                if (_defaultElementStyle == null)
                {
                    Style style = new Style(typeof(CheckBox));

                    // When not in edit mode, the end-user should not be able to toggle the state
                    style.Setters.Add(new Setter(UIElement.IsHitTestVisibleProperty, false));
                    style.Setters.Add(new Setter(UIElement.FocusableProperty, false));
                    style.Setters.Add(new Setter(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                    style.Setters.Add(new Setter(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Top));

                    style.Seal();
                    _defaultElementStyle = style;
                }

                return _defaultElementStyle;
            }
        }

        /// <summary>
        ///     The default value of the EditingElementStyle property.
        ///     This value can be used as the BasedOn for new styles.
        /// </summary>
        public static Style DefaultEditingElementStyle
        {
            get
            {
                if (_defaultEditingElementStyle == null)
                {
                    Style style = new Style(typeof(CheckBox));

                    style.Setters.Add(new Setter(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                    style.Setters.Add(new Setter(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Top));

                    style.Seal();
                    _defaultEditingElementStyle = style;
                }

                return _defaultEditingElementStyle;
            }
        }

        #endregion

        #region Element Generation

        /// <summary>
        ///     Creates the visual tree for boolean based cells.
        /// </summary>
        protected override FrameworkElement GenerateElement(EGC.DataGridCell cell, object dataItem)
        {
            return GenerateCheckBox(/* isEditing = */ false, cell);
        }

        /// <summary>
        ///     Creates the visual tree for boolean based cells.
        /// </summary>
        protected override FrameworkElement GenerateEditingElement(EGC.DataGridCell cell, object dataItem)
        {
            return GenerateCheckBox(/* isEditing = */ true, cell);
        }

        private CheckBox GenerateCheckBox(bool isEditing, EGC.DataGridCell cell)
        {
            CheckBox checkBox = (cell != null) ? (cell.Content as CheckBox) : null;
            if (checkBox == null)
            {
                checkBox = new CheckBox();
            }

            checkBox.IsThreeState = IsThreeState;

            ApplyStyle(isEditing, /* defaultToElementStyle = */ true, checkBox);
            ApplyBinding(checkBox, CheckBox.IsCheckedProperty);

            return checkBox;
        }

        protected internal override void RefreshCellContent(FrameworkElement element, string propertyName)
        {
            EGC.DataGridCell cell = element as EGC.DataGridCell;
            if (cell != null &&
                string.Compare(propertyName, "IsThreeState", StringComparison.Ordinal) == 0)
            {
                var checkBox = cell.Content as CheckBox;
                if (checkBox != null)
                {
                    checkBox.IsThreeState = IsThreeState;
                }
            }
            else
            {
                base.RefreshCellContent(element, propertyName);
            }
        }

        #endregion

        #region Editing

        /// <summary>
        ///     The DependencyProperty for the IsThreeState property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsThreeStateProperty =
                CheckBox.IsThreeStateProperty.AddOwner(
                        typeof(EGC.DataGridCheckBoxColumn),
                        new FrameworkPropertyMetadata(false, EGC.DataGridColumn.NotifyPropertyChangeForRefreshContent));

        /// <summary>
        ///     The IsThreeState property determines whether the control supports two or three states.
        ///     IsChecked property can be set to null as a third state when IsThreeState is true
        /// </summary>
        public bool IsThreeState
        {
            get { return (bool)GetValue(IsThreeStateProperty); }
            set { SetValue(IsThreeStateProperty, value); }
        }

        /// <summary>
        ///     Called when a cell has just switched to edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <param name="editingEventArgs">The event args of the input event that caused the cell to go into edit mode. May be null.</param>
        /// <returns>The unedited value of the cell.</returns>
        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            CheckBox checkBox = editingElement as CheckBox;
            if (checkBox != null)
            {
                checkBox.Focus();
                bool? uneditedValue = checkBox.IsChecked;

                // If a click or a space key invoked the begin edit, then do an immediate toggle
                if ((IsMouseLeftButtonDown(editingEventArgs) && IsMouseOver(checkBox, editingEventArgs)) ||
                    IsSpaceKeyDown(editingEventArgs))
                {
                    checkBox.IsChecked = (uneditedValue != true);
                }

                return uneditedValue;
            }

            return (bool?) false;
        }

        /// <summary>
        ///     Called when a cell's value is to be committed, just before it exits edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <returns>false if there is a validation error. true otherwise.</returns>
        protected override bool CommitCellEdit(FrameworkElement editingElement)
        {
            CheckBox checkBox = editingElement as CheckBox;
            if (checkBox != null)
            {
                EGC.DataGridHelper.UpdateSource(checkBox, CheckBox.IsCheckedProperty);
                return !Validation.GetHasError(checkBox);
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
            CheckBox checkBox = editingElement as CheckBox;
            if (checkBox != null)
            {
                EGC.DataGridHelper.UpdateTarget(checkBox, CheckBox.IsCheckedProperty);
            }
        }

        internal override void OnInput(InputEventArgs e)
        {
            // Space key down will begin edit mode
            if (IsSpaceKeyDown(e))
            {
                BeginEdit(e);
            }
        }

        private static bool IsMouseLeftButtonDown(RoutedEventArgs e)
        {
            MouseButtonEventArgs mouseArgs = e as MouseButtonEventArgs;
            return (mouseArgs != null) &&
                   (mouseArgs.ChangedButton == MouseButton.Left) &&
                   (mouseArgs.ButtonState == MouseButtonState.Pressed);
        }

        private static bool IsMouseOver(CheckBox checkBox, RoutedEventArgs e)
        {
            // This element is new, so the IsMouseOver property will not have been updated
            // yet, but there is enough information to do a hit-test.
            return checkBox.InputHitTest(((MouseButtonEventArgs)e).GetPosition(checkBox)) != null;
        }

        private static bool IsSpaceKeyDown(RoutedEventArgs e)
        {
            KeyEventArgs keyArgs = e as KeyEventArgs;
            return (keyArgs != null) &&
                   ((keyArgs.KeyStates & KeyStates.Down) == KeyStates.Down) &&
                   (keyArgs.Key == Key.Space);
        }

        #endregion

        #region Data

        private static Style _defaultElementStyle;
        private static Style _defaultEditingElementStyle;

        #endregion
    }
}
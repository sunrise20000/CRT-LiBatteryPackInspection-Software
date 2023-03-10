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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    ///     A column that displays editable text.
    /// </summary>
    public class DataGridTextColumn : EGC.DataGridBoundColumn
    {
        static DataGridTextColumn()
        {
            ElementStyleProperty.OverrideMetadata(typeof(EGC.DataGridTextColumn), new FrameworkPropertyMetadata(DefaultElementStyle));
            EditingElementStyleProperty.OverrideMetadata(typeof(EGC.DataGridTextColumn), new FrameworkPropertyMetadata(DefaultEditingElementStyle));
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
                    Style style = new Style(typeof(TextBlock));

                    // Use the same margin used on the TextBox to provide space for the caret
                    style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(2.0, 0.0, 2.0, 0.0)));

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
                    Style style = new Style(typeof(TextBox));

                    style.Setters.Add(new Setter(TextBox.BorderThicknessProperty, new Thickness(0.0)));
                    style.Setters.Add(new Setter(TextBox.PaddingProperty, new Thickness(0.0)));

                    style.Seal();
                    _defaultEditingElementStyle = style;
                }

                return _defaultEditingElementStyle;
            }
        }

        #endregion

        #region Element Generation

        /// <summary>
        ///     Creates the visual tree for text based cells.
        /// </summary>
        protected override FrameworkElement GenerateElement(EGC.DataGridCell cell, object dataItem)
        {
            TextBlock textBlock = new TextBlock();

            SyncProperties(textBlock);

            ApplyStyle(/* isEditing = */ false, /* defaultToElementStyle = */ false, textBlock);
            ApplyBinding(textBlock, TextBlock.TextProperty);

            return textBlock;
        }

        /// <summary>
        ///     Creates the visual tree for text based cells.
        /// </summary>
        protected override FrameworkElement GenerateEditingElement(EGC.DataGridCell cell, object dataItem)
        {
            TextBox textBox = new TextBox();

            SyncProperties(textBox);

            ApplyStyle(/* isEditing = */ true, /* defaultToElementStyle = */ false, textBox);
            ApplyBinding(textBox, TextBox.TextProperty);

            return textBox;
        }

        private void SyncProperties(FrameworkElement e)
        {
            EGC.DataGridHelper.SyncColumnProperty(this, e, TextElement.FontFamilyProperty, FontFamilyProperty);
            EGC.DataGridHelper.SyncColumnProperty(this, e, TextElement.FontSizeProperty, FontSizeProperty);
            EGC.DataGridHelper.SyncColumnProperty(this, e, TextElement.FontStyleProperty, FontStyleProperty);
            EGC.DataGridHelper.SyncColumnProperty(this, e, TextElement.FontWeightProperty, FontWeightProperty);
            EGC.DataGridHelper.SyncColumnProperty(this, e, TextElement.ForegroundProperty, ForegroundProperty);
        }

        protected internal override void RefreshCellContent(FrameworkElement element, string propertyName)
        {
            EGC.DataGridCell cell = element as EGC.DataGridCell;
            
            if (cell != null)
            {
                FrameworkElement textElement = cell.Content as FrameworkElement;

                if (textElement != null)
                {
                    switch (propertyName)
                    {
                        case "FontFamily":
                            EGC.DataGridHelper.SyncColumnProperty(this, textElement, TextElement.FontFamilyProperty, FontFamilyProperty);
                            break;
                        case "FontSize":
                            EGC.DataGridHelper.SyncColumnProperty(this, textElement, TextElement.FontSizeProperty, FontSizeProperty);
                            break;
                        case "FontStyle":
                            EGC.DataGridHelper.SyncColumnProperty(this, textElement, TextElement.FontStyleProperty, FontStyleProperty);
                            break;
                        case "FontWeight":
                            EGC.DataGridHelper.SyncColumnProperty(this, textElement, TextElement.FontWeightProperty, FontWeightProperty);
                            break;
                        case "Foreground":
                            EGC.DataGridHelper.SyncColumnProperty(this, textElement, TextElement.ForegroundProperty, ForegroundProperty);
                            break;
                    }
                }
            }
            
            base.RefreshCellContent(element, propertyName);
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
            TextBox textBox = editingElement as TextBox;
            if (textBox != null)
            {
                textBox.Focus();

                string originalValue = textBox.Text;

                TextCompositionEventArgs textArgs = editingEventArgs as TextCompositionEventArgs;
                if (textArgs != null)
                {
                    // If text input started the edit, then replace the text with what was typed.
                    string inputText = textArgs.Text;
                    textBox.Text = inputText;
                    
                    // Place the caret after the end of the text.
                    textBox.Select(inputText.Length, 0);
                }
                else
                {
                    // If a mouse click started the edit, then place the caret under the mouse.
                    MouseButtonEventArgs mouseArgs = editingEventArgs as MouseButtonEventArgs;
                    if ((mouseArgs == null) || !PlaceCaretOnTextBox(textBox, Mouse.GetPosition(textBox)))
                    {
                        // If the mouse isn't over the textbox or something else started the edit, then select the text.
                        textBox.SelectAll();
                    }
                }

                return originalValue;
            }

            return null;
        }

        private static bool PlaceCaretOnTextBox(TextBox textBox, Point position)
        {
            int characterIndex = textBox.GetCharacterIndexFromPoint(position, /* snapToText = */ false);
            if (characterIndex >= 0)
            {
                textBox.Select(characterIndex, 0);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Called when a cell's value is to be committed, just before it exits edit mode.
        /// </summary>
        /// <param name="editingElement">A reference to element returned by GenerateEditingElement.</param>
        /// <returns>false if there is a validation error. true otherwise.</returns>
        protected override bool CommitCellEdit(FrameworkElement editingElement)
        {
            TextBox textBox = editingElement as TextBox;
            if (textBox != null)
            {
                EGC.DataGridHelper.UpdateSource(textBox, TextBox.TextProperty);
                return !Validation.GetHasError(textBox);
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
            TextBox textBox = editingElement as TextBox;
            if (textBox != null)
            {
                EGC.DataGridHelper.UpdateTarget(textBox, TextBox.TextProperty);
            }
        }

        internal override void OnInput(InputEventArgs e)
        {
            // Text input will start an edit
            if (e is TextCompositionEventArgs)
            {
                BeginEdit(e);
            }
        }

        #endregion

        #region Element Properties

        /// <summary>
        ///     The DependencyProperty for the FontFamily property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty =
                TextElement.FontFamilyProperty.AddOwner(
                        typeof(EGC.DataGridTextColumn),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.Inherits, EGC.DataGridColumn.NotifyPropertyChangeForRefreshContent));

        /// <summary>
        ///     The font family of the desired font.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the FontSize property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font Size
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty =
                TextElement.FontSizeProperty.AddOwner(
                        typeof(EGC.DataGridTextColumn),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.Inherits, EGC.DataGridColumn.NotifyPropertyChangeForRefreshContent));

        /// <summary>
        ///     The size of the desired font.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        [TypeConverter(typeof(FontSizeConverter))]
        [Localizability(LocalizationCategory.None)]
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the FontStyle property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font Style
        /// </summary>
        public static readonly DependencyProperty FontStyleProperty =
                TextElement.FontStyleProperty.AddOwner(
                        typeof(EGC.DataGridTextColumn),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle, FrameworkPropertyMetadataOptions.Inherits, EGC.DataGridColumn.NotifyPropertyChangeForRefreshContent));

        /// <summary>
        ///     The style of the desired font.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the FontWeight property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font Weight
        /// </summary>
        public static readonly DependencyProperty FontWeightProperty =
                TextElement.FontWeightProperty.AddOwner(
                        typeof(EGC.DataGridTextColumn),
                        new FrameworkPropertyMetadata(SystemFonts.MessageFontWeight, FrameworkPropertyMetadataOptions.Inherits, EGC.DataGridColumn.NotifyPropertyChangeForRefreshContent));

        /// <summary>
        ///     The weight or thickness of the desired font.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the Foreground property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Font Color
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty =
                TextElement.ForegroundProperty.AddOwner(
                        typeof(EGC.DataGridTextColumn),
                        new FrameworkPropertyMetadata(SystemColors.ControlTextBrush, FrameworkPropertyMetadataOptions.Inherits, EGC.DataGridColumn.NotifyPropertyChangeForRefreshContent));

        /// <summary>
        ///     An brush that describes the foreground color.
        ///     This will only affect controls whose template uses the property
        ///     as a parameter. On other controls, the property will do nothing.
        /// </summary>
        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        #endregion

        #region Data

        private static Style _defaultElementStyle;
        private static Style _defaultEditingElementStyle;
        
        #endregion
    }
}
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Sicentury.Core.AttachedProperties
{
    public static class TextBlockHighlighter
    {
        public static string GetSelection(DependencyObject obj)
        {
            return (string)obj.GetValue(SelectionProperty);
        }

        public static void SetSelection(DependencyObject obj, string value)
        {
            obj.SetValue(SelectionProperty, value);
        }

        public static readonly DependencyProperty SelectionProperty =
            DependencyProperty.RegisterAttached("Selection", typeof(string), typeof(TextBlockHighlighter),
                new PropertyMetadata(new PropertyChangedCallback(SelectText)));

        private static void SelectText(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) 
                return;
            if (!(d is TextBlock txtBlock)) 
                throw new InvalidOperationException("Only valid for TextBlock");

            var text = txtBlock.Text;
            if (string.IsNullOrEmpty(text)) 
                return;

            var highlightText = (string)txtBlock.GetValue(SelectionProperty);
            if (string.IsNullOrEmpty(highlightText))
            {
                txtBlock.Inlines.Clear();
                txtBlock.Inlines.Add(text);
                return;
            }

            var index = text.IndexOf(highlightText, StringComparison.CurrentCultureIgnoreCase);
            if (index < 0) 
                return;

            var selectionColor = (Brush)txtBlock.GetValue(HighlightColorProperty);
            var foreColor = (Brush)txtBlock.GetValue(ForeColorProperty);

            txtBlock.Inlines.Clear();
            while (true)
            {
                txtBlock.Inlines.AddRange(new Inline[]
                {
                    new Run(text.Substring(0, index)),
                    new Run(text.Substring(index, highlightText.Length))
                    {
                        Background = selectionColor,
                        Foreground = foreColor
                    }
                });

                text = text.Substring(index + highlightText.Length);
                index = text.IndexOf(highlightText, StringComparison.CurrentCultureIgnoreCase);

                if (index < 0)
                {
                    txtBlock.Inlines.Add(new Run(text));
                    break;
                }
            }
        }

        public static Brush GetHighlightColor(DependencyObject obj)
        {
            return (Brush)obj.GetValue(HighlightColorProperty);
        }

        public static void SetHighlightColor(DependencyObject obj, Brush value)
        {
            obj.SetValue(HighlightColorProperty, value);
        }

        public static readonly DependencyProperty HighlightColorProperty =
            DependencyProperty.RegisterAttached("HighlightColor", typeof(Brush), typeof(TextBlockHighlighter),
                new PropertyMetadata(Brushes.Yellow, new PropertyChangedCallback(SelectText)));


        public static Brush GetForeColor(DependencyObject obj)
        {
            return (Brush)obj.GetValue(ForeColorProperty);
        }

        public static void SetForeColor(DependencyObject obj, Brush value)
        {
            obj.SetValue(ForeColorProperty, value);
        }

        public static readonly DependencyProperty ForeColorProperty =
            DependencyProperty.RegisterAttached("ForeColor", typeof(Brush), typeof(TextBlockHighlighter),
                new PropertyMetadata(Brushes.Black, new PropertyChangedCallback(SelectText)));

    }
}
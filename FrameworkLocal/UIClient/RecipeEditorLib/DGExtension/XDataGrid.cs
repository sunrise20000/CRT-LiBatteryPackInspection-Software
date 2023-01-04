using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MECF.Framework.UI.Client.RecipeEditorLib.DGExtension
{
    public class XDataGrid : ExtendedGrid.Microsoft.Windows.Controls.DataGrid
    {
        #region Variables

        public ScrollViewer ref_scrollviewer;

        #endregion

        #region Constructors

        public XDataGrid()
        {
            SelectionChanged += OnSelectionChanged;
        }

        #endregion

  
        public static readonly DependencyProperty SelectedItemsListProperty = DependencyProperty.Register(
            nameof(SelectedItemsList), typeof(IList), typeof(XDataGrid), new PropertyMetadata(default(IList)));

        public IList SelectedItemsList
        {
            get => (IList)GetValue(SelectedItemsListProperty);
            set => SetValue(SelectedItemsListProperty, value);
        }


        public static readonly DependencyProperty UseHorizontalScrollingProperty = DependencyProperty.RegisterAttached("UseHorizontalScrolling", typeof(bool), typeof(XDataGrid),
              new PropertyMetadata(default(bool), UseHorizontalScrollingChangedCallback));

        public bool UseHorizontalScrolling
        {
            get => (bool)GetValue(UseHorizontalScrollingProperty);
            set => SetValue(UseHorizontalScrollingProperty, value);
        }
        

        #region Methods

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItemsList = SelectedItems;
        }


        private void _UseHorizontalScrollingChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            PreviewMouseWheel += delegate(object sender, MouseWheelEventArgs args)
            {
                ScrollViewer scrollViewer = GetTemplateChild("DG_ScrollViewer") as ScrollViewer;
                if (scrollViewer != null)
                {
                    if (args.Delta > 0)
                        scrollViewer.LineLeft();
                    else
                        scrollViewer.LineRight();
                }
                ref_scrollviewer = scrollViewer;
                args.Handled = true;
            };
        }

        private static void UseHorizontalScrollingChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            XDataGrid datagrid = dependencyObject as XDataGrid;

            if (datagrid == null)
                throw new ArgumentException("Element is not an ItemsControl");

            datagrid._UseHorizontalScrollingChangedCallback(dependencyObject, dependencyPropertyChangedEventArgs);
        }

        public static void SetUseHorizontalScrolling(ItemsControl element, bool value)
        {
            element.SetValue(UseHorizontalScrollingProperty, value);
        }

        public static bool GetUseHorizontalScrolling(ItemsControl element)
        {
            return (bool)element.GetValue(UseHorizontalScrollingProperty);
        }

        #endregion
    }
}

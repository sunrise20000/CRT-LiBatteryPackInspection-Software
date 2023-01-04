using System.Collections.Generic;
using System.Windows;
using ExtendedGrid.Microsoft.Windows.Controls;

namespace MECF.Framework.UI.Client.RecipeEditorLib
{
    public class DataGridSelectedCellsBehavior
    {
        // Source : https://archive.codeplex.com/?p=datagridthemesfromsl
        // Credit to : T. Webster, https://stackoverflow.com/users/266457/t-webster

        public static IList<DataGridCellInfo> GetSelectedCells(DependencyObject obj)
        {
            return (IList<DataGridCellInfo>)obj.GetValue(SelectedCellsProperty);
        }

        public static void SetSelectedCells(DependencyObject obj, IList<DataGridCellInfo> value)
        {
            obj.SetValue(SelectedCellsProperty, value);
        }

        public static readonly DependencyProperty SelectedCellsProperty =
            DependencyProperty.RegisterAttached("SelectedCells", typeof(IList<DataGridCellInfo>),
                typeof(DataGridSelectedCellsBehavior), new UIPropertyMetadata(null, OnSelectedCellsChanged));

        static SelectedCellsChangedEventHandler GetSelectionChangedHandler(DependencyObject obj)
        {
            return (SelectedCellsChangedEventHandler)obj.GetValue(SelectionChangedHandlerProperty);
        }

        static void SetSelectionChangedHandler(DependencyObject obj, SelectedCellsChangedEventHandler value)
        {
            obj.SetValue(SelectionChangedHandlerProperty, value);
        }

        static readonly DependencyProperty SelectionChangedHandlerProperty =
            DependencyProperty.RegisterAttached("SelectedCellsChangedEventHandler",
                typeof(SelectedCellsChangedEventHandler), typeof(DataGridSelectedCellsBehavior),
                new UIPropertyMetadata(null));

        //d is MultiSelector (d as ListBox not supported)
        static void OnSelectedCellsChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (GetSelectionChangedHandler(d) != null)
                return;

            if (d is DataGrid) //DataGrid
            {
                DataGrid datagrid = d as DataGrid;
                SelectedCellsChangedEventHandler selectionchanged = null;
                foreach (var selected in GetSelectedCells(d) as IList<DataGridCellInfo>)
                    datagrid.SelectedCells.Add(selected);

                selectionchanged = (sender, e) => { SetSelectedCells(d, datagrid.SelectedCells); };
                SetSelectionChangedHandler(d, selectionchanged);
                datagrid.SelectedCellsChanged += GetSelectionChangedHandler(d);
            }
            //else if (d is ListBox)
            //{
            //    ListBox listbox = d as ListBox;
            //    SelectionChangedEventHandler selectionchanged = null;

            //    selectionchanged = (sender, e) =>
            //    {
            //        SetSelectedCells(d, listbox.SelectedCells);
            //    };
            //    SetSelectionChangedHandler(d, selectionchanged);
            //    listbox.SelectionChanged += GetSelectionChangedHandler(d);
            //}
        }

    }
}

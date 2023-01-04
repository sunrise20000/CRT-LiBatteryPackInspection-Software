using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel.Params;
using OpenSEMI.Controls.Controls;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;
using DataGridBeginningEditEventArgs = ExtendedGrid.Microsoft.Windows.Controls.DataGridBeginningEditEventArgs;
using DataGridRowEventArgs = ExtendedGrid.Microsoft.Windows.Controls.DataGridRowEventArgs;
using DataGridSelectionMode = ExtendedGrid.Microsoft.Windows.Controls.DataGridSelectionMode;
using DataGridSelectionUnit = ExtendedGrid.Microsoft.Windows.Controls.DataGridSelectionUnit;
using ExDataGridRow = ExtendedGrid.Microsoft.Windows.Controls.DataGridRow;

namespace SicUI.Models.RecipeEditors
{
    /// <summary>
    /// Interaction logic for RecipePM1View.xaml
    /// </summary>
    public partial class RecipeEditorView
    {
        public RecipeEditorView()
        {
            InitializeComponent();
        }
        EditorDataGridTemplateColumnBase _PreColumn = null;
        private void dgCustom_CurrentCellChanged(object sender, System.EventArgs e)
        {
            var datagrid = sender as XDataGrid;
            if (datagrid == null) return;
            var column = datagrid.CurrentColumn as EditorDataGridTemplateColumnBase;
            if (column == null) return;

            if (_PreColumn == datagrid.CurrentColumn) return;

            if (_PreColumn != null)
            {
                _PreColumn.IsColumnSelected = false;
                foreach (var item in datagrid.Items)
                {
                    var list = item as System.Collections.ObjectModel.ObservableCollection<Param>;
                    if (list == null) continue;
                    foreach (var p in list)
                    {
                        if (p.Name == _PreColumn.ControlName) p.IsColumnSelected = false;
                    }
                }
            }
            column.IsColumnSelected = true;
            _PreColumn = column;
            //var jj = datagrid.Items as System.Collections.ObjectModel.ObservableCollection<RecipeEditorLib.RecipeModel.Params.Param>;
            foreach (var item in datagrid.Items)
            {
                var list = item as System.Collections.ObjectModel.ObservableCollection<Param>;
                if (list == null) continue;
                foreach (var p in list)
                {
                    if (p.Name == column.ControlName) p.IsColumnSelected = true;
                }
            }
        }

        private void DgCustom_OnLoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Selected += RowOnSelected;
            e.Row.Unselected += RowOnUnselected;
        }

        private void DgCustom_OnUnloadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Selected -= RowOnSelected;
            e.Row.Unselected -= RowOnUnselected;
        }

        private void RowOnSelected(object sender, RoutedEventArgs e)
        {
            if (!(e.Source is ExDataGridRow dgr)) 
                return;

            if (!(dgr.DataContext is RecipeStep recipeStep)) 
                return;

            var param = recipeStep.FirstOrDefault(x => x.Name == RecipColNo.StepNo.ToString());
            if (param is StepParam sp)
            {
                sp.IsChecked = true;
            }
        }


        private void RowOnUnselected(object sender, RoutedEventArgs e)
        {
            if (!(e.Source is ExDataGridRow dgr))
                return;

            if (!(dgr.DataContext is RecipeStep recipeStep))
                return;

            var param = recipeStep.FirstOrDefault(x => x.Name == RecipColNo.StepNo.ToString());
            if (param is StepParam sp)
            {
                sp.IsChecked = false;
            }
        }

     
        private void DgCustom_OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is RecipeStep step)
            {
                var param = step[e.Column.DisplayIndex];
             
                // 如果当前参数是只读的，则退出编辑模式。
                // 参数只读有两种原因：
                // 1. RecipeFormat中配置为ReadOnly
                // 2. 当前列权限配置为Read
                if (e.Column.IsReadOnly || param.IsHideValue)
                    e.Cancel = true;
            }
        }

        private void UIElement_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is RecipeEditorViewModel vm)
            {
                // 仅在编辑AccessPermissionWhitelist编辑模式下有效。
                if (dgCustom.SelectionUnit != DataGridSelectionUnit.Cell)
                    return;

                if (sender is Border bdr && bdr.DataContext is Param param)
                {
                    if (param.IsHighlighted)
                        param.ResetHighlight();
                    else
                        param.Highlight();
                }

                vm.CountHaveAccessPermParams();
                //vm.SelectedGridCellCollection = e.AddedCells;
                //vm.ToggleCellAccessPermissionWhitelistMark();
            }
        }
    }
}

using MECF.Framework.UI.Client.CenterViews.Editors;
using RecipeEditorLib.RecipeModel.Params;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel;
using DataGridRowEventArgs = ExtendedGrid.Microsoft.Windows.Controls.DataGridRowEventArgs;
using ExDataGridRow = ExtendedGrid.Microsoft.Windows.Controls.DataGridRow;

namespace MECF.Framework.UI.Client.RecipeEditorLib
{
    /// <summary>
    /// Interaction logic for RecipeEditDataGrid.xaml
    /// </summary>
    public partial class RecipeEditDataGrid : UserControl
    {
        #region Constructor


        public RecipeEditDataGrid()
        {
            InitializeComponent();
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty SelectedRecipeStepsProperty = DependencyProperty.Register(
            nameof( SelectedRecipeStepsProperty), typeof(RecipeStepCollection), typeof(RecipeEditDataGrid), new PropertyMetadata(default(RecipeStepCollection)));

        public RecipeStepCollection  SelectedRecipeSteps
        {
            get => (RecipeStepCollection)GetValue( SelectedRecipeStepsProperty);
            set => SetValue(SelectedRecipeStepsProperty, value);
        }

        #endregion


        #region Events

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

        #endregion

        #region Methods

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

        #endregion
    }
}

using System.Collections.ObjectModel;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel.Params;
using RecipeEditorLib.DGExtension.CustomColumn;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class MultipleSelectParam : Param
    {
        public MultipleSelectParam(MultipleSelectColumn col)
        {
            this.Options = new ObservableCollection<MultipleSelectColumn.Option>();
            col.CloneOptions(this).ForEach(opt => this.Options.Add(opt));
            this.IsSaved = true;
        }

        public ObservableCollection<MultipleSelectColumn.Option> Options { get; set; }
    }
}

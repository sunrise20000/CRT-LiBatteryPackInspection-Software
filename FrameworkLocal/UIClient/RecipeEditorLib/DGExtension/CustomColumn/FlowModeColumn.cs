using System.Collections.ObjectModel;

namespace RecipeEditorLib.DGExtension.CustomColumn
{
    public class FlowModeColumn : ComboxColumn
    {
        public FlowModeColumn() : base()
        {
            Options = new ObservableCollection<Option>();
        }
    }
}

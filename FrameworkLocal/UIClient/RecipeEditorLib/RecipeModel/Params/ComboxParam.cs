using System.Collections.ObjectModel;
using RecipeEditorLib.DGExtension.CustomColumn;
namespace RecipeEditorLib.RecipeModel.Params
{
    public class ComboxParam : ParamBaseWithGenericValue<string>
    {
        #region Constructors

        public ComboxParam()
        {
        }

        public ComboxParam(string initValue) : base(initValue)
        {
        }

        #endregion


        public ObservableCollection<ComboxColumn.Option> Options { get; set; }
        
        private bool _isEditable;
        public bool IsEditable
        {
            get
            {
                return _isEditable;
            }
            set
            {
                _isEditable = value;
                this.NotifyOfPropertyChange(nameof(IsEditable));
            }
        }

        private string foreground = "Black";
        public string Foreground
        {
            get
            {
                return foreground;
            }
            set
            {
                foreground = value;
                this.NotifyOfPropertyChange(nameof(Foreground));
            }
        }

        public string LoopBackground
        {
            get
            {
                return "Transparent";
            }
        }


        public bool IsLoopItem
        {
            get { return false; }
        }
    }
}

using System.Collections.ObjectModel;
using RecipeEditorLib.DGExtension.CustomColumn;
namespace RecipeEditorLib.RecipeModel.Params
{
    public class LoopComboxParam : ParamBaseWithGenericValue<string>
    {

        #region Constructors

        public LoopComboxParam()
        {
        }

        public LoopComboxParam(string initValue) : base(initValue)
        {
        }

        #endregion

        public ObservableCollection<LoopComboxColumn.Option> Options { get; set; }

        private bool _isEditable;
        public bool IsEditable
        {
            get => _isEditable;
            set
            {
                _isEditable = value;
                NotifyOfPropertyChange(nameof(IsEditable));
            }
        }

        private bool _isLoopStep;
        public bool IsLoopStep
        {
            get => _isLoopStep;
            set
            {
                _isLoopStep = value;
                NotifyOfPropertyChange(nameof(IsLoopStep));
                NotifyOfPropertyChange(nameof(LoopBackground));
            }
        }

        private bool _isValidLoop;
        public bool IsValidLoop
        {
            get => _isValidLoop;
            set
            {
                _isValidLoop = value;
                NotifyOfPropertyChange(nameof(IsValidLoop));
                NotifyOfPropertyChange(nameof(LoopBackground));
            }
        }

        public string LoopBackground => IsLoopStep ? (IsValidLoop ? "#90EE90" : "#FFC0CB") : "Transparent";


        public bool IsLoopItem { get; set; }
    }
}

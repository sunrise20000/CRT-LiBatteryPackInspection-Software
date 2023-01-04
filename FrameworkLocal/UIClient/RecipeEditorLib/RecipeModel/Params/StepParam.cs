namespace RecipeEditorLib.RecipeModel.Params
{
    public class StepParam : ParamBaseWithGenericValue<int>
    {
        #region Variables

        private bool _checked;

        #endregion

        #region Constructors

        public StepParam()
        {
        }

        public StepParam(int initValue) : base(initValue)
        {
        }

        #endregion

        #region Properties

        public override bool IsHideValue
        {
            get => false;
            set
            {
                
            }
        }

        public bool IsChecked
        {
            get => _checked;
            set
            {
                _checked = value;
                NotifyOfPropertyChange(nameof(IsChecked));
            }
        }

        #endregion
    }
}

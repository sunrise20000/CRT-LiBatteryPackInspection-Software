namespace RecipeEditorLib.RecipeModel.Params
{
    public class IntParam : NumParamBase<int>
    {
        #region Constructors

        public IntParam()
        {
        }

        public IntParam(int initValue) : base(initValue)
        {
        }

        public IntParam(int initValue, int minimum, int maximum) : base(initValue, minimum, maximum)
        {
        }

        #endregion
      
    }
}

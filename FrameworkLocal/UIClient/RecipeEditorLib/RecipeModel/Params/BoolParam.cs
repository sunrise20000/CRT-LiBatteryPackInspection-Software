namespace RecipeEditorLib.RecipeModel.Params
{
    public class BoolParam : ParamBaseWithGenericValue<bool>
    {
        #region Constructors

        public BoolParam()
        {
        }

        public BoolParam(bool initValue) : base(initValue)
        {
        }

        #endregion
    }
}

using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class RecipeStepValidationInfo
    {
        #region Constructors

        public RecipeStepValidationInfo(RecipeStep step, IParam param, string message)
        {
            Step = step;
            Param = param;
            Message = message;

            RecipeStep.ValidateStepNo(step.StepNo, out var vStepNo);
            StepNo = vStepNo;

            ParamCaption = param?.DisplayName ?? "Unknown";
            Value = param?.GetValue() ?? "Unknown";
        }

        #endregion

        #region Properties

        public RecipeStep Step { get; }

        public IParam Param { get; }

        public int StepNo { get; }

        public string ParamCaption { get; }

        public object Value { get; }

        public string Message { get; }

        #endregion

        #region Methdos

        public override string ToString()
        {
            return $"[Step No. {Step.StepNo}]-{Param.DisplayName} {Message}";
        }

        #endregion
    }
}

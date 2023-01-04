namespace RecipeEditorLib.RecipeModel.Params
{
    public class DoubleParam : NumParamBase<double>
    {
        #region Variables
        
        private int _resolution;

        #endregion

        #region Constructors

        public DoubleParam()
        {
            Placeholder = "*";
        }

        public DoubleParam(double initValue) : base(initValue)
        {
            Placeholder = "*";
        }

        public DoubleParam(double initValue, string placeholder) : base(initValue)
        {
            Placeholder = placeholder;
        }

        public DoubleParam(double initValue, double minimum, double maximum) : base(initValue, minimum, maximum)
        {
            Placeholder = "*";
        }

        public DoubleParam(double initValue, double minimum, double maximum, string placeholder) : base(initValue, minimum, maximum)
        {
            Placeholder = placeholder;
        }

        #endregion

        /// <summary>
        /// 返回当Value不可用时的占位符。
        /// </summary>
        public string Placeholder { get; }


        public int Resolution
        {
            get => _resolution;
            set
            {
                _resolution = value;
                NotifyOfPropertyChange(nameof(Resolution));
            }
        }
    }
}

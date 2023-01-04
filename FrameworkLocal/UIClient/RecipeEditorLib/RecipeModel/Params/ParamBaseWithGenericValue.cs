using System.Collections.Generic;
using System.ComponentModel;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel.Params;

namespace RecipeEditorLib.RecipeModel.Params
{
    public abstract class ParamBaseWithGenericValue<TValue> : Param
    {
        #region Variables

        protected TValue _value;
        protected TValue _valueSnapshot;
        protected IParam _previous;

        #endregion

        #region Constructors

        protected ParamBaseWithGenericValue()
        {
        }

        protected ParamBaseWithGenericValue(TValue initValue) : this()
        {
            if (initValue.GetType() != typeof(TValue))
                initValue = default;
            
            _value = initValue;
            _valueSnapshot = initValue;
        }

        #endregion

        #region Properties


        public virtual TValue Value
        {
            get => _value;
            set
            {
                _value = value;
                NotifyOfPropertyChange(nameof(Value));

                Feedback?.Invoke(this);

                _isSaved = _value.Equals(_valueSnapshot);
                NotifyOfPropertyChange(nameof(IsSaved));

                // 当Value发生变化时，要办一下事项，以及时刷新UI状态。
                CheckValueEqualityWithPrevious();
                Validate();
            }
        }

        public override IParam Previous
        {
            get => _previous;
            set
            {
                //! 如果前序参数的Value发生了变化，则需要通知我进行比较，以确定我的Value是否和前序参数的Value相等。

                // 注销之前注册的事件。
                if (_previous is Param preParam)
                    preParam.PropertyChanged -= OnPreviousParamValueChanged;

                _previous = value;

                // 注册事件.
                if (_previous is Param p)
                {
                    p.PropertyChanged += OnPreviousParamValueChanged;

                    // 立即执行一次比较
                    CheckValueEqualityWithPrevious();
                }

            }
        }
        
        public override IParam Next { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// 判断Value属性是否相等。
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        protected static bool IsTValueEqual(TValue v1, TValue v2)
        {
            return Comparer<TValue>.Default.Compare(v1, v2) == 0;
        }

        /// <summary>
        /// 检查我的Value是否和前序参数的Value相等。
        /// </summary>
        private void CheckValueEqualityWithPrevious()
        {
            if (_previous is ParamBaseWithGenericValue<TValue> valuedParam)
            {
                IsEqualsToPrevious = IsTValueEqual(_value, valuedParam.Value);
            }
        }

        /// <summary>
        /// 当前序参数的Value发生变化时，判断其是否和我的Value属性相等。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPreviousParamValueChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Value) && _previous is ParamBaseWithGenericValue<TValue> valuedParam)
            {
                IsEqualsToPrevious = IsTValueEqual(_value, valuedParam.Value);
            }
        }

        /// <summary>
        /// 保存数据。
        /// </summary>
        public override void Save()
        {
            _valueSnapshot = _value;
            base.Save();
        }

        /// <summary>
        /// 获取当前参数的数值。
        /// </summary>
        /// <returns></returns>
        public override object GetValue()
        {
            return Value;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, Value={_value}";
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro.Core;
using ExtendedGrid.Microsoft.Windows.Controls;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;

namespace MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel.Params
{
    public abstract class Param : PropertyChangedBase, IParam
    {
        #region Variables

        protected string _name;
        protected string _displayName;
        protected string _unitName;
        protected bool _isEnabled;
        protected bool _isSaved;
        protected bool _isColumnSelected;
        protected Visibility _visibility;
        protected bool _isValidated;
        protected string _validationError;
        protected bool _isEqualsToPrevious;
        protected bool _isHighlighted;
        protected bool _isHideValue;

        #endregion

        #region Constructors

        protected Param()
        {
            _isSaved = true;
            _isEnabled = true;
            _isValidated = true;
            _validationError = null;
            _isEqualsToPrevious = true;
            _visibility = Visibility.Visible;
            _isHighlighted = false;
            _isHideValue = false;
        }
        
        #endregion

        #region Properties

        public Action<Param> Feedback { get; set; }

        public Func<Param, bool> Check { get; set; }

        public RecipeStep Parent { get; set; }

        public DataGridRow RowOwner { get; set; }

        public DataGridColumn ColumnOwner { get; set; }

        public virtual IParam Previous { get; set; }

        public virtual IParam Next { get; set; }


        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyOfPropertyChange();
            }
        }


        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                NotifyOfPropertyChange();
            }
        }


        public string UnitName
        {
            get => _unitName;
            set
            {
                _unitName = value;
                NotifyOfPropertyChange();
            }
        }


        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                NotifyOfPropertyChange();
            }
        }
        
        public virtual bool IsSaved
        {
            get => _isSaved;
            set
            {
                _isSaved = value;
                NotifyOfPropertyChange();
            }
        }


        /// <summary>
        /// 是否和前序值相等。
        /// </summary>
        public bool IsEqualsToPrevious
        {
            get => _isEqualsToPrevious;
            set
            {
                _isEqualsToPrevious = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// 是否通过验证。
        /// </summary>
        public bool IsValidated
        {
            get => _isValidated;
            internal set
            {
                _isValidated = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// 校验错误原因。
        /// </summary>
        public string ValidationError
        {
            get => _validationError;
            protected set
            {
                _validationError = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// 返回是否高亮当前参数。
        /// </summary>
        public bool IsHighlighted
        {
            get => _isHighlighted;
            private set
            {
                _isHighlighted = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// 返回是否隐藏参数的值。
        /// </summary>
        public virtual bool IsHideValue
        {
            get => _isHideValue;
            set
            {
                _isHideValue = value;
                NotifyOfPropertyChange();
            }
        }
        

        public bool IsColumnSelected
        {
            get => _isColumnSelected;
            set
            {
                _isColumnSelected = value;
                NotifyOfPropertyChange();
            }
        }

        public Visibility Visible
        {
            get => _visibility;
            set
            {
                _visibility = value;
                NotifyOfPropertyChange();
            }
        }

        public bool EnableConfig { get; set; }

        public bool EnableTolerance { get; set; }

        public Visibility StepCheckVisibility { get; set; }
        
        /// <summary>
        /// 高亮显示当前参数。
        /// </summary>
        public void Highlight()
        {
            IsHighlighted = true;
        }

        /// <summary>
        /// 取消高亮显示。
        /// </summary>
        public void ResetHighlight()
        {
            if(IsHighlighted)
                IsHighlighted = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// 将参数链表转换为数组。
        /// </summary>
        /// <returns></returns>
        public virtual List<IParam> Flatten()
        {
            if (Previous != null)
                return Previous.Flatten();

            var list = new List<IParam>();
            IParam p = this;
            while (p != null)
            {
                list.Add(p);
                p = p.Next;
            }

            return list;
        }

        /// <summary>
        /// 校验数据。
        /// </summary>
        /// <returns></returns>
        public virtual void Validate()
        {
            IsValidated = true;
            ValidationError = "";
        }

        public virtual void Save()
        {
            IsSaved = true;
        }

        public virtual object GetValue()
        {
            return "Not Support";
        }

        public override string ToString()
        {
            return $"{DisplayName}, ControlName={Name}";
        }

        #endregion
    }
}

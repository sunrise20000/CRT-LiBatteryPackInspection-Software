using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel;
using Sicentury.Core;

namespace RecipeEditorLib.RecipeModel.Params
{
    public sealed class RecipeStepCollection : ObservableCollection<RecipeStep>
    {
        #region Variables

        private bool _isSaved;
        private bool _isHideValue;

        #endregion

        #region Constructors

        public RecipeStepCollection()
        {
            _isSaved = true;
        }

        public RecipeStepCollection(RecipeData parent)
        {
            _isSaved = true;
            Parent = parent;
        }

        public RecipeStepCollection(IList<RecipeStep> steps, RecipeData parent) : base(steps)
        {
            _isSaved = true;
            Parent = parent;

            foreach (var step in steps)
                step.Parent = parent;
        }

        #endregion
        
        #region Properties

        public RecipeData Parent { get; }

        /// <summary>
        /// 返回当前配方是否已经被保存。
        /// </summary>
        public bool IsSaved
        {
            get => _isSaved;
            private set
            {
                _isSaved = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSaved)));
            }
        }

        /// <summary>
        /// 返回被选中的配方步骤。
        /// </summary>
        public IReadOnlyList<RecipeStep> SelectedSteps =>
            this.ToList().Where(x => x.StepNoParam != null && x.StepNoParam.IsChecked).ToList();

        
        /// <summary>
        /// 返回是否隐藏所有步骤的参数值。
        /// </summary>
        public bool IsHideValue
        {
            get => _isHideValue;
            set
            {
                _isHideValue = value;
                this.ToList().ForEach(x => x.IsHideValue = value);
            }
        }
        
        #endregion

        #region Methods

        /// <summary>
        /// 当配方中的某步骤的IsSaved状态发生变化时，触发此事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemOnSavedStateChanged(object sender, bool e)
        {
            if (e == false)
                IsSaved = false;
            else
                IsSaved = (this.ToList().FirstOrDefault(x => x.IsSaved == false) == null);
        }

        /// <summary>
        /// 更新Step序列号。
        /// </summary>
        /// <param name="MarkAsSaved"></param>
        public void UpdateStepIndex(bool MarkAsSaved = false)
        {
            var step = this.FirstOrDefault();

            var stepNo = step?.StepNoParam;
            if(stepNo == null)
                return;

            var index = 1;
            while (true)
            {
                stepNo.Value = index;
                if(MarkAsSaved)
                    stepNo.Save();

                //! 最后一个步骤的Next应该为Null，此时跳出循环。
                if (stepNo.Next is StepParam nextParam)
                    stepNo = nextParam;
                else
                    break;

                index++;
            }
        }
        
        /// <summary>
        /// 保存当前配方。
        /// </summary>
        public void Save()
        {
            this.ToList().ForEach(x => x.Save());
        }

        /// <summary>
        /// 取消所有参数的Highlight状态。
        /// </summary>
        public void ResetHighlight()
        {
            this.ToList().ForEach(x => x.ResetHighlight());
        }

        /// <summary>
        /// 计算被赋予访问权限的配方参数。
        /// </summary>
        /// <returns></returns>
        public int GetParamsCountWhoHaveAccessPerm()
        {
            var total = this.ToList().Sum(x => x.GetHighlightedParams().Count);
            return total;
        }

        #endregion

        #region Overrided Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="next"></param>
        private static void LinkSteps(RecipeStep previous, RecipeStep next)
        {
            if (previous != null && next != null)
            {
                previous.SetNextStep(next);
                next.SetPreviousStep(previous);
            }
            else if (previous == null && next != null)
            {
                next.SetPreviousStep(null);
            }
            else
            {
                previous?.SetNextStep(null);
            }
        }

        protected override void SetItem(int index, RecipeStep item)
        {
            throw new NotSupportedException();
        }

        protected override void InsertItem(int index, RecipeStep item)
        {

            if (item != null)
            {
                // 如果Step没有分配Uid，先分配一个。
                if (string.IsNullOrEmpty(item.StepUid))
                {
                    // 尝试20次，如果均重号，则报错。
                    var retry = 0;
                    for (retry = 0; retry < 20; retry++)
                    {
                        var uid = GlobalDefs.GetShortUid();
                        if (this.FirstOrDefault(x => x.StepUid == uid) == null)
                        {
                            item.StepUid = uid;
                            break;
                        }
                    }

                    if (retry == 20)
                        throw new Exception("unable to add recipe step since the exclusive UID can not be assigned.");
                }
            }

            base.InsertItem(index, item);

            if (item != null)
            {
                item.Parent = Parent;
                item.SavedStateChanged += ItemOnSavedStateChanged;

                if (index > 0 && index < Count - 1)
                {
                    LinkSteps(this[index - 1], this[index]);
                    LinkSteps(this[index], this[index + 1]);
                }
                else if (index == 0)
                {
                    if (Count > 1)
                    {
                        LinkSteps(null, this[index]);
                        LinkSteps(this[index], this[index + 1]);
                    }
                    else
                    {
                        LinkSteps(null, this[index]);
                        LinkSteps(this[index], null);
                    }
                }
                else if (index == Count - 1)
                {
                    LinkSteps(this[index - 1], this[index]);
                    LinkSteps(this[index], null);
                }
            }

            UpdateStepIndex();
            this[index].Validate();
        }

        protected override void RemoveItem(int index)
        {
            if (Count > 0)
            {
                if (index >= 0 && index < Count)
                {
                    this[index].SavedStateChanged -= ItemOnSavedStateChanged;
                }
            }

            IsSaved = false;

            // 将前后两个Param链接起来
            var preStep = this[index].Previous;
            var nextStep = this[index].Next;
            LinkSteps(preStep, nextStep);
            preStep?.Validate();
            nextStep?.Validate();

            base.RemoveItem(index);

            UpdateStepIndex();
        }

        protected override void ClearItems()
        {
            if (Count > 0)
            {
                this.ToList().ForEach(x => x.SavedStateChanged -= ItemOnSavedStateChanged);
                IsSaved = false;
            }
            else
            {
                IsSaved = true;
            }

            base.ClearItems();
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
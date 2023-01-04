using System.Collections.Generic;
using ExtendedGrid.Microsoft.Windows.Controls;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel;

namespace RecipeEditorLib.RecipeModel.Params
{
    public interface IParam
    {
        #region Properties

        /// <summary>
        /// 返回当前参数所属列名称。
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 返回ControlName。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 返回父级配方步骤。
        /// </summary>
        RecipeStep Parent { get; }

        /// <summary>
        /// 当前参数在表格中的所属行。
        /// </summary>
        DataGridRow RowOwner { get; set; }

        /// <summary>
        /// 当前参数在表格中的所属列。
        /// </summary>
        DataGridColumn ColumnOwner { get; set; }

        /// <summary>
        /// 返回前序参数。
        /// </summary>
        IParam Previous { get; set; }

        /// <summary>
        /// 返回后序参数。
        /// </summary>
        IParam Next { get; set; }

        /// <summary>
        /// 返回当前参数是否已保存。
        /// </summary>
        bool IsSaved { get; }
        
        /// <summary>
        /// 返回当前参数的值是否和前序参数相等。
        /// </summary>
        bool IsEqualsToPrevious { get; }

        /// <summary>
        /// 返回当前参数是否校验成功。
        /// </summary>
        bool IsValidated { get; }

        /// <summary>
        /// 返回当前参数校验错误信息。
        /// </summary>
        string ValidationError { get; }

        /// <summary>
        /// 返回是否高亮显示当前参数。
        /// </summary>
        bool IsHighlighted { get; }
        
        /// <summary>
        /// 返回是否隐藏参数的值。
        /// </summary>
        bool IsHideValue { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// 将参数链表转换为数组。
        /// </summary>
        /// <returns></returns>
        List<IParam> Flatten();

        /// <summary>
        /// 校验用户输入值。
        /// </summary>
        void Validate();

        /// <summary>
        /// 设置为已保存状态。
        /// </summary>
        void Save();

        /// <summary>
        /// 获取当前参数的数值。
        /// </summary>
        /// <returns></returns>
        object GetValue();
        
        /// <summary>
        /// 高亮显示当前参数。
        /// </summary>
        void Highlight();
        
        /// <summary>
        /// 取消高亮显示。
        /// </summary>
        void ResetHighlight();

        #endregion
    }
}

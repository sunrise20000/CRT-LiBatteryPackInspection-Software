using System.Collections.Generic;
using System.Windows;

namespace RecipeEditorLib.DGExtension.CustomColumn
{
    public class ExpanderColumn : EditorDataGridTemplateColumnBase
    {
        #region Constructors

        public ExpanderColumn()
        {
            IsExpander = true;
            ChildColumns = new List<EditorDataGridTemplateColumnBase>();
        }

        #endregion

        #region Properties

        public static DependencyProperty IsExpanderProperty = DependencyProperty.Register("IsExpanded", typeof(bool),
            typeof(ExpanderColumn), new PropertyMetadata(true));

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpanderProperty);
            set => SetValue(IsExpanderProperty, value);
        }

        /// <summary>
        /// 返回当前组的所有列。
        /// </summary>
        public List<EditorDataGridTemplateColumnBase> ChildColumns { get; }

        #endregion
    }
}

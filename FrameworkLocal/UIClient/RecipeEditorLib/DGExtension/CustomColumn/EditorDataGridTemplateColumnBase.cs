using System;
using System.Windows;
using MECF.Framework.UI.Client.ClientBase;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel.Params;

namespace RecipeEditorLib.DGExtension.CustomColumn
{
    public class EditorDataGridTemplateColumnBase: ExtendedGrid.Microsoft.Windows.Controls.DataGridTemplateColumn
    {
        #region Variables

        private MenuPermissionEnum _permission;

        #endregion

        public EditorDataGridTemplateColumnBase()
        {
            IsReadOnly = false;
            IsEnable = true;
            IsEditable = true;
            _permission = MenuPermissionEnum.MP_NONE;
            Visibility = Visibility.Visible;
            IsExpander = false;
        }
        public string ModuleName { get; set; }

        public string DisplayName { get; set; }

        public string UnitName { get; set; }

        public string ControlName { get; set; }

        public string Description { get; set; }

        public string StringCellTemplate { get; set; }

        public string StringCellEditingTemplate { get; set; }

        public string StringHeaderTemplate { get; set; }

        public bool  IsEnable { get; set; }

        public bool IsEditable { get; set; }

        /// <summary>
        /// 返回当前列是否为列扩展器。
        /// </summary>
        public bool IsExpander { get; protected set; }

        public string Default { get; set; }

        public bool EnableConfig { get; set; }

        public bool EnableTolerance { get; set; }

        /// <summary>
        /// 设置当前列权限。
        /// </summary>
        public MenuPermissionEnum Permission
        {
            get => _permission;
            set
            {
                _permission = value;
                UpdateVisibility();
                SetFeedbackAction();
            }
        }

        public bool IsColumnSelected
        {
            get => (bool)GetValue(IsColumnSelectedProperty);
            set => SetValue(IsColumnSelectedProperty, value);
        }
        /// <summary>
        ///     The DependencyProperty that represents the IsReadOnly property.
        /// </summary>
        public static readonly DependencyProperty IsColumnSelectedProperty =
            DependencyProperty.Register("IsColumnSelected", typeof(bool), typeof(EditorDataGridTemplateColumnBase), new FrameworkPropertyMetadata(false, OnNotifyCellPropertyChanged));

        public Action<Param> Feedback { get; private set; }

        public Func<Param,bool> Check { get; set; }

        #region Methods

        private void SetFeedbackAction()
        {
            switch (Permission)
            {
                default:
                case MenuPermissionEnum.MP_NONE:
                    Feedback = (p) =>
                    {
                        p.IsEnabled = false;
                        p.Visible = Visibility.Hidden;
                    };
                    break;

                case MenuPermissionEnum.MP_READ:
                    Feedback = (p) =>
                    {
                        p.IsEnabled = false;
                        p.Visible = Visibility.Visible;
                    };
                    break;

                case MenuPermissionEnum.MP_READ_WRITE:

                    Feedback = (p) =>
                    {
                        p.IsEnabled = true;
                        p.Visible = Visibility.Visible;
                    };
                    break;
            }

        }

        public void UpdateVisibility()
        {
            if (!IsExpander)
            {
                Visibility = _permission == MenuPermissionEnum.MP_NONE
                    ? Visibility.Collapsed
                    : Visibility.Visible;

                IsReadOnly = _permission == MenuPermissionEnum.MP_NONE || _permission == MenuPermissionEnum.MP_READ;
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }

        #endregion
    }
}

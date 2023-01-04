using System;
using System.Windows;
using System.Windows.Input;
using RecipeEditorLib.RecipeModel.Params;
using Sicentury.Core.Collections;
using SicUI.Models.RecipeEditors;

namespace SicUI
{
    /// <summary>
    /// Interaction logic for RecipeEditorValidationDetailWindow.xaml
    /// </summary>
    public partial class RecipeEditorValidationDetailWindow : Window
    {
        #region Variables

        /// <summary>
        /// 窗口加载时在屏幕中的位置。
        /// </summary>
        private readonly Point? _location = null;

        #endregion
        
        public RecipeEditorValidationDetailWindow(ObservableRangeCollection<RecipeStepValidationInfo> errorInfo, Point? location = null)
        {
            InitializeComponent();
            _location = location;
            listView.ItemsSource = errorInfo;
            
            SourceInitialized += OnSourceInitialized;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            if (!_location.HasValue)
            {
                // 如果未指定显示位置，则默认显示在右下角。
                var desktopWorkingArea = SystemParameters.WorkArea;
                Left = desktopWorkingArea.Right - Width - 20;
                Top = desktopWorkingArea.Bottom - Height - 30;
            }
            else
            {
                Left = _location.Value.X;
                Top = _location.Value.Y;
            }
        }
        
        private void EventSetter_OnHandler(object sender, MouseButtonEventArgs e)
        {
            if (!(DataContext is RecipeEditorViewModel vm))
                return;

            if(listView.SelectedItem is RecipeStepValidationInfo info && info.Param != null)
                vm.FocusToParam(info.Param);
        }
    }
}

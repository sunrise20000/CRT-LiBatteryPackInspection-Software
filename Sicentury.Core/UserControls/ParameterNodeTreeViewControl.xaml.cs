/************************************************************************
*@file FrameworkLocal\UIClient\CenterViews\Core\UserControls\ParameterNodeTreeViewControl.cs
* @author Su Liang
* @Date 2022-08-01
*
* @copyright &copy Sicentury Inc.
*
* @brief Reconstructed to support rich functions.
*
* @details
* *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualBasic.FileIO;
using Sicentury.Core.Dialogs;
using Sicentury.Core.Tree;

namespace Sicentury.Core.UserControls
{
    /// <summary>
    /// Interaction logic for ParameterNodeTreeViewControl.xaml
    /// </summary>
    public partial class ParameterNodeTreeViewControl
    {
        #region Variables

        private bool _clearGroupSelectionOnly = false;

        #endregion

        /// <summary>
        /// 父级文件夹。
        /// </summary>
        private const string PRESET_GROUP_SUB_FOLDER_NAME = "PresetGroups";

        public ParameterNodeTreeViewControl()
        {
            //InitializeComponent();
        }

        protected override void OnInitialized(System.EventArgs e)
        {
            InitializeComponent();
            base.OnInitialized(e);

            LoadPresetGroupFilesList();
        }

        #region Properties

        public static readonly DependencyProperty TreeRootProperty = DependencyProperty.Register(
            nameof(TreeRoot), typeof(TreeNode), typeof(ParameterNodeTreeViewControl),
            new PropertyMetadata(default(TreeNode), (o, p) =>
            {
                if (o is ParameterNodeTreeViewControl control)
                {
                    if (p.NewValue is TreeNode tree)
                    {
                        tree.TerminalNodeSelectionChanged += (sender, b) =>
                        {
                            control.RaiseEvent(new RoutedEventArgs(TerminalNodeSelectionChangedEvent, sender));
                        };
                    }
                }
            }));

        /// <summary>
        /// 设置或返回TreeView数据源。
        /// </summary>
        public TreeNode TreeRoot
        {
            get => (TreeNode)GetValue(TreeRootProperty);
            set => SetValue(TreeRootProperty, value);
        }


        public static readonly DependencyProperty PresetGroupsFolderNameProperty = DependencyProperty.Register(
            "PresetGroupsFolderName", typeof(string), typeof(ParameterNodeTreeViewControl),
            new PropertyMetadata(default(string)));

        /// <summary>
        /// 设置或返回存放预设组信息的子文件夹名称。
        /// </summary>
        public string PresetGroupsFolderName
        {
            get => (string)GetValue(PresetGroupsFolderNameProperty);
            set => SetValue(PresetGroupsFolderNameProperty, value);
        }


        public static readonly DependencyProperty VisibilityExcludeNodesButtonProperty = DependencyProperty.Register(
            nameof(VisibilityExcludeNodesButton), typeof(Visibility), typeof(ParameterNodeTreeViewControl), new PropertyMetadata(System.Windows.Visibility.Hidden));

        /// <summary>
        /// 是否显示节点排除列表编辑按钮。
        /// </summary>
        public Visibility VisibilityExcludeNodesButton
        {
            get => (Visibility)GetValue(VisibilityExcludeNodesButtonProperty);
            set => SetValue(VisibilityExcludeNodesButtonProperty, value);
        }

        //
        public static readonly DependencyProperty VisibilityTopToolsProperty = DependencyProperty.Register(
           nameof(VisibilityTopTools), typeof(Visibility), typeof(ParameterNodeTreeViewControl), new PropertyMetadata(System.Windows.Visibility.Visible));
        /// <summary>
        /// 顶部工具栏显示属性，不需要编辑时;
        /// </summary>
        public Visibility VisibilityTopTools
        {
            get => (Visibility)GetValue(VisibilityTopToolsProperty);
            set => SetValue(VisibilityTopToolsProperty, value);
        }

        #endregion

        #region Routed Events

        // Register a custom routed event using the Bubble routing strategy.
        public static readonly RoutedEvent TerminalNodeSelectionChangedEvent = EventManager.RegisterRoutedEvent(
            name: nameof(TerminalNodeSelectionChanged),
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(RoutedEventHandler),
            ownerType: typeof(ParameterNodeTreeViewControl));

        // Provide CLR accessors for assigning an event handler.
        public event RoutedEventHandler TerminalNodeSelectionChanged
        {
            add => AddHandler(TerminalNodeSelectionChangedEvent, value);
            remove => RemoveHandler(TerminalNodeSelectionChangedEvent, value);
        }


        // Register a custom routed event using the Bubble routing strategy.
        public static readonly RoutedEvent EditExcludeNodesListEvent = EventManager.RegisterRoutedEvent(
            name: nameof(EditExcludeNodesList),
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(RoutedEventHandler),
            ownerType: typeof(ParameterNodeTreeViewControl));

        // Provide CLR accessors for assigning an event handler.
        public event RoutedEventHandler EditExcludeNodesList
        {
            add => AddHandler(EditExcludeNodesListEvent, value);
            remove => RemoveHandler(EditExcludeNodesListEvent, value);
        }

        #endregion

        #region Methods


        /// <summary>
        /// 返回组信息文件名的完整路径。
        /// </summary>
        /// <param name="groupName">组名称</param>
        /// <returns></returns>
        private string GetPresetGroupFullFileName(string groupName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PRESET_GROUP_SUB_FOLDER_NAME,
                PresetGroupsFolderName, groupName + ".json");
        }

        /// <summary>
        /// 返回保存组信息的文件夹名称。
        /// </summary>
        /// <returns></returns>
        private string GetPresetGroupsFolderName()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PRESET_GROUP_SUB_FOLDER_NAME,
                PresetGroupsFolderName);
        }

        /// <summary>
        /// 加载指定文件夹内的所有json文件。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private void LoadPresetGroupFilesList(string selectedGroupName = "")
        {
            if (string.IsNullOrEmpty(PresetGroupsFolderName))
                throw new ArgumentException("the preset group files folder is not set.",
                    paramName: nameof(PresetGroupsFolderName));

            var folderName = GetPresetGroupsFolderName();


            var previousSelection = selectedGroupName;
            // 如果未指定下次待加载的项目，则保存当前的选择
            if (string.IsNullOrEmpty(previousSelection))
                if (cbxPresetList.SelectedItem != null)
                    previousSelection = cbxPresetList.SelectedItem.ToString();

            // 如果文件夹不存在，返回空列表
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
                cbxPresetList.ItemsSource = new List<string>();
            }
            else
            {
                var source =
                    Directory.EnumerateFiles(folderName, "*.json")
                        .Select(Path.GetFileNameWithoutExtension)
                        .ToList();
                cbxPresetList.ItemsSource = source;

                // 重新选择上次的选择
                if (string.IsNullOrEmpty(previousSelection) == false)
                {
                    if (source.FirstOrDefault(x => x == previousSelection) != null)
                        cbxPresetList.SelectedItem = previousSelection;
                }
                else
                {
                    // 如果TreeView还没有加载内容，则不要自动选择
                    if (TreeRoot != null)
                        cbxPresetList.SelectedIndex = 0;
                }
            }

        }

        /// <summary>
        /// 仅清空Group下拉框的选项，而不清除TreeView的选择状态。
        /// <para>通常用于DataGrid中删除条目后，清除Group选项。</para>
        /// </summary>
        public void ClearPresetGroupSelectionOnly()
        {
            _clearGroupSelectionOnly = true;
            cbxPresetList.SelectedIndex = -1;
            _clearGroupSelectionOnly = false;
        }

        #endregion

        #region Events

        /// <summary>
        /// 创建节点排除清单。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void BtnExcludeNode_OnClick(object sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(EditExcludeNodesListEvent, this));
        }


        /// <summary>
        /// 重新加载组列表。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRefreshPresetList_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadPresetGroupFilesList();

            }
            catch (Exception ex)
            {
                var err = $"Unable to refresh the group list, {ex.Message}";
                MessageBox.Show(err, "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 新建组。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnNewGroup_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeRoot.UnselectAll();
                cbxPresetList.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                var err = $"Unable to create new group, {ex.Message}";
                MessageBox.Show(err, "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存组。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSave_OnClick(object sender, RoutedEventArgs e)
        {

            var groupName = "";

            //if (TreeRoot.SelectedTerminalCount > TreeNode.QUERY_LIMIT_COUNT)
            //{
            //    MessageBox.Show($"Too many items are selected, it's limited to {TreeNode.QUERY_LIMIT_COUNT}.",
            //        "Error", MessageBoxButton.OK,
            //        MessageBoxImage.Error);
            //    return;
            //}

            if (cbxPresetList.SelectedIndex == -1)
            {
                var dlg = new PresetGroupSaveDialog("Save Group", cbxPresetList.ItemsSource?.Cast<string>());
                var ret = dlg.ShowDialog();
                if (ret.HasValue && ret.Value == true)
                {
                    groupName = dlg.GroupName;
                }
                else
                {
                    return;
                }
            }
            else
            {
                groupName = cbxPresetList.SelectedItem.ToString();
            }

            try
            {
                // 保存
                TreeNodeSelectionGroupInfo.SaveToJsonFile(GetPresetGroupFullFileName(groupName), TreeRoot);

                LoadPresetGroupFilesList(groupName);
            }
            catch (Exception ex)
            {
                var err = $"Unable to save group {groupName}, {ex.Message}";
                MessageBox.Show(err, "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 另存为组。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSaveAs_OnClick(object sender, RoutedEventArgs e)
        {
            if (cbxPresetList.SelectedIndex == -1)
            {
                MessageBox.Show("No group is selected.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var newName = $"Copy of {cbxPresetList.SelectedValue}";

            var dlg = new PresetGroupSaveDialog("Save As Group", 
                cbxPresetList.ItemsSource?.Cast<string>(),
                newName);

            var ret = dlg.ShowDialog();
            if (ret.HasValue && ret.Value)
            {
                var groupName = cbxPresetList.SelectedItem.ToString();

                try
                {
                    // 保存
                    TreeNodeSelectionGroupInfo.SaveToJsonFile(GetPresetGroupFullFileName(groupName), TreeRoot);

                    // 保存一个空文件
                    TreeNodeSelectionGroupInfo.SaveToJsonFile(GetPresetGroupFullFileName(dlg.GroupName), TreeRoot);

                    LoadPresetGroupFilesList(dlg.GroupName);
                }
                catch (Exception ex)
                {
                    var err = $"Unable to save as group {groupName}, {ex.Message}";
                    MessageBox.Show(err, "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 选择组。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbxPresetList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbxPresetList.SelectedIndex == -1)
            {
                // 
                if (_clearGroupSelectionOnly)
                    return;

                TreeRoot.UnselectAll();
                return;
            }

            var groupName = cbxPresetList.SelectedValue.ToString();

            try
            {
                TreeNodeSelectionGroupInfo.RecoveryFromJsonFile(GetPresetGroupFullFileName(groupName), TreeRoot);
            }
            catch (Exception ex)
            {
                var err = $"Unable to recovery group {groupName}, {ex.Message}";
                MessageBox.Show(err, "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除组。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDelete_OnClick(object sender, RoutedEventArgs e)
        {
            // 如果没选中任何组，忽略操作
            if (cbxPresetList.SelectedIndex == -1)
                return;

            var groupName = cbxPresetList.SelectedItem.ToString();
            if (string.IsNullOrEmpty(groupName))
                return;

            var ret = MessageBox.Show($"Are you sure to delete the group {groupName}", "Warning",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ret == MessageBoxResult.Yes)
            {
                try
                {
                    FileSystem.DeleteFile(GetPresetGroupFullFileName(groupName), UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin);
                    LoadPresetGroupFilesList();
                }
                catch (Exception ex)
                {
                    var err = $"Unable to delete group {groupName}, {ex.Message}";
                    MessageBox.Show(err, "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 折叠所有节点。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCollapseAll_OnClick(object sender, RoutedEventArgs e)
        {
            if (TreeRoot.ChildNodes.Count > 0)
            {
                TreeRoot.CollapseAll();
            }
        }

        /// <summary>
        /// 展开所有节点。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnExpandAll_OnClick(object sender, RoutedEventArgs e)
        {
            if (TreeRoot.ChildNodes.Count > 0)
            {
                TreeRoot.ExpandAll();
            }
        }

        /// <summary>
        /// 应用筛选器。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void BtnApplyFilter_OnClick(object sender, RoutedEventArgs e)
        {
            TreeRoot.SuspendUpdate();
            TreeRoot.ApplyFilter(txtFilterKeyword.Text);
            TreeRoot.ResumeUpdate();
            txtFilterKeyword.SelectAll();
            txtFilterKeyword.Focus();
        }

        /// <summary>
        /// 清除筛选器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClearFilter_OnClick(object sender, RoutedEventArgs e)
        {
            TreeRoot.SuspendUpdate();
            TreeRoot.ClearFilter();
            TreeRoot.ResumeUpdate();
            txtFilterKeyword.Text = "";
            txtFilterKeyword.Focus();

        }

        private void TxtFilterKeyword_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) 
                return;
            
            TreeRoot.ApplyFilter(txtFilterKeyword.Text);
            txtFilterKeyword.SelectAll();
            txtFilterKeyword.Focus();
        }

        private void BtnShowSelectedOnly_OnClick(object sender, RoutedEventArgs e)
        {
            TreeRoot.ShowSelectedOnly();
        }

        private void BtnShowAll_OnClick(object sender, RoutedEventArgs e)
        {
            TreeRoot.ShowAll();
        }

        #endregion

       
    }
}

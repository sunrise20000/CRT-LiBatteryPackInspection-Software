/************************************************************************
*@file FrameworkLocal\UIClient\CenterViews\DataLogs\Core\TreeNode.cs
* @author Su Liang
* @Date 2022-08-01
*
* @copyright &copy Sicentury Inc.
*
* @brief Reconstructed to support rich functions.
*
* @details
*    All functions to operate the TreeView are integrated into one class, which
*    also makes it meet the MVVM coding pattern.
* *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Sicentury.Core.EventArgs;

namespace Sicentury.Core.Tree
{
    public class TreeNode : BindableBase
    {
        #region Variables

        /// <summary>
        /// 当末端的节点选择属性发生变化时触发此事件。
        /// </summary>
        public event EventHandler<TreeNodeSelectionChangedEventArgs> TerminalNodeSelectionChanged;

        /// <summary>
        /// 当子节点中有元素发生变化时触发事件。
        /// </summary>
        public event EventHandler<NotifyCollectionChangedEventArgs> ChildNodesChanged;

        private bool? _isSelected = false;
        private double _averageValue;
        private double _minValue;
        private double _maxValue;
        private bool _isExpanded;
        private bool _isMatch = true;
        private string _filterKeyword;
        private Visibility _visibility = Visibility.Visible;
        private int _totalTerminalCount;
        private int _selectedTerminalCount;

        /// <summary>
        /// 挂起更新操作时的变量锁，防止多线程问题。
        /// </summary>
        private readonly object _suspendUpdateLocker;

        /// <summary>
        /// 时否挂起更新操作。
        /// </summary>
        private bool _isUpdateSuspended;


        #endregion

        #region Constructors

        public TreeNode(string name)
        {
            _suspendUpdateLocker = new object();
            _isUpdateSuspended = false;

            Name = name;
            //RawData = new List<ParameterNodePoint>();
            MaxTerminalSelectionAllowed = -1;
            ChildNodes = new TreeNodeCollection(this);
            ChildNodes.TerminalNodeSelectionChanged += OnTerminalNodeSelectionChanged;
            ChildNodes.CollectionChanged += ChildNodesOnCollectionChanged;
        }

        #endregion

        #region Properties

        /// <summary>
        /// 当前树中被选中的终端节点的数量。
        /// </summary>
        public int SelectedTerminalCount
        {
            get => _selectedTerminalCount;
            private set => Set(ref _selectedTerminalCount, value);
        }

        /// <summary>
        /// 当前树中终端节点的总数量。
        /// </summary>
        public int TotalTerminalCount
        {
            get => _totalTerminalCount;
            private set => Set(ref _totalTerminalCount, value);
        }

        /// <summary>
        /// 返回允许的最大终端节点选中数量。
        /// <para>-1表示可选中所有终端节点。</para>
        /// </summary>
        public int MaxTerminalSelectionAllowed { get; set; }

        public bool? IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected)
                {
                    OnPropertyChanged();
                    return;
                }

                var snapshot = _isSelected;
                Set(ref _isSelected, value);

                // 仅当该节点未最末端节点时触发事件。
                var cancelEventArgs = new TreeNodeSelectionChangedEventArgs(this, snapshot, value);
                RaiseTerminalNodeSelectionChangedEvent(cancelEventArgs);
                if (cancelEventArgs.Cancel)
                {
                    _isSelected = snapshot;
                    OnPropertyChanged();
                }

                // 处理子节点的选择状态
                UpdateChildrenSelection();

                // 处理我的状态。
                UpdateSelectionState(this);

                // 处理父节点的选择状态
                UpdateParentSelection();
            }
        }

        public string Name { get; }

        /// <summary>
        /// 返回包含完整路劲的节点名称。
        /// </summary>
        public string FullName => ToString();

        public double AverageValue
        {
            get => _averageValue;
            private set => Set(ref _averageValue, value);
        }
        
        public double MinValue
        {
            get => _minValue;
            private set => Set(ref _minValue, value);
        }
        
        public double MaxValue
        {
            get => _maxValue;
            private set => Set(ref _maxValue, value);
        }
        
        /// <summary>
        /// 是否展开节点。
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => Set(ref _isExpanded, value);
        }
        
        /// <summary>
        /// 是否包含Filter指定的关键字。
        /// </summary>
        public bool IsMatch
        {
            get => _isMatch;
            set => Set(ref _isMatch, value);
        }

        /// <summary>
        /// 设置或返回节点是否可见。
        /// </summary>
        public Visibility Visibility
        {
            get => _visibility;
            set
            {
                Set(ref _visibility, value);

                if (IsTerminal)
                    UpdateParentVisibility(ParentNode);
            }
        }

        public TreeNode ParentNode { get; set; }

        //public List<ParameterNodePoint> RawData { get; }

        public Visibility IsVisibilityParentNode { get; set; }

        public TreeNodeCollection ChildNodes { get; set; }
        
        /// <summary>
        /// 筛选器的关键字。
        /// </summary>
        public string FilterKeyWord
        {
            get => _filterKeyword;
            set => Set(ref _filterKeyword, value);

        }

        /// <summary>
        /// 返回我是否为最末端节点。
        /// </summary>
        public bool IsTerminal => (ChildNodes == null || ChildNodes.Count == 0);

        /// <summary>
        /// 返回我是否属于一级节点。
        /// </summary>
        public bool IsTopLevel => ParentNode == null;

        /// <summary>
        /// 返回我是否属于二级节点。
        /// </summary>
        public bool IsSecondLevel => ParentNode != null && ParentNode.ParentNode == null;

        /// <summary>
        /// 返回是否有终端节点被选中。
        /// </summary>
        public bool HasTerminalSelected => Flatten(true).FirstOrDefault(x => x.IsSelected == true) != null;

        #endregion

        #region Methods
        
        /// <summary>
        /// 如果是末端节点，触发事件。
        /// </summary>
        private void RaiseTerminalNodeSelectionChangedEvent(TreeNodeSelectionChangedEventArgs e)
        {
            // 仅当该节点未最末端节点时触发事件。
            if (IsTerminal)
                TerminalNodeSelectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 处理子节点和父节点的选择状态。
        /// </summary>
        private void UpdateChildrenSelection()
        {
            if (_isSelected.HasValue == false)
                return;

            // 刷新子节点的选择状态
            if (IsTerminal) 
                return;

            //! 遍历子节点时，本节点的_isSelected属性可能被改写。
            // 例如：某个终端节点的Selected改变时，会反向设置父节点的Selected属性；则接下来所有子节点赋值Selected属性时会使用新值。
            var fixedIsSelected = _isSelected;
            Flatten(true).ToList()
                .ForEach(x =>
                {
                    if(x.IsMatch)
                        x.SetSelectionProperty(fixedIsSelected);
                });
        }
        
        /// <summary>
        /// 刷新父节点的选择状态。
        /// </summary>
        public void UpdateParentSelection()
        {
            if (ParentNode == null)
                return;

            // 递归更新IsSelected属性
            UpdateSelectionState(ParentNode);
            ParentNode.UpdateParentSelection();
        }

        /// <summary>
        /// 刷新指定节点的IsSelected属性。
        /// </summary>
        /// <param name="node"></param>
        private static void UpdateSelectionState(TreeNode node)
        {
            if (node.IsTerminal)
                return;

            // 刷新父节点的选择状态
            var selectionGroup = node.Flatten(true).GroupBy(x => x.IsSelected);
            var enumerable = selectionGroup.ToList();
            if (!enumerable.Any())
                return;

            if (enumerable.Count() > 1)
                node.SetSelectionProperty(null);
            else if (enumerable.Count() == 1)
            {
                var ss = enumerable[0].Key;
                if (ss.HasValue)
                    node.SetSelectionProperty(ss.Value);
            }
            else
            {
                // 非法情况
                Debugger.Break();
            }
        }

        /// <summary>
        /// 刷新父级节点的显示属性。
        /// </summary>
        private static void UpdateParentVisibility(TreeNode node)
        {
            while (true)
            {
                // 刷新父节点的选择状态
                var selectionGroup = node.Flatten(true).GroupBy(x => x.Visibility);
                var enumerable = selectionGroup.ToList();
                if (!enumerable.Any()) 
                    return;

                if (enumerable.Count() > 1)
                    // 如果终端节点存在多种情况，则一定显示本节点。
                    node.Visibility = Visibility.Visible;
                else if (enumerable.Count == 1)
                {
                    // 如果存在一种情况，则判断是全部为显示还是全部为折叠。

                    var ss = enumerable[0].Key;
                    if (ss == Visibility.Visible)
                        node.Visibility = Visibility.Visible;
                    else
                        node.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // 非法情况
                    Debugger.Break();
                }


                // 如果本节点已经是顶层节点，则退出
                if (node.ParentNode == null)
                    break;

                node = node.ParentNode;
            }
        }

        /// <summary>
        /// 当终端节点被选则或取消选择时触发此事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTerminalNodeSelectionChanged(object sender, TreeNodeSelectionChangedEventArgs e)
        {
            //! 在树的根部检查终端节点选中数是否超过最大限制
            if (IsTopLevel)
            {
                if (MaxTerminalSelectionAllowed > -1)
                    if (GetSelectedTerminalCount() > MaxTerminalSelectionAllowed)
                    {
                        e.Cancel = true;
                        return;
                    }

                UpdateTerminalNodeCountInfo();
            }

            TerminalNodeSelectionChanged?.Invoke(sender, e);
        }

        /// <summary>
        /// 当ChildNodes元素发生变化时触发此事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChildNodesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            /*
             * 如果当前节点是顶层节点，则统计终端节点总数，否则继续向父级节点传递事件。
             */

            if (IsTopLevel)
                UpdateTerminalNodeCountInfo();

            ChildNodesChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 更新终端节点数量信息。
        /// </summary>
        private void UpdateTerminalNodeCountInfo()
        {
            lock (_suspendUpdateLocker)
            {
                if (_isUpdateSuspended)
                    return;

                SelectedTerminalCount = GetSelectedTerminalCount();
                TotalTerminalCount = ChildNodes.Sum(x => x.Flatten(true).ToList().Count);
            }
        }

        /// <summary>
        /// 该方法用于Tree内部设置<see cref="IsSelected"/>属性的状态。
        /// </summary>
        /// <param name="isSelected"></param>
        private void SetSelectionProperty(bool? isSelected)
        {
            var snapshot = _isSelected;
            _isSelected = isSelected;

            var args = new TreeNodeSelectionChangedEventArgs(this, snapshot, _isSelected);
            RaiseTerminalNodeSelectionChangedEvent(args);
            if (args.Cancel)
                _isSelected = snapshot;

            OnPropertyChanged(nameof(IsSelected));

            if (IsTerminal)
                UpdateParentSelection();
        }

        /// <summary>
        /// 查找指定完整路径名称的末端节点。
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        public TreeNode FindTerminalByFullName(string fullName)
        {
            return Flatten(true).FirstOrDefault(x => x.FullName == fullName);
        }

        /// <summary>
        /// 获取指定层级的节点。
        /// </summary>
        /// <param name="level">0-based层级，0表示Tree的根节点。</param>
        /// <returns></returns>
        public TreeNode GetNodeByLevel(int level)
        {
            var list = new List<TreeNode>();
            list.Add(this);
            while (true)
            {
                if (ParentNode != null)
                    list.Add(this.ParentNode);
                else
                    break;
            }

            if (level >= list.Count)
                return null;

            return list[level];
        }

        /// <summary>
        /// 获取被选中的终端节点的数量。
        /// </summary>
        /// <returns></returns>
        public int GetSelectedTerminalCount()
        {
            return Flatten(true).Where(x => x.IsSelected == true).ToList().Count;
        }

        /// <summary>
        /// 将数中的节点转换为节点数组。
        /// </summary>
        /// <param name="terminalOnly">是否仅枚举最末端节点。</param>
        /// <para>更新子节点的Selected属性时，需要同时更新中间节点的属性；更新父节点时，只评估终端节点的Selected属性。</para>
        /// <returns></returns>
        public List<TreeNode> Flatten(bool terminalOnly)
        {
            if (ChildNodes == null || ChildNodes.Count <= 0)
                return new List<TreeNode>(new[] { this });
          
            var lst = ChildNodes.SelectMany(x => x.Flatten(terminalOnly)).ToList();
            if(!terminalOnly)
                lst.Add(this);

            return lst;
        }

        /// <summary>
        /// 清除统计数据。
        /// </summary>
        public void ClearStatistic()
        {
            MinValue = double.NaN;
            MaxValue = double.NaN;
            AverageValue = double.NaN;
        }

        /// <summary>
        /// 设置统计数据。
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="average"></param>
        public void SetStatistic(double min, double max, double average)
        {
            MinValue = min;
            MaxValue = max;
            AverageValue = average;
        }

        /// <summary>
        /// 取消所有节点选择状态。
        /// </summary>
        public void UnselectAll()
        {
            SuspendUpdate();
            Flatten(true).ForEach(x => x.IsSelected = false);
            ResumeUpdate();
        }

        /// <summary>
        /// 选择所有节点。
        /// </summary>
        public void SelectAll()
        {
            SuspendUpdate();
            Flatten(true).ForEach(x => x.IsSelected = true);
            ResumeUpdate();
        }

        /// <summary>
        /// 折叠所有节点。
        /// </summary>
        public void CollapseAll()
        {
            Flatten(false).ForEach(x => x.IsExpanded = false);
        }

        /// <summary>
        /// 展开所有节点。
        /// </summary>
        public void ExpandAll()
        {
            Flatten(false).ForEach(x => x.IsExpanded = true);
        }

        /// <summary>
        /// 显示所有节点。
        /// </summary>
        public void ShowAll()
        {
            Flatten(true).ForEach(x => x.Visibility = Visibility.Visible);
        }

        /// <summary>
        /// 显示选中的项目。
        /// </summary>
        public void ShowSelectedOnly()
        {
            Flatten(true).Where(x => x.IsSelected == false).ToList()
                .ForEach(x => x.Visibility = Visibility.Collapsed);
        }

        /// <summary>
        /// 应用筛选器。
        /// </summary>
        /// <param name="keyWord"></param>
        public void ApplyFilter(string keyWord)
        {
            //! 自顶向下遍历，仅需关心子节点状态。

            // 如果父节点匹配，则我强制匹配。
            IsMatch = (ParentNode != null && ParentNode.IsMatch) || Name.ToLower().Contains(keyWord.ToLower());
            FilterKeyWord = keyWord;

            // 如果是终端节点，则仅检查是否匹配关键字，然后退出
            if (IsTerminal)
                return;
            
            // 否则检查我的名称是否匹配关键字，并检查我的子节点中是否有匹配关键字的节点，如果有，则我也要显示。
            foreach (var node in ChildNodes)
            {
                node.ApplyFilter(keyWord);
            }

            var isMatchedNodes = Flatten(false).Where(x => x.IsMatch).ToList();
            if (isMatchedNodes.Any())
                IsMatch = true;
        }

        /// <summary>
        /// 清除筛选器。
        /// </summary>
        public void ClearFilter()
        {
            IsMatch = true;
            FilterKeyWord = "";

            if (IsTerminal)
                return;

            foreach (var node in ChildNodes)
            {
                node.ClearFilter();
            }
        }
        
        /// <summary>
        /// 挂起TreeView视图更新。
        /// <para>通常在后端对节点Selected属性操作时，挂起视图更新，以提高性能。</para>
        /// </summary>
        public void SuspendUpdate()
        {
            lock (_suspendUpdateLocker)
            {
                _isUpdateSuspended = true;
            }
        }

        /// <summary>
        /// 恢复TreeView视图更新。
        /// <para>通常在后端对节点Selected属性操作时，挂起视图更新，以提高性能。</para>
        /// <para>恢复视图更新后，主动更新节点数量统计值。</para>
        /// </summary>
        public void ResumeUpdate()
        {
            lock (_suspendUpdateLocker)
            {
                _isUpdateSuspended = false;
            }

            UpdateTerminalNodeCountInfo();
        }

        public override string ToString()
        {
            // 完整路径中删除根节点的名称，以确保FullName匹配系统中的定义。
            return (ParentNode?.ParentNode == null) ? Name : $"{ParentNode}.{Name}";
        }

        #endregion
    }
}

using MECF.Framework.UI.Client.ClientBase;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MECF.Framework.UI.Client.CenterViews.DataLogs.ProcessHistory;
using Sicentury.Core.Collections;
using Sicentury.Core.Tree;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.DataHistory
{
    public class SelectUserDefineViewModel : UiViewModelBase
    {
        #region 
        private const int MAX_PARAMETERS = 50;
        
        public ObservableRangeCollection<TreeNode> ParameterNodes { get; }
        public ObservableRangeCollection<string> SelectedParameters { get; set; }
        public ObservableRangeCollection<TreeNode> ParameterNodes1 = new ObservableRangeCollection<TreeNode>();
        object _lockSelection = new object();

        public ObservableRangeCollection<string> SourcePM { get; set; }
        public string SelectedValuePM { get; set; }
        public string SelectedItem { get; set; }

        #endregion

        #region Popup window
        public SelectUserDefineViewModel()
        {
            SelectedParameters = new ObservableRangeCollection<string>();
            ParameterNodes = new ObservableRangeCollection<TreeNode>(ProcessHistoryProvider.Instance.GetParameters());
            SourcePM = new ObservableRangeCollection<string>(new[] { "PMA", "PMB" });
        }

        protected override void OnViewLoaded(object _view)
        {
            base.OnViewLoaded(_view);
            this.view = (SelectUserDefineView)_view;
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            ParameterNodes.Clear();
            ParameterNodes.AddRange(ProcessHistoryProvider.Instance.GetParameters());
            SetParameterNode(ParameterNodes, true);
        }

        private void SetParameterNode(ObservableCollection<TreeNode> nodes, bool isCheck)
        {
            foreach (TreeNode n in nodes)
            {
                if (isCheck)
                {
                    if (SelectedParameters.Any(x => x == n.Name))
                        n.IsSelected = true;
                }
                else
                    n.IsSelected = false;
                SetParameterNode(n.ChildNodes, isCheck);
            }
        }

        private SelectUserDefineView view;

        public void Preset()
        {

        }

        public void UnSelect()
        {

        }

        //public void ParameterCheck(TreeNode node)
        //{
        //    bool result = RefreshTreeStatusToChild(node);
        //    if (!result)
        //        node.Selected = !node.Selected;
        //    else
        //        RefreshTreeStatusToParent(node);
        //}

        ///// <summary>
        ///// Refresh tree node status from current to children, and add data to SelectedData
        ///// </summary>
        //private bool RefreshTreeStatusToChild(TreeNode node)
        //{
        //    if (node.ChildNodes.Count > 0)
        //    {
        //        for (int i = 0; i < node.ChildNodes.Count; i++)
        //        {

        //            TreeNode n = node.ChildNodes[i];
        //            n.Selected = node.Selected;
        //            //for (int j = 0; j < SelectedParameters.Count; j++)
        //            //{
        //            //    if ((SelectedParameters[j].Contains("PMA.") && node.Name.Contains("PMB")) || (SelectedParameters[j].Contains("PMB.") && node.Name.Contains("PMA")))
        //            //    {
        //            //        n.Selected = !node.Selected;
        //            //        DialogBox.ShowWarning("PMA,PMB you can only choose one or the other");
        //            //        return false;
        //            //    }
        //            //}
        //            if (!RefreshTreeStatusToChild(n))
        //            {
        //                //uncheck left node
        //                for (int j = i; j < node.ChildNodes.Count; j++)
        //                {
        //                    node.ChildNodes[j].Selected = !node.Selected;
        //                }
        //                node.Selected = !node.Selected;
        //                return false;
        //            }
        //        }
        //    }
        //    else //leaf node
        //    {
        //        lock (_lockSelection)
        //        {
        //            bool flag = SelectedParameters.Contains(node.Name);
        //            if (node.Selected && !flag)
        //            {
        //                if (SelectedParameters.Count < MAX_PARAMETERS)
        //                {
        //                    SelectedParameters.Add(node.Name);
        //                }
        //                else
        //                {
        //                    DialogBox.ShowWarning($"The max number of parameters is {MAX_PARAMETERS}.");
        //                    return false;
        //                }
        //            }
        //            else if (!node.Selected && flag)
        //            {
        //                SelectedParameters.Remove(node.Name);
        //            }
        //        }

        //    }
        //    return true;
        //}

        ///// <summary>
        ///// Refresh tree node status from current to parent
        ///// </summary>
        ///// <param name="node"></param>
        ///// <returns></returns>
        //private void RefreshTreeStatusToParent(TreeNode node)
        //{
        //    if (node.ParentNode != null)
        //    {
        //        if (node.Selected)
        //        {
        //            bool flag = true;
        //            for (int i = 0; i < node.ParentNode.ChildNodes.Count; i++)
        //            {
        //                if (!node.ParentNode.ChildNodes[i].Selected)
        //                {
        //                    flag = false;  //as least one child is unselected
        //                    break;
        //                }
        //            }
        //            if (flag)
        //                node.ParentNode.Selected = true;
        //        }
        //        else
        //        {
        //            node.ParentNode.Selected = false;
        //        }
        //        RefreshTreeStatusToParent(node.ParentNode);
        //    }
        //}

        public void OnTreeSelectedChanged(object obj)
        {

        }

        public void OK()
        {

            this.TryClose(true);

        }

        public void DeleteAll()
        {
            SelectedParameters.Clear();
            SetParameterNode(ParameterNodes, false);
        }

        public void Delete()
        {
            if (view._selectListBox.SelectedItems != null)
            {
                List<object> temp = new List<object>();
                foreach (var item in view._selectListBox.SelectedItems)
                {
                    temp.Add(item.ToString());
                }
                foreach (var item in temp)
                {
                    SelectedParameters.Remove(item.ToString());
                }
                SetParameterNode(ParameterNodes, false);
                SetParameterNode(ParameterNodes, true);
            }
        }

        public void Cancel()
        {
            this.TryClose(false);
        }
        #endregion
    }
}

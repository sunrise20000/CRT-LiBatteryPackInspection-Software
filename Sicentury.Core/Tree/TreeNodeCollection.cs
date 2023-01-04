using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Sicentury.Core.Collections;
using Sicentury.Core.EventArgs;

namespace Sicentury.Core.Tree
{
    public class TreeNodeCollection : ObservableRangeCollection<TreeNode>
    {
        #region Variables

        public event EventHandler<TreeNodeSelectionChangedEventArgs> TerminalNodeSelectionChanged;

        #endregion

        #region Constructors

        public TreeNodeCollection(TreeNode parent, IEnumerable<TreeNode> collection) : base(collection)
        {
            Parent = parent;
            if (collection != null)
            {
                var list = collection.ToList();
                foreach (var subNode in list)
                {
                    subNode.ParentNode = Parent;
                    subNode.TerminalNodeSelectionChanged += SubNodeOnTerminalNodeSelectionChanged;
                    subNode.ChildNodesChanged += SubNodeOnChildNodesChanged;
                }
            }
        }
        
        public TreeNodeCollection(TreeNode parent) : this(parent, new TreeNode[0])
        {

        }

        #endregion

        #region Properties

     

        /// <summary>
        /// 父节点。
        /// </summary>
        public TreeNode Parent { get; }

        #endregion

        #region Methods

        public override void AddRange(IEnumerable<TreeNode> collection)
        {
            var lst = collection?.ToList();
            lst?.ForEach(subNode =>
            {
                subNode.ParentNode = Parent;
                subNode.TerminalNodeSelectionChanged += SubNodeOnTerminalNodeSelectionChanged;
                subNode.ChildNodesChanged += SubNodeOnChildNodesChanged;
            });

            base.AddRange(lst);
        }

        protected override void InsertItem(int index, TreeNode subNode)
        {
            if (subNode == null)
                return;

            subNode.ParentNode = Parent;
            subNode.TerminalNodeSelectionChanged += SubNodeOnTerminalNodeSelectionChanged;
            subNode.ChildNodesChanged += SubNodeOnChildNodesChanged;

            base.InsertItem(index, subNode);
        }

        /// <summary>
        /// 当子节点触发TerminalNodeSelectionChanged事件时调用该方法，通知父节点。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubNodeOnTerminalNodeSelectionChanged(object sender, TreeNodeSelectionChangedEventArgs e)
        {
            TerminalNodeSelectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 当某个节点的ChildNodes列表发生变化时调用该方法，通知父节点。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubNodeOnChildNodesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanged(e);
        }

        #endregion
    }
}

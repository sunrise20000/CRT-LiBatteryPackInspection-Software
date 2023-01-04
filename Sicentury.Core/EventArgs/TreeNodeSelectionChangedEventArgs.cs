using System.ComponentModel;
using Sicentury.Core.Tree;

namespace Sicentury.Core.EventArgs
{
    public class TreeNodeSelectionChangedEventArgs : CancelEventArgs
    {
        #region Constructors

        public TreeNodeSelectionChangedEventArgs(TreeNode source, bool? oldValue, bool? newValue)
        {
            Source = source;
            OldValue = oldValue;
            NewValue = newValue;
        }

        #endregion

        #region Properties

        /// <summary>
        /// 触发事件的<see cref="TreeNode"/>对象。
        /// </summary>
        public TreeNode Source { get; }

        /// <summary>
        /// 返回IsSelected旧值。
        /// </summary>
        public bool? OldValue { get; }
        
        /// <summary>
        /// 返回IsSelected新值。
        /// </summary>
        public bool? NewValue { get; }

        #endregion
    }
}

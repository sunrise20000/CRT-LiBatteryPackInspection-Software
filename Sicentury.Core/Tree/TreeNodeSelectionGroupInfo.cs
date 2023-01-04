using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Sicentury.Core.Tree
{
    public class TreeNodeSelectionGroupInfo
    {
        #region Constructors

        public TreeNodeSelectionGroupInfo()
        {
            SelectedTerminalNodes = new List<string>();
        }

        public TreeNodeSelectionGroupInfo(IEnumerable<string> collection)
        {
            SelectedTerminalNodes = new List<string>(collection);
        }

        #endregion

        #region Properties


        public List<string> SelectedTerminalNodes
        {
            get;
        }

        #endregion

        #region Methods

        /// <summary>
        /// 从指定的文件恢复节点选择
        /// </summary>
        /// <param name="fileName"></param>
        internal static void RecoveryFromJsonFile(string fileName, TreeNode treeRoot)
        {
            if (treeRoot == null)
                throw new ArgumentNullException(nameof(treeRoot));

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("the file name is not specified.", nameof(fileName));

            var json = File.ReadAllText(fileName);
            var info = JsonConvert.DeserializeObject<TreeNodeSelectionGroupInfo>(json);

            // 如果没有正确恢复Preset Group文件，则提示错误
            if (info.SelectedTerminalNodes == null)
                throw new JsonException($"the file of preset group might be incorrect.");

            treeRoot.UnselectAll();

            treeRoot.SuspendUpdate();
            var flattenTree = treeRoot.Flatten(true);
            info.SelectedTerminalNodes.ForEach(x =>
            {
                var matched = flattenTree.FirstOrDefault(f => f.ToString() == x.ToString());
                if (matched != null)
                    matched.IsSelected = true;
            });
            treeRoot.ResumeUpdate();
        }


        internal static void SaveToJsonFile(string fileName, TreeNode treeRoot)
        {
            if (treeRoot == null)
                throw new ArgumentNullException(nameof(treeRoot));

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("the file name is not specified.", nameof(fileName));

            var selectedNodes =
                treeRoot.Flatten(true)
                    .Where(x => x.IsSelected == true)
                    .Select(x=>x.ToString());
            var info = new TreeNodeSelectionGroupInfo(selectedNodes);
            var json = JsonConvert.SerializeObject(info, Formatting.Indented);
            File.WriteAllText(fileName, json);
        }

        #endregion
    }
}

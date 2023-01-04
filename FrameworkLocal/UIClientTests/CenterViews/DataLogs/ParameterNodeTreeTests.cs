using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using MECF.Framework.UI.Client.CenterViews.Core;
using MECF.Framework.UI.Client.CenterViews.DataLogs.Core;
using Sicentury.Core.Tree;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.Tests
{
    [TestClass()]
    public class ParameterNodeTreeTests
    {
        private IEnumerable<TreeNode> CreateParamNodeList(TreeNode parentNode, int count)
        {
            var ll = Enumerable.Range(0, count);
            return ll.Select(x => new TreeNode($"{parentNode.Name}.{x}")
            {
                ParentNode = parentNode

            }).ToList();
        }

        [TestMethod()]
        public void FlattenNodesTest()
        {
            var tree = new TreeNode("Root");

            var node1 = new TreeNode("node1");
            node1.ChildNodes = new TreeNodeCollection(node1, CreateParamNodeList(node1, 10));


            var node2 = new TreeNode("node2");
            node2.ChildNodes = new TreeNodeCollection(node2, CreateParamNodeList(node2, 5));

            tree.ChildNodes.Add(node1);
            tree.ChildNodes.Add(node2);

            var list = tree.Flatten(false);

            Assert.AreEqual(15, list.Count);
        }
    }
}
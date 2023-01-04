using System;
using System.Collections.Generic;
using System.Linq;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Core;
using OpenSEMI.ClientBase.ServiceProvider;
using Sicentury.Core.Tree;

namespace MECF.Framework.UI.Client.CenterViews.Operations.RealTime
{
    public class RealtimeProvider : IProvider
    {
        /// <summary>
        /// 使用字符串队列创建树结构的分支。
        /// </summary>
        /// <param name="branchString"></param>
        /// <param name="node"></param>
        private static TreeNode CreateTreeBranch(Queue<string> branchString, TreeNode node)
        {
            var rootNode = node;

            while (true)
            {
                if (branchString.Count <= 0) 
                    return rootNode;

                var name = branchString.Dequeue();

                if (node == null)
                {
                    // 根节点
                    node = new TreeNode(name);
                    rootNode = node;
                }
                else
                {
                    if(node.Name == name)
                        continue;

                    // 向ChildNode插入
                    var subNode = node.ChildNodes.FirstOrDefault(x => x.Name == name);
                    if (subNode == null)
                    {
                        subNode = new TreeNode(name);
                        node.ChildNodes.Add(subNode);
                    }

                    node = subNode;
                }
            }
        }

        public void Create()
        {

        }

        public List<string> GetUserDefineParameters()
        {
            var typedContents = ((string)QueryDataClient.Instance.Service.GetTypedConfigContent("UserDefine", ""));
            var dataList = new List<string>();
            if (typedContents != null)
            {
                var contentList = typedContents.Split(',').ToList();
                contentList.ForEach(x => { if (!string.IsNullOrEmpty(x)) dataList.Add($"{x}"); });
            }

            return dataList;
        }
        
        public List<TreeNode> GetParameters()
        {
            var root = new List<TreeNode>();

            try
            {
                var dataList = (List<string>)QueryDataClient.Instance.Service.GetConfig("System.NumericDataList");
                //var typedContents = QueryDataClient.Instance.Service.GetTypedConfigContent("UserDefine", "");
                //if (string.IsNullOrEmpty(typedContents))
                //{
                //    dataList.Add($"UserDefine");
                //}
                //else
                //{
                //    var contentList = typedContents.Split(',').ToList();
                //    contentList.ForEach(x =>
                //    {
                //        if (!string.IsNullOrEmpty(x)) dataList.Add($"UserDefine.{x}");
                //    });
                //}

                dataList.Sort();
                
                foreach (var dataName in dataList)
                {
                    var nodeName = new Queue<string>(dataName.Split('.'));
                    if (nodeName.Count <= 0) 
                        continue;
                    
                    // 开始创建Tree分支
                    var rootNode = root.FirstOrDefault(x => x.Name == nodeName.Peek());
                    var node = CreateTreeBranch(nodeName, rootNode);
                    if(rootNode == null)
                        root.Add(node);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
                
            return root;
        }
    }
}

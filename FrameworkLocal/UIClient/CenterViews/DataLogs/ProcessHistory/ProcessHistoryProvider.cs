using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using OpenSEMI.ClientBase.ServiceProvider;
using Sicentury.Core.Tree;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.ProcessHistory
{
    public class ProcessHistoryProvider : IProvider
    {
        private static ProcessHistoryProvider _Instance = null;
        public static ProcessHistoryProvider Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new ProcessHistoryProvider();

                return _Instance;
            }
        }

        public void Create()
        {

        }

        public ObservableCollection<RecipeItem> SearchRecipe()
        {
            var result = new ObservableCollection<RecipeItem>();

            for (var i = 0; i < 5; i++)
            {
                var r = new RecipeItem() { Chamber = "c " + i.ToString(), Recipe = "recipe " + i.ToString(), Selected = false, Status = "s " + i.ToString(), EndTime = "", StartTime = "" };
                result.Add(r);
            }

            return result;
        }

        public ObservableCollection<TreeNode> GetParameters( )
        {
            try
            {
                var dataList = (List<string>)QueryDataClient.Instance.Service.GetConfig("System.NumericDataList");
                dataList.Sort();

                var rootNode = new ObservableCollection<TreeNode>();
                var indexer = new Dictionary<string, TreeNode>();
                foreach (var dataName in dataList)
                {
 
                    if (!dataName.StartsWith("PM1.") && !dataName.StartsWith("PM2.")
                                                     && !dataName.StartsWith("PM3.") && !dataName.StartsWith("PM4."))
                    {
                        continue;
                    }

                    var nodeName = dataName.Split('.');
                    TreeNode parentNode = null;
                    var pathName = "";
                    for (var i = 0; i < nodeName.Length; i++)
                    {
                        pathName = (i == 0) ? nodeName[i] : (pathName + "." + nodeName[i]);
                        if (!indexer.ContainsKey(pathName))
                        {
                            indexer[pathName] = new TreeNode(pathName);

                            if (parentNode == null)
                                rootNode.Add(indexer[pathName]);
                            else
                            {
                                parentNode.ChildNodes.Add(indexer[pathName]);
                            }
                        }

                        parentNode = indexer[pathName];
                    }
                }

                return rootNode;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return null;
        }
    }
}

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using Caliburn.Micro.Core;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Sequence
{
    public class FileNode : PropertyChangedBase
    {
        public FileNode()
        {
            this.Files = new ObservableCollection<FileNode>();
            this.IsFile = false;
        }

        private string _name = string.Empty;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyOfPropertyChange();
            }
        }

        public string FullPath { get; set; }

        public FileNode Parent { get; set; }

        public ObservableCollection<FileNode> Files { get; set; }

        public bool IsFile { get; set; }

        public string PrefixPath { get; set; }

        public bool IsSelected { get; set; }

        public bool IsExpanded { get; set; }

        public override string ToString()
        {
            if (Parent != null)
                return $"{Parent}{Name}{(IsFile ? "" : "\\")}";
            else
                return $"{Name}{(IsFile ? "" : "\\")}"; ;
        }
    }

    public class RecipeSequenceTreeBuilder
    {
        public static ObservableCollection<FileNode> GetFiles(string prefixPath, List<string> filenames)
        {
            var folders = new ObservableCollection<FileNode>();
            var root = new FileNode() { Name = "Files", IsFile = false, FullPath="", PrefixPath = prefixPath};
            folders.Add(root);

            foreach(var filename in filenames)
            {
                var filesplits = filename.Split('\\');

                var parent = root;
                if (filesplits.Length>1)
                {
                    for(var index =0;index< filesplits.Length-1; index++)
                    {
                        var found = false;
                        for(var j = 0; j < parent.Files.Count; j++)
                        {
                            if(parent.Files[j].Name == filesplits[index] && !parent.Files[j].IsFile)
                            {
                                found = true;
                                parent = parent.Files[j];
                                break;
                            }
                        }
                        if (!found)
                        {
                            var folder = new FileNode() { Name = filesplits[index], IsFile = false, PrefixPath = prefixPath };
                            folder.FullPath = (parent.FullPath == string.Empty ? filesplits[index] : parent.FullPath + "\\" + filesplits[index]);
                            folder.Parent = parent;
                            parent.Files.Add(folder);
                            parent = folder;
                        }
                    }
                }
                var file = new FileNode() { Name = filesplits[filesplits.Length - 1], IsFile = true, PrefixPath = prefixPath };
                file.FullPath = (parent.FullPath == string.Empty ? filesplits[filesplits.Length - 1] : parent.FullPath + "\\" + filesplits[filesplits.Length - 1]);
                file.Parent = parent;
                parent.Files.Add(file);
            }
            return folders;
        }

        public static void CreateTreeViewItems(XmlElement curElementNode, FileNode parent, string prefixPath, string selectionName, bool selectionIsFolder, bool isFolderNodeOnly)
        {
            foreach (XmlElement ele in curElementNode.ChildNodes)
            {
                if (ele.Name == "File" && !isFolderNodeOnly)
                {
                    var fileName = ele.Attributes["Name"].Value;
                    fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                    var fullPath = string.IsNullOrEmpty(parent.FullPath)
                        ? fileName
                        : parent.FullPath + "\\" + fileName;
                    var file = new FileNode()
                    {
                        Name = fileName,
                        IsFile = true,
                        PrefixPath = prefixPath,
                        Parent = parent,
                        FullPath = fullPath,
                    };
                    if (!selectionIsFolder && selectionName == file.FullPath)
                    {
                        file.IsSelected = true;
                        var node = file.Parent;
                        while (node.FullPath.Contains("\\"))
                        {
                            node.IsExpanded = true;
                            node = node.Parent;
                        }
                    }
                    parent.Files.Add(file);
                }
                else if (ele.Name == "Folder")
                {
                    var folderName = ele.Attributes["Name"].Value;
                    folderName = folderName.Substring(folderName.LastIndexOf('\\') + 1);
                    var fullPath = string.IsNullOrEmpty(parent.FullPath)
                        ? folderName
                        : parent.FullPath + "\\" + folderName;
                    var folder = new FileNode()
                    {
                        Name = folderName,
                        IsFile = false,
                        PrefixPath = prefixPath,
                        Parent = parent,
                        FullPath = fullPath,
                        IsExpanded = !fullPath.Contains("\\"),
                    };
                    parent.Files.Add(folder);
                    if (selectionIsFolder && selectionName == folder.FullPath)
                    {
                        folder.IsSelected = true;
                        var node = folder;
                        while (node.FullPath.Contains("\\"))
                        {
                            node.IsExpanded = true;
                            node = node.Parent;
                        }
                    }
                    CreateTreeViewItems(ele, folder, prefixPath, selectionName, selectionIsFolder, isFolderNodeOnly);
                }
            }
        }

        /// <summary>
        /// 创建Recipe文件树。
        /// </summary>
        /// <param name="prefixPath"></param>
        /// <param name="selectionName"></param>
        /// <param name="selectionIsFolder"></param>
        /// <param name="fileListInXml"></param>
        /// <param name="isFolderNodeOnly">是否仅显示文件夹</param>
        /// <param name="isShowRoot">是否显示根节点</param>
        /// <returns></returns>
        public static ObservableCollection<FileNode> BuildFileNode(string prefixPath, string selectionName, bool selectionIsFolder, string fileListInXml, bool isFolderNodeOnly = false, bool isShowRoot = false)
        {
            var folders = new ObservableCollection<FileNode>();
            var root = new FileNode() { Name = "Files", IsFile = false, FullPath = "", PrefixPath = prefixPath };
            folders.Add(root);

            // 是否显示根目录节点
            if (isShowRoot)
            {
                root.Files.Add(new FileNode()
                    { Name = "\\", IsExpanded = true, PrefixPath = prefixPath, IsFile = false, FullPath = "", });

                root = root.Files[0];
            }

            var doc = new XmlDocument();
            doc.LoadXml(fileListInXml);
            CreateTreeViewItems(doc.DocumentElement, root, prefixPath, selectionName, selectionIsFolder, isFolderNodeOnly);
 
            return folders;
        }
     }
}

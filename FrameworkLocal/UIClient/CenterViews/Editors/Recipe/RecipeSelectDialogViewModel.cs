using System.Collections.Generic;
using System.Collections.ObjectModel;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using OpenSEMI.ClientBase;
using static MECF.Framework.UI.Client.CenterViews.Editors.ProcessTypeFileItem;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class RecipeSelectDialogViewModel : DialogViewModel<string>
    {
     
        #region Variables

        private readonly ProcessFileTypes _fileTypes;
        private readonly bool _isFolderOnly;
        private readonly bool _isShowRootNode;
        private FileNode _currentFileNode;

        #endregion

        #region Constructors

        public RecipeSelectDialogViewModel()
        {
            _fileTypes = ProcessFileTypes.Process | ProcessFileTypes.Routine | ProcessFileTypes.Clean;
            _isFolderOnly = false;
            _isShowRootNode = false;
            
            BuildFileTree(_isShowRootNode, _isFolderOnly, _fileTypes);
        }

        public RecipeSelectDialogViewModel(bool isShowRootNode, bool isFolderOnly, ProcessFileTypes fileType)
        {
            _fileTypes = fileType;
            _isFolderOnly = isFolderOnly;
            _isShowRootNode = isShowRootNode;

            BuildFileTree(isShowRootNode, isFolderOnly, fileType);
        }

        #endregion

        #region Properties
        
        public List<ProcessTypeFileItem> ProcessTypeFileList { get; set; }

        public FileNode CurrentFileNode { get; set; }

        public int ProcessTypeIndexSelection { get; set; }

        public ObservableCollection<FileNode> Files { get; set; }

        #endregion

        #region Private Methods

        private void BuildFileTree(bool isShowRootNode, bool isFolderOnly, ProcessFileTypes fileType)
        {
            var recipeProvider = new RecipeProvider();
            var processType = QueryDataClient.Instance.Service.GetConfig("System.Recipe.SupportedProcessType").ToString();
            if (string.IsNullOrEmpty(processType))
            {
                processType = $"{ProcessFileTypes.Process.ToString()}, {ProcessFileTypes.Routine.ToString()}";
            }
            else
            {
                // 根据参数过滤掉不需要加载的文件类型。
                var newProcessFileType = new List<string>();
                if (fileType.HasFlag(ProcessFileTypes.Process) && processType.Contains(ProcessFileTypes.Process.ToString()))
                    newProcessFileType.Add(ProcessFileTypes.Process.ToString());
                
                if (fileType.HasFlag(ProcessFileTypes.Routine) && processType.Contains(ProcessFileTypes.Routine.ToString()))
                    newProcessFileType.Add(ProcessFileTypes.Routine.ToString());

                if (fileType.HasFlag(ProcessFileTypes.Clean) && processType.Contains(ProcessFileTypes.Clean.ToString()))
                    newProcessFileType.Add(ProcessFileTypes.Clean.ToString());

                processType = string.Join(",", newProcessFileType);
            }

            var processTypeFileList = new List<ProcessTypeFileItem>();
            var recipeProcessType = ((string)processType).Split(',');

            for (var i = 0; i < recipeProcessType.Length; i++)
            {
                var type = new ProcessTypeFileItem
                {
                    ProcessType = recipeProcessType[i]
                };
                var prefix = $"Sic\\{recipeProcessType[i]}";
                var recipes = recipeProvider.GetXmlRecipeList(prefix);
                type.FileListByProcessType = RecipeSequenceTreeBuilder.BuildFileNode(prefix, "", false, recipes, isFolderOnly, isShowRootNode)[0].Files;
                processTypeFileList.Add(type);
            }

            this.ProcessTypeFileList = processTypeFileList;
        }

        #endregion

        #region Methods

        
        public void TreeSelectChanged(FileNode file)
        {
            _currentFileNode = file;
        }

        public void TreeMouseDoubleClick(FileNode file)
        {
            _currentFileNode = file;
            OK();
        }

        public void OK()
        {
            if (this._currentFileNode == null) 
                return;
            
            if (_isFolderOnly)
            {
                if (this._currentFileNode.IsFile) 
                    return;

                this.DialogResult = _currentFileNode.PrefixPath + "\\" + _currentFileNode.FullPath;
                IsCancel = false;
                TryClose(true);
            }
            else
            {
                if (!this._currentFileNode.IsFile) 
                    return;

                this.DialogResult = _currentFileNode.PrefixPath + "\\" + _currentFileNode.FullPath;
                IsCancel = false;
                TryClose(true);

            }
        }

        public void Cancel()
        {
            IsCancel = true;
            TryClose(false);
        }

        #endregion
    }
}

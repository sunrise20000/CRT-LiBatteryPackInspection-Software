using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MECF.Framework.Common.CommonData;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;

namespace MECF.Framework.UI.Client.CenterViews.Editors
{
    public class ProcessTypeFileItem : NotifiableItem
    {
        /// <summary>
        /// 工艺文件类型。
        /// </summary>
        [Flags]
        public enum ProcessFileTypes
        {
            Process = 0x1 << 0,
            Routine = 0x1 << 1,
            Clean = 0x1 << 2
        }

        #region Variablers

        private ObservableCollection<FileNode> _filterFileListByProcessType;

        #endregion

        #region Constructors
        
        public ProcessTypeFileItem()
        {
            FileListByProcessType = new ObservableCollection<FileNode>();
            FilterFileListByProcessType = new ObservableCollection<FileNode>();
        }

        public ProcessTypeFileItem(ProcessFileTypes processType) : this()
        {
            ProcessType = processType.ToString();
        }

        #endregion

        #region Properties

        public string ProcessType { get; set; }

        public ObservableCollection<FileNode> FileListByProcessType { get; set; }

        public ObservableCollection<FileNode> FilterFileListByProcessType
        {
            get => _filterFileListByProcessType;
            set
            {
                _filterFileListByProcessType = value;
                InvokePropertyChanged(nameof(FilterFileListByProcessType));
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 根据指定的工艺文件类型获取工艺文件前缀。
        /// </summary>
        /// <param name="fileTypes"></param>
        /// <returns></returns>
        public static string GetProcessFilesPrefix(string fileTypes)
        {
          if(string.IsNullOrEmpty(fileTypes))
              return string.Empty;

          switch (fileTypes.ToLower())
          {
              case "process":
                  return GetProcessFilesPrefix(ProcessFileTypes.Process);
              case "routine":
                  return GetProcessFilesPrefix(ProcessFileTypes.Routine);
              case "clean":
                  return GetProcessFilesPrefix(ProcessFileTypes.Clean);
              default:
                  return string.Empty;
          }
        }


        /// <summary>
        /// 根据指定的工艺文件类型获取工艺文件前缀。
        /// </summary>
        /// <param name="fileTypes"></param>
        /// <returns></returns>
        public static string GetProcessFilesPrefix(ProcessFileTypes fileTypes)
        {
            switch (fileTypes)
            {
                case ProcessFileTypes.Process:
                    return "Sic\\Process";

                case ProcessFileTypes.Routine:
                    return "Sic\\Routine";

                case ProcessFileTypes.Clean:
                    return "Sic\\Clean";

                default:
                    return string.Empty;
            }
        }

        #endregion
    }
}

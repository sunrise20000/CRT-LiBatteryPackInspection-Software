using MECF.Framework.Common.CommonData;
using System.Collections.ObjectModel;

namespace MECF.Framework.UI.Client.CenterViews.Editors
{
    public class ChamberTypeItem : NotifiableItem
    {
        public string ChamberType { get; set; }
        public ObservableCollection<ProcessTypeFileItem> FileListByChamberType { get; set; }

        public ChamberTypeItem()
        {
            FileListByChamberType = new ObservableCollection<ProcessTypeFileItem>();
        }
    }
}

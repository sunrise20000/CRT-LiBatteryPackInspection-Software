using System.Collections.ObjectModel;
using System.Windows.Forms.VisualStyles;
using OpenSEMI.ClientBase;

namespace SicUI.Controls
{
    public class ChooseDialogBoxViewModel : DialogViewModel<string>
    {
        public string InfoStr { get; set; }

        public void OK()
        {
            IsCancel = false;
            TryClose(true);
        }

        public void Cancel()
        {
            IsCancel = true;
            TryClose(false);
        }
    }
}

using OpenSEMI.ClientBase;

namespace SicUI.Controls.Dialogs
{
    public class YesNoDialogBoxViewModel : DialogViewModel<string>
    {
        public string InfoStr { get; set; }

        public void Yes()
        {
            IsCancel = false;
            TryClose(true);
        }

        public void No()
        {
            IsCancel = true;
            TryClose(false);
        }
    }
}

using MECF.Framework.UI.Client.ClientBase;

namespace OpenSEMI.ClientBase
{
    public class DialogViewModel<T> : BaseModel
    {
        public T DialogResult { get; set; }

        public bool IsCancel { get; set; }
    }
}

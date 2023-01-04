using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;
using MECF.Framework.Common.RecipeCenter;
namespace SicUI.Client.Dialog
{
    public class ItemsSelectDialogViewModel : DialogViewModel<string>
    {
        #region properties
        private string _SelectedName;
        public string SelectedName
        {
            get { return _SelectedName; }
            set { _SelectedName = value; }
        }
        
        public ObservableCollection<string> Items { get; set; }

        #endregion

        #region Command
        private ICommand _BtnSelectCommand;
        public ICommand SelectCommand
        {
            get
            {
                if (this._BtnSelectCommand == null)
                    this._BtnSelectCommand = new BaseCommand<EventCommandParameter<object, RoutedEventArgs>>((EventCommandParameter<object, RoutedEventArgs> arg) => this.OnSelectCommand(arg));
                return this._BtnSelectCommand;
            }
        }

        private ICommand _BtnCancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                if (this._BtnCancelCommand == null)
                    this._BtnCancelCommand = new BaseCommand<EventCommandParameter<object, RoutedEventArgs>>((EventCommandParameter<object, RoutedEventArgs> arg) => this.OnCancelCommand(arg));
                return this._BtnCancelCommand;
            }
        }

        private ICommand _ListViewDoubleClick;
        public ICommand ListViewDoubleClick
        {
            get
            {
                if (this._ListViewDoubleClick == null)
                    this._ListViewDoubleClick = new BaseCommand<EventCommandParameter<object, RoutedEventArgs>>((EventCommandParameter<object, RoutedEventArgs> arg) => this.OnListViewDoubleClick(arg));
                return this._ListViewDoubleClick;
            }
        }
        #endregion

        #region Function

        protected override void OnInitialize()
        {
            base.OnInitialize();
            this.DisplayName = "Select Dialog";
        }

        private void OnSelectCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            DialogResult = SelectedName;
            IsCancel = false;
            TryClose(true);
        }

        private void OnListViewDoubleClick(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            if (arg != null)
            {
                this.DialogResult = SelectedName;
                IsCancel = false;
                TryClose(true);
            }
        }

        private void OnCancelCommand(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            IsCancel = true;
            TryClose(false);
        }
        #endregion
    }
}

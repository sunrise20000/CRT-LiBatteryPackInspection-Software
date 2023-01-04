using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace Sicentury.Core.Dialogs
{
    /// <summary>
    /// Interaction logic for PresetGroupSaveDialog.xaml
    /// </summary>
    public partial class PresetGroupSaveDialog
    {
        #region Variables

        private readonly IEnumerable<string> _existFileNames;

        #endregion

        #region Constructors

        public PresetGroupSaveDialog(string title, IEnumerable<string> existFileNames, string defaultGroupName = "")
        {
            InitializeComponent();

            Title = title;
            _existFileNames = existFileNames;

            if (!string.IsNullOrEmpty(defaultGroupName))
                txtGroupName.Text = defaultGroupName;
        }

        #endregion
        
        #region Properties

        public string GroupName => txtGroupName.Text;

        #endregion

        #region Methods

        protected override void OnActivated(System.EventArgs e)
        {
            txtGroupName.SelectAll();
            txtGroupName.Focus();
        }

        #endregion

        #region Events

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            // 检查文件明是否为空
            if (string.IsNullOrEmpty(GroupName))
            {
                txtErrors.Text = "Group Name is Empty";
                txtGroupName.SelectAll();
                txtGroupName.Focus();
                return;
            }

            // 检查文件名是否包含非法字符
            if (GroupName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                txtErrors.Text = "illegal char(s) in Group Name";
                txtGroupName.SelectAll();
                txtGroupName.Focus();
                return;
            }

            if (_existFileNames != null && _existFileNames.Contains(GroupName))
            {
                txtErrors.Text = "Group Name has existed";
                txtGroupName.SelectAll();
                txtGroupName.Focus();
                return;
            }

            this.DialogResult = true;
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        #endregion

    }
}

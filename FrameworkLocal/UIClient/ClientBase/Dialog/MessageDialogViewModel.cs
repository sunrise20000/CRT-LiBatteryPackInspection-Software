using System;
using System.Windows.Controls;
using MECF.Framework.UI.Client.ClientBase;

namespace OpenSEMI.ClientBase
{
    public class MessageDialogViewModel : MessageDialog
    {
        protected override void OnInitialize()
        {
            base.OnInitialize();
            DisplayName = "Dialog Box";
        }

        public void OnButtonClick(object sender)
        {
            if (sender is Button btn)
            {
                // 某些按钮的标题可能来自于Description属性，无法直接从Content还原为DialogButton枚举。
                // 先尝试从Button.Tag获取其DialogButton枚举值，如果失败再尝试从Button.Content获取（兼容老代码）

                if (Enum.TryParse(btn.Content.ToString(), out DialogButton dlgRet))
                {
                    DialogResult = dlgRet;
                    TryClose();
                }
                else if(btn.Tag is DialogButton dlgRet1)
                {
                    DialogResult = dlgRet1;
                    TryClose();
                }
            }
        }
    }
}

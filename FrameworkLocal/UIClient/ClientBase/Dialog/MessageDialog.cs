using System;
using System.Collections.Generic;
using MECF.Framework.UI.Client.ClientBase;
using Sicentury.Core.Extensions;

namespace OpenSEMI.ClientBase
{
    public class MessageDialog : DialogViewModel<DialogButton>
    {
        private string m_text;
        public string Text
        {
            get { return m_text; }
            set { m_text = value; NotifyOfPropertyChange(); }
        }

        public DialogButton DialogButton
        {
            set
            {
                this.m_buttons.Clear();
                foreach (int v in Enum.GetValues(typeof(DialogButton)))
                {
                    var m_btn = (DialogButton)v;
                    if (value.HasFlag(m_btn))
                    {
                        var bc = new ButtonControl();

                        // 使用Description属性指定按钮名称。
                        var btnContent = m_btn.GetDescription();
                        bc.Name = string.IsNullOrEmpty(btnContent) ? m_btn.ToString() : btnContent;
                        bc.Tag = m_btn;

                        if (m_btn == DialogButton.OK || m_btn == DialogButton.Yes)
                            bc.IsDefault = true;
                        else
                            bc.IsDefault = false;

                        if (m_btn == DialogButton.Cancel || m_btn == DialogButton.No)
                            bc.IsCancel = true;
                        else
                            bc.IsCancel = false;

                        m_buttons.Add(bc);
                    }
                }
            }
        }

        private List<ButtonControl> m_buttons = new List<ButtonControl>();
        public List<ButtonControl> Buttons
        {
            get { return m_buttons; }
        }

        private DialogType m_DialogType = DialogType.INFO;
        public DialogType DialogType
        {
            get { return m_DialogType; }
            set { m_DialogType = value; NotifyOfPropertyChange("DialogType"); }
        }
    }

    public class ButtonControl
    {
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public bool IsCancel { get; set; }
        public object Tag { get; set; }
    }
}

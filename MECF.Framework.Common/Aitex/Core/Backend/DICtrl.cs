using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Aitex.Core.RT.Simulator;
using Aitex.Core.Util;

namespace Aitex.Core.Backend
{
	public class DICtrl : UserControl
	{
		private string _ioName;

		private bool _rawValue;

		private IContainer components = null;

		private Label labelRaw;

		private CheckBox checkBox1;

		private CheckBox checkBox2;

		public DICtrl()
		{
			InitializeComponent();
			base.Load += DICtrl_Load;
		}

		private void DICtrl_Load(object sender, EventArgs e)
		{
		}

		public void SetIoName(string group, string ioName)
		{
			_ioName = ioName;
		}

		public void SetName(string name)
		{
			labelRaw.Text = name;
		}

		public void SetValue(bool latchedValue, bool rawValue)
		{
			_rawValue = rawValue;
			labelRaw.BackColor = (rawValue ? Color.Lime : Color.LightGray);
		}

		private void labelName_Click(object sender, EventArgs e)
		{
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			if (checkBox1.Checked)
			{
				checkBox2.Checked = _rawValue;
				Singleton<DiForce>.Instance.Set(_ioName, checkBox2.Checked);
				checkBox2.Enabled = true;
				BackColor = Color.DodgerBlue;
			}
			else
			{
				Singleton<DiForce>.Instance.Unset(_ioName);
				checkBox2.Enabled = false;
				BackColor = Color.White;
			}
		}

		private void checkBox2_CheckedChanged(object sender, EventArgs e)
		{
			Singleton<DiForce>.Instance.Set(_ioName, checkBox2.Checked);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.labelRaw = new System.Windows.Forms.Label();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.checkBox2 = new System.Windows.Forms.CheckBox();
			base.SuspendLayout();
			this.labelRaw.BackColor = System.Drawing.Color.LightGray;
			this.labelRaw.Font = new System.Drawing.Font("Microsoft Sans Serif", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.labelRaw.Location = new System.Drawing.Point(25, 1);
			this.labelRaw.Name = "labelRaw";
			this.labelRaw.Size = new System.Drawing.Size(250, 23);
			this.labelRaw.TabIndex = 0;
			this.labelRaw.Text = "123.Leak_Senor_Alarm(SW)";
			this.labelRaw.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.labelRaw.Click += new System.EventHandler(labelName_Click);
			this.checkBox1.AutoSize = true;
			this.checkBox1.Location = new System.Drawing.Point(3, 5);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(15, 14);
			this.checkBox1.TabIndex = 1;
			this.checkBox1.UseVisualStyleBackColor = true;
			this.checkBox1.CheckedChanged += new System.EventHandler(checkBox1_CheckedChanged);
			this.checkBox2.AutoSize = true;
			this.checkBox2.Enabled = false;
			this.checkBox2.Location = new System.Drawing.Point(280, 5);
			this.checkBox2.Name = "checkBox2";
			this.checkBox2.Size = new System.Drawing.Size(15, 14);
			this.checkBox2.TabIndex = 2;
			this.checkBox2.UseVisualStyleBackColor = true;
			this.checkBox2.CheckedChanged += new System.EventHandler(checkBox2_CheckedChanged);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
			this.BackColor = System.Drawing.SystemColors.ControlLight;
			base.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			base.CausesValidation = false;
			base.Controls.Add(this.checkBox2);
			base.Controls.Add(this.labelRaw);
			base.Controls.Add(this.checkBox1);
			base.Name = "DICtrl";
			base.Size = new System.Drawing.Size(301, 25);
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}
}

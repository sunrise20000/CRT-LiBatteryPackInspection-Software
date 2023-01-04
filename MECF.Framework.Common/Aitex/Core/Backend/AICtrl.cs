using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Aitex.Core.RT.Simulator;
using Aitex.Core.Util;

namespace Aitex.Core.Backend
{
	public class AICtrl : UserControl
	{
		private string _ioName;

		private IContainer components = null;

		private Label labelName;

		private CheckBox checkBox1;

		private TextBox textBox1;

		private TextBox textBox2;

		private Button button1;

		public AICtrl()
		{
			InitializeComponent();
		}

		public void SetName(string name)
		{
			labelName.Text = name;
		}

		public void SetIoName(string group, string ioName)
		{
			_ioName = ioName;
		}

		public void SetValue(float value)
		{
			textBox1.Text = $"{value:f2}";
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			if (checkBox1.Checked)
			{
				textBox2.Text = textBox1.Text;
				float.TryParse(textBox2.Text.Trim(), out var result);
				Singleton<AiForce>.Instance.Set(_ioName, result);
				button1.Enabled = true;
				textBox2.Enabled = true;
				BackColor = Color.DodgerBlue;
			}
			else
			{
				Singleton<AiForce>.Instance.Unset(_ioName);
				button1.Enabled = false;
				textBox2.Enabled = false;
				BackColor = Color.White;
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (float.TryParse(textBox2.Text.Trim(), out var result))
			{
				Singleton<AiForce>.Instance.Set(_ioName, result);
			}
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
			this.labelName = new System.Windows.Forms.Label();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			base.SuspendLayout();
			this.labelName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.labelName.Location = new System.Drawing.Point(24, 3);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(233, 23);
			this.labelName.TabIndex = 5;
			this.labelName.Text = "123.Leak_Senor_Alarm(SW)";
			this.checkBox1.AutoSize = true;
			this.checkBox1.Location = new System.Drawing.Point(4, 6);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(15, 14);
			this.checkBox1.TabIndex = 6;
			this.checkBox1.UseVisualStyleBackColor = true;
			this.checkBox1.CheckedChanged += new System.EventHandler(checkBox1_CheckedChanged);
			this.textBox1.Location = new System.Drawing.Point(3, 26);
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.Size = new System.Drawing.Size(100, 21);
			this.textBox1.TabIndex = 7;
			this.textBox2.Location = new System.Drawing.Point(109, 26);
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new System.Drawing.Size(100, 21);
			this.textBox2.TabIndex = 8;
			this.button1.Location = new System.Drawing.Point(215, 24);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(42, 23);
			this.button1.TabIndex = 9;
			this.button1.Text = "Set";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(button1_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.ControlLight;
			base.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			base.Controls.Add(this.button1);
			base.Controls.Add(this.textBox2);
			base.Controls.Add(this.textBox1);
			base.Controls.Add(this.checkBox1);
			base.Controls.Add(this.labelName);
			base.Name = "AICtrl";
			base.Size = new System.Drawing.Size(260, 50);
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}
}

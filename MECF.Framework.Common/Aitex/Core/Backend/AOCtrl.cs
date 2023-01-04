using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Aitex.Core.RT.IOCore;

namespace Aitex.Core.Backend
{
	public class AOCtrl : UserControl
	{
		private string _ioName;

		private IContainer components = null;

		private Label labelName;

		private TextBox textBox1;

		private TextBox textBox2;

		private Button button1;

		public AOCtrl()
		{
			InitializeComponent();
		}

		public void SetIoName(string group, string ioName)
		{
			_ioName = ioName;
		}

		public void SetName(string name)
		{
			labelName.Text = name;
		}

		public void SetValue(float value)
		{
			textBox1.Text = $"{value:f2}";
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (double.TryParse(textBox2.Text, out var result))
			{
				IO.AO[_ioName].Value = (short)result;
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
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			base.SuspendLayout();
			this.labelName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.labelName.Location = new System.Drawing.Point(-1, 1);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(236, 22);
			this.labelName.TabIndex = 5;
			this.labelName.Text = "123.Leak_Senor_Alarm(SW)";
			this.textBox1.Location = new System.Drawing.Point(3, 22);
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.Size = new System.Drawing.Size(100, 21);
			this.textBox1.TabIndex = 10;
			this.textBox2.Location = new System.Drawing.Point(109, 22);
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new System.Drawing.Size(100, 21);
			this.textBox2.TabIndex = 11;
			this.button1.Location = new System.Drawing.Point(215, 20);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(42, 23);
			this.button1.TabIndex = 12;
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
			base.Controls.Add(this.labelName);
			base.Name = "AOCtrl";
			base.Size = new System.Drawing.Size(260, 50);
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}
}

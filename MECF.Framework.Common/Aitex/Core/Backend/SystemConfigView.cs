using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Aitex.Core.RT.SCCore;

namespace Aitex.Core.Backend
{
	public class SystemConfigView : UserControl
	{
		private Dictionary<string, Type> _scItems = new Dictionary<string, Type>();

		private IContainer components = null;

		private FlowLayoutPanel flowLayoutPanel1;

		private Button button1;

		public SystemConfigView()
		{
			InitializeComponent();
		}

		private void SystemConfigView_Load(object sender, EventArgs e)
		{
			Dock = DockStyle.Fill;
		}

		private void bt_Click(object sender, EventArgs e)
		{
			Button button = sender as Button;
			FlowLayoutPanel flowLayoutPanel = button.Parent as FlowLayoutPanel;
			TextBox textBox = new TextBox();
			TextBox textBox2 = new TextBox();
			foreach (object control in flowLayoutPanel.Controls)
			{
				if (control is TextBox textBox3)
				{
					if (textBox3.ReadOnly)
					{
						textBox = textBox3;
					}
					else
					{
						textBox2 = textBox3;
					}
				}
			}
			SC.SetItemValue(button.Tag.ToString(), textBox2.Text);
		}

		private void button1_Click(object sender, EventArgs e)
		{
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
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.button1 = new System.Windows.Forms.Button();
			this.flowLayoutPanel1.SuspendLayout();
			base.SuspendLayout();
			this.flowLayoutPanel1.AutoScroll = true;
			this.flowLayoutPanel1.Controls.Add(this.button1);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(859, 639);
			this.flowLayoutPanel1.TabIndex = 3;
			this.button1.Location = new System.Drawing.Point(3, 3);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 0;
			this.button1.Text = "Reload";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(button1_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.flowLayoutPanel1);
			base.Name = "SystemConfigView";
			base.Size = new System.Drawing.Size(859, 639);
			base.Load += new System.EventHandler(SystemConfigView_Load);
			this.flowLayoutPanel1.ResumeLayout(false);
			base.ResumeLayout(false);
		}
	}
}

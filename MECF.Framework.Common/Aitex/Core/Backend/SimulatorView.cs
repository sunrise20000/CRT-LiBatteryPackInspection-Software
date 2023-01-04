using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Aitex.Core.RT.Simulator;

namespace Aitex.Core.Backend
{
	public class SimulatorView : UserControl
	{
		private Dictionary<string, Dictionary<string, string>> _caseGroup = new Dictionary<string, Dictionary<string, string>>();

		private IContainer components = null;

		private FlowLayoutPanel flowLayoutPanel1;

		public SimulatorView()
		{
			InitializeComponent();
		}

		private void SimulatorView_Load(object sender, EventArgs e)
		{
			Dock = DockStyle.Fill;
			PropertyInfo[] properties = typeof(ExceptionCase).GetProperties();
			foreach (PropertyInfo propertyInfo in properties)
			{
				object[] customAttributes = propertyInfo.GetCustomAttributes(inherit: false);
				foreach (object obj in customAttributes)
				{
					if (obj is DisplayAttribute displayAttribute)
					{
						if (!_caseGroup.ContainsKey(displayAttribute.GroupName))
						{
							_caseGroup[displayAttribute.GroupName] = new Dictionary<string, string>();
							TextBox textBox = new TextBox();
							textBox.ReadOnly = true;
							textBox.Enabled = false;
							textBox.Text = displayAttribute.GroupName;
							textBox.TextAlign = HorizontalAlignment.Center;
							textBox.BackColor = SystemColors.MenuHighlight;
							textBox.Location = new Point(3, 3);
							textBox.Size = new Size(262, 35);
							textBox.TabIndex = 0;
							flowLayoutPanel1.Controls.Add(textBox);
						}
						_caseGroup[displayAttribute.GroupName][propertyInfo.Name] = displayAttribute.Description;
						CheckBox checkBox = new CheckBox();
						checkBox.AutoSize = true;
						checkBox.Location = new Point(16, 34);
						checkBox.Name = "ck" + propertyInfo.Name;
						checkBox.Size = new Size(126, 16);
						checkBox.TabIndex = 1;
						checkBox.Text = displayAttribute.Description;
						checkBox.UseVisualStyleBackColor = true;
						checkBox.CheckedChanged += cbCheckedChanged;
						checkBox.Tag = propertyInfo;
						flowLayoutPanel1.Controls.Add(checkBox);
					}
				}
			}
		}

		private void cbCheckedChanged(object sender, EventArgs e)
		{
			CheckBox checkBox = sender as CheckBox;
			PropertyInfo propertyInfo = checkBox.Tag as PropertyInfo;
			propertyInfo.SetValue(null, checkBox.Checked, null);
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
			base.SuspendLayout();
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(793, 555);
			this.flowLayoutPanel1.TabIndex = 2;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoScroll = true;
			base.Controls.Add(this.flowLayoutPanel1);
			base.Name = "SimulatorView";
			base.Size = new System.Drawing.Size(793, 555);
			base.Load += new System.EventHandler(SimulatorView_Load);
			base.ResumeLayout(false);
		}
	}
}

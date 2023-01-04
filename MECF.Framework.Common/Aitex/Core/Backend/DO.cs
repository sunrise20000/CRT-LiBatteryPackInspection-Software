using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;

namespace Aitex.Core.Backend
{
	public class DO : UserControl, IIOView
	{
		private PeriodicJob _thread;

		private IContainer components = null;

		private Panel panel1;

		public DO()
		{
			InitializeComponent();
			base.Load += DO_Load;
		}

		private void DO_Load(object sender, EventArgs e)
		{
			if (panel1.Controls.Count > 0)
			{
				return;
			}
			List<Tuple<int, int, string>> iONameList = IO.GetIONameList("", IOType.DO);
			int num = 0;
			foreach (Tuple<int, int, string> item in iONameList)
			{
				Button button = new Button();
				button.FlatStyle = FlatStyle.Flat;
				button.Location = new Point(num % 3 * 305, num / 3 * 30);
				button.Size = new Size(300, 25);
				button.Text = $"DO-{item.Item1}. {item.Item3}";
				button.Tag = item.Item3;
				button.BackColor = Color.LightGray;
				button.TextAlign = ContentAlignment.MiddleCenter;
				button.Font = new Font("Arial,SimSun", 9f, FontStyle.Regular, GraphicsUnit.Point, 134);
				button.UseVisualStyleBackColor = false;
				button.Click += delegate(object sender2, EventArgs e2)
				{
					Button button2 = (Button)sender2;
					string name = (string)button2.Tag;
					if (!IO.DO[name].SetValue(!IO.DO[name].Value, out var reason))
					{
						MessageBox.Show(reason);
					}
				};
				panel1.Controls.Add(button);
				num++;
			}
			_thread = new PeriodicJob(500, OnTimer, "DOTimer");
			base.VisibleChanged += delegate
			{
				if (base.Visible)
				{
					_thread.Start();
				}
				else
				{
					_thread.Pause();
				}
			};
		}

		private bool OnTimer()
		{
			Invoke((Action)delegate
			{
				foreach (Button control in panel1.Controls)
				{
					if (control != null)
					{
						string text = (string)control.Tag;
						control.BackColor = (IO.DO[(string)control.Tag].Value ? Color.Lime : Color.LightGray);
					}
				}
			});
			return true;
		}

		public void EnableTimer(bool enable)
		{
			if (enable)
			{
				_thread.Start();
			}
			else
			{
				_thread.Pause();
			}
		}

		public void Close()
		{
			if (_thread != null)
			{
				_thread.Stop();
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
			this.panel1 = new System.Windows.Forms.Panel();
			base.SuspendLayout();
			this.panel1.AutoScroll = true;
			this.panel1.BackColor = System.Drawing.SystemColors.Control;
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(984, 761);
			this.panel1.TabIndex = 0;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoScroll = true;
			this.BackColor = System.Drawing.Color.White;
			base.Controls.Add(this.panel1);
			base.Name = "DO";
			base.Size = new System.Drawing.Size(984, 761);
			base.ResumeLayout(false);
		}
	}
}

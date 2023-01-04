using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;

namespace Aitex.Core.Backend
{
	public class DI : UserControl, IIOView
	{
		private PeriodicJob _thread;

		private IContainer components = null;

		private Panel panel1;

		public DI()
		{
			InitializeComponent();
			base.Load += DI_Load;
		}

		private void DI_Load(object sender, EventArgs e)
		{
			if (panel1.Controls.Count > 0)
			{
				return;
			}
			List<Tuple<int, int, string>> iONameList = IO.GetIONameList("", IOType.DI);
			int num = 3;
			int num2 = iONameList.Count / num + ((iONameList.Count % num > 0) ? 1 : 0);
			int num3 = 0;
			foreach (Tuple<int, int, string> item in iONameList)
			{
				int num4 = num3 % num2;
				int num5 = num3 / num2;
				DICtrl dICtrl = new DICtrl();
				dICtrl.Location = new Point(num5 * 305, num4 * 30);
				dICtrl.SetName($"DI-{item.Item1}. {item.Item3}");
				dICtrl.SetIoName("local", item.Item3);
				dICtrl.Name = $"DI_{item.Item1}";
				dICtrl.Size = new Size(300, 25);
				dICtrl.Tag = item.Item3;
				panel1.Controls.Add(dICtrl);
				num3++;
			}
			_thread = new PeriodicJob(500, OnTimer, "DITimer");
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
				foreach (DICtrl control in panel1.Controls)
				{
					string name = (string)control.Tag;
					control?.SetValue(IO.DI[name].Value, IO.DI[name].Value);
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
			this.panel1.Size = new System.Drawing.Size(1500, 629);
			this.panel1.TabIndex = 0;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.LightGray;
			base.Controls.Add(this.panel1);
			base.Name = "DI";
			base.Size = new System.Drawing.Size(1500, 629);
			base.ResumeLayout(false);
		}
	}
}

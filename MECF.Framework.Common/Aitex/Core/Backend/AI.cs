using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;

namespace Aitex.Core.Backend
{
	public class AI : UserControl, IIOView
	{
		private PeriodicJob _thread;

		private IContainer components = null;

		public AI()
		{
			InitializeComponent();
			base.Load += AI_Load;
		}

		private void AI_Load(object sender, EventArgs e)
		{
			if (base.Controls.Count > 0)
			{
				return;
			}
			List<Tuple<int, int, string>> iONameList = IO.GetIONameList("", IOType.AI);
			int num = 0;
			foreach (Tuple<int, int, string> item in iONameList)
			{
				AICtrl aICtrl = new AICtrl();
				aICtrl.Location = new Point(num % 3 * 305, num / 3 * 55);
				aICtrl.SetName($"AI-{item.Item1}.{item.Item3}");
				aICtrl.SetIoName("local", item.Item3);
				aICtrl.Name = $"AI_{item.Item1}";
				aICtrl.Size = new Size(300, 50);
				aICtrl.Visible = true;
				aICtrl.Tag = item.Item3;
				base.Controls.Add(aICtrl);
				num++;
			}
			_thread = new PeriodicJob(500, OnTimer, "AITimer");
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
				foreach (AICtrl control in base.Controls)
				{
					control?.SetValue(IO.AI[(string)control.Tag].Value);
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
			base.SuspendLayout();
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			base.Name = "AI";
			base.Size = new System.Drawing.Size(1065, 746);
			base.ResumeLayout(false);
		}
	}
}

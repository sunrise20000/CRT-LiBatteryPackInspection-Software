using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;

namespace Aitex.Core.Backend
{
	public class AO : UserControl, IIOView
	{
		private PeriodicJob _thread;

		private IContainer components = null;

		public AO()
		{
			InitializeComponent();
			base.Load += AO_Load;
		}

		private void AO_Load(object sender, EventArgs e)
		{
			if (base.Controls.Count > 0)
			{
				return;
			}
			List<Tuple<int, int, string>> iONameList = IO.GetIONameList("", IOType.AO);
			int num = 0;
			foreach (Tuple<int, int, string> item in iONameList)
			{
				AOCtrl aOCtrl = new AOCtrl();
				aOCtrl.Location = new Point(num % 3 * 305, num / 3 * 55);
				aOCtrl.SetName($"AO-{item.Item1}.{item.Item3}");
				aOCtrl.SetIoName("local", item.Item3);
				aOCtrl.Name = $"AO_{item.Item1}";
				aOCtrl.Size = new Size(300, 50);
				aOCtrl.Visible = true;
				aOCtrl.Tag = item.Item3;
				base.Controls.Add(aOCtrl);
				num++;
			}
			_thread = new PeriodicJob(500, OnTimer, "AOTimer");
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
				foreach (AOCtrl control in base.Controls)
				{
					control?.SetValue(IO.AO[(string)control.Tag].Value);
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
			base.Name = "AO";
			base.Size = new System.Drawing.Size(987, 756);
			base.ResumeLayout(false);
		}
	}
}

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Aitex.Core.Backend
{
	public class IoDataView : UserControl
	{
		private DI _diView = new DI();

		private DO _doView = new DO();

		private AI _aiView = new AI();

		private AO _aoView = new AO();

		private IContainer components = null;

		private TabControl tabControl1;

		public IoDataView()
		{
			InitializeComponent();
			base.Load += IoDataView_Load;
		}

		private void IoDataView_Load(object sender, EventArgs e)
		{
			if (tabControl1.TabPages.Count == 0)
			{
				AddView("DI", _diView);
				AddView("DO", _doView);
				AddView("AI", _aiView);
				AddView("AO", _aoView);
			}
		}

		private void AddView(string name, UserControl uc)
		{
			uc.Dock = DockStyle.Fill;
			uc.AutoScroll = true;
			TabPage tabPage = new TabPage();
			tabPage.Text = name;
			tabPage.Controls.Add(uc);
			tabControl1.Controls.Add(tabPage);
		}

		public void Close()
		{
			((IIOView)_diView).Close();
			((IIOView)_doView).Close();
			((IIOView)_aiView).Close();
			((IIOView)_aoView).Close();
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
			this.tabControl1 = new System.Windows.Forms.TabControl();
			base.SuspendLayout();
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(1200, 725);
			this.tabControl1.TabIndex = 0;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.tabControl1);
			base.Name = "IoDataView";
			base.Size = new System.Drawing.Size(1200, 725);
			base.ResumeLayout(false);
		}
	}
}

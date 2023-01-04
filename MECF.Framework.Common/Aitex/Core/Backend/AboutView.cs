using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Aitex.Core.Backend
{
	public class AboutView : UserControl
	{
		private IContainer components = null;

		public AboutView()
		{
			InitializeComponent();
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
			base.Name = "AboutView";
			base.Size = new System.Drawing.Size(410, 372);
			base.ResumeLayout(false);
		}
	}
}

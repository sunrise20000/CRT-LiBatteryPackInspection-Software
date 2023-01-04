using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Aitex.Core.RT.OperationCenter;

namespace Aitex.Core.Backend
{
	public class MainView : Form
	{
		private NotifyIcon _notifyIcon = new NotifyIcon();

		private Dictionary<string, UserControl> _views;

		private object _msgLock = new object();

		private List<string> _events = new List<string>();

		private IContainer components = null;

		private SplitContainer splitContainer1;

		private Button btnHide;

		private SplitContainer splitContainer2;

		private TreeView treeView1;

		private Button btnReset;

		protected override CreateParams CreateParams
		{
			get
			{
				int num = 512;
				CreateParams createParams = base.CreateParams;
				createParams.ClassStyle |= num;
				return createParams;
			}
		}

		public MainView()
		{
			InitializeComponent();
			_views = new Dictionary<string, UserControl>();
			_views.Add("About", new AboutView());
			foreach (KeyValuePair<string, UserControl> view in _views)
			{
				splitContainer2.Panel2.Controls.Add(view.Value);
			}
			ShowView("About");
			base.SizeChanged += MainView_SizeChanged;
		}

		private void MainView_SizeChanged(object sender, EventArgs e)
		{
			foreach (KeyValuePair<string, UserControl> view in _views)
			{
				view.Value.Width = splitContainer1.Panel2.Width;
				view.Value.Height = splitContainer1.Panel2.Height;
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing)
			{
				e.Cancel = true;
				Hide();
			}
		}

		public void AddCustomView(string name, UserControl uc)
		{
			if (uc != null)
			{
				treeView1.Nodes.Add(new TreeNode
				{
					Tag = name,
					Text = name
				});
				uc.Show();
				uc.Hide();
				_views.Add(name, uc);
				splitContainer2.Panel2.Controls.Add(uc);
				uc.Width = splitContainer2.Panel2.Width;
				uc.Height = splitContainer2.Panel2.Height;
			}
		}

		private void ShowView(string viewName)
		{
			foreach (KeyValuePair<string, UserControl> view in _views)
			{
				if (view.Key != viewName)
				{
					view.Value.Hide();
				}
				else
				{
					view.Value.Show();
				}
			}
		}

		private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
		{
			string viewName = (string)e.Node.Tag;
			ShowView(viewName);
		}

		private void btnLogout_Click(object sender, EventArgs e)
		{
			Hide();
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			OP.DoOperation("Reset");
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
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.btnHide = new System.Windows.Forms.Button();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.btnReset = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)this.splitContainer1).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.splitContainer2).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			base.SuspendLayout();
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer1.IsSplitterFixed = true;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.splitContainer1.Panel1.Controls.Add(this.btnReset);
			this.splitContainer1.Panel1.Controls.Add(this.btnHide);
			this.splitContainer1.Panel2.BackColor = System.Drawing.Color.White;
			this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
			this.splitContainer1.Size = new System.Drawing.Size(1107, 784);
			this.splitContainer1.SplitterDistance = 30;
			this.splitContainer1.TabIndex = 0;
			this.btnHide.Location = new System.Drawing.Point(984, 5);
			this.btnHide.Name = "btnHide";
			this.btnHide.Size = new System.Drawing.Size(111, 23);
			this.btnHide.TabIndex = 3;
			this.btnHide.Text = "Hide";
			this.btnHide.UseVisualStyleBackColor = true;
			this.btnHide.Click += new System.EventHandler(btnLogout_Click);
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			this.splitContainer2.Panel1.Controls.Add(this.treeView1);
			this.splitContainer2.Size = new System.Drawing.Size(1107, 750);
			this.splitContainer2.SplitterDistance = 150;
			this.splitContainer2.SplitterWidth = 6;
			this.splitContainer2.TabIndex = 0;
			this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView1.Location = new System.Drawing.Point(0, 0);
			this.treeView1.Name = "treeView1";
			this.treeView1.Size = new System.Drawing.Size(150, 750);
			this.treeView1.TabIndex = 0;
			this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(treeView1_AfterSelect);
			this.btnReset.Location = new System.Drawing.Point(12, 3);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(111, 23);
			this.btnReset.TabIndex = 4;
			this.btnReset.Text = "Reset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(btnReset_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(1107, 784);
			base.Controls.Add(this.splitContainer1);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			base.Name = "MainView";
			this.Text = "Backend Management Console";
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.splitContainer1).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.splitContainer2).EndInit();
			this.splitContainer2.ResumeLayout(false);
			base.ResumeLayout(false);
		}
	}
}

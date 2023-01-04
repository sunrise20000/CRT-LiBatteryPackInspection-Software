using System;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using Aitex.Core.RT.Log;
using Aitex.Core.Utilities;

namespace Aitex.Core.Backend
{
	public class UserLoginView : Form
	{
		private MainView _mainView;

		private static UserLoginView _instance;

		private IContainer components = null;

		private TextBox textBoxAccountId;

		private TextBox textBoxPassword;

		private Label label1;

		private Label label2;

		private Button buttonLogin;

		private Button buttonCancel;

		private GroupBox groupBox1;

		private UserLoginView()
		{
			InitializeComponent();
			base.AcceptButton = buttonLogin;
			base.CancelButton = buttonCancel;
			base.Load += UserLoginView_Load;
		}

		private void UserLoginView_Load(object sender, EventArgs e)
		{
			Text = "Login";
		}

		private void ResetInput()
		{
			textBoxPassword.Clear();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			buttonCancel_Click(null, null);
			e.Cancel = true;
			base.OnClosing(e);
		}

		public static void Display(bool ignorePassword)
		{
			if (_instance == null)
			{
				_instance = new UserLoginView();
			}
			_instance.ResetInput();
			if (ignorePassword)
			{
				if (_instance._mainView == null)
				{
					_instance._mainView = new MainView();
				}
				_instance._mainView.Show();
			}
			else if (_instance._mainView != null && _instance._mainView.Visible)
			{
				_instance._mainView.Show();
			}
			else
			{
				_instance.Show();
			}
		}

		public static void AddCustomView(string name, UserControl uc)
		{
			if (_instance == null)
			{
				_instance = new UserLoginView();
			}
			if (_instance._mainView == null)
			{
				_instance._mainView = new MainView();
			}
			_instance._mainView.AddCustomView(name, uc);
		}

		private void buttonLogin_Click(object sender, EventArgs e)
		{
			string hash = ConfigurationManager.AppSettings["Su"];
			string strA = textBoxAccountId.Text;
			string input = textBoxPassword.Text;
			if (string.Compare(strA, "admin", ignoreCase: true) == 0 && Md5Helper.VerifyMd5Hash(input, hash))
			{
				LOG.Write("用户登入后台界面");
				if (_mainView == null)
				{
					_mainView = new MainView();
				}
				_mainView.Show();
				Hide();
			}
			else
			{
				LOG.Write("用户密码错误，登入后台界面失败");
				MessageBox.Show("Account name or password is error, login failed.", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			Hide();
			base.DialogResult = DialogResult.Cancel;
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
			this.textBoxAccountId = new System.Windows.Forms.TextBox();
			this.textBoxPassword = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonLogin = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox1.SuspendLayout();
			base.SuspendLayout();
			this.textBoxAccountId.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.textBoxAccountId.Location = new System.Drawing.Point(144, 33);
			this.textBoxAccountId.Margin = new System.Windows.Forms.Padding(4);
			this.textBoxAccountId.Name = "textBoxAccountId";
			this.textBoxAccountId.Size = new System.Drawing.Size(148, 25);
			this.textBoxAccountId.TabIndex = 0;
			this.textBoxAccountId.Text = "admin";
			this.textBoxPassword.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.textBoxPassword.Location = new System.Drawing.Point(144, 74);
			this.textBoxPassword.Margin = new System.Windows.Forms.Padding(4);
			this.textBoxPassword.Name = "textBoxPassword";
			this.textBoxPassword.Size = new System.Drawing.Size(148, 25);
			this.textBoxPassword.TabIndex = 1;
			this.textBoxPassword.UseSystemPasswordChar = true;
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.label1.Location = new System.Drawing.Point(52, 36);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(64, 19);
			this.label1.TabIndex = 2;
			this.label1.Text = "Account:";
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.label2.Location = new System.Drawing.Point(52, 77);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(70, 19);
			this.label2.TabIndex = 3;
			this.label2.Text = "Password:";
			this.buttonLogin.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.buttonLogin.Location = new System.Drawing.Point(93, 19);
			this.buttonLogin.Name = "buttonLogin";
			this.buttonLogin.Size = new System.Drawing.Size(87, 26);
			this.buttonLogin.TabIndex = 0;
			this.buttonLogin.Text = "Login";
			this.buttonLogin.UseVisualStyleBackColor = true;
			this.buttonLogin.Click += new System.EventHandler(buttonLogin_Click);
			this.buttonCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.buttonCancel.Location = new System.Drawing.Point(212, 19);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(87, 26);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(buttonCancel_Click);
			this.groupBox1.Controls.Add(this.buttonCancel);
			this.groupBox1.Controls.Add(this.buttonLogin);
			this.groupBox1.Location = new System.Drawing.Point(-24, 114);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(397, 64);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			base.AutoScaleDimensions = new System.Drawing.SizeF(9f, 18f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(363, 171);
			base.Controls.Add(this.groupBox1);
			base.Controls.Add(this.label2);
			base.Controls.Add(this.label1);
			base.Controls.Add(this.textBoxPassword);
			base.Controls.Add(this.textBoxAccountId);
			this.Font = new System.Drawing.Font("Verdana", 11.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			base.Margin = new System.Windows.Forms.Padding(4);
			base.MaximizeBox = false;
			base.Name = "UserLoginView";
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Login";
			this.groupBox1.ResumeLayout(false);
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}
}

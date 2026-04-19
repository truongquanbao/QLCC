using System;
using System.Windows.Forms;
using ApartmentManager.BLL;
using ApartmentManager.Utilities;
using Serilog;

using System.Drawing;
namespace ApartmentManager.GUI.Forms;

public partial class FrmLogin : Form
{
    public FrmLogin()
    {
        // InitializeComponent();
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = true;
        this.Text = "Quáº£n LÃ½ Khu Chung CÆ° - ÄÄƒng Nháº­p";
        this.Size = new Size(500, 400);
    }

    private void FrmLogin_Load(object sender, EventArgs e)
    {
        ConfigureUI();
        // ConfigurationHelper.Initialize();
        LoadRememberedUsername();
    }

    private void ConfigureUI()
    {
        // Panel chÃ­nh
        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 245, 250),
            Padding = new Padding(40)
        };

        // Panel tiÃªu Ä‘á»
        var pnlHeader = new Panel
        {
            Height = 60,
            Dock = DockStyle.Top,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 20)
        };

        var lblTitle = new Label
        {
            Text = "ÄÄ‚NG NHáº¬P Há»† THá»NG",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 86, 155),
            AutoSize = false,
            Width = 400,
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlHeader.Controls.Add(lblTitle);

        // Username
        var lblUsername = new Label
        {
            Text = "TÃªn Ä‘Äƒng nháº­p:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(64, 64, 64),
            AutoSize = true,
            Location = new Point(0, 100)
        };

        var txtUsername = new TextBox
        {
            Name = "txtUsername",
            Width = 400,
            Height = 35,
            Font = new Font("Segoe UI", 10),
            Location = new Point(0, 125),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(5)
        };

        // Password
        var lblPassword = new Label
        {
            Text = "Máº­t kháº©u:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(64, 64, 64),
            AutoSize = true,
            Location = new Point(0, 170)
        };

        var txtPassword = new TextBox
        {
            Name = "txtPassword",
            Width = 400,
            Height = 35,
            Font = new Font("Segoe UI", 10),
            Location = new Point(0, 195),
            UseSystemPasswordChar = true,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(5)
        };

        // Remember me
        var chkRemember = new CheckBox
        {
            Name = "chkRemember",
            Text = "Nhá»› Ä‘Äƒng nháº­p",
            Font = new Font("Segoe UI", 9),
            Location = new Point(0, 240),
            AutoSize = true
        };

        // Login button
        var btnLogin = new Button
        {
            Name = "btnLogin",
            Text = "ÄÄ‚NG NHáº¬P",
            Width = 400,
            Height = 45,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(33, 150, 243),
            ForeColor = Color.White,
            Location = new Point(0, 280),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;

        // Forgot password link
        var linkForgotPassword = new LinkLabel
        {
            Name = "linkForgotPassword",
            Text = "QuÃªn máº­t kháº©u?",
            Width = 400,
            Height = 30,
            Font = new Font("Segoe UI", 9),
            LinkColor = Color.FromArgb(33, 150, 243),
            Location = new Point(0, 330)
        };
        linkForgotPassword.LinkClicked += LinkForgotPassword_LinkClicked;

        // Register link
        var linkRegister = new LinkLabel
        {
            Name = "linkRegister",
            Text = "ChÆ°a cÃ³ tÃ i khoáº£n? ÄÄƒng kÃ½ ngay",
            Width = 400,
            Height = 30,
            Font = new Font("Segoe UI", 9),
            LinkColor = Color.FromArgb(33, 150, 243),
            Location = new Point(0, 360)
        };
        linkRegister.LinkClicked += LinkRegister_LinkClicked;

        // Status label
        var lblStatus = new Label
        {
            Name = "lblStatus",
            Text = "",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(255, 87, 34),
            AutoSize = true,
            Location = new Point(0, 390)
        };

        pnlMain.Controls.Add(pnlHeader);
        pnlMain.Controls.Add(lblUsername);
        pnlMain.Controls.Add(txtUsername);
        pnlMain.Controls.Add(lblPassword);
        pnlMain.Controls.Add(txtPassword);
        pnlMain.Controls.Add(chkRemember);
        pnlMain.Controls.Add(btnLogin);
        pnlMain.Controls.Add(linkForgotPassword);
        pnlMain.Controls.Add(linkRegister);
        pnlMain.Controls.Add(lblStatus);

        this.Controls.Add(pnlMain);
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        var pnlMain = this.Controls[0];
        var txtUsername = pnlMain.Controls["txtUsername"] as TextBox;
        var txtPassword = pnlMain.Controls["txtPassword"] as TextBox;
        var chkRemember = pnlMain.Controls["chkRemember"] as CheckBox;
        var lblStatus = pnlMain.Controls["lblStatus"] as Label;

        if (txtUsername == null || txtPassword == null || lblStatus == null)
            return;

        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            lblStatus.Text = "Vui lÃ²ng nháº­p tÃªn Ä‘Äƒng nháº­p vÃ  máº­t kháº©u";
            lblStatus.ForeColor = Color.FromArgb(255, 87, 34);
            return;
        }

        // Perform login
        var (success, message, session) = AuthenticationBLL.Login(username, password);

        if (success && session != null)
        {
            // Remember login if checked
            if (chkRemember?.Checked == true)
                RememberUsername(username);

            lblStatus.Text = message;
            lblStatus.ForeColor = Color.FromArgb(76, 175, 80);

            Log.Information("User logged in: {Username}", username);

            // Navigate to dashboard based on role
            this.Hide();
            OpenDashboard(session.RoleName);
            this.Close();
        }
        else
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = Color.FromArgb(255, 87, 34);
            txtPassword.Clear();
            txtPassword.Focus();
        }
    }

    private void LinkForgotPassword_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        // TODO: Open forgot password form
        MessageBox.Show("TÃ­nh nÄƒng Ä‘áº·t láº¡i máº­t kháº©u sáº½ Ä‘Æ°á»£c cáº­p nháº­t sá»›m", "ThÃ´ng bÃ¡o", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LinkRegister_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        // Open registration form
        var frmRegister = new FrmRegister();
        frmRegister.ShowDialog();
    }

    private void LoadRememberedUsername()
    {
        try
        {
            var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\ApartmentManager\RememberMe");
            if (registryKey != null)
            {
                var username = registryKey.GetValue("Username") as string;
                if (!string.IsNullOrEmpty(username))
                {
                    var pnlMain = this.Controls[0];
                    var txtUsername = pnlMain.Controls["txtUsername"] as TextBox;
                    var chkRemember = pnlMain.Controls["chkRemember"] as CheckBox;

                    if (txtUsername != null)
                        txtUsername.Text = username;
                    if (chkRemember != null)
                        chkRemember.Checked = true;
                }
                registryKey.Close();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load remembered username");
        }
    }

    private void RememberUsername(string username)
    {
        try
        {
            var registryKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\ApartmentManager\RememberMe");
            registryKey.SetValue("Username", username);
            registryKey.SetValue("RememberedAt", DateTime.Now.ToString());
            registryKey.Close();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not save remembered username");
        }
    }

    private void OpenDashboard(string roleName)
    {
        // Navigate to appropriate dashboard based on role
        Form dashboardForm = roleName switch
        {
            "Super Admin" => new FrmMainDashboard(),
            "Manager" => new FrmMainDashboard(),
            "Resident" => new FrmMainDashboard(),
            _ => new FrmMainDashboard()
        };

        dashboardForm.Show();
    }
}



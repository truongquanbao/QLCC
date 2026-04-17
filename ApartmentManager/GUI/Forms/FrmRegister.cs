using ApartmentManager.BLL;
using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.GUI.Forms;

public partial class FrmRegister : Form
{
    public FrmRegister()
    {
        InitializeComponent();
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.Text = "Quản Lý Khu Chung Cư - Đăng Ký";
        this.Size = new Size(600, 700);
    }

    private void FrmRegister_Load(object sender, EventArgs e)
    {
        ConfigureUI();
    }

    private void ConfigureUI()
    {
        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 245, 250),
            AutoScroll = true,
            Padding = new Padding(30)
        };

        int yPos = 10;
        const int fieldHeight = 25;
        const int spacing = 10;
        const int labelWidth = 150;
        const int controlWidth = 300;

        // Title
        var lblTitle = new Label
        {
            Text = "ĐĂNG KÝ TÀI KHOẢN CƯ DÂN",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 86, 155),
            Width = controlWidth,
            Location = new Point(30, yPos)
        };
        pnlMain.Controls.Add(lblTitle);
        yPos += 50;

        // Username
        pnlMain.Controls.Add(CreateLabel("Tên đăng nhập:", 0, yPos));
        var txtUsername = new TextBox
        {
            Name = "txtUsername",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(txtUsername);
        yPos += fieldHeight + spacing;

        // Password
        pnlMain.Controls.Add(CreateLabel("Mật khẩu:", 0, yPos));
        var txtPassword = new TextBox
        {
            Name = "txtPassword",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            UseSystemPasswordChar = true,
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(txtPassword);
        yPos += fieldHeight + spacing;

        // Confirm Password
        pnlMain.Controls.Add(CreateLabel("Xác nhận mật khẩu:", 0, yPos));
        var txtConfirmPassword = new TextBox
        {
            Name = "txtConfirmPassword",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            UseSystemPasswordChar = true,
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(txtConfirmPassword);
        yPos += fieldHeight + spacing;

        // Full Name
        pnlMain.Controls.Add(CreateLabel("Họ tên:", 0, yPos));
        var txtFullName = new TextBox
        {
            Name = "txtFullName",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(txtFullName);
        yPos += fieldHeight + spacing;

        // Email
        pnlMain.Controls.Add(CreateLabel("Email:", 0, yPos));
        var txtEmail = new TextBox
        {
            Name = "txtEmail",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(txtEmail);
        yPos += fieldHeight + spacing;

        // Phone
        pnlMain.Controls.Add(CreateLabel("Số điện thoại:", 0, yPos));
        var txtPhone = new TextBox
        {
            Name = "txtPhone",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(txtPhone);
        yPos += fieldHeight + spacing;

        // CCCD
        pnlMain.Controls.Add(CreateLabel("CCCD/CMND:", 0, yPos));
        var txtCCCD = new TextBox
        {
            Name = "txtCCCD",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(txtCCCD);
        yPos += fieldHeight + spacing + 20;

        // Status message
        var lblStatus = new Label
        {
            Name = "lblStatus",
            Text = "",
            Width = controlWidth + 100,
            Height = 40,
            Location = new Point(0, yPos),
            AutoSize = false,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(255, 87, 34)
        };
        pnlMain.Controls.Add(lblStatus);
        yPos += 50;

        // Buttons
        var pnlButtons = new Panel
        {
            Width = controlWidth + 100,
            Height = 45,
            Location = new Point(0, yPos)
        };

        var btnRegister = new Button
        {
            Text = "ĐĂNG KÝ",
            Width = 150,
            Height = 40,
            Location = new Point(0, 0),
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnRegister.FlatAppearance.BorderSize = 0;
        btnRegister.Click += (s, e) => BtnRegister_Click(s, e, pnlMain, lblStatus);

        var btnCancel = new Button
        {
            Text = "HỦY BỎ",
            Width = 150,
            Height = 40,
            Location = new Point(160, 0),
            BackColor = Color.FromArgb(158, 158, 158),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => this.Close();

        pnlButtons.Controls.Add(btnRegister);
        pnlButtons.Controls.Add(btnCancel);
        pnlMain.Controls.Add(pnlButtons);

        this.Controls.Add(pnlMain);
    }

    private Label CreateLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(64, 64, 64),
            Location = new Point(x, y),
            AutoSize = true
        };
    }

    private void BtnRegister_Click(object? sender, EventArgs e, Panel pnlMain, Label lblStatus)
    {
        var txtUsername = pnlMain.Controls["txtUsername"] as TextBox;
        var txtPassword = pnlMain.Controls["txtPassword"] as TextBox;
        var txtConfirmPassword = pnlMain.Controls["txtConfirmPassword"] as TextBox;
        var txtFullName = pnlMain.Controls["txtFullName"] as TextBox;
        var txtEmail = pnlMain.Controls["txtEmail"] as TextBox;
        var txtPhone = pnlMain.Controls["txtPhone"] as TextBox;
        var txtCCCD = pnlMain.Controls["txtCCCD"] as TextBox;

        if (txtUsername == null || txtPassword == null || txtConfirmPassword == null ||
            txtFullName == null || txtEmail == null || txtPhone == null || txtCCCD == null)
            return;

        // Get values
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;
        string passwordConfirm = txtConfirmPassword.Text;
        string fullName = txtFullName.Text.Trim();
        string email = txtEmail.Text.Trim();
        string phone = txtPhone.Text.Trim();
        string cccd = txtCCCD.Text.Trim();

        // Call BLL
        var (success, message, userID) = AuthenticationBLL.RegisterResident(
            username, password, passwordConfirm, fullName, email, phone, cccd);

        if (success)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
            
            MessageBox.Show(
                "Đăng ký thành công! Tài khoản của bạn đang chờ xác minh từ quản lý khu chung cư.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            this.Close();
        }
        else
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = Color.FromArgb(255, 87, 34);
        }
    }
}

using ApartmentManager.Utilities;
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmDatabaseSetup : Form
{
    public FrmDatabaseSetup()
    {
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Text = "Cấu hình cơ sở dữ liệu";
        Size = new Size(500, 500);

        ConfigureUI();
    }

    private void ConfigureUI()
    {
        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 245, 250),
            Padding = new Padding(30)
        };

        int yPos = 10;
        const int fieldHeight = 25;
        const int spacing = 10;
        const int labelWidth = 120;
        const int controlWidth = 300;

        var lblTitle = new Label
        {
            Text = "CẤU HÌNH KẾT NỐI CSDL",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 86, 155),
            Width = controlWidth,
            Location = new Point(30, yPos)
        };
        pnlMain.Controls.Add(lblTitle);
        yPos += 50;

        var lblServer = new Label { Text = "Máy chủ:", Location = new Point(0, yPos), AutoSize = true };
        var txtServer = new TextBox
        {
            Name = "txtServer",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            Text = "localhost",
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(lblServer);
        pnlMain.Controls.Add(txtServer);
        yPos += fieldHeight + spacing;

        var lblDatabase = new Label { Text = "Cơ sở dữ liệu:", Location = new Point(0, yPos), AutoSize = true };
        var txtDatabase = new TextBox
        {
            Name = "txtDatabase",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            Text = "ApartmentManagerDB",
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(lblDatabase);
        pnlMain.Controls.Add(txtDatabase);
        yPos += fieldHeight + spacing;

        var lblAuth = new Label { Text = "Xác thực:", Location = new Point(0, yPos), AutoSize = true };
        var cmbAuth = new ComboBox
        {
            Name = "cmbAuth",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbAuth.Items.AddRange(new object[] { "Xác thực Windows", "Xác thực SQL Server" });
        cmbAuth.SelectedIndex = 0;
        cmbAuth.SelectedIndexChanged += (s, e) => UpdateAuthFields(pnlMain);
        pnlMain.Controls.Add(lblAuth);
        pnlMain.Controls.Add(cmbAuth);
        yPos += fieldHeight + spacing;

        var lblUsername = new Label
        {
            Name = "lblUsername",
            Text = "Tên người dùng:",
            Location = new Point(0, yPos),
            AutoSize = true,
            Visible = false
        };
        var txtUsername = new TextBox
        {
            Name = "txtUsername",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };
        pnlMain.Controls.Add(lblUsername);
        pnlMain.Controls.Add(txtUsername);
        yPos += fieldHeight + spacing;

        var lblPassword = new Label
        {
            Name = "lblPassword",
            Text = "Mật khẩu:",
            Location = new Point(0, yPos),
            AutoSize = true,
            Visible = false
        };
        var txtPassword = new TextBox
        {
            Name = "txtPassword",
            Width = controlWidth,
            Height = fieldHeight,
            Location = new Point(labelWidth + 20, yPos),
            UseSystemPasswordChar = true,
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };
        pnlMain.Controls.Add(lblPassword);
        pnlMain.Controls.Add(txtPassword);
        yPos += fieldHeight + spacing + 20;

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

        var btnTest = new Button
        {
            Text = "KIỂM TRA KẾT NỐI",
            Width = 150,
            Height = 40,
            Location = new Point(0, yPos),
            BackColor = Color.FromArgb(33, 150, 243),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnTest.FlatAppearance.BorderSize = 0;
        btnTest.Click += (s, e) => BtnTest_Click(s, e, pnlMain, lblStatus);

        var btnSave = new Button
        {
            Text = "LƯU VÀ TIẾP TỤC",
            Width = 150,
            Height = 40,
            Location = new Point(160, yPos),
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (s, e) => BtnSave_Click(s, e, pnlMain);

        pnlMain.Controls.Add(btnTest);
        pnlMain.Controls.Add(btnSave);

        Controls.Add(pnlMain);
    }

    private void UpdateAuthFields(Panel pnlMain)
    {
        var cmbAuth = pnlMain.Controls["cmbAuth"] as ComboBox;
        var lblUsername = pnlMain.Controls["lblUsername"] as Label;
        var txtUsername = pnlMain.Controls["txtUsername"] as TextBox;
        var lblPassword = pnlMain.Controls["lblPassword"] as Label;
        var txtPassword = pnlMain.Controls["txtPassword"] as TextBox;

        if (cmbAuth == null)
        {
            return;
        }

        bool isSqlAuth = cmbAuth.SelectedIndex == 1;
        if (lblUsername != null) lblUsername.Visible = isSqlAuth;
        if (txtUsername != null) txtUsername.Visible = isSqlAuth;
        if (lblPassword != null) lblPassword.Visible = isSqlAuth;
        if (txtPassword != null) txtPassword.Visible = isSqlAuth;
    }

    private void BtnTest_Click(object? sender, EventArgs e, Panel pnlMain, Label lblStatus)
    {
        _ = BuildConnectionString(pnlMain);

        var (success, message) = DatabaseHelper.TestConnection();
        if (success)
        {
            lblStatus.Text = "✓ Kết nối thành công!";
            lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
        }
        else
        {
            lblStatus.Text = "✗ Kết nối thất bại. Kiểm tra thông tin cấu hình.";
            lblStatus.ForeColor = Color.FromArgb(255, 87, 34);
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e, Panel pnlMain)
    {
        _ = BuildConnectionString(pnlMain);

        var (success, message) = DatabaseHelper.TestConnection();
        if (!success)
        {
            MessageBox.Show("Không thể kết nối đến cơ sở dữ liệu. Vui lòng kiểm tra lại thông tin.",
                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("Cấu hình đã được lưu. Ứng dụng sẽ khởi động lại.",
            "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

        Close();
    }

    private string BuildConnectionString(Panel pnlMain)
    {
        var txtServer = pnlMain.Controls["txtServer"] as TextBox;
        var txtDatabase = pnlMain.Controls["txtDatabase"] as TextBox;
        var cmbAuth = pnlMain.Controls["cmbAuth"] as ComboBox;
        var txtUsername = pnlMain.Controls["txtUsername"] as TextBox;
        var txtPassword = pnlMain.Controls["txtPassword"] as TextBox;

        if (txtServer == null || txtDatabase == null || cmbAuth == null)
        {
            return string.Empty;
        }

        string server = txtServer.Text;
        string database = txtDatabase.Text;

        if (cmbAuth.SelectedIndex == 0)
        {
            return $"Server={server};Database={database};Integrated Security=true;Encrypt=False;TrustServerCertificate=True;";
        }

        return $"Server={server};Database={database};User Id={txtUsername?.Text};Password={txtPassword?.Text};Encrypt=False;TrustServerCertificate=True;";
    }
}

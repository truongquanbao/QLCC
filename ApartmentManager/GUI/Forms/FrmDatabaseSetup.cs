using ApartmentManager.Utilities;
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmDatabaseSetup : Form
{
    private readonly TextBox _txtServer = new();
    private readonly TextBox _txtDatabase = new();
    private readonly ComboBox _cmbAuth = new();
    private readonly Label _lblUsername = new();
    private readonly TextBox _txtUsername = new();
    private readonly Label _lblPassword = new();
    private readonly TextBox _txtPassword = new();
    private readonly Label _lblStatus = new();

    public FrmDatabaseSetup()
    {
        Text = "Cấu hình cơ sở dữ liệu";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 470);
        ModernUi.ApplyFormDefaults(this, new Size(560, 470));

        ConfigureUi();
        LoadCurrentSettings();
    }

    private void ConfigureUi()
    {
        var root = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = Padding.Empty,
            BackColor = Color.FromArgb(240, 245, 250)
        };
        Controls.Add(root);

        var title = new Label
        {
            Text = "CẤU HÌNH KẾT NỐI CSDL",
            Font = ModernUi.Font(15f, FontStyle.Bold),
            ForeColor = ModernUi.Navy,
            AutoSize = false,
            Location = new Point(0, 0),
            Size = new Size(480, 32)
        };
        root.Controls.Add(title);

        var description = new Label
        {
            Text = "Dùng màn hình này để cập nhật máy chủ SQL Server và cơ sở dữ liệu đăng nhập.",
            Font = ModernUi.Font(9.5f),
            ForeColor = ModernUi.Muted,
            AutoSize = false,
            Location = new Point(0, 36),
            Size = new Size(500, 36)
        };
        root.Controls.Add(description);

        int labelX = 0;
        int controlX = 160;
        int fieldWidth = 320;
        int y = 92;
        const int rowHeight = 34;
        const int rowGap = 16;

        root.Controls.Add(CreateFieldLabel("Máy chủ:", labelX, y));
        ConfigureTextBox(_txtServer, controlX, y, fieldWidth);
        root.Controls.Add(_txtServer);
        y += rowHeight + rowGap;

        root.Controls.Add(CreateFieldLabel("Cơ sở dữ liệu:", labelX, y));
        ConfigureTextBox(_txtDatabase, controlX, y, fieldWidth);
        root.Controls.Add(_txtDatabase);
        y += rowHeight + rowGap;

        root.Controls.Add(CreateFieldLabel("Xác thực:", labelX, y));
        _cmbAuth.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbAuth.Font = ModernUi.Font(9.5f);
        _cmbAuth.Items.AddRange(new object[] { "Windows", "SQL Server" });
        _cmbAuth.Location = new Point(controlX, y);
        _cmbAuth.Size = new Size(fieldWidth, rowHeight);
        _cmbAuth.SelectedIndexChanged += (_, _) => UpdateAuthFields();
        root.Controls.Add(_cmbAuth);
        y += rowHeight + rowGap;

        _lblUsername.Text = "Tên đăng nhập:";
        _lblUsername.Font = ModernUi.Font(9.5f);
        _lblUsername.ForeColor = ModernUi.Text;
        _lblUsername.Location = new Point(labelX, y + 6);
        _lblUsername.Size = new Size(150, 22);
        root.Controls.Add(_lblUsername);

        ConfigureTextBox(_txtUsername, controlX, y, fieldWidth);
        root.Controls.Add(_txtUsername);
        y += rowHeight + rowGap;

        _lblPassword.Text = "Mật khẩu:";
        _lblPassword.Font = ModernUi.Font(9.5f);
        _lblPassword.ForeColor = ModernUi.Text;
        _lblPassword.Location = new Point(labelX, y + 6);
        _lblPassword.Size = new Size(150, 22);
        root.Controls.Add(_lblPassword);

        ConfigureTextBox(_txtPassword, controlX, y, fieldWidth);
        _txtPassword.UseSystemPasswordChar = true;
        root.Controls.Add(_txtPassword);
        y += rowHeight + rowGap;

        _lblStatus.Font = ModernUi.Font(9f, FontStyle.Bold);
        _lblStatus.ForeColor = ModernUi.Red;
        _lblStatus.Location = new Point(0, y);
        _lblStatus.Size = new Size(500, 40);
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_lblStatus);
        y += 56;

        var btnTest = ModernUi.Button("Kiểm tra kết nối", Color.FromArgb(33, 150, 243), 156, 42);
        btnTest.Location = new Point(0, y);
        btnTest.Click += (_, _) => TestConnection();
        root.Controls.Add(btnTest);

        var btnDefault = ModernUi.OutlineButton("Khôi phục mặc định", 156, 42);
        btnDefault.Location = new Point(168, y);
        btnDefault.Click += (_, _) => ResetToDefaults();
        root.Controls.Add(btnDefault);

        var btnSave = ModernUi.Button("Lưu và đóng", Color.FromArgb(76, 175, 80), 132, 42);
        btnSave.Location = new Point(336, y);
        btnSave.Click += (_, _) => SaveAndClose();
        root.Controls.Add(btnSave);

        var btnClose = ModernUi.OutlineButton("Đóng", 88, 42);
        btnClose.Location = new Point(416, y);
        btnClose.Click += (_, _) => Close();
        root.Controls.Add(btnClose);

        AcceptButton = btnSave;
        CancelButton = btnClose;
    }

    private static Label CreateFieldLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Font = ModernUi.Font(9.5f),
            ForeColor = ModernUi.Text,
            Location = new Point(x, y + 6),
            Size = new Size(150, 22)
        };
    }

    private static void ConfigureTextBox(TextBox textBox, int x, int y, int width)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = ModernUi.Font(9.5f);
        textBox.Location = new Point(x, y);
        textBox.Size = new Size(width, 34);
    }

    private void UpdateAuthFields()
    {
        bool useSqlLogin = _cmbAuth.SelectedIndex == 1;
        _lblUsername.Visible = useSqlLogin;
        _txtUsername.Visible = useSqlLogin;
        _lblPassword.Visible = useSqlLogin;
        _txtPassword.Visible = useSqlLogin;
    }

    private void TestConnection()
    {
        var connectionString = BuildConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            ShowStatus("Vui lòng nhập đầy đủ thông tin kết nối.", false);
            return;
        }

        var (success, message) = DatabaseHelper.TestConnection(connectionString);
        ShowStatus(success ? "Kết nối thành công." : message, success);
    }

    private void SaveAndClose()
    {
        var connectionString = BuildConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ thông tin kết nối.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var (success, message) = DatabaseHelper.TestConnection(connectionString);
        if (!success)
        {
            MessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!DatabaseHelper.SaveConnectionString(connectionString, out var saveMessage))
        {
            MessageBox.Show(saveMessage, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        MessageBox.Show("Cấu hình kết nối đã được lưu thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ResetToDefaults()
    {
        _txtServer.Text = @".\SQLEXPRESS";
        _txtDatabase.Text = "ApartmentManagerDB";
        _cmbAuth.SelectedIndex = 0;
        _txtUsername.Clear();
        _txtPassword.Clear();
        ShowStatus("Đã khôi phục cấu hình mặc định.", true);
    }

    private void ShowStatus(string message, bool success)
    {
        _lblStatus.Text = message;
        _lblStatus.ForeColor = success ? ModernUi.Green : ModernUi.Red;
    }

    private string BuildConnectionString()
    {
        string server = _txtServer.Text.Trim();
        string database = _txtDatabase.Text.Trim();

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
        {
            return string.Empty;
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            Encrypt = false,
            TrustServerCertificate = true
        };

        if (_cmbAuth.SelectedIndex == 0)
        {
            builder.IntegratedSecurity = true;
            return builder.ConnectionString;
        }

        if (string.IsNullOrWhiteSpace(_txtUsername.Text) || string.IsNullOrWhiteSpace(_txtPassword.Text))
        {
            return string.Empty;
        }

        builder.UserID = _txtUsername.Text.Trim();
        builder.Password = _txtPassword.Text;
        builder.IntegratedSecurity = false;
        return builder.ConnectionString;
    }

    private void LoadCurrentSettings()
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(DatabaseHelper.GetConnectionString());
            _txtServer.Text = string.IsNullOrWhiteSpace(builder.DataSource) ? @".\SQLEXPRESS" : builder.DataSource;
            _txtDatabase.Text = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "ApartmentManagerDB" : builder.InitialCatalog;
            _cmbAuth.SelectedIndex = builder.IntegratedSecurity ? 0 : 1;
            _txtUsername.Text = builder.UserID;
            _txtPassword.Text = builder.Password;
            UpdateAuthFields();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load current database settings");
            ResetToDefaults();
        }
    }
}

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
        ClientSize = new Size(720, 430);

        ModernUi.ApplyFormDefaults(this, new Size(720, 430));

        ConfigureUi();
        LoadCurrentSettings();
    }

    private void ConfigureUi()
    {
        Controls.Clear();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(26, 20, 26, 18),
            BackColor = Color.FromArgb(240, 245, 250)
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

        Controls.Add(root);

        root.Controls.Add(CreateHeaderPanel(), 0, 0);
        root.Controls.Add(CreateContentPanel(), 0, 1);
        root.Controls.Add(CreateButtonPanel(), 0, 2);
    }

    private Control CreateHeaderPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        var title = new Label
        {
            Text = "CẤU HÌNH KẾT NỐI CSDL",
            Font = ModernUi.Font(17f, FontStyle.Bold),
            ForeColor = ModernUi.Navy,
            AutoSize = false,
            Location = new Point(0, 0),
            Size = new Size(650, 36)
        };

        var description = new Label
        {
            Text = "Dùng màn hình này để cập nhật máy chủ SQL Server và cơ sở dữ liệu đăng nhập.",
            Font = ModernUi.Font(10f),
            ForeColor = ModernUi.Muted,
            AutoSize = false,
            Location = new Point(0, 42),
            Size = new Size(650, 30)
        };

        panel.Controls.Add(title);
        panel.Controls.Add(description);

        return panel;
    }

    private Control CreateContentPanel()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 230,
            ColumnCount = 2,
            RowCount = 6,
            BackColor = Color.Transparent
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 5; i++)
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        AddRow(table, 0, "Máy chủ:", _txtServer);
        AddRow(table, 1, "Cơ sở dữ liệu:", _txtDatabase);

        _cmbAuth.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbAuth.Font = ModernUi.Font(10f);
        _cmbAuth.Items.AddRange(new object[] { "Windows", "SQL Server" });
        _cmbAuth.SelectedIndexChanged += (_, _) => UpdateAuthFields();

        AddRow(table, 2, "Xác thực:", _cmbAuth);

        _lblUsername.Text = "Tên đăng nhập:";
        ConfigureLabel(_lblUsername);
        ConfigureTextBox(_txtUsername);

        table.Controls.Add(_lblUsername, 0, 3);
        table.Controls.Add(_txtUsername, 1, 3);

        _lblPassword.Text = "Mật khẩu:";
        ConfigureLabel(_lblPassword);
        ConfigureTextBox(_txtPassword);
        _txtPassword.UseSystemPasswordChar = true;

        table.Controls.Add(_lblPassword, 0, 4);
        table.Controls.Add(_txtPassword, 1, 4);

        _lblStatus.Font = ModernUi.Font(9.5f, FontStyle.Bold);
        _lblStatus.ForeColor = ModernUi.Red;
        _lblStatus.Dock = DockStyle.Fill;
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;

        table.Controls.Add(_lblStatus, 0, 5);
        table.SetColumnSpan(_lblStatus, 2);

        return table;
    }

    private Control CreateButtonPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = Color.Transparent
        };

        var btnClose = ModernUi.OutlineButton("Đóng", 95, 38);
        btnClose.Margin = new Padding(8, 0, 0, 0);
        btnClose.Click += (_, _) => Close();

        var btnSave = ModernUi.Button("Lưu và đóng", Color.FromArgb(76, 175, 80), 145, 38);
        btnSave.Margin = new Padding(8, 0, 0, 0);
        btnSave.Click += (_, _) => SaveAndClose();

        var btnDefault = ModernUi.OutlineButton("Khôi phục mặc định", 165, 38);
        btnDefault.Margin = new Padding(8, 0, 0, 0);
        btnDefault.Click += (_, _) => ResetToDefaults();

        var btnTest = ModernUi.Button("Kiểm tra kết nối", Color.FromArgb(33, 150, 243), 155, 38);
        btnTest.Margin = new Padding(0);
        btnTest.Click += (_, _) => TestConnection();

        panel.Controls.Add(btnClose);
        panel.Controls.Add(btnSave);
        panel.Controls.Add(btnDefault);
        panel.Controls.Add(btnTest);

        AcceptButton = btnSave;
        CancelButton = btnClose;

        return panel;
    }

    private static void AddRow(TableLayoutPanel table, int row, string labelText, Control input)
    {
        var label = new Label
        {
            Text = labelText,
            Font = ModernUi.Font(10f),
            ForeColor = ModernUi.Text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        if (input is TextBox textBox)
            ConfigureTextBox(textBox);

        if (input is ComboBox comboBox)
        {
            comboBox.Dock = DockStyle.Fill;
            comboBox.Margin = new Padding(0, 7, 0, 7);
        }

        table.Controls.Add(label, 0, row);
        table.Controls.Add(input, 1, row);
    }

    private static void ConfigureLabel(Label label)
    {
        label.Font = ModernUi.Font(10f);
        label.ForeColor = ModernUi.Text;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static void ConfigureTextBox(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = ModernUi.Font(10f);
        textBox.Dock = DockStyle.Fill;
        textBox.Margin = new Padding(0, 7, 0, 7);
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
            return string.Empty;

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
            return string.Empty;

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

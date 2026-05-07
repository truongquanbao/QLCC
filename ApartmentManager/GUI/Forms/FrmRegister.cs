using ApartmentManager.BLL;
using Serilog;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmRegister : Form
{
    private TextBox _txtUsername = null!;
    private TextBox _txtPassword = null!;
    private TextBox _txtConfirmPassword = null!;
    private TextBox _txtFullName = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtPhone = null!;
    private TextBox _txtCCCD = null!;
    private TextBox _txtApartmentCode = null!;
    private CheckBox _chkTerms = null!;
    private Label _lblStatus = null!;
    private Button _btnRegister = null!;

    public FrmRegister()
    {
        Text = "Phần mềm quản lý khu chung cư - Đăng ký";
        Size = new Size(1280, 720);
        MinimumSize = new Size(1100, 640);
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;

        ModernUi.ApplyFormDefaults(this, new Size(1100, 640));

        ConfigureUI();
    }

    private void ConfigureUI()
    {
        Controls.Clear();

        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = ModernUi.Navy
        };

        var appTitle = ModernUi.Label("▦  PHẦN MỀM QUẢN LÝ KHU CHUNG CƯ", 16f, FontStyle.Bold, Color.White);
        appTitle.Location = new Point(18, 8);
        appTitle.Size = new Size(620, 34);
        topBar.Controls.Add(appTitle);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ModernUi.Surface
        };

        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        body.Controls.Add(CreateFormCard(), 0, 0);
        body.Controls.Add(CreateProcessCard(), 1, 0);

        Controls.Add(body);
        Controls.Add(topBar);
    }

    private Control CreateFormCard()
    {
        var card = ModernUi.CardPanel(8);
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(0, 0, 14, 0);
        card.Padding = new Padding(20);
        card.AutoScroll = true;

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Color.Transparent
        };

        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));   // header
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 330));  // inputs
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // checkbox
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));   // warning
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));   // status
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // buttons

        card.Controls.Add(main);

        main.Controls.Add(CreateHeaderPanel(), 0, 0);
        main.Controls.Add(CreateInputPanel(), 0, 1);

        _chkTerms = new CheckBox
        {
            Text = "Tôi đã đọc và đồng ý với Điều khoản sử dụng và Chính sách bảo mật của hệ thống",
            Font = ModernUi.Font(10f),
            ForeColor = ModernUi.Text,
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 4, 4, 4),
            AutoSize = false
        };
        _chkTerms.CheckedChanged += (_, _) => _btnRegister.Enabled = _chkTerms.Checked;
        main.Controls.Add(_chkTerms, 0, 2);

        main.Controls.Add(CreateWarningPanel(), 0, 3);

        _lblStatus = ModernUi.Label("", 9.5f, FontStyle.Bold, ModernUi.Red);
        _lblStatus.Dock = DockStyle.Fill;
        _lblStatus.Padding = new Padding(6, 4, 6, 0);
        main.Controls.Add(_lblStatus, 0, 4);

        main.Controls.Add(CreateActionPanel(), 0, 5);


        // ACTION BUTTONS
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 6, 8, 6),
            WrapContents = false,
            AutoSize = false
        };

        _btnRegister = ModernUi.Button("Đăng ký", ModernUi.Blue, 170, 36);
        _btnRegister.Font = ModernUi.Font(10.5f, FontStyle.Bold);
        _btnRegister.Enabled = false;
        _btnRegister.Click += BtnRegister_Click;

        var btnRefresh = ModernUi.Button("Làm mới", Color.FromArgb(108, 117, 125), 130, 36);
        btnRefresh.Font = ModernUi.Font(10f, FontStyle.Bold);
        btnRefresh.Click += (_, _) => ClearInputs();

        var btnBack = ModernUi.OutlineButton("Quay lại đăng nhập", 170, 36);
        btnBack.Font = ModernUi.Font(10f, FontStyle.Bold);
        btnBack.Click += (_, _) => Close();

        buttonPanel.Controls.Add(btnBack);
        buttonPanel.Controls.Add(btnRefresh);
        buttonPanel.Controls.Add(_btnRegister);

        card.Controls.Add(buttonPanel);
        buttonPanel.BringToFront();

        return card;
    }

    private Control CreateHeaderPanel()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 4, 4, 4)
        };

        var icon = new CircleLabel
        {
            Text = "●+",
            CircleColor = ModernUi.Blue,
            ForeColor = Color.White,
            Font = ModernUi.Font(18f, FontStyle.Bold),
            Size = new Size(58, 58),
            Location = new Point(4, 8)
        };
        header.Controls.Add(icon);

        var title = ModernUi.Label("ĐĂNG KÝ TÀI KHOẢN CƯ DÂN", 18f, FontStyle.Bold, ModernUi.Blue);
        title.Location = new Point(82, 10);
        title.Size = new Size(620, 32);
        header.Controls.Add(title);

        var subtitle = ModernUi.Label("Vui lòng điền đầy đủ thông tin để đăng ký tài khoản cư dân", 10f, FontStyle.Regular, ModernUi.Muted);
        subtitle.Location = new Point(84, 45);
        subtitle.Size = new Size(620, 24);
        header.Controls.Add(subtitle);

        return header;
    }

    private Control CreateInputPanel()
    {
        var inputsTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(0, 4, 0, 4),
            BackColor = Color.Transparent
        };

        inputsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        inputsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        for (int i = 0; i < 5; i++)
            inputsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

        Panel MakeCell()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 2, 12, 2)
            };
        }

        var cell00 = MakeCell();
        var cell01 = MakeCell();
        _txtUsername = AddInput(cell00, "Tên đăng nhập *", "Nhập tên đăng nhập");
        _txtPhone = AddInput(cell01, "Số điện thoại *", "Nhập số điện thoại");
        inputsTable.Controls.Add(cell00, 0, 0);
        inputsTable.Controls.Add(cell01, 1, 0);

        var cell10 = MakeCell();
        var cell11 = MakeCell();
        _txtPassword = AddInput(cell10, "Mật khẩu *", "Nhập mật khẩu", true);
        _txtCCCD = AddInput(cell11, "CCCD/CMND *", "Nhập số CCCD/CMND");
        inputsTable.Controls.Add(cell10, 0, 1);
        inputsTable.Controls.Add(cell11, 1, 1);

        var cell20 = MakeCell();
        var cell21 = MakeCell();
        _txtConfirmPassword = AddInput(cell20, "Xác nhận mật khẩu *", "Nhập lại mật khẩu", true);
        _txtApartmentCode = AddInput(cell21, "Mã căn hộ hoặc căn hộ liên kết *", "Nhập mã căn hộ hoặc căn hộ liên kết");
        inputsTable.Controls.Add(cell20, 0, 2);
        inputsTable.Controls.Add(cell21, 1, 2);

        var cell30 = MakeCell();
        var cell31 = MakeCell();
        _txtFullName = AddInput(cell30, "Họ tên *", "Nhập họ và tên");

        var accountType = AddInput(cell31, "Loại tài khoản", "Cư dân");
        accountType.Text = "Cư dân";
        accountType.ReadOnly = true;
        accountType.BackColor = Color.FromArgb(247, 249, 252);

        inputsTable.Controls.Add(cell30, 0, 3);
        inputsTable.Controls.Add(cell31, 1, 3);

        var cell40 = MakeCell();
        _txtEmail = AddInput(cell40, "Email *", "Nhập địa chỉ email");
        inputsTable.Controls.Add(cell40, 0, 4);
        inputsTable.SetColumnSpan(cell40, 2);

        return inputsTable;
    }

    private static TextBox AddInput(Control parent, string labelText, string placeholder, bool password = false)
    {
        var label = ModernUi.Label(labelText, 10f, FontStyle.Regular, ModernUi.Text);
        label.Dock = DockStyle.Top;
        label.Height = 24;
        parent.Controls.Add(label);

        var input = ModernUi.TextBox(placeholder, 300);
        input.Dock = DockStyle.Top;
        input.Height = 36;
        input.Margin = new Padding(0, 4, 0, 0);
        input.UseSystemPasswordChar = password;
        parent.Controls.Add(input);

        input.BringToFront();
        label.BringToFront();

        return input;
    }

    private Control CreateWarningPanel()
    {
        var warning = new RoundedPanel
        {
            Radius = 7,
            BackColor = Color.FromArgb(255, 248, 230),
            BorderColor = Color.FromArgb(244, 210, 133),
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(8, 4, 12, 4)
        };

        var warnLbl = ModernUi.Label(
            "⚠  Tài khoản cư dân sau khi đăng ký sẽ ở trạng thái chờ phê duyệt.",
            10.5f,
            FontStyle.Bold,
            Color.FromArgb(156, 88, 0));

        warnLbl.Dock = DockStyle.Fill;
        warnLbl.TextAlign = ContentAlignment.MiddleLeft;
        warning.Controls.Add(warnLbl);

        return warning;
    }

    private Control CreateActionPanel()
    {
        var wrapper = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 12, 12, 0)
        };

        var actions = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Top,
            Height = 54,
            WrapContents = false
        };

        _btnRegister = ModernUi.Button("●+  Đăng ký", ModernUi.Blue, 300, 46);
        _btnRegister.Font = ModernUi.Font(12f, FontStyle.Bold);
        _btnRegister.Enabled = false;
        _btnRegister.Click += BtnRegister_Click;

        var btnReset = ModernUi.Button("↻  Làm mới", Color.FromArgb(116, 125, 138), 150, 46);
        btnReset.Font = ModernUi.Font(12f, FontStyle.Bold);
        btnReset.Click += (_, _) => ClearInputs();

        var btnBack = ModernUi.OutlineButton("←  Quay lại đăng nhập", 180, 46);
        btnBack.Font = ModernUi.Font(11f, FontStyle.Bold);
        btnBack.Click += (_, _) => Close();

        actions.Controls.Add(btnBack);
        actions.Controls.Add(btnReset);
        actions.Controls.Add(_btnRegister);

        wrapper.Controls.Add(actions);
        return wrapper;
    }

    private Control CreateProcessCard()
    {
        var panel = ModernUi.CardPanel(8);
        panel.Dock = DockStyle.Fill;
        panel.BackColor = Color.FromArgb(242, 247, 254);
        panel.Padding = new Padding(22);
        panel.Margin = new Padding(0);

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };

        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));

        panel.Controls.Add(main);

        var title = ModernUi.Label("QUY TRÌNH ĐĂNG KÝ", 12f, FontStyle.Bold, ModernUi.Blue);
        title.Dock = DockStyle.Fill;
        title.TextAlign = ContentAlignment.MiddleCenter;
        main.Controls.Add(title, 0, 0);

        var stepsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 8, 4, 8)
        };

        AddStep(stepsPanel, 1, "▤", ModernUi.Blue, "Đăng ký", "Cư dân điền thông tin đăng ký tài khoản và gửi yêu cầu.", 20);
        AddStep(stepsPanel, 2, "⌛", ModernUi.Orange, "Chờ duyệt", "Ban quản lý sẽ kiểm tra thông tin và phê duyệt tài khoản.", 150);
        AddStep(stepsPanel, 3, "✓", ModernUi.Green, "Kích hoạt", "Tài khoản được kích hoạt. Cư dân có thể đăng nhập và sử dụng hệ thống.", 280);

        main.Controls.Add(stepsPanel, 0, 1);

        var security = new RoundedPanel
        {
            Radius = 8,
            BackColor = Color.FromArgb(238, 247, 255),
            BorderColor = Color.FromArgb(190, 214, 245),
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            Margin = new Padding(6, 8, 6, 0)
        };

        var securityTitle = ModernUi.Label("◆  Thông tin bảo mật", 10f, FontStyle.Bold, ModernUi.Blue);
        securityTitle.Dock = DockStyle.Top;
        securityTitle.Height = 28;
        security.Controls.Add(securityTitle);

        var securityText = ModernUi.Label(
            "Thông tin của bạn được bảo mật tuyệt đối\r\nvà chỉ sử dụng cho mục đích quản lý khu chung cư.",
            9.5f,
            FontStyle.Regular,
            ModernUi.Text);

        securityText.Dock = DockStyle.Fill;
        securityText.TextAlign = ContentAlignment.TopLeft;
        security.Controls.Add(securityText);
        securityText.BringToFront();

        main.Controls.Add(security, 0, 2);

        return panel;
    }

    private static void AddStep(Control parent, int number, string icon, Color color, string title, string body, int y)
    {
        var circle = new CircleLabel
        {
            Text = icon,
            CircleColor = color,
            ForeColor = Color.White,
            Font = ModernUi.Font(18f, FontStyle.Bold),
            Location = new Point(18, y),
            Size = new Size(52, 52)
        };
        parent.Controls.Add(circle);

        if (number < 3)
        {
            var line = new Panel
            {
                BackColor = Color.FromArgb(151, 181, 220),
                Location = new Point(43, y + 56),
                Size = new Size(2, 62)
            };
            parent.Controls.Add(line);
            line.SendToBack();
        }

        var stepTitle = ModernUi.Label($"{number}. {title}", 11f, FontStyle.Bold, ModernUi.Text);
        stepTitle.Location = new Point(88, y + 2);
        stepTitle.Size = new Size(210, 26);
        parent.Controls.Add(stepTitle);

        var stepBody = ModernUi.Label(body, 9f, FontStyle.Regular, ModernUi.Text);
        stepBody.Location = new Point(88, y + 32);
        stepBody.Size = new Size(230, 58);
        stepBody.AutoSize = false;
        parent.Controls.Add(stepBody);
    }

    private void BtnRegister_Click(object? sender, EventArgs e)
    {
        if (!_chkTerms.Checked)
        {
            _lblStatus.Text = "Bạn cần đồng ý với điều khoản sử dụng và chính sách bảo mật.";
            _lblStatus.ForeColor = ModernUi.Red;
            return;
        }

        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text;
        var passwordConfirm = _txtConfirmPassword.Text;
        var fullName = _txtFullName.Text.Trim();
        var email = _txtEmail.Text.Trim();
        var phone = _txtPhone.Text.Trim();
        var cccd = _txtCCCD.Text.Trim();

        var (success, message, _) = AuthenticationBLL.RegisterResident(username, password, passwordConfirm, fullName, email, phone, cccd);

        _lblStatus.Text = message;
        _lblStatus.ForeColor = success ? ModernUi.Green : ModernUi.Red;

        if (success)
        {
            Log.Information("Resident registration UI completed: {Username}", username);

            MessageBox.Show(
                "Đăng ký thành công! Tài khoản của bạn đang chờ Ban quản lý phê duyệt.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Close();
        }
    }

    private void ClearInputs()
    {
        foreach (var textBox in new[]
        {
            _txtUsername,
            _txtPassword,
            _txtConfirmPassword,
            _txtFullName,
            _txtEmail,
            _txtPhone,
            _txtCCCD,
            _txtApartmentCode
        })
        {
            textBox.Clear();
        }

        _chkTerms.Checked = false;
        _btnRegister.Enabled = false;
        _lblStatus.Text = "";
        _txtUsername.Focus();
    }
}

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
        Controls.Add(topBar);

        var title = ModernUi.Label("▦  PHẦN MỀM QUẢN LÝ KHU CHUNG CƯ", 16f, FontStyle.Bold, Color.White);
        title.Location = new Point(18, 8);
        title.Size = new Size(560, 34);
        topBar.Controls.Add(title);

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
        Controls.Add(body);
        body.BringToFront();
        topBar.BringToFront();

        body.Controls.Add(CreateFormCard(), 0, 0);
        body.Controls.Add(CreateProcessCard(), 1, 0);
    }

    private Control CreateFormCard()
    {
        var card = ModernUi.CardPanel(8);
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(0, 0, 14, 0);
        card.Padding = new Padding(22);

        var icon = new CircleLabel
        {
            Text = "●+",
            CircleColor = ModernUi.Blue,
            ForeColor = Color.White,
            Font = ModernUi.Font(18f, FontStyle.Bold),
            Size = new Size(58, 58),
            Location = new Point(22, 24)
        };
        card.Controls.Add(icon);

        var title = ModernUi.Label("ĐĂNG KÝ TÀI KHOẢN CƯ DÂN", 18f, FontStyle.Bold, ModernUi.Blue);
        title.Location = new Point(94, 26);
        title.Size = new Size(520, 30);
        card.Controls.Add(title);

        var subtitle = ModernUi.Label("Vui lòng điền đầy đủ thông tin để đăng ký tài khoản cư dân", 10f, FontStyle.Regular, ModernUi.Muted);
        subtitle.Location = new Point(95, 56);
        subtitle.Size = new Size(520, 24);
        card.Controls.Add(subtitle);

        int leftX = 22;
        int rightX = 486;
        int y = 114;
        int fieldW = 414;

        _txtUsername = AddInput(card, "Tên đăng nhập *", "Nhập tên đăng nhập", leftX, y, fieldW);
        _txtPhone = AddInput(card, "Số điện thoại *", "Nhập số điện thoại", rightX, y, fieldW);
        y += 82;

        _txtPassword = AddInput(card, "Mật khẩu *", "Nhập mật khẩu", leftX, y, fieldW, true);
        _txtCCCD = AddInput(card, "CCCD/CMND *", "Nhập số CCCD/CMND", rightX, y, fieldW);
        y += 82;

        _txtConfirmPassword = AddInput(card, "Xác nhận mật khẩu *", "Nhập lại mật khẩu", leftX, y, fieldW, true);
        _txtApartmentCode = AddInput(card, "Mã căn hộ hoặc căn hộ liên kết *", "Nhập mã căn hộ hoặc căn hộ liên kết", rightX, y, fieldW);
        y += 82;

        _txtFullName = AddInput(card, "Họ tên *", "Nhập họ và tên", leftX, y, fieldW);
        var accountType = AddInput(card, "Loại tài khoản", "Cư dân", rightX, y, fieldW);
        accountType.ReadOnly = true;
        accountType.BackColor = Color.FromArgb(247, 249, 252);
        y += 82;

        _txtEmail = AddInput(card, "Email *", "Nhập địa chỉ email", leftX, y, fieldW * 2 + 50);
        y += 78;

        _chkTerms = new CheckBox
        {
            Text = "Tôi đã đọc và đồng ý với Điều khoản sử dụng và Chính sách bảo mật của hệ thống",
            Font = ModernUi.Font(10f),
            ForeColor = ModernUi.Text,
            Location = new Point(leftX, y),
            Size = new Size(760, 28)
        };
        card.Controls.Add(_chkTerms);
        y += 48;

        var warning = new RoundedPanel
        {
            Radius = 7,
            BackColor = Color.FromArgb(255, 248, 230),
            BorderColor = Color.FromArgb(244, 210, 133),
            Location = new Point(leftX, y),
            Size = new Size(900, 48)
        };
        var warningText = ModernUi.Label("△   Tài khoản cư dân sau khi đăng ký sẽ ở trạng thái chờ phê duyệt.", 10.5f, FontStyle.Bold, Color.FromArgb(156, 88, 0));
        warningText.Dock = DockStyle.Fill;
        warningText.Padding = new Padding(18, 0, 0, 0);
        warning.Controls.Add(warningText);
        card.Controls.Add(warning);
        y += 76;

        var register = ModernUi.Button("●+  Đăng ký", ModernUi.Blue, 300, 48);
        register.Font = ModernUi.Font(12f, FontStyle.Bold);
        register.Location = new Point(leftX, y);
        register.Click += BtnRegister_Click;
        card.Controls.Add(register);
        AcceptButton = register;

        var reset = ModernUi.Button("↻  Làm mới", Color.FromArgb(116, 125, 138), 300, 48);
        reset.Font = ModernUi.Font(12f, FontStyle.Bold);
        reset.Location = new Point(leftX + 330, y);
        reset.Click += (_, _) => ClearInputs();
        card.Controls.Add(reset);

        var back = ModernUi.OutlineButton("←  Quay lại đăng nhập", 220, 48);
        back.Font = ModernUi.Font(11f, FontStyle.Bold);
        back.Location = new Point(leftX + 676, y);
        back.Click += (_, _) => Close();
        card.Controls.Add(back);

        _lblStatus = ModernUi.Label("", 9.5f, FontStyle.Bold, ModernUi.Red);
        _lblStatus.Location = new Point(leftX, y + 56);
        _lblStatus.Size = new Size(880, 30);
        card.Controls.Add(_lblStatus);

        return card;
    }

    private static TextBox AddInput(Control parent, string labelText, string placeholder, int x, int y, int width, bool password = false)
    {
        var label = ModernUi.Label(labelText, 10f, FontStyle.Regular, ModernUi.Text);
        label.Location = new Point(x, y);
        label.Size = new Size(width, 24);
        parent.Controls.Add(label);

        var input = ModernUi.TextBox(placeholder, width);
        input.Location = new Point(x, y + 30);
        input.Height = 40;
        input.UseSystemPasswordChar = password;
        parent.Controls.Add(input);
        return input;
    }

    private Control CreateProcessCard()
    {
        var panel = ModernUi.CardPanel(8);
        panel.Dock = DockStyle.Fill;
        panel.BackColor = Color.FromArgb(242, 247, 254);
        panel.Padding = new Padding(22);

        var title = ModernUi.Label("QUY TRÌNH ĐĂNG KÝ", 12f, FontStyle.Bold, ModernUi.Blue);
        title.Location = new Point(22, 28);
        title.Size = new Size(270, 28);
        title.TextAlign = ContentAlignment.MiddleCenter;
        panel.Controls.Add(title);

        AddStep(panel, 1, "▤", ModernUi.Blue, "Đăng ký", "Cư dân điền thông tin đăng ký tài khoản và gửi yêu cầu.", 96);
        AddStep(panel, 2, "⌛", ModernUi.Orange, "Chờ duyệt", "Ban quản lý sẽ kiểm tra thông tin và phê duyệt tài khoản.", 238);
        AddStep(panel, 3, "✓", ModernUi.Green, "Kích hoạt", "Tài khoản được kích hoạt. Cư dân có thể đăng nhập và sử dụng hệ thống.", 380);

        var security = new RoundedPanel
        {
            Radius = 8,
            BackColor = Color.FromArgb(238, 247, 255),
            BorderColor = Color.FromArgb(190, 214, 245),
            Location = new Point(22, 552),
            Size = new Size(292, 104),
            Padding = new Padding(18)
        };
        var securityTitle = ModernUi.Label("◆  Thông tin bảo mật", 10f, FontStyle.Bold, ModernUi.Blue);
        securityTitle.Location = new Point(16, 10);
        securityTitle.Size = new Size(250, 26);
        security.Controls.Add(securityTitle);

        var securityText = ModernUi.Label("Thông tin của bạn được bảo mật tuyệt đối\r\nvà chỉ sử dụng cho mục đích quản lý\r\nkhu chung cư.", 9.5f, FontStyle.Regular, ModernUi.Text);
        securityText.Location = new Point(16, 40);
        securityText.Size = new Size(250, 58);
        security.Controls.Add(securityText);
        panel.Controls.Add(security);

        return panel;
    }

    private static void AddStep(Control parent, int number, string icon, Color color, string title, string body, int y)
    {
        var circle = new CircleLabel
        {
            Text = icon,
            CircleColor = color,
            ForeColor = Color.White,
            Font = ModernUi.Font(22f, FontStyle.Bold),
            Location = new Point(48, y),
            Size = new Size(78, 78)
        };
        parent.Controls.Add(circle);

        if (number < 3)
        {
            var line = new Panel
            {
                BackColor = Color.FromArgb(151, 181, 220),
                Location = new Point(86, y + 80),
                Size = new Size(2, 58)
            };
            parent.Controls.Add(line);
        }

        var stepTitle = ModernUi.Label($"{number}. {title}", 12f, FontStyle.Bold, ModernUi.Text);
        stepTitle.Location = new Point(154, y + 6);
        stepTitle.Size = new Size(170, 28);
        parent.Controls.Add(stepTitle);

        var stepBody = ModernUi.Label(body, 9.5f, FontStyle.Regular, ModernUi.Text);
        stepBody.Location = new Point(154, y + 38);
        stepBody.Size = new Size(170, 66);
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
        foreach (var textBox in new[] { _txtUsername, _txtPassword, _txtConfirmPassword, _txtFullName, _txtEmail, _txtPhone, _txtCCCD, _txtApartmentCode })
        {
            textBox.Clear();
        }

        _chkTerms.Checked = false;
        _lblStatus.Text = "";
        _txtUsername.Focus();
    }
}

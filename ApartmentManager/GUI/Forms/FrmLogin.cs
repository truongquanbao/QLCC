using ApartmentManager.BLL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmLogin : Form
{
    private TextBox _txtUsername = null!;
    private TextBox _txtPassword = null!;
    private CheckBox _chkRemember = null!;
    private Label _lblStatus = null!;
    private Label _lblUsernameError = null!;
    private Label _lblPasswordError = null!;

    public FrmLogin()
    {
        Text = "Phần mềm quản lý khu chung cư - Đăng nhập";
        Size = new Size(1440, 810);
        MinimumSize = new Size(1180, 700);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = true;
        MinimizeBox = true;
        ModernUi.ApplyFormDefaults(this, new Size(1180, 700));

        ConfigureUI();
        LoadRememberedUsername();
    }

    private void ConfigureUI()
    {
        Controls.Clear();

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = ModernUi.Surface
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53f));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47f));
        Controls.Add(body);

        var footer = CreateFooter();
        Controls.Add(footer);
        footer.BringToFront();

        body.Controls.Add(CreateHero(), 0, 0);
        body.Controls.Add(CreateLoginSurface(), 1, 0);
    }

    private Panel CreateFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            BackColor = ModernUi.Navy
        };

        var version = ModernUi.Label("▰  Phiên bản: 1.0.0.0", 11f, FontStyle.Regular, Color.White);
        version.Location = new Point(24, 10);
        version.Size = new Size(260, 34);
        footer.Controls.Add(version);

        var connection = ModernUi.Label("●  Kết nối: Đã kết nối", 11f, FontStyle.Bold, Color.FromArgb(95, 220, 88));
        connection.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        connection.Location = new Point(footer.Width - 300, 10);
        connection.Size = new Size(230, 34);
        footer.Controls.Add(connection);

        var divider = new Panel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.FromArgb(36, 83, 145),
            Location = new Point(footer.Width - 70, 12),
            Size = new Size(1, 30)
        };
        footer.Controls.Add(divider);

        var gear = ModernUi.Label("⚙", 17f, FontStyle.Bold, Color.White);
        gear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        gear.Location = new Point(footer.Width - 52, 10);
        gear.Size = new Size(34, 34);
        gear.TextAlign = ContentAlignment.MiddleCenter;
        footer.Controls.Add(gear);

        footer.Resize += (_, _) =>
        {
            connection.Left = footer.ClientSize.Width - 300;
            divider.Left = footer.ClientSize.Width - 70;
            gear.Left = footer.ClientSize.Width - 52;
        };

        return footer;
    }

    private Control CreateHero()
    {
        var hero = new LoginHeroPanel { Dock = DockStyle.Fill };

        var logo = new RoundedPanel
        {
            Radius = 8,
            BackColor = Color.FromArgb(4, 74, 150),
            BorderColor = Color.FromArgb(88, 145, 215)
        };
        var logoText = ModernUi.Label("▦", 34f, FontStyle.Bold, Color.White);
        logoText.Dock = DockStyle.Fill;
        logoText.TextAlign = ContentAlignment.MiddleCenter;
        logo.Controls.Add(logoText);
        hero.Controls.Add(logo);

        var title = ModernUi.Label("PHẦN MỀM\r\nQUẢN LÝ KHU CHUNG CƯ", 28f, FontStyle.Bold, Color.White);
        title.TextAlign = ContentAlignment.MiddleCenter;
        hero.Controls.Add(title);

        var line = new Panel { BackColor = Color.FromArgb(130, 177, 230), Height = 1 };
        hero.Controls.Add(line);

        var dot = new CircleLabel
        {
            CircleColor = Color.White,
            Text = ""
        };
        hero.Controls.Add(dot);

        var subtitle = ModernUi.Label(
            "Giải pháp toàn diện giúp quản lý cư dân,\r\ncăn hộ, hóa đơn và phản ánh hiệu quả.",
            13.5f,
            FontStyle.Regular,
            Color.White);
        subtitle.TextAlign = ContentAlignment.MiddleCenter;
        hero.Controls.Add(subtitle);

        var featureLabels = new[] { "Quản lý cư dân", "Quản lý căn hộ", "Quản lý hóa đơn", "Tiếp nhận phản ánh" };
        var featureIcons = new[] { "●●", "⌂", "▤", "■" };
        var features = new Panel[featureLabels.Length];
        for (int i = 0; i < featureLabels.Length; i++)
        {
            var feature = new Panel { BackColor = Color.Transparent };
            var featureIcon = new RoundedPanel
            {
                Radius = 7,
                BackColor = Color.FromArgb(28, 88, 165),
                BorderColor = Color.FromArgb(59, 119, 196),
                Size = new Size(58, 48)
            };
            var iconText = ModernUi.Label(featureIcons[i], 17f, FontStyle.Bold, Color.White);
            iconText.Dock = DockStyle.Fill;
            iconText.TextAlign = ContentAlignment.MiddleCenter;
            featureIcon.Controls.Add(iconText);
            feature.Controls.Add(featureIcon);

            var label = ModernUi.Label(featureLabels[i], 9f, FontStyle.Bold, Color.White);
            label.TextAlign = ContentAlignment.MiddleCenter;
            feature.Controls.Add(label);

            features[i] = feature;
            hero.Controls.Add(feature);
        }

        hero.Resize += (_, _) =>
        {
            int w = hero.ClientSize.Width;
            int h = hero.ClientSize.Height;
            float scale = Math.Max(0.78f, Math.Min(1.28f, w / 760f));

            logo.Size = new Size((int)(96 * scale), (int)(74 * scale));
            logo.Location = new Point((w - logo.Width) / 2, (int)(64 * scale));
            logoText.Font = ModernUi.Font(32f * scale, FontStyle.Bold);

            title.Font = ModernUi.Font(28f * scale, FontStyle.Bold);
            title.Location = new Point(28, (int)(178 * scale));
            title.Size = new Size(w - 56, (int)(118 * scale));

            int lineWidth = Math.Min((int)(380 * scale), w - 180);
            line.Location = new Point((w - lineWidth) / 2, (int)(323 * scale));
            line.Size = new Size(lineWidth, 1);
            dot.Size = new Size((int)(10 * scale), (int)(10 * scale));
            dot.Location = new Point((w - dot.Width) / 2, line.Top - dot.Height / 2);

            subtitle.Font = ModernUi.Font(13.5f * scale);
            subtitle.Location = new Point((w - (int)(480 * scale)) / 2, (int)(354 * scale));
            subtitle.Size = new Size((int)(480 * scale), (int)(64 * scale));

            int featureTop = h - (int)(102 * scale);
            int slot = w / 4;
            for (int i = 0; i < features.Length; i++)
            {
                features[i].Size = new Size(slot, (int)(82 * scale));
                features[i].Location = new Point(i * slot, featureTop);

                var icon = features[i].Controls[0];
                icon.Size = new Size((int)(58 * scale), (int)(48 * scale));
                icon.Location = new Point((slot - icon.Width) / 2, 0);
                icon.Controls[0].Font = ModernUi.Font(17f * scale, FontStyle.Bold);

                var label = features[i].Controls[1];
                label.Font = ModernUi.Font(9f * scale, FontStyle.Bold);
                label.Location = new Point(0, icon.Bottom + 8);
                label.Size = new Size(slot, 24);
            }
        };

        return hero;
    }

    private Control CreateLoginSurface()
    {
        var surface = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernUi.Surface
        };

        var card = ModernUi.CardPanel(8);
        card.BackColor = Color.White;
        card.BorderColor = Color.FromArgb(226, 232, 240);
        surface.Controls.Add(card);

        var lockIcon = new CircleLabel
        {
            Text = "□",
            CircleColor = ModernUi.Blue,
            ForeColor = Color.White,
            Font = ModernUi.Font(25f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(lockIcon);

        var title = ModernUi.Label("ĐĂNG NHẬP", 20f, FontStyle.Bold, ModernUi.Navy);
        card.Controls.Add(title);

        var subtitle = ModernUi.Label("Chào mừng bạn trở lại!", 10.5f, FontStyle.Regular, ModernUi.Muted);
        card.Controls.Add(subtitle);

        var usernameLabel = CreateFormLabel("Tên đăng nhập hoặc Email");
        card.Controls.Add(usernameLabel);
        _lblUsernameError = CreateValidationLabel("Vui lòng nhập tên đăng nhập hoặc email.");
        _lblUsernameError.Visible = false;
        card.Controls.Add(_lblUsernameError);
        _txtUsername = AddInputShell(card, "●", "Nhập tên đăng nhập hoặc email", false, true);

        var passwordLabel = CreateFormLabel("Mật khẩu");
        card.Controls.Add(passwordLabel);
        _lblPasswordError = CreateValidationLabel("Vui lòng nhập mật khẩu.");
        _lblPasswordError.Visible = false;
        card.Controls.Add(_lblPasswordError);
        _txtPassword = AddInputShell(card, "▣", "Nhập mật khẩu", true, false);

        _chkRemember = new CheckBox
        {
            Name = "chkRemember",
            Text = "Nhớ đăng nhập",
            Font = ModernUi.Font(10f),
            ForeColor = ModernUi.Text,
            AutoSize = true
        };
        card.Controls.Add(_chkRemember);

        var forgot = new LinkLabel
        {
            Text = "Quên mật khẩu?",
            Font = ModernUi.Font(10f),
            LinkColor = ModernUi.Blue,
            ActiveLinkColor = ModernUi.Navy,
            TextAlign = ContentAlignment.MiddleRight
        };
        forgot.LinkClicked += LinkForgotPassword_LinkClicked;
        card.Controls.Add(forgot);

        var login = ModernUi.Button("↪  Đăng nhập", ModernUi.Blue, 100, 48);
        login.Name = "btnLogin";
        login.Font = ModernUi.Font(12f, FontStyle.Bold);
        login.Click += BtnLogin_Click;
        card.Controls.Add(login);
        AcceptButton = login;

        var register = ModernUi.OutlineButton("♙  Đăng ký tài khoản", 100, 48);
        register.Font = ModernUi.Font(12f, FontStyle.Bold);
        register.Click += LinkRegister_LinkClicked;
        card.Controls.Add(register);

        var note = new RoundedPanel
        {
            Radius = 6,
            BackColor = Color.FromArgb(232, 242, 255),
            BorderColor = Color.FromArgb(190, 213, 242),
            Padding = new Padding(18, 10, 18, 10)
        };
        var noteIcon = new CircleLabel
        {
            Text = "i",
            CircleColor = ModernUi.Blue,
            ForeColor = Color.White,
            Font = ModernUi.Font(11f, FontStyle.Bold),
            Size = new Size(34, 34)
        };
        note.Controls.Add(noteIcon);
        var noteText = ModernUi.Label(
            "Tài khoản cư dân cần được Ban quản lý phê duyệt\r\ntrước khi có thể đăng nhập vào hệ thống.",
            10f,
            FontStyle.Regular,
            ModernUi.Navy);
        note.Controls.Add(noteText);
        card.Controls.Add(note);

        _lblStatus = ModernUi.Label("", 9.5f, FontStyle.Bold, ModernUi.Red);
        _lblStatus.Name = "lblStatus";
        card.Controls.Add(_lblStatus);

        surface.Resize += (_, _) =>
        {
            int cardWidth = Math.Min(620, surface.ClientSize.Width - 72);
            int cardHeight = Math.Min(650, surface.ClientSize.Height - 86);
            card.Size = new Size(cardWidth, cardHeight);
            card.Location = new Point((surface.ClientSize.Width - cardWidth) / 2, Math.Max(28, (surface.ClientSize.Height - cardHeight) / 2));

            int x = 46;
            int fieldWidth = card.Width - 92;
            int y = 42;

            lockIcon.Size = new Size(58, 58);
            lockIcon.Location = new Point((card.Width - 290) / 2, y);

            title.Location = new Point(lockIcon.Right + 22, y + 2);
            title.Size = new Size(220, 32);
            subtitle.Location = new Point(title.Left, y + 34);
            subtitle.Size = new Size(250, 24);

            y = 132;
            usernameLabel.Location = new Point(x, y);
            usernameLabel.Size = new Size(fieldWidth, 24);
            _txtUsername.Parent!.Location = new Point(x, y + 32);
            _txtUsername.Parent.Size = new Size(fieldWidth, 48);
            LayoutInputShell(_txtUsername);
            _lblUsernameError.Location = new Point(x, y + 84);
            _lblUsernameError.Size = new Size(fieldWidth, 24);

            y += 112;
            passwordLabel.Location = new Point(x, y);
            passwordLabel.Size = new Size(fieldWidth, 24);
            _txtPassword.Parent!.Location = new Point(x, y + 32);
            _txtPassword.Parent.Size = new Size(fieldWidth, 48);
            LayoutInputShell(_txtPassword);
            _lblPasswordError.Location = new Point(x, y + 84);
            _lblPasswordError.Size = new Size(fieldWidth, 24);

            _chkRemember.Location = new Point(x, y + 124);
            forgot.Location = new Point(x + fieldWidth - 150, y + 120);
            forgot.Size = new Size(150, 28);

            login.Location = new Point(x, y + 176);
            login.Size = new Size(fieldWidth, 50);
            register.Location = new Point(x, y + 240);
            register.Size = new Size(fieldWidth, 50);

            note.Location = new Point(x, y + 318);
            note.Size = new Size(fieldWidth, 64);
            noteIcon.Location = new Point(22, 15);
            noteText.Location = new Point(70, 10);
            noteText.Size = new Size(note.Width - 92, 44);

            _lblStatus.Location = new Point(x, note.Bottom + 10);
            _lblStatus.Size = new Size(fieldWidth, 24);
        };

        return surface;
    }

    private static Label CreateFormLabel(string text)
    {
        return ModernUi.Label(text, 10f, FontStyle.Bold, ModernUi.Navy);
    }

    private static Label CreateValidationLabel(string text)
    {
        return ModernUi.Label(text, 9.5f, FontStyle.Regular, ModernUi.Red);
    }

    private static TextBox AddInputShell(Control parent, string icon, string placeholder, bool password, bool focused)
    {
        var shell = new RoundedPanel
        {
            Radius = 5,
            BackColor = Color.White,
            BorderColor = focused ? ModernUi.Blue : ModernUi.Border
        };

        var iconLabel = ModernUi.Label(icon, 16f, FontStyle.Bold, ModernUi.Muted);
        iconLabel.Name = "icon";
        iconLabel.TextAlign = ContentAlignment.MiddleCenter;
        shell.Controls.Add(iconLabel);

        var input = new TextBox
        {
            Name = password ? "txtPassword" : "txtUsername",
            BorderStyle = BorderStyle.None,
            Font = ModernUi.Font(10.5f),
            PlaceholderText = placeholder,
            UseSystemPasswordChar = password
        };
        shell.Controls.Add(input);

        if (password)
        {
            var eye = new Button
            {
                Name = "eye",
                Text = "◉",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = ModernUi.Muted,
                Font = ModernUi.Font(12f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            eye.FlatAppearance.BorderSize = 0;
            eye.Click += (_, _) => input.UseSystemPasswordChar = !input.UseSystemPasswordChar;
            shell.Controls.Add(eye);
        }

        parent.Controls.Add(shell);
        return input;
    }

    private static void LayoutInputShell(TextBox input)
    {
        var shell = input.Parent;
        if (shell == null)
        {
            return;
        }

        var icon = shell.Controls["icon"];
        icon.SetBounds(12, 8, 40, 32);

        var eye = shell.Controls["eye"];
        int rightSpace = eye == null ? 14 : 48;
        input.SetBounds(58, 14, shell.Width - 58 - rightSpace, 22);

        if (eye != null)
        {
            eye.SetBounds(shell.Width - 44, 8, 34, 32);
        }
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        string username = _txtUsername.Text.Trim();
        string password = _txtPassword.Text;

        _lblUsernameError.Visible = string.IsNullOrWhiteSpace(username);
        _lblPasswordError.Visible = string.IsNullOrWhiteSpace(password);

        if (_lblUsernameError.Visible || _lblPasswordError.Visible)
        {
            _lblStatus.Text = "Vui lòng nhập tên đăng nhập và mật khẩu";
            _lblStatus.ForeColor = ModernUi.Red;
            return;
        }

        var (success, message, session) = AuthenticationBLL.Login(username, password);
        if (success && session != null)
        {
            if (_chkRemember.Checked)
            {
                RememberUsername(username);
            }

            _lblStatus.Text = message;
            _lblStatus.ForeColor = ModernUi.Green;
            Log.Information("User logged in: {Username}", username);
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            _lblStatus.Text = message;
            _lblStatus.ForeColor = ModernUi.Red;
            _txtPassword.Clear();
            _txtPassword.Focus();
        }
    }

    private void LinkForgotPassword_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        MessageBox.Show("Tính năng đặt lại mật khẩu sẽ được cập nhật sớm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LinkRegister_LinkClicked(object? sender, EventArgs e)
    {
        using var frmRegister = new FrmRegister();
        frmRegister.ShowDialog(this);
    }

    private void LinkRegister_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        LinkRegister_LinkClicked(sender, EventArgs.Empty);
    }

    private void LoadRememberedUsername()
    {
        try
        {
            var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\ApartmentManager\RememberMe");
            if (registryKey == null)
            {
                return;
            }

            var username = registryKey.GetValue("Username") as string;
            if (!string.IsNullOrEmpty(username))
            {
                _txtUsername.Text = username;
                _chkRemember.Checked = true;
            }

            registryKey.Close();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load remembered username");
        }
    }

    private static void RememberUsername(string username)
    {
        try
        {
            var registryKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\ApartmentManager\RememberMe");
            registryKey.SetValue("Username", username);
            registryKey.Close();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not save remembered username");
        }
    }
}

internal sealed class LoginHeroPanel : Panel
{
    public LoginHeroPanel()
    {
        DoubleBuffered = true;
        BackColor = ModernUi.Navy;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var bg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(0, 48, 110), Color.FromArgb(0, 89, 184), 90f);
        e.Graphics.FillRectangle(bg, ClientRectangle);

        using var glow = new SolidBrush(Color.FromArgb(28, 0, 110, 210));
        e.Graphics.FillEllipse(glow, Width / 2 - 360, Height - 380, 720, 360);

        using var dot = new SolidBrush(Color.FromArgb(78, 143, 214));
        for (int y = 26; y < 105; y += 21)
        {
            for (int x = 36; x < 145; x += 21)
            {
                e.Graphics.FillEllipse(dot, x, y, 4, 4);
            }
        }

        int groundTop = Height - 96;
        DrawBuilding(e.Graphics, -4, groundTop - 270, 174, 268, Color.FromArgb(39, 132, 220), 1.04f);
        DrawBuilding(e.Graphics, 215, groundTop - 178, 140, 176, Color.FromArgb(44, 125, 205), 0.88f);
        DrawBuilding(e.Graphics, Width - 380, groundTop - 260, 174, 258, Color.FromArgb(48, 135, 220), 1.00f);
        DrawBuilding(e.Graphics, Width - 250, groundTop - 320, 200, 318, Color.FromArgb(53, 145, 232), 1.08f);
        DrawGate(e.Graphics, Width / 2 - 290, groundTop - 80, 580, 86);

        using var overlay = new SolidBrush(Color.FromArgb(90, 0, 39, 96));
        e.Graphics.FillRectangle(overlay, 0, groundTop - 18, Width, 118);
    }

    private static void DrawBuilding(Graphics graphics, int x, int y, int width, int height, Color color, float depth)
    {
        using var body = new SolidBrush(color);
        using var side = new SolidBrush(ControlPaint.Dark(color, 0.22f));
        using var light = new SolidBrush(Color.FromArgb(213, 235, 255));
        using var warm = new SolidBrush(Color.FromArgb(255, 224, 126));
        using var dark = new SolidBrush(Color.FromArgb(20, 70, 142));

        graphics.FillRectangle(body, x, y, width, height);
        graphics.FillPolygon(side, new[]
        {
            new Point(x + width, y + (int)(16 * depth)),
            new Point(x + width + (int)(34 * depth), y + (int)(36 * depth)),
            new Point(x + width + (int)(34 * depth), y + height),
            new Point(x + width, y + height)
        });

        int cols = Math.Max(3, width / 32);
        int rows = Math.Max(5, height / 31);
        int stepX = width / (cols + 1);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var brush = (r + c) % 3 == 0 ? warm : light;
                graphics.FillRectangle(brush, x + 15 + c * stepX, y + 18 + r * 28, 12, 17);
            }
        }

        graphics.FillRectangle(dark, x, y + height - 20, width, 20);
    }

    private static void DrawGate(Graphics graphics, int x, int y, int width, int height)
    {
        using var baseBrush = new SolidBrush(Color.FromArgb(40, 125, 210));
        using var roofBrush = new SolidBrush(Color.FromArgb(139, 190, 238));
        using var windowBrush = new SolidBrush(Color.FromArgb(211, 234, 255));
        graphics.FillRectangle(baseBrush, x, y + 28, width, height - 28);
        graphics.FillRectangle(roofBrush, x - 18, y + 8, width + 36, 22);
        graphics.FillRectangle(windowBrush, x + width / 2 - 34, y + 46, 68, 42);
        using var doorBrush = new SolidBrush(Color.FromArgb(11, 58, 130));
        graphics.FillRectangle(doorBrush, x + width / 2 - 6, y + 46, 12, 42);
    }
}

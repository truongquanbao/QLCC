using ApartmentManager.Utilities;
using Serilog;

namespace ApartmentManager.GUI.Forms;

public partial class FrmMainDashboard : Form
{
    private readonly UserSession _session;

    public FrmMainDashboard()
    {
        InitializeComponent();
        this.Text = "Quản Lý Khu Chung Cư - Dashboard";
        this.WindowState = FormWindowState.Maximized;
        this.FormClosing += FrmMainDashboard_FormClosing;

        _session = SessionManager.GetSession() ?? throw new InvalidOperationException("No active session");
    }

    private void FrmMainDashboard_Load(object sender, EventArgs e)
    {
        ConfigureUI();
        LoadDashboardData();
    }

    private void ConfigureUI()
    {
        // Main layout
        var mainContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 200,
            Orientation = Orientation.Vertical
        };

        // Left menu panel
        var pnlMenu = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(33, 86, 155),
            Padding = new Padding(0)
        };

        // Header with user info
        var pnlHeader = new Panel
        {
            Height = 120,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(25, 64, 115),
            Padding = new Padding(10)
        };

        var lblRole = new Label
        {
            Text = $"Chào, {_session.FullName}",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(10, 10)
        };

        var lblRoleName = new Label
        {
            Text = $"Vai trò: {_session.RoleName}",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(200, 200, 200),
            AutoSize = true,
            Location = new Point(10, 35)
        };

        var lblLogin = new Label
        {
            Text = $"Đăng nhập: {_session.LoginTime:dd/MM/yyyy HH:mm}",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(150, 150, 150),
            AutoSize = true,
            Location = new Point(10, 55)
        };

        pnlHeader.Controls.Add(lblRole);
        pnlHeader.Controls.Add(lblRoleName);
        pnlHeader.Controls.Add(lblLogin);

        // Menu buttons
        var pnlMenuContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(33, 86, 155),
            AutoScroll = true,
            Padding = new Padding(10)
        };

        int yPos = 10;
        const int btnHeight = 45;
        const int spacing = 5;

        // Add menu items based on role
        var menuItems = GetMenuItemsForRole(_session.RoleName);
        foreach (var item in menuItems)
        {
            var btn = new Button
            {
                Text = item.Name,
                Width = 170,
                Height = btnHeight,
                Location = new Point(0, yPos),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Tag = item.FormType,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => MenuButton_Click(s, e);

            pnlMenuContent.Controls.Add(btn);
            yPos += btnHeight + spacing;
        }

        pnlMenu.Controls.Add(pnlMenuContent);
        pnlMenu.Controls.Add(pnlHeader);

        // Right content panel
        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 245, 250),
            Padding = new Padding(20)
        };

        var lblWelcome = new Label
        {
            Text = $"Chào mừng {_session.FullName} đến với Hệ Thống Quản Lý Khu Chung Cư",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 86, 155),
            AutoSize = true,
            Location = new Point(20, 20)
        };

        var lblVersion = new Label
        {
            Text = "Phiên bản 1.0.0",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 100, 100),
            AutoSize = true,
            Location = new Point(20, 60)
        };

        pnlContent.Controls.Add(lblWelcome);
        pnlContent.Controls.Add(lblVersion);

        // Status bar
        var pnlStatus = new Panel
        {
            Height = 30,
            Dock = DockStyle.Bottom,
            BackColor = Color.FromArgb(220, 220, 220),
            Padding = new Padding(10, 5, 10, 5)
        };

        var lblStatus = new Label
        {
            Name = "lblStatus",
            Text = $"Người dùng: {_session.Username} | Vai trò: {_session.RoleName} | Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(64, 64, 64),
            Dock = DockStyle.Fill,
            AutoSize = false
        };

        pnlStatus.Controls.Add(lblStatus);

        // Logout button
        var btnLogout = new Button
        {
            Text = "Đăng Xuất",
            Width = 100,
            Height = 25,
            Location = new Point(850, 2),
            BackColor = Color.FromArgb(255, 87, 34),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnLogout.FlatAppearance.BorderSize = 0;
        btnLogout.Click += BtnLogout_Click;
        pnlStatus.Controls.Add(btnLogout);

        // Assemble
        mainContainer.Panel1.Controls.Add(pnlMenu);
        mainContainer.Panel2.Controls.Add(pnlContent);

        this.Controls.Add(pnlStatus);
        this.Controls.Add(mainContainer);

        // Timer for status update
        var tmrStatus = new System.Windows.Forms.Timer();
        tmrStatus.Interval = 1000;
        tmrStatus.Tick += (s, e) => lblStatus.Text = $"Người dùng: {_session.Username} | Vai trò: {_session.RoleName} | Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
        tmrStatus.Start();
    }

    private List<MenuItem> GetMenuItemsForRole(string roleName)
    {
        return roleName switch
        {
            "Super Admin" => new List<MenuItem>
            {
                new("📊 Dashboard", null),
                new("👤 Quản Lý Tài Khoản", null),
                new("🏢 Quản Lý Tòa Nhà", null),
                new("🏠 Quản Lý Căn Hộ", null),
                new("👥 Quản Lý Cư Dân", null),
                new("💰 Quản Lý Hóa Đơn", null),
                new("📋 Quản Lý Phản Ánh", null),
                new("📢 Quản Lý Thông Báo", null),
                new("🚗 Quản Lý Phương Tiện", null),
                new("👤 Quản Lý Khách", null),
                new("🔧 Quản Lý Tài Sản", null),
                new("📈 Báo Cáo", null),
                new("📜 Log Hệ Thống", null),
                new("⚙️ Cấu Hình Hệ Thống", null),
                new("💾 Backup/Restore", null),
                new("🔑 Thay Đổi Mật Khẩu", null),
            },
            "Manager" => new List<MenuItem>
            {
                new("📊 Dashboard", null),
                new("🏠 Quản Lý Căn Hộ", null),
                new("👥 Quản Lý Cư Dân", null),
                new("💰 Quản Lý Hóa Đơn", null),
                new("📋 Quản Lý Phản Ánh", null),
                new("📢 Quản Lý Thông Báo", null),
                new("🚗 Quản Lý Phương Tiện", null),
                new("👤 Quản Lý Khách", null),
                new("🔧 Quản Lý Tài Sản", null),
                new("📈 Báo Cáo", null),
                new("🔑 Thay Đổi Mật Khẩu", null),
            },
            "Resident" => new List<MenuItem>
            {
                new("📊 Thông Tin Cá Nhân", null),
                new("🏠 Thông Tin Căn Hộ", null),
                new("💰 Hóa Đơn của Tôi", null),
                new("💳 Lịch Sử Thanh Toán", null),
                new("📋 Gửi Phản Ánh", null),
                new("📢 Thông Báo", null),
                new("🚗 Xe của Tôi", null),
                new("👤 Khách của Tôi", null),
                new("🔑 Thay Đổi Mật Khẩu", null),
            },
            _ => new List<MenuItem>()
        };
    }

    private void MenuButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implement menu navigation
        MessageBox.Show("Chức năng này sẽ sớm được cập nhật", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnLogout_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            SessionManager.ClearSession();
            Log.Information("User logged out");
            this.Close();
        }
    }

    private void FrmMainDashboard_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (SessionManager.IsLoggedIn())
        {
            SessionManager.ClearSession();
            Application.Exit();
        }
    }

    private void LoadDashboardData()
    {
        // TODO: Load dashboard statistics based on role
        Log.Information("Dashboard loaded for user: {Username}", _session.Username);
    }

    private class MenuItem
    {
        public string Name { get; set; }
        public Type? FormType { get; set; }

        public MenuItem(string name, Type? formType)
        {
            Name = name;
            FormType = formType;
        }
    }
}

using System;
using System.Windows.Forms;
using System.Collections.Generic;
using ApartmentManager.Utilities;
using Serilog;

using System.Drawing;
namespace ApartmentManager.GUI.Forms;

public partial class FrmMainDashboard : Form
{
    private readonly UserSession _session;

    public FrmMainDashboard()
    {
        InitializeComponent();
        this.Text = "Quáº£n LÃ½ Khu Chung CÆ° - Dashboard";
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
            Text = $"ChÃ o, {_session.FullName}",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(10, 10)
        };

        var lblRoleName = new Label
        {
            Text = $"Vai trÃ²: {_session.RoleName}",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(200, 200, 200),
            AutoSize = true,
            Location = new Point(10, 35)
        };

        var lblLogin = new Label
        {
            Text = $"ÄÄƒng nháº­p: {_session.LoginTime:dd/MM/yyyy HH:mm}",
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
            Text = $"ChÃ o má»«ng {_session.FullName} Ä‘áº¿n vá»›i Há»‡ Thá»‘ng Quáº£n LÃ½ Khu Chung CÆ°",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 86, 155),
            AutoSize = true,
            Location = new Point(20, 20)
        };

        var lblVersion = new Label
        {
            Text = "PhiÃªn báº£n 1.0.0",
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
            Text = $"NgÆ°á»i dÃ¹ng: {_session.Username} | Vai trÃ²: {_session.RoleName} | Thá»i gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(64, 64, 64),
            Dock = DockStyle.Fill,
            AutoSize = false
        };

        pnlStatus.Controls.Add(lblStatus);

        // Logout button
        var btnLogout = new Button
        {
            Text = "ÄÄƒng Xuáº¥t",
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
        tmrStatus.Tick += (s, e) => lblStatus.Text = $"NgÆ°á»i dÃ¹ng: {_session.Username} | Vai trÃ²: {_session.RoleName} | Thá»i gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
        tmrStatus.Start();
    }

    private List<MenuItem> GetMenuItemsForRole(string roleName)
    {
        return roleName switch
        {
            "Super Admin" => new List<MenuItem>
            {
                new("ðŸ“Š Dashboard", null),
                new("ðŸ‘¤ Quáº£n LÃ½ TÃ i Khoáº£n", null),
                new("ðŸ¢ Quáº£n LÃ½ TÃ²a NhÃ ", null),
                new("ðŸ  Quáº£n LÃ½ CÄƒn Há»™", null),
                new("ðŸ‘¥ Quáº£n LÃ½ CÆ° DÃ¢n", null),
                new("ðŸ’° Quáº£n LÃ½ HÃ³a ÄÆ¡n", null),
                new("ðŸ“‹ Quáº£n LÃ½ Pháº£n Ãnh", null),
                new("ðŸ“¢ Quáº£n LÃ½ ThÃ´ng BÃ¡o", null),
                new("ðŸš— Quáº£n LÃ½ PhÆ°Æ¡ng Tiá»‡n", null),
                new("ðŸ‘¤ Quáº£n LÃ½ KhÃ¡ch", null),
                new("ðŸ”§ Quáº£n LÃ½ TÃ i Sáº£n", null),
                new("ðŸ“ˆ BÃ¡o CÃ¡o", null),
                new("ðŸ“œ Log Há»‡ Thá»‘ng", null),
                new("âš™ï¸ Cáº¥u HÃ¬nh Há»‡ Thá»‘ng", null),
                new("ðŸ’¾ Backup/Restore", null),
                new("ðŸ”‘ Thay Äá»•i Máº­t Kháº©u", null),
            },
            "Manager" => new List<MenuItem>
            {
                new("ðŸ“Š Dashboard", null),
                new("ðŸ  Quáº£n LÃ½ CÄƒn Há»™", null),
                new("ðŸ‘¥ Quáº£n LÃ½ CÆ° DÃ¢n", null),
                new("ðŸ’° Quáº£n LÃ½ HÃ³a ÄÆ¡n", null),
                new("ðŸ“‹ Quáº£n LÃ½ Pháº£n Ãnh", null),
                new("ðŸ“¢ Quáº£n LÃ½ ThÃ´ng BÃ¡o", null),
                new("ðŸš— Quáº£n LÃ½ PhÆ°Æ¡ng Tiá»‡n", null),
                new("ðŸ‘¤ Quáº£n LÃ½ KhÃ¡ch", null),
                new("ðŸ”§ Quáº£n LÃ½ TÃ i Sáº£n", null),
                new("ðŸ“ˆ BÃ¡o CÃ¡o", null),
                new("ðŸ”‘ Thay Äá»•i Máº­t Kháº©u", null),
            },
            "Resident" => new List<MenuItem>
            {
                new("ðŸ“Š ThÃ´ng Tin CÃ¡ NhÃ¢n", null),
                new("ðŸ  ThÃ´ng Tin CÄƒn Há»™", null),
                new("ðŸ’° HÃ³a ÄÆ¡n cá»§a TÃ´i", null),
                new("ðŸ’³ Lá»‹ch Sá»­ Thanh ToÃ¡n", null),
                new("ðŸ“‹ Gá»­i Pháº£n Ãnh", null),
                new("ðŸ“¢ ThÃ´ng BÃ¡o", null),
                new("ðŸš— Xe cá»§a TÃ´i", null),
                new("ðŸ‘¤ KhÃ¡ch cá»§a TÃ´i", null),
                new("ðŸ”‘ Thay Äá»•i Máº­t Kháº©u", null),
            },
            _ => new List<MenuItem>()
        };
    }

    private void MenuButton_Click(object? sender, EventArgs e)
    {
        // TODO: Implement menu navigation
        MessageBox.Show("Chá»©c nÄƒng nÃ y sáº½ sá»›m Ä‘Æ°á»£c cáº­p nháº­t", "ThÃ´ng bÃ¡o", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnLogout_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show("Báº¡n cÃ³ cháº¯c cháº¯n muá»‘n Ä‘Äƒng xuáº¥t?", "XÃ¡c nháº­n", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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



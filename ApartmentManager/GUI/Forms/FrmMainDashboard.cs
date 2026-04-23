using ApartmentManager.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public class CanHo
{
    public string MaCanHo { get; set; } = string.Empty;
    public string Tang { get; set; } = string.Empty;
    public string LoaiCanHo { get; set; } = string.Empty;
    public double DienTich { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string MaCuDan { get; set; } = string.Empty;
}

public class CuDan
{
    public string MaCuDan { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string CCCD { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string MaCanHo { get; set; } = string.Empty;
    public DateTime NgayVao { get; set; }
}

public class PhiDichVu
{
    public string MaPhieu { get; set; } = string.Empty;
    public string MaCanHo { get; set; } = string.Empty;
    public int Thang { get; set; }
    public int Nam { get; set; }
    public double PhiQuanLy { get; set; }
    public double PhiDienNuoc { get; set; }
    public double PhiGuiXe { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public double TongPhi => PhiQuanLy + PhiDienNuoc + PhiGuiXe;
}

public partial class FrmMainDashboard : Form
{
    private const int SidebarWidth = 232;
    private const int HeaderHeight = 60;
    private const int FooterHeight = 64;
    private static readonly Font GridBadgeFont = ModernUi.Font(8.4f, FontStyle.Bold);

    private readonly List<CanHo> _apartments = new();
    private readonly List<CuDan> _residents = new();
    private readonly List<PhiDichVu> _fees = new();
    private readonly Dictionary<string, Button> _navButtons = new();
    private readonly UserSession? _session;

    private Panel _sidebar = null!;
    private Panel _content = null!;
    private Panel _footer = null!;
    private Label _clockLabel = null!;
    private string _activePage = "dashboard";
    private Timer _clockTimer = null!;

    public FrmMainDashboard()
    {
        _session = SessionManager.GetSession();
        InitSampleData();
        InitializeComponent();
        BuildShell();
        Shown += (_, _) => Navigate(GetDefaultPage());
    }

    private void InitializeComponent()
    {
        Text = "PHẦN MỀM QUẢN LÝ KHU CHUNG CƯ";
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(1360, 760);
        ModernUi.ApplyFormDefaults(this, new Size(1360, 760));
    }

    private void BuildShell()
    {
        Controls.Clear();

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = ModernUi.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderHeight));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, FooterHeight));
        Controls.Add(shell);

        var topBar = new Panel
        {
            Dock = DockStyle.Fill,
            Height = HeaderHeight,
            BackColor = ModernUi.Navy
        };
        shell.Controls.Add(topBar, 0, 0);

        var appTitle = ModernUi.Label("▦  PHẦN MỀM QUẢN LÝ KHU CHUNG CƯ", 16f, FontStyle.Bold, Color.White);
        appTitle.Location = new Point(20, 10);
        appTitle.Size = new Size(560, 42);
        topBar.Controls.Add(appTitle);

        var close = CreateWindowButton("×");
        close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        close.Location = new Point(ClientSize.Width - 48, 10);
        close.Click += (_, _) => Close();
        topBar.Controls.Add(close);

        var maximize = CreateWindowButton("□");
        maximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        maximize.Location = new Point(ClientSize.Width - 88, 10);
        maximize.Click += (_, _) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        topBar.Controls.Add(maximize);

        var minimize = CreateWindowButton("−");
        minimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        minimize.Location = new Point(ClientSize.Width - 128, 10);
        minimize.Click += (_, _) => WindowState = FormWindowState.Minimized;
        topBar.Controls.Add(minimize);

        _footer = new Panel
        {
            Dock = DockStyle.Fill,
            Height = FooterHeight,
            BackColor = ModernUi.Navy
        };
        shell.Controls.Add(_footer, 0, 2);
        BuildFooter();

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ModernUi.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SidebarWidth));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        shell.Controls.Add(body, 0, 1);

        _sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(0, 46, 99)
        };
        body.Controls.Add(_sidebar, 0, 0);
        BuildSidebar();

        _content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernUi.Surface
        };
        body.Controls.Add(_content, 1, 0);

        _clockTimer = new Timer { Interval = 1000 };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
    }

    private static Button CreateWindowButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(34, 34),
            BackColor = ModernUi.Navy,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = ModernUi.Font(15f, FontStyle.Regular),
            TabStop = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ModernUi.Navy2;
        return button;
    }

    private void BuildFooter()
    {
        _footer.Controls.Clear();

        var user = ModernUi.Label($"●  {RoleFooterLabel()}: {CurrentDisplayName()}", 10f, FontStyle.Regular, Color.White);
        user.Location = new Point(22, 9);
        user.Size = new Size(300, 30);
        _footer.Controls.Add(user);

        var role = ModernUi.Label($"◆  Vai trò: {RoleDisplay()}", 10f, FontStyle.Regular, Color.White);
        role.Location = new Point(380, 9);
        role.Size = new Size(280, 30);
        _footer.Controls.Add(role);

        _clockLabel = ModernUi.Label("", 10f, FontStyle.Regular, Color.White);
        _clockLabel.Location = new Point(720, 9);
        _clockLabel.Size = new Size(420, 30);
        _footer.Controls.Add(_clockLabel);

        var db = ModernUi.Label("▰  Trạng thái kết nối:  Đã kết nối SQL Server", 10f, FontStyle.Regular, Color.White);
        db.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        db.Location = new Point(Width - 420, 9);
        db.Size = new Size(390, 30);
        _footer.Controls.Add(db);
        _footer.Resize += (_, _) => db.Left = _footer.ClientSize.Width - 420;
    }

    private void BuildSidebar()
    {
        _sidebar.Controls.Clear();
        _navButtons.Clear();

        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12, 18, 12, 0),
            AutoScroll = false,
            BackColor = _sidebar.BackColor
        };
        _sidebar.Controls.Add(menu);

        foreach (var item in MenuItemsForRole())
        {
            var button = new Button
            {
                Text = $"{item.Icon}   {item.Text}",
                Tag = item.Key,
                Width = SidebarWidth - 24,
                Height = 44,
                Margin = new Padding(0, 0, 0, 6),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Font = ModernUi.Font(10f, FontStyle.Bold),
                BackColor = _sidebar.BackColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ModernUi.Navy2;
            button.Click += (_, _) => Navigate(item.Key);
            _navButtons[item.Key] = button;
            menu.Controls.Add(button);
        }
    }

    private IEnumerable<(string Key, string Text, string Icon)> MenuItemsForRole()
    {
        if (IsResident)
        {
            return new[]
            {
                ("dashboard", "Dashboard cá nhân", "⌂"),
                ("profile", "Hồ sơ cá nhân", "●"),
                ("apartment-info", "Thông tin căn hộ", "▦"),
                ("my-invoices", "Hóa đơn của tôi", "▤"),
                ("payment", "Thanh toán / Lịch sử", "▰"),
                ("send-complaint", "Gửi phản ánh", "■"),
                ("notifications", "Thông báo", "◆"),
                ("vehicles", "Xe của tôi", "▣"),
                ("visitors", "Khách của tôi", "♙"),
                ("password", "Đổi mật khẩu", "□")
            };
        }

        if (IsManager)
        {
            return new[]
            {
                ("dashboard", "Dashboard", "◉"),
                ("apartments", "Căn hộ", "▦"),
                ("residents", "Cư dân", "●"),
                ("invoices", "Hóa đơn / phí", "▤"),
                ("complaints", "Phản ánh", "■"),
                ("vehicles", "Phương tiện", "▣"),
                ("visitors", "Khách ra vào", "♙"),
                ("assets", "Tài sản", "◇"),
                ("notifications", "Thông báo", "◆"),
                ("reports", "Báo cáo", "▥"),
                ("profile", "Hồ sơ cá nhân", "●")
            };
        }

        return new[]
        {
            ("dashboard", "Dashboard", "◉"),
            ("accounts", "Quản lý tài khoản", "●"),
            ("permissions", "Phân quyền", "□"),
            ("apartments", "Tòa nhà / căn hộ", "▦"),
            ("residents", "Cư dân", "●"),
            ("invoices", "Hóa đơn / phí", "▤"),
            ("complaints", "Phản ánh", "■"),
            ("vehicles", "Phương tiện", "▣"),
            ("visitors", "Khách ra vào", "♙"),
            ("assets", "Tài sản", "◇"),
            ("reports", "Báo cáo", "▥"),
            ("logs", "Log hệ thống", "▤"),
            ("settings", "Cấu hình hệ thống", "⚙")
        };
    }

    private void Navigate(string page)
    {
        _activePage = page;
        foreach (var pair in _navButtons)
        {
            bool active = pair.Key == page || (page == "permissions" && pair.Key == "accounts");
            pair.Value.BackColor = active ? ModernUi.Blue : _sidebar.BackColor;
        }

        if (page is "dashboard")
        {
            if (IsResident)
            {
                RenderResidentDashboard();
            }
            else if (IsManager)
            {
                RenderManagerDashboard();
            }
            else
            {
                RenderAdminDashboard();
            }
            return;
        }

        switch (page)
        {
            case "accounts":
            case "permissions":
                RenderAccounts();
                break;
            case "apartments":
            case "apartment-info":
                RenderApartments();
                break;
            case "residents":
                RenderResidents();
                break;
            case "invoices":
            case "my-invoices":
            case "payment":
                RenderInvoices();
                break;
            case "complaints":
            case "send-complaint":
                RenderComplaints();
                break;
            default:
                RenderPlaceholder(page);
                break;
        }
    }

    private Panel BeginPage(string title, string breadcrumb = "")
    {
        _content.Controls.Clear();

        var page = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ModernUi.Surface,
            Padding = Padding.Empty
        };
        _content.Controls.Add(page);

        var header = new Panel
        {
            Location = Point.Empty,
            Size = new Size(Math.Max(1120, _content.ClientSize.Width), 64),
            BackColor = Color.White,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        page.Controls.Add(header);

        var headerIcon = new CircleLabel
        {
            Text = "◉",
            CircleColor = ModernUi.Blue,
            ForeColor = Color.White,
            Font = ModernUi.Font(14f, FontStyle.Bold),
            Location = new Point(22, 15),
            Size = new Size(34, 34)
        };
        header.Controls.Add(headerIcon);

        var label = ModernUi.Label(title, 15f, FontStyle.Bold, ModernUi.Navy);
        label.Location = new Point(72, 11);
        label.Size = new Size(150, 30);
        header.Controls.Add(label);

        var divider = new Panel
        {
            BackColor = ModernUi.Border,
            Location = new Point(232, 14),
            Size = new Size(1, 36)
        };
        header.Controls.Add(divider);

        var crumb = ModernUi.Label(breadcrumb, 8.8f, FontStyle.Regular, ModernUi.Muted);
        crumb.Location = new Point(252, 19);
        crumb.Size = new Size(430, 24);
        header.Controls.Add(crumb);

        if (!IsResident)
        {
            var search = ModernUi.SearchBox("Tìm kiếm nhanh...", 310, 36);
            search.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            search.Location = new Point(header.Width - 530, 14);
            header.Controls.Add(search);

            var bell = ModernUi.IconButton("🔔", 36);
            bell.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bell.Location = new Point(header.Width - 170, 14);
            header.Controls.Add(bell);

            var badge = new CircleLabel
            {
                Text = NotificationCount().ToString(),
                CircleColor = ModernUi.Red,
                ForeColor = Color.White,
                Font = ModernUi.Font(8f, FontStyle.Bold),
                Location = new Point(header.Width - 150, 7),
                Size = new Size(19, 19)
            };
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            header.Controls.Add(badge);

            var avatar = new CircleLabel
            {
                Text = "●",
                CircleColor = Color.FromArgb(226, 236, 248),
                ForeColor = ModernUi.Navy,
                Font = ModernUi.Font(13f, FontStyle.Bold),
                Location = new Point(header.Width - 112, 15),
                Size = new Size(34, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            header.Controls.Add(avatar);

            var userName = ModernUi.Label($"{CurrentUsername()} ▾", 9f, FontStyle.Bold, ModernUi.Text);
            userName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            userName.Location = new Point(header.Width - 72, 17);
            userName.Size = new Size(66, 28);
            header.Controls.Add(userName);
        }

        return page;
    }

    private void RenderAdminDashboard()
    {
        var page = BeginPage("Dashboard", "Trang chủ / Tổng quan hệ thống");
        int w = Math.Max(1150, _content.ClientSize.Width - 58);
        int y = 76;
        int gap = 12;
        int cardW = (w - gap * 5) / 6;

        AddRow(page, y, gap,
            ModernUi.StatCard("Tổng số tài khoản", "128", "Tài khoản", ModernUi.Blue, "●", "↗ +12 so với tháng trước", cardW),
            ModernUi.StatCard("Số cư dân", "1.248", "Người", ModernUi.Green, "●●", "↗ +36 so với tháng trước", cardW),
            ModernUi.StatCard("Số căn hộ", "856", "Căn hộ", ModernUi.Purple, "▦", "↗ +0 so với tháng trước", cardW),
            ModernUi.StatCard("Căn hộ đang ở / còn trống", "692 / 164", "Đang ở / Còn trống", ModernUi.Teal, "⌂", "↗ 81% tỷ lệ lấp đầy", cardW),
            ModernUi.StatCard("Doanh thu tháng", "1,285,000,000", "VNĐ", ModernUi.Orange, "$", "↗ +18.7% so với tháng trước", cardW),
            ModernUi.StatCard("Hóa đơn chưa thanh toán", "215", "Hóa đơn", ModernUi.Red, "▤", "↗ 850,450,000 VNĐ", cardW));

        y += 144;

        var revenue = ModernUi.Section("Doanh thu theo tháng (VNĐ)", (int)(w * 0.49), 245);
        revenue.Location = new Point(18, y);
        var chart = new BarChartPanel
        {
            Location = new Point(12, 40),
            Size = new Size(revenue.Width - 24, 188),
            BarColor = ModernUi.Blue,
            AxisMax = 1_500_000_000,
            SeriesLabel = "Doanh thu (VNĐ)"
        };
        chart.Bars.AddRange(new[]
        {
            ("T1", 920_000_000), ("T2", 900_000_000), ("T3", 1_200_000_000), ("T4", 840_000_000),
            ("T5", 1_180_000_000), ("T6", 1_500_000_000), ("T7", 1_240_000_000), ("T8", 1_180_000_000),
            ("T9", 1_240_000_000), ("T10", 1_060_000_000), ("T11", 920_000_000), ("T12", 1_190_000_000)
        });
        revenue.Controls.Add(chart);
        page.Controls.Add(revenue);

        var occupancy = ModernUi.Section("Tỷ lệ lấp đầy căn hộ", (int)(w * 0.29), 245);
        occupancy.Location = new Point(revenue.Right + gap, y);
        occupancy.Controls.Add(new DonutChartPanel { Location = new Point(12, 40), Size = new Size(occupancy.Width - 24, 178) });
        page.Controls.Add(occupancy);

        var alert = ModernUi.Section("Cảnh báo hệ thống", w - revenue.Width - occupancy.Width - gap * 2, 245);
        alert.BackColor = Color.FromArgb(255, 249, 235);
        alert.BorderColor = Color.FromArgb(241, 213, 153);
        alert.Location = new Point(occupancy.Right + gap, y);
        var alertIcon = new WarningTriangleControl
        {
            TriangleColor = ModernUi.Orange,
            Size = new Size(82, 72),
            Location = new Point((alert.Width - 82) / 2, 54)
        };
        alert.Controls.Add(alertIcon);
        var alertText = ModernUi.Label("CHƯA BACKUP QUÁ 7 NGÀY\r\nLần backup gần nhất:\r\n10/05/2024 08:30", 11f, FontStyle.Bold, Color.FromArgb(220, 85, 0));
        alertText.Location = new Point(20, 142);
        alertText.Size = new Size(alert.Width - 40, 66);
        alertText.TextAlign = ContentAlignment.MiddleCenter;
        alert.Controls.Add(alertText);
        var backup = ModernUi.Button("▤  Backup ngay", ModernUi.Orange, 160, 36);
        backup.Location = new Point((alert.Width - 160) / 2, 204);
        alert.Controls.Add(backup);
        page.Controls.Add(alert);

        y += 260;

        var complaints = ModernUi.Section("Phản ánh đang xử lý", (int)(w * 0.57), 254);
        complaints.Location = new Point(18, y);
        var complaintsGrid = CreateGrid(
            new[] { "STT", "Mã phản ánh", "Nội dung", "Cư dân", "Căn hộ", "Ngày tạo", "Ưu tiên", "Trạng thái" },
            new object[][]
            {
                new object[] { 1, "PA240517-01", "Thang máy tòa A bị kẹt", "Nguyễn Văn An", "A-1205", "17/05/2024 09:15", "Cao", "Đang xử lý" },
                new object[] { 2, "PA240517-02", "Rò rỉ nước tại ban công", "Trần Thị Bình", "B-0803", "17/05/2024 10:02", "Trung bình", "Đang xử lý" },
                new object[] { 3, "PA240517-03", "Đèn hành lang không sáng", "Lê Văn Cường", "A-0512", "17/05/2024 11:20", "Thấp", "Đang xử lý" },
                new object[] { 4, "PA240517-04", "Tiếng ồn vào ban đêm", "Phạm Thị Dung", "B-1510", "17/05/2024 13:45", "Cao", "Đang xử lý" },
                new object[] { 5, "PA240517-05", "Cửa kính sảnh bị hỏng", "Hoàng Minh Đức", "A-0102", "17/05/2024 14:30", "Trung bình", "Đang xử lý" }
            });
        complaintsGrid.Location = new Point(12, 42);
        complaintsGrid.Size = new Size(complaints.Width - 24, 166);
        complaints.Controls.Add(complaintsGrid);
        var allComplaints = ModernUi.OutlineButton("Xem tất cả phản ánh  →", 170, 30);
        allComplaints.Location = new Point(16, 216);
        allComplaints.Click += (_, _) => Navigate("complaints");
        complaints.Controls.Add(allComplaints);
        page.Controls.Add(complaints);

        var actions = ModernUi.Section("Thao tác nhanh", w - complaints.Width - gap, 254);
        actions.Location = new Point(complaints.Right + gap, y);
        AddActionTile(actions, "⊕", "Thêm mới", "Tạo dữ liệu mới", ModernUi.Green, 20, 54);
        AddActionTile(actions, "✎", "Sửa dữ liệu", "Chỉnh sửa dữ liệu", ModernUi.Orange, 220, 54);
        AddActionTile(actions, "▥", "Xóa dữ liệu", "Xóa dữ liệu", ModernUi.Red, 420, 54);
        AddActionTile(actions, "▣", "Lưu dữ liệu", "Lưu thay đổi", ModernUi.Blue, 20, 148);
        AddActionTile(actions, "×", "Hủy thao tác", "Hủy bỏ thay đổi", Color.FromArgb(107, 116, 128), 220, 148);
        AddActionTile(actions, "▤", "Báo cáo nhanh", "Xem báo cáo", Color.FromArgb(250, 252, 255), 420, 148, ModernUi.Navy);
        page.Controls.Add(actions);
    }

    private void RenderManagerDashboard()
    {
        var page = BeginPage("Dashboard", "Trang chủ / Dashboard");
        int w = Math.Max(1150, _content.ClientSize.Width - 58);
        int y = 76;
        int gap = 12;
        int cardW = (w - gap * 5) / 6;

        AddRow(page, y, gap,
            ModernUi.StatCard("Số cư dân hiện tại", "1.248", "Người", ModernUi.Blue, "●●", "↑ 36 so với tháng trước", cardW),
            ModernUi.StatCard("Phản ánh mới", "23", "Phản ánh", ModernUi.Green, "■", "↑ 21 so với hôm qua", cardW),
            ModernUi.StatCard("Hóa đơn chưa thanh toán", "215", "Hóa đơn", Color.FromArgb(232, 58, 30), "▤", "↑ 18 so với tháng trước", cardW),
            ModernUi.StatCard("Lịch bảo trì sắp tới", "7", "Lịch", ModernUi.Purple, "⚒", "Trong 7 ngày tới", cardW),
            ModernUi.StatCard("Khách đăng ký hôm nay", "18", "Khách", ModernUi.Teal, "●", "↑ 5 so với hôm qua", cardW),
            ModernUi.StatCard("Tỷ lệ lấp đầy căn hộ", "81%", "Đang ở", ModernUi.Green, "◔", "↑ 2% so với tháng trước", cardW));

        y += 132;

        var chartPanel = ModernUi.Section("Top loại phản ánh nhiều nhất", (int)(w * 0.40), 260);
        chartPanel.Location = new Point(18, y);
        var chart = new BarChartPanel { Location = new Point(12, 42), Size = new Size(chartPanel.Width - 24, 190), BarColor = ModernUi.Blue };
        chart.Bars.AddRange(new[] { ("Thang máy", 45), ("Vệ sinh", 38), ("Điện nước", 29), ("An ninh", 22), ("Hạ tầng", 18), ("Khác", 12) });
        chartPanel.Controls.Add(chart);
        page.Controls.Add(chartPanel);

        var newComplaints = ModernUi.Section("Phản ánh mới cần xử lý", w - chartPanel.Width - gap, 260);
        newComplaints.Location = new Point(chartPanel.Right + gap, y);
        var grid = CreateGrid(
            new[] { "STT", "Mã phản ánh", "Nội dung", "Căn hộ", "Người gửi", "Thời gian", "Ưu tiên", "Trạng thái" },
            new object[][]
            {
                new object[] { 1, "PA240517-025", "Thang máy tòa A bị kẹt", "A-1205", "Nguyễn Văn An", "17/05/2024 09:15", "Cao", "Mới" },
                new object[] { 2, "PA240517-024", "Đèn hành lang không sáng", "B-0803", "Trần Thị Bình", "17/05/2024 08:32", "Trung bình", "Mới" },
                new object[] { 3, "PA240517-023", "Rò rỉ nước tại ban công", "C-0912", "Lê Văn Cường", "17/05/2024 07:45", "Cao", "Mới" },
                new object[] { 4, "PA240517-022", "Tiếng ồn vào ban đêm", "A-0510", "Phạm Thị Dung", "17/05/2024 07:20", "Thấp", "Mới" },
                new object[] { 5, "PA240517-021", "Cửa kính sảnh bị hỏng", "B-1106", "Hoàng Minh Đức", "17/05/2024 06:55", "Trung bình", "Mới" }
            });
        grid.Location = new Point(12, 42);
        grid.Size = new Size(newComplaints.Width - 24, 168);
        newComplaints.Controls.Add(grid);
        var link = ModernUi.OutlineButton("Xem tất cả phản ánh mới  →", 210, 30);
        link.Location = new Point(newComplaints.Width - 230, 216);
        link.Click += (_, _) => Navigate("complaints");
        newComplaints.Controls.Add(link);
        page.Controls.Add(newComplaints);

        y += 276;

        var schedule = ModernUi.Section("Lịch bảo trì 7 ngày tới", (int)(w * 0.40), 240);
        schedule.Location = new Point(18, y);
        var scheduleGrid = CreateGrid(
            new[] { "Ngày", "Hạng mục", "Khu vực", "Nội dung", "Trạng thái" },
            new object[][]
            {
                new object[] { "18/05/2024", "Thang máy", "Tòa A", "Bảo trì định kỳ thang máy A1, A2", "Đã lên lịch" },
                new object[] { "19/05/2024", "Hệ thống PCCC", "Toàn khu", "Kiểm tra hệ thống PCCC định kỳ", "Đã lên lịch" },
                new object[] { "20/05/2024", "Hệ thống điện", "Tòa B", "Bảo trì trạm biến áp tòa B", "Đã lên lịch" },
                new object[] { "21/05/2024", "Máy bơm nước", "Tòa C", "Bảo trì máy bơm tầng hầm", "Đã lên lịch" },
                new object[] { "22/05/2024", "Sân vườn", "Khuôn viên", "Cắt tỉa cây xanh định kỳ", "Đã lên lịch" }
            });
        scheduleGrid.Location = new Point(12, 42);
        scheduleGrid.Size = new Size(schedule.Width - 24, 170);
        schedule.Controls.Add(scheduleGrid);
        page.Controls.Add(schedule);

        var notice = ModernUi.Section("Thông báo nhanh", w - schedule.Width - gap, 240);
        notice.Location = new Point(schedule.Right + gap, y);
        string[] notices =
        {
            "Thông báo tạm ngưng cấp nước tòa B vào ngày 20/05/2024 từ 08:00-12:00 để bảo trì.",
            "Phí quản lý tháng 5/2024 đã được phát hành. Quý cư dân vui lòng thanh toán đúng hạn.",
            "Mời cư dân tham gia cuộc họp định kỳ Ban quản trị vào ngày 25/05/2024 lúc 09:00.",
            "Khu vui chơi trẻ em sẽ được bảo trì từ ngày 18/05 đến 19/05/2024.",
            "Cảnh báo mưa lớn trong 2 ngày tới. Quý cư dân chú ý an toàn khi di chuyển."
        };
        for (int i = 0; i < notices.Length; i++)
        {
            var row = ModernUi.Label("▸  " + notices[i], 9.4f, FontStyle.Regular, ModernUi.Text);
            row.Location = new Point(20, 48 + i * 34);
            row.Size = new Size(notice.Width - 40, 30);
            notice.Controls.Add(row);
        }
        page.Controls.Add(notice);
    }

    private void RenderResidentDashboard()
    {
        var page = BeginPage("Dashboard cá nhân", $"Xin chào, {CurrentDisplayName()}! Chúc bạn một ngày tốt lành.");
        int w = Math.Max(1150, _content.ClientSize.Width - 58);
        int y = 76;
        int gap = 12;
        int cardW = (w - gap * 5) / 6;

        AddRow(page, y, gap,
            ResidentCard("Thông tin cá nhân", CurrentDisplayName(), "0901 234 567\r\nan.nguyenvan@gmail.com", ModernUi.Blue, "●", "Xem chi tiết", cardW),
            ResidentCard("Căn hộ đang ở", "A-1205", "Tòa A - Tầng 12\r\nDiện tích: 68.5 m²", ModernUi.Green, "⌂", "Xem chi tiết", cardW),
            ResidentCard("Hóa đơn mới nhất", "Tháng 05/2024", "1,285,000 VNĐ\r\nNgày phát hành: 10/05/2024", ModernUi.Orange, "▤", "Xem hóa đơn", cardW),
            ResidentCard("Trạng thái thanh toán", "Đã thanh toán", "Thanh toán ngày: 12/05/2024\r\nCảm ơn bạn đã thanh toán đúng hạn!", ModernUi.Green, "✓", "Xem lịch sử", cardW),
            ResidentCard("Thông báo chưa đọc", "5", "Thông báo mới", ModernUi.Blue, "◆", "Xem tất cả", cardW),
            ResidentCard("Phản ánh đang xử lý", "1", "Phản ánh đang xử lý", ModernUi.Purple, "■", "Xem chi tiết", cardW));

        y += 142;

        var invoices = ModernUi.Section("Hóa đơn gần đây", (int)(w * 0.40), 270);
        invoices.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "Kỳ hóa đơn", "Ngày phát hành", "Hạn thanh toán", "Số tiền (VNĐ)", "Trạng thái", "Hành động" },
            new object[][]
            {
                new object[] { "05/2024", "10/05/2024", "25/05/2024", "1,285,000", "Đã thanh toán", "Xem" },
                new object[] { "04/2024", "10/04/2024", "25/04/2024", "1,250,000", "Đã thanh toán", "Xem" },
                new object[] { "03/2024", "10/03/2024", "25/03/2024", "1,220,000", "Đã thanh toán", "Xem" },
                new object[] { "02/2024", "10/02/2024", "25/02/2024", "1,180,000", "Đã thanh toán", "Xem" },
                new object[] { "01/2024", "10/01/2024", "25/01/2024", "1,150,000", "Đã thanh toán", "Xem" }
            });
        grid.Location = new Point(12, 46);
        grid.Size = new Size(invoices.Width - 24, 190);
        invoices.Controls.Add(grid);
        page.Controls.Add(invoices);

        var notices = ModernUi.Section("Thông báo gần đây", (int)(w * 0.26), 270);
        notices.Location = new Point(invoices.Right + gap, y);
        string[] rows =
        {
            "Thông báo bảo trì thang máy định kỳ        16/05/2024 09:30",
            "Thông báo tạm ngưng cấp nước              15/05/2024 14:15",
            "Chương trình ưu đãi phí dịch vụ tháng 5   13/05/2024 11:02",
            "Thông báo lễ tân nghỉ lễ 30/4 - 1/5       28/04/2024 17:45",
            "Cập nhật quy định sử dụng hồ bơi          25/04/2024 10:20"
        };
        for (int i = 0; i < rows.Length; i++)
        {
            var row = ModernUi.Label("•  " + rows[i], 9.3f, FontStyle.Regular, ModernUi.Text);
            row.Location = new Point(18, 54 + i * 34);
            row.Size = new Size(notices.Width - 36, 28);
            notices.Controls.Add(row);
        }
        page.Controls.Add(notices);

        var progress = ModernUi.Section("Tiến độ phản ánh của tôi", w - invoices.Width - notices.Width - gap * 2, 506);
        progress.Location = new Point(notices.Right + gap, y);
        AddTimeline(progress);
        page.Controls.Add(progress);

        y += 286;

        var qr = ModernUi.Section("Thanh toán nhanh qua QR Code", invoices.Width + notices.Width + gap, 220);
        qr.Location = new Point(18, y);
        var qrCode = new QrPanel { Location = new Point(18, 54), Size = new Size(140, 140) };
        qr.Controls.Add(qrCode);
        var qrText = ModernUi.Label("Quét QR để thanh toán hóa đơn mới nhất\r\n\r\nHóa đơn:   Tháng 05/2024\r\nSố tiền:   1,285,000 VNĐ\r\nNội dung CK: A1205-052024-NguyenVanAn", 10.2f, FontStyle.Regular, ModernUi.Text);
        qrText.Location = new Point(190, 54);
        qrText.Size = new Size(qr.Width - 220, 118);
        qr.Controls.Add(qrText);
        var note = ModernUi.Badge("Sau khi thanh toán, hệ thống sẽ tự động cập nhật trong vòng 5-10 phút.", ModernUi.Blue);
        note.Location = new Point(190, 174);
        note.Size = new Size(qr.Width - 220, 26);
        qr.Controls.Add(note);
        page.Controls.Add(qr);
    }

    private void RenderApartments()
    {
        var page = BeginPage("Quản lý tòa nhà / block / tầng / căn hộ", "Dashboard / Quản lý tòa nhà / block / tầng / căn hộ");
        int w = Math.Max(1150, _content.ClientSize.Width - 58);
        int y = 72;

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 68);
        page.Controls.Add(filters);
        AddFilter(filters, "Tòa nhà", "Chọn tòa nhà", 14);
        AddFilter(filters, "Block", "Chọn block", 270);
        AddFilter(filters, "Tầng", "Chọn tầng", 526);
        AddFilter(filters, "Trạng thái căn hộ", "Tất cả", 782);
        var search = ModernUi.TextBox("Tìm kiếm mã căn hộ...", 260);
        search.Location = new Point(1038, 27);
        filters.Controls.Add(search);
        var refresh = ModernUi.OutlineButton("⟳  Làm mới", 110, 30);
        refresh.Location = new Point(w - 124, 27);
        filters.Controls.Add(refresh);

        y += 84;
        var tabs = ModernUi.CardPanel();
        tabs.Location = new Point(18, y);
        tabs.Size = new Size(w, 48);
        string[] tabNames = { "Tòa nhà", "Block", "Tầng", "Căn hộ" };
        for (int i = 0; i < tabNames.Length; i++)
        {
            var tab = ModernUi.Label(tabNames[i], 9.5f, i == 3 ? FontStyle.Bold : FontStyle.Regular, i == 3 ? ModernUi.Blue : ModernUi.Text);
            tab.Location = new Point(18 + i * 110, 8);
            tab.Size = new Size(100, 32);
            tab.TextAlign = ContentAlignment.MiddleCenter;
            tabs.Controls.Add(tab);
        }
        page.Controls.Add(tabs);

        y += 58;
        int leftW = (int)(w * 0.49);
        int midW = (int)(w * 0.29);
        int rightW = w - leftW - midW - 24;

        var list = ModernUi.Section("Danh sách căn hộ", leftW, 438);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "Mã căn hộ", "Tòa nhà", "Block", "Tầng", "Diện tích (m²)", "Loại căn hộ", "Trạng thái", "Số người tối đa" },
            new object[][]
            {
                new object[] { "A-1205", "Tòa A", "A", "12", "68.50", "2 PN - 2 WC", "Đang sử dụng", "4" },
                new object[] { "A-1206", "Tòa A", "A", "12", "54.20", "1 PN - 1 WC", "Đang sử dụng", "2" },
                new object[] { "A-1207", "Tòa A", "A", "12", "76.30", "3 PN - 2 WC", "Đang trống", "6" },
                new object[] { "A-1208", "Tòa A", "A", "12", "89.10", "3 PN - 2 WC", "Đang sử dụng", "6" },
                new object[] { "A-1209", "Tòa A", "A", "12", "63.40", "2 PN - 1 WC", "Bảo trì", "3" },
                new object[] { "B-0701", "Tòa B", "B", "07", "52.10", "1 PN - 1 WC", "Đang sử dụng", "2" },
                new object[] { "B-0702", "Tòa B", "B", "07", "68.20", "2 PN - 2 WC", "Đang trống", "4" },
                new object[] { "B-0703", "Tòa B", "B", "07", "92.00", "3 PN - 2 WC", "Đang khóa", "6" },
                new object[] { "C-0301", "Tòa C", "C", "03", "58.60", "1 PN - 1 WC", "Đang sử dụng", "2" },
                new object[] { "C-0302", "Tòa C", "C", "03", "70.00", "2 PN - 2 WC", "Đang sử dụng", "4" }
            });
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 330);
        list.Controls.Add(grid);
        var total = ModernUi.Label("Tổng số: 128 căn hộ", 9f, FontStyle.Bold, ModernUi.Blue);
        total.Location = new Point(18, 392);
        total.Size = new Size(180, 24);
        list.Controls.Add(total);
        page.Controls.Add(list);

        var details = ModernUi.Section("Thông tin căn hộ", midW, 438);
        details.Location = new Point(list.Right + 12, y);
        AddDetailField(details, "Mã căn hộ *", "A-1208", 44);
        AddDetailField(details, "Tòa nhà *", "Tòa A", 82);
        AddDetailField(details, "Block *", "A", 120);
        AddDetailField(details, "Tầng *", "12", 158);
        AddDetailField(details, "Diện tích (m²) *", "89.10", 196);
        AddDetailField(details, "Loại căn hộ *", "3 PN - 2 WC", 234);
        AddDetailField(details, "Trạng thái *", "Đang sử dụng", 272);
        AddDetailField(details, "Số người tối đa *", "6", 310);
        var noteBox = new TextBox
        {
            Text = "Căn góc hướng Đông Nam, view hồ bơi.",
            Location = new Point(120, 348),
            Size = new Size(details.Width - 138, 52),
            Multiline = true,
            Font = ModernUi.Font(9f)
        };
        details.Controls.Add(noteBox);
        AddFormActions(details, 24, 404);
        page.Controls.Add(details);

        var tree = ModernUi.Section("Sơ đồ block - tầng - căn hộ", rightW, 438);
        tree.Location = new Point(details.Right + 12, y);
        var treeText = ModernUi.Label(
            "▣ Dự án Khu Chung Cư\r\n  ▣ Tòa A\r\n    ▣ Block A\r\n      ▣ Tầng 10\r\n      ▣ Tầng 11\r\n      ▣ Tầng 12\r\n        ▦ A-1201\r\n        ▦ A-1202\r\n        ▦ A-1203\r\n        ▦ A-1204\r\n        ▦ A-1205\r\n        ▦ A-1206\r\n        ▦ A-1207\r\n        ▦ A-1208\r\n        ▦ A-1209\r\n    ▣ Block B\r\n    ▣ Block C\r\n  ▣ Tòa B\r\n  ▣ Tòa C",
            9.5f, FontStyle.Regular, ModernUi.Text);
        treeText.Location = new Point(22, 44);
        treeText.Size = new Size(tree.Width - 44, 360);
        tree.Controls.Add(treeText);
        page.Controls.Add(tree);
    }

    private void RenderResidents()
    {
        var page = BeginPage("Quản lý cư dân", "");
        int w = Math.Max(1150, _content.ClientSize.Width - 58);
        int y = 72;

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 72);
        AddFilter(filters, "Tòa nhà:", "Tất cả", 16);
        AddFilter(filters, "Căn hộ:", "Tất cả", 276);
        AddFilter(filters, "Trạng thái cư trú:", "Tất cả", 536);
        AddFilter(filters, "Vai trò trong căn hộ:", "Tất cả", 796);
        var search = ModernUi.TextBox("Nhập thông tin cần tìm...", 300);
        search.Location = new Point(1038, 30);
        filters.Controls.Add(search);
        page.Controls.Add(filters);

        y += 88;
        int leftW = (int)(w * 0.58);
        var list = ModernUi.Section("Danh sách cư dân", leftW, 536);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "STT", "Mã cư dân", "Họ tên", "CCCD", "SĐT", "Email", "Căn hộ", "Tình trạng cư trú", "Ngày vào ở" },
            new object[][]
            {
                new object[] { 1, "CD0001", "Nguyễn Văn An", "001098012345", "0901 234 567", "an.nguyen@gmail.com", "A-1205", "Đang cư trú", "12/03/2024" },
                new object[] { 2, "CD0002", "Trần Thị Bình", "001198023456", "0912 345 678", "binh.tran@gmail.com", "A-1205", "Đang cư trú", "12/03/2024" },
                new object[] { 3, "CD0003", "Lê Văn Cường", "001297034567", "0934 567 890", "cuong.le@gmail.com", "B-0803", "Đang cư trú", "05/01/2024" },
                new object[] { 4, "CD0004", "Phạm Thị Dung", "001296045678", "0909 876 543", "dung.pham@gmail.com", "B-0803", "Đang cư trú", "05/01/2024" },
                new object[] { 5, "CD0005", "Hoàng Minh Đức", "001395056789", "0918 765 432", "duc.hoang@gmail.com", "A-1002", "Đang cư trú", "20/02/2024" },
                new object[] { 6, "CD0006", "Vũ Thị Hạnh", "001497067890", "0911 222 333", "hanh.vu@gmail.com", "C-1501", "Người thuê", "01/04/2024" },
                new object[] { 7, "CD0007", "Đặng Quốc Huy", "001598078901", "0902 333 444", "huy.dang@gmail.com", "A-0701", "Đang cư trú", "15/03/2024" },
                new object[] { 8, "CD0008", "Bùi Thu Thảo", "001699089012", "0936 555 666", "thao.bui@gmail.com", "C-1501", "Người thuê", "01/04/2024" },
                new object[] { 9, "CD0009", "Nguyễn Hải Nam", "001700910123", "0907 777 888", "nam.nguyen@gmail.com", "B-1106", "Chuyển ra", "30/04/2024" },
                new object[] { 10, "CD0010", "Phan Thị Lan", "001801021234", "0913 999 000", "lan.phan@gmail.com", "A-0902", "Đang cư trú", "10/02/2024" },
                new object[] { 11, "CD0011", "Trịnh Minh Khôi", "001902312345", "0988 111 222", "khoi.trinh@gmail.com", "B-1602", "Đang cư trú", "28/02/2024" },
                new object[] { 12, "CD0012", "Đỗ Quang Vinh", "002003423456", "0905 666 777", "vinh.do@gmail.com", "C-0703", "Người thuê", "31/04/2024" }
            });
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 422);
        list.Controls.Add(grid);
        var export = ModernUi.Button("▣  In danh sách cư dân", ModernUi.Blue, 170, 36);
        export.Location = new Point(0, 488);
        list.Controls.Add(export);
        var excel = ModernUi.Button("▥  Xuất Excel", ModernUi.Green, 130, 36);
        excel.Location = new Point(184, 488);
        list.Controls.Add(excel);
        var pdf = ModernUi.Button("▤  Xuất PDF", ModernUi.Red, 130, 36);
        pdf.Location = new Point(326, 488);
        list.Controls.Add(pdf);
        page.Controls.Add(list);

        var details = ModernUi.Section("Thông tin cư dân", w - leftW - 12, 536);
        details.Location = new Point(list.Right + 12, y);
        AddResidentProfile(details);
        page.Controls.Add(details);
    }

    private void RenderInvoices()
    {
        var page = BeginPage("Quản lý phí dịch vụ & hóa đơn", "");
        int w = Math.Max(1150, _content.ClientSize.Width - 58);
        int y = 72;

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 58);
        AddFilter(filters, "Tháng:", "05/2024", 14);
        AddFilter(filters, "Năm:", "2024", 236);
        AddFilter(filters, "Căn hộ:", "Tất cả", 458);
        AddFilter(filters, "Trạng thái thanh toán:", "Tất cả", 680);
        var search = ModernUi.TextBox("Tìm kiếm nhanh...", 300);
        search.Location = new Point(970, 18);
        filters.Controls.Add(search);
        page.Controls.Add(filters);

        y += 74;
        int leftW = (int)(w * 0.58);
        var list = ModernUi.Section("Danh sách hóa đơn", leftW, 430);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "", "Mã hóa đơn", "Tháng/Năm", "Căn hộ", "Chủ hộ", "Tổng tiền (VNĐ)", "Trạng thái thanh toán", "Ngày thanh toán" },
            new object[][]
            {
                new object[] { "›", "HD2405-00128", "05/2024", "A-1205", "Nguyễn Văn An", "1,285,000", "Đã thanh toán", "05/05/2024 09:15" },
                new object[] { "", "HD2405-00127", "05/2024", "B-0803", "Trần Thị Bình", "980,000", "Đã thanh toán", "05/05/2024 10:02" },
                new object[] { "", "HD2405-00126", "05/2024", "A-0512", "Lê Văn Cường", "1,350,000", "Chưa thanh toán", "-" },
                new object[] { "", "HD2405-00125", "05/2024", "B-1510", "Phạm Thị Dung", "1,715,000", "Đã thanh toán", "05/05/2024 11:20" },
                new object[] { "", "HD2405-00124", "05/2024", "C-0707", "Hoàng Minh Đức", "1,180,000", "Chưa thanh toán", "-" },
                new object[] { "", "HD2405-00123", "05/2024", "A-1002", "Đỗ Thị Hà", "965,000", "Quá hạn", "-" },
                new object[] { "", "HD2405-00122", "05/2024", "B-0910", "Nguyễn Đức Huy", "1,450,000", "Đã thanh toán", "05/05/2024 14:30" },
                new object[] { "", "HD2405-00121", "05/2024", "C-0306", "Vũ Thị Lan", "1,075,000", "Chưa thanh toán", "-" },
                new object[] { "", "HD2405-00120", "05/2024", "A-1608", "Bùi Quang Hưng", "1,620,000", "Đã thanh toán", "04/05/2024 16:45" },
                new object[] { "", "HD2405-00119", "05/2024", "B-1201", "Trần Quốc Tuấn", "1,320,000", "Quá hạn", "-" }
            });
        grid.Location = new Point(0, 44);
        grid.Size = new Size(list.Width, 336);
        list.Controls.Add(grid);
        var paging = ModernUi.Label("Hiển thị 1 - 10 / 128 hóa đơn        ‹    1    2    3    ...    13    ›        10", 9f, FontStyle.Regular, ModernUi.Text);
        paging.Location = new Point(16, 386);
        paging.Size = new Size(list.Width - 32, 28);
        list.Controls.Add(paging);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Chi tiết hóa đơn", w - leftW - 12, 430);
        detail.Location = new Point(list.Right + 12, y);
        AddInvoiceDetail(detail);
        page.Controls.Add(detail);

        y += 446;

        var stats = ModernUi.Section("Tình hình thanh toán tháng 05/2024", (int)(w * 0.50), 170);
        stats.Location = new Point(18, y);
        var donut = new DonutChartPanel
        {
            Percent = 66,
            CenterText = "128",
            SubText = "Tổng hóa đơn",
            AccentColor = ModernUi.Green,
            Location = new Point(20, 36),
            Size = new Size(360, 118)
        };
        stats.Controls.Add(donut);
        var summary = ModernUi.Label("▣  Tổng số hóa đơn:        128\r\n✓  Đã thanh toán:          84 hóa đơn\r\n△  Chưa thanh toán:        44 hóa đơn\r\n↗  Tổng doanh thu:         133,845,000 VNĐ\r\n▣  Đã thu:                 87,630,000 VNĐ (65.4%)\r\n◇  Còn phải thu:           46,215,000 VNĐ (34.6%)",
            9.5f, FontStyle.Regular, ModernUi.Text);
        summary.Location = new Point(390, 34);
        summary.Size = new Size(stats.Width - 410, 124);
        stats.Controls.Add(summary);
        page.Controls.Add(stats);

        var actions = ModernUi.Section("", w - stats.Width - 12, 170);
        actions.Location = new Point(stats.Right + 12, y);
        AddActionTile(actions, "▤", "Tạo hóa đơn\ntháng", "", ModernUi.Blue, 22, 42, Color.White, 126, 104);
        AddActionTile(actions, "▦", "Tính phí\ntự động", "", ModernUi.Green, 164, 42, Color.White, 126, 104);
        AddActionTile(actions, "▰", "Cập nhật\nthanh toán", "", ModernUi.Orange, 306, 42, Color.White, 126, 104);
        AddActionTile(actions, "▣", "In hóa đơn", "", ModernUi.Purple, 448, 42, Color.White, 126, 104);
        AddActionTile(actions, "▥", "Xuất Excel", "", ModernUi.Green, 590, 42, Color.White, 126, 104);
        AddActionTile(actions, "PDF", "Xuất PDF", "", ModernUi.Red, 732, 42, Color.White, 126, 104);
        page.Controls.Add(actions);
    }

    private void RenderComplaints()
    {
        var page = BeginPage("Phản ánh / Thông báo / Hợp đồng", "");
        int w = Math.Max(1150, _content.ClientSize.Width - 58);
        int y = 72;

        var actions = new Panel { Location = new Point(18, y), Size = new Size(w, 42), BackColor = ModernUi.Surface };
        page.Controls.Add(actions);
        var receive = ModernUi.Button("+  Tiếp nhận phản ánh", ModernUi.Blue, 170, 38);
        receive.Location = new Point(w - 520, 0);
        actions.Controls.Add(receive);
        var transfer = ModernUi.Button("⚒  Chuyển xử lý", ModernUi.Orange, 150, 38);
        transfer.Location = new Point(w - 335, 0);
        actions.Controls.Add(transfer);
        var complete = ModernUi.Button("✓  Hoàn tất", ModernUi.Green, 140, 38);
        complete.Location = new Point(w - 170, 0);
        actions.Controls.Add(complete);

        y += 50;
        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 82);
        string[] tabs = { "Phản ánh", "Thông báo", "Hợp đồng" };
        for (int i = 0; i < tabs.Length; i++)
        {
            var tab = ModernUi.Label(tabs[i], 9.5f, i == 0 ? FontStyle.Bold : FontStyle.Regular, i == 0 ? ModernUi.Blue : ModernUi.Muted);
            tab.Location = new Point(12 + i * 120, 4);
            tab.Size = new Size(110, 26);
            filters.Controls.Add(tab);
        }
        AddFilter(filters, "Tòa nhà", "Tất cả", 12, 34);
        AddFilter(filters, "Căn hộ", "Tất cả", 180, 34);
        AddFilter(filters, "Loại phản ánh", "Tất cả", 348, 34);
        AddFilter(filters, "Ưu tiên", "Tất cả", 516, 34);
        AddFilter(filters, "Trạng thái", "Tất cả", 684, 34);
        AddFilter(filters, "Từ ngày", "01/05/2024", 852, 34);
        AddFilter(filters, "Đến ngày", "17/05/2024", 1020, 34);
        page.Controls.Add(filters);

        y += 96;
        int leftW = (int)(w * 0.52);
        var list = ModernUi.Section("Danh sách phản ánh (128)", leftW, 400);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "", "Mã phản ánh", "Tiêu đề", "Căn hộ", "Loại phản ánh", "Ưu tiên", "Trạng thái", "Ngày gửi" },
            new object[][]
            {
                new object[] { "☑", "PA240517-001", "Thang máy tòa A bị kẹt", "A-1205", "Thang máy", "Cao", "Đang xử lý", "17/05/2024 09:15" },
                new object[] { "2", "PA240517-002", "Rò rỉ nước tại ban công", "B-0803", "Nước / Ống", "Trung bình", "Đang xử lý", "17/05/2024 10:02" },
                new object[] { "3", "PA240517-003", "Đèn hành lang không sáng", "A-0512", "Điện chiếu sáng", "Thấp", "Đã tiếp nhận", "17/05/2024 11:20" },
                new object[] { "4", "PA240517-004", "Tiếng ồn vào ban đêm", "B-1510", "An ninh / Trật tự", "Cao", "Đang xử lý", "17/05/2024 13:45" },
                new object[] { "5", "PA240517-005", "Cửa kính sảnh bị nứt", "A-0102", "Cơ sở vật chất", "Trung bình", "Chờ phản hồi", "16/05/2024 16:30" },
                new object[] { "6", "PA240516-021", "Nước yếu vào giờ cao điểm", "C-1210", "Nước / Ống", "Trung bình", "Đã xử lý", "16/05/2024 14:12" },
                new object[] { "7", "PA240516-020", "Camera tầng hầm không hoạt động", "A-0908", "An ninh / Trật tự", "Cao", "Đang xử lý", "16/05/2024 09:41" },
                new object[] { "8", "PA240515-018", "Bãi xe ngập khi mưa lớn", "B-0201", "Cơ sở vật chất", "Cao", "Đã xử lý", "15/05/2024 17:28" },
                new object[] { "9", "PA240515-017", "Khuôn viên nhiều rác", "C-0506", "Vệ sinh", "Thấp", "Đã xử lý", "15/05/2024 11:05" },
                new object[] { "10", "PA240515-016", "Điều hòa hành lang không mát", "A-1601", "Điều hòa", "Trung bình", "Đã tiếp nhận", "15/05/2024 09:32" }
            });
        grid.Location = new Point(12, 42);
        grid.Size = new Size(list.Width - 24, 302);
        list.Controls.Add(grid);
        var paging = ModernUi.Label("Hiển thị 1 - 10 / 128 bản ghi                         ‹  1  2  3  4  5  ...  13  ›       10 bản ghi/trang",
            9f, FontStyle.Regular, ModernUi.Text);
        paging.Location = new Point(18, 354);
        paging.Size = new Size(list.Width - 36, 30);
        list.Controls.Add(paging);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Chi tiết phản ánh", w - leftW - 12, 400);
        detail.Location = new Point(list.Right + 12, y);
        AddComplaintDetail(detail);
        page.Controls.Add(detail);

        y += 416;
        var notices = ModernUi.Section("Thông báo mới nhất  3", (int)(w * 0.46), 238);
        notices.Location = new Point(18, y);
        var noticeGrid = CreateGrid(
            new[] { "", "Tiêu đề", "Loại", "Tòa nhà", "Từ ngày", "Đến ngày", "Trạng thái" },
            new object[][]
            {
                new object[] { 1, "Bảo trì hệ thống PCCC định kỳ tháng 05/2024", "Khẩn", "Tòa A", "18/05/2024", "18/05/2024", "Đã gửi" },
                new object[] { 2, "Thông báo tạm ngưng cấp nước", "Khẩn", "Tòa B, C", "20/05/2024", "20/05/2024", "Đã gửi" },
                new object[] { 3, "Thông báo vệ sinh bể nước", "Thông thường", "Tòa A, B, C", "25/05/2024", "25/05/2024", "Đang soạn" },
                new object[] { 4, "Cập nhật quy định gửi xe ô tô", "Thông thường", "Tất cả", "15/05/2024", "-", "Đã gửi" },
                new object[] { 5, "Thay thẻ từ thang máy miễn phí tháng 05", "Thông thường", "Tất cả", "12/05/2024", "12/05/2024", "Đã gửi" }
            });
        noticeGrid.Location = new Point(12, 42);
        noticeGrid.Size = new Size(notices.Width - 24, 150);
        notices.Controls.Add(noticeGrid);
        page.Controls.Add(notices);

        var contracts = ModernUi.Section("Hợp đồng sắp hết hạn (trong 30 ngày)", w - notices.Width - 12, 238);
        contracts.Location = new Point(notices.Right + 12, y);
        var contractGrid = CreateGrid(
            new[] { "", "Mã hợp đồng", "Khách hàng", "Căn hộ", "Loại hợp đồng", "Ngày hết hạn", "Số ngày còn lại", "Trạng thái" },
            new object[][]
            {
                new object[] { 1, "HD240201-001", "Nguyễn Văn An", "A-1205", "Hợp đồng mua bán", "28/05/2024", "11 ngày", "Sắp hết hạn" },
                new object[] { 2, "HD231615-045", "Trần Thị Bình", "B-0803", "Hợp đồng thuê", "01/06/2024", "15 ngày", "Sắp hết hạn" },
                new object[] { 3, "HD230722-032", "Lê Hoàng Nam", "C-0510", "Hợp đồng thuê", "05/06/2024", "19 ngày", "Sắp hết hạn" },
                new object[] { 4, "HD230801-018", "Phạm Thu Hà", "A-0102", "Hợp đồng mua bán", "10/06/2024", "24 ngày", "Bình thường" },
                new object[] { 5, "HD230930-009", "Đỗ Mạnh Hùng", "B-1604", "Hợp đồng thuê", "12/06/2024", "26 ngày", "Bình thường" }
            });
        contractGrid.Location = new Point(12, 42);
        contractGrid.Size = new Size(contracts.Width - 24, 150);
        contracts.Controls.Add(contractGrid);
        page.Controls.Add(contracts);
    }

    private void RenderAccounts()
    {
        _content.Controls.Clear();

        var page = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = ModernUi.Surface
        };
        _content.Controls.Add(page);

        int margin = 18;
        int contentWidth = _content.ClientSize.Width > 0 ? _content.ClientSize.Width : _content.Width;
        int w = Math.Max(980, contentWidth - margin * 2 - SystemInformation.VerticalScrollBarWidth);

        var title = ModernUi.Label("QUẢN LÝ TÀI KHOẢN & PHÂN QUYỀN", 14f, FontStyle.Bold, ModernUi.Navy);
        title.Location = new Point(margin + 8, 14);
        title.Size = new Size(520, 36);
        page.Controls.Add(title);

        var headerBell = ModernUi.Label("♧", 17f, FontStyle.Regular, ModernUi.Navy);
        headerBell.Location = new Point(w - 188, 12);
        headerBell.Size = new Size(32, 32);
        headerBell.TextAlign = ContentAlignment.MiddleCenter;
        page.Controls.Add(headerBell);
        var headerBadge = new CircleLabel
        {
            Text = "0",
            CircleColor = ModernUi.Red,
            ForeColor = Color.White,
            Font = ModernUi.Font(7.5f, FontStyle.Bold),
            Size = new Size(16, 16),
            Location = new Point(w - 166, 8)
        };
        page.Controls.Add(headerBadge);
        var headerUser = ModernUi.Label("●  superadmin ▾", 9.5f, FontStyle.Bold, ModernUi.Navy);
        headerUser.Location = new Point(w - 132, 14);
        headerUser.Size = new Size(136, 30);
        page.Controls.Add(headerUser);

        var toolbar = new Panel
        {
            Location = new Point(margin, 58),
            Size = new Size(w, 50),
            BackColor = ModernUi.Surface
        };
        page.Controls.Add(toolbar);

        int searchW = Math.Min(380, Math.Max(260, (int)(w * 0.24)));
        var search = ModernUi.TextBox("Tìm kiếm nhanh (username/email)...", searchW);
        search.Location = new Point(0, 10);
        search.Height = 34;
        toolbar.Controls.Add(search);

        var roleLabel = ModernUi.Label("Vai trò:", 9.2f, FontStyle.Bold, ModernUi.Text);
        roleLabel.Location = new Point(search.Right + 26, 14);
        roleLabel.Size = new Size(58, 26);
        toolbar.Controls.Add(roleLabel);
        var role = ModernUi.ComboBox(new[] { "Tất cả", "Super Admin", "Quản trị viên", "Kế toán", "Cư dân" }, 168);
        role.Location = new Point(roleLabel.Right + 6, 10);
        toolbar.Controls.Add(role);

        var statusLabel = ModernUi.Label("Trạng thái:", 9.2f, FontStyle.Bold, ModernUi.Text);
        statusLabel.Location = new Point(role.Right + 30, 14);
        statusLabel.Size = new Size(78, 26);
        toolbar.Controls.Add(statusLabel);
        var status = ModernUi.ComboBox(new[] { "Tất cả", "Hoạt động", "Tạm khóa", "Chờ duyệt" }, 168);
        status.Location = new Point(statusLabel.Right + 8, 10);
        toolbar.Controls.Add(status);

        int toolbarButtonsWidth = 574;
        bool wrapToolbar = status.Right + 24 + toolbarButtonsWidth > w;
        toolbar.Height = wrapToolbar ? 92 : 50;

        int buttonY = wrapToolbar ? 52 : 10;
        int buttonX = wrapToolbar ? 0 : Math.Max(status.Right + 24, w - 640);
        AddToolbarButton(toolbar, "+  Thêm", ModernUi.Blue, buttonX, buttonY, 88);
        AddToolbarButton(toolbar, "✎  Sửa", Color.FromArgb(241, 166, 0), buttonX + 102, buttonY, 86);
        AddToolbarButton(toolbar, "×  Xóa", ModernUi.Red, buttonX + 202, buttonY, 86);
        AddToolbarButton(toolbar, "▣  Khóa/Mở khóa", ModernUi.Blue, buttonX + 302, buttonY, 130);
        AddToolbarButton(toolbar, "⚿  Reset MK", ModernUi.Purple, buttonX + 446, buttonY, 128);

        int topY = toolbar.Bottom + 12;
        int listW = w;
        int topH = 408;

        var users = ModernUi.CardPanel(5);
        users.Location = new Point(margin, topY);
        users.Size = new Size(listW, topH);
        users.Padding = new Padding(0);
        page.Controls.Add(users);

        var userGrid = CreateAccountUsersGrid();
        userGrid.Location = new Point(0, 0);
        userGrid.Size = new Size(users.Width, 330);
        users.Controls.Add(userGrid);

        var paging = ModernUi.Label("Hiển thị 1 - 8 / 8 tài khoản", 9f, FontStyle.Regular, ModernUi.Text);
        paging.Location = new Point(14, 352);
        paging.Size = new Size(250, 26);
        users.Controls.Add(paging);
        AddMiniPager(users, users.Width - 710, 344);
        var perPage = ModernUi.ComboBox(new[] { "20", "10", "50" }, 78);
        perPage.Location = new Point(users.Width - 520, 344);
        users.Controls.Add(perPage);
        var perPageText = ModernUi.Label("mục/trang", 9f, FontStyle.Regular, ModernUi.Text);
        perPageText.Location = new Point(users.Width - 430, 347);
        perPageText.Size = new Size(100, 26);
        users.Controls.Add(perPageText);

        var account = ModernUi.CardPanel(5);
        account.Location = new Point(margin, users.Bottom + 14);
        account.Size = new Size(w, 260);
        account.Padding = new Padding(14);
        page.Controls.Add(account);

        var accountTitle = ModernUi.Label("THÔNG TIN TÀI KHOẢN", 10f, FontStyle.Bold, ModernUi.Blue);
        accountTitle.Location = new Point(18, 8);
        accountTitle.Size = new Size(260, 28);
        account.Controls.Add(accountTitle);

        var avatar = new AccountAvatarControl
        {
            Location = new Point(30, 52),
            Size = new Size(116, 116)
        };
        account.Controls.Add(avatar);

        var choose = ModernUi.OutlineButton("▣ Chọn ảnh", 118, 30);
        choose.Location = new Point(38, 178);
        account.Controls.Add(choose);

        int fx = 188;
        int formGap = 24;
        int colW = Math.Max(330, (account.Width - fx - formGap * 2) / 2);
        int col2X = fx + colW + formGap;
        AddAccountInput(account, "Username *", "superadmin", fx, 42, colW);
        AddAccountInput(account, "Họ tên *", "Nguyễn Văn An", col2X, 42, colW);
        AddAccountInput(account, "Email *", "superadmin@chungcu.vn", fx, 84, colW);
        AddAccountInput(account, "SĐT", "0909123456", col2X, 84, colW);
        AddAccountCombo(account, "Vai trò *", new[] { "Super Admin", "Quản trị viên", "Kế toán", "Cư dân" }, fx, 126, colW);
        AddAccountCombo(account, "Trạng thái *", new[] { "Hoạt động", "Tạm khóa", "Chờ duyệt" }, col2X, 126, colW);

        var approved = new CheckBox
        {
            Text = "Đã duyệt",
            Checked = true,
            AutoSize = true,
            Font = ModernUi.Font(9f),
            ForeColor = ModernUi.Text,
            Location = new Point(fx, 172)
        };
        account.Controls.Add(approved);
        var forceChange = new CheckBox
        {
            Text = "Yêu cầu đổi mật khẩu lần đăng nhập tới",
            AutoSize = true,
            Font = ModernUi.Font(9f),
            ForeColor = ModernUi.Text,
            Location = new Point(fx, 202)
        };
        account.Controls.Add(forceChange);

        int actionW = 132;
        var save = ModernUi.Button("▣  Lưu", ModernUi.Blue, actionW, 34);
        save.Location = new Point(col2X, 190);
        account.Controls.Add(save);
        var cancel = ModernUi.OutlineButton("⊘  Hủy", actionW, 34);
        cancel.Location = new Point(save.Right + 10, 190);
        account.Controls.Add(cancel);

        int bottomY = account.Bottom + 18;
        bool stackBottom = w < 1380;
        int bottomH = stackBottom ? 742 : Math.Max(392, _content.ClientSize.Height - bottomY - 18);
        var bottom = ModernUi.CardPanel(5);
        bottom.Location = new Point(margin, bottomY);
        bottom.Size = new Size(w, bottomH);
        bottom.Padding = new Padding(0);
        page.Controls.Add(bottom);

        var activeTab = new Label
        {
            Text = "◆  Phân quyền vai trò",
            Location = new Point(0, 0),
            Size = new Size(190, 44),
            BackColor = Color.White,
            ForeColor = ModernUi.Navy,
            Font = ModernUi.Font(9.3f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        bottom.Controls.Add(activeTab);
        var inactiveTab = new Label
        {
            Text = "▤  Nhật ký hoạt động",
            Location = new Point(190, 0),
            Size = new Size(180, 44),
            BackColor = ModernUi.Header,
            ForeColor = ModernUi.Text,
            Font = ModernUi.Font(9.3f, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleCenter
        };
        bottom.Controls.Add(inactiveTab);
        var activeLine = new Panel { BackColor = ModernUi.Blue, Location = new Point(12, 42), Size = new Size(166, 2) };
        bottom.Controls.Add(activeLine);
        var divider = new Panel { BackColor = ModernUi.Border, Location = new Point(0, 44), Size = new Size(bottom.Width, 1) };
        bottom.Controls.Add(divider);

        int matrixW = stackBottom ? bottom.Width - 24 : Math.Min(790, (int)(bottom.Width * 0.56));
        var matrix = CreatePermissionMatrixGrid();
        matrix.Location = new Point(12, 58);
        matrix.Size = stackBottom
            ? new Size(matrixW, 322)
            : new Size(matrixW, bottom.Height - 76);
        bottom.Controls.Add(matrix);

        var logGrid = CreateAccountLogGrid();
        logGrid.Location = stackBottom ? new Point(12, matrix.Bottom + 18) : new Point(matrix.Right + 14, 58);
        logGrid.Size = stackBottom
            ? new Size(bottom.Width - 24, bottom.Height - matrix.Bottom - 30)
            : new Size(bottom.Width - matrix.Right - 26, bottom.Height - 76);
        bottom.Controls.Add(logGrid);

        static void AddToolbarButton(Control parent, string text, Color color, int x, int y, int width)
        {
            var button = ModernUi.Button(text, color, width, 36);
            button.Location = new Point(x, y);
            parent.Controls.Add(button);
        }
    }

    private static DataGridView CreateAccountUsersGrid()
    {
        var grid = ModernUi.Grid();
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ColumnHeadersHeight = 36;
        grid.RowTemplate.Height = 34;
        grid.ScrollBars = ScrollBars.None;

        AddGridColumn(grid, "Username", 1.05f);
        AddGridColumn(grid, "Họ tên", 1.18f);
        AddGridColumn(grid, "Email", 1.52f);
        AddGridColumn(grid, "SĐT", 1f);
        AddGridColumn(grid, "Vai trò", 1.05f);
        AddGridColumn(grid, "Trạng thái", 0.9f);
        AddGridColumn(grid, "Đã duyệt", 0.82f);
        AddGridColumn(grid, "Lần đăng nhập cuối", 1.18f);

        grid.Rows.Add("superadmin", "Nguyễn Văn An", "superadmin@chungcu.vn", "0909123456", "Super Admin", "Hoạt động", "✓", "17/05/2024 14:52");
        grid.Rows.Add("admin1", "Trần Thị Bình", "admin1@chungcu.vn", "0912345678", "Quản trị viên", "Hoạt động", "✓", "17/05/2024 14:31");
        grid.Rows.Add("ketoan01", "Lê Thị Cúc", "ketoan01@chungcu.vn", "0911222333", "Kế toán", "Hoạt động", "✓", "17/05/2024 10:15");
        grid.Rows.Add("qttoanhaA", "Phạm Minh Đức", "qttoanha@chungcu.vn", "0903456789", "Quản lý tòa nhà", "Hoạt động", "✓", "17/05/2024 09:02");
        grid.Rows.Add("baove01", "Hoàng Văn Em", "baove01@chungcu.vn", "0933555777", "Bảo vệ", "Hoạt động", "✓", "16/05/2024 18:45");
        grid.Rows.Add("cudan123", "Nguyễn Thị Hoa", "hoa.nguyen@email.com", "0908123456", "Cư dân", "Hoạt động", "✓", "16/05/2024 12:21");
        grid.Rows.Add("ktthanhtra", "Đặng Văn Tùng", "thanhtra@chungcu.vn", "0987654321", "Kiểm tra", "Tạm khóa", "✓", "15/05/2024 16:05");
        grid.Rows.Add("userdemo", "Demo User", "demo@chungcu.vn", "0900000000", "Cư dân", "Chờ duyệt", "×", "--");

        grid.Rows[0].DefaultCellStyle.BackColor = Color.FromArgb(222, 237, 255);
        for (int i = 0; i < grid.Rows.Count; i++)
        {
            var status = grid.Rows[i].Cells[5].Value?.ToString() ?? "";
            Color color = status == "Hoạt động" ? ModernUi.Green : status == "Tạm khóa" ? ModernUi.Red : ModernUi.Blue;
            grid.Rows[i].Cells[5].Style.ForeColor = color;
            grid.Rows[i].Cells[5].Style.BackColor = status == "Hoạt động"
                ? Color.FromArgb(224, 247, 231)
                : status == "Tạm khóa"
                    ? Color.FromArgb(255, 232, 232)
                    : Color.FromArgb(230, 241, 255);
            grid.Rows[i].Cells[6].Style.ForeColor = grid.Rows[i].Cells[6].Value?.ToString() == "✓" ? ModernUi.Green : ModernUi.Red;
            grid.Rows[i].Cells[6].Style.Font = ModernUi.Font(10f, FontStyle.Bold);
        }

        return grid;
    }

    private static DataGridView CreatePermissionMatrixGrid()
    {
        var grid = ModernUi.Grid();
        grid.ReadOnly = false;
        grid.AllowUserToAddRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ColumnHeadersHeight = 34;
        grid.RowTemplate.Height = 30;
        grid.ScrollBars = ScrollBars.None;

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Module / Chức năng",
            FillWeight = 1.9f,
            ReadOnly = true
        });

        foreach (var role in new[] { "Super Admin", "Quản trị viên", "Kế toán", "Quản lý tòa nhà", "Bảo vệ", "Cư dân", "Kiểm tra" })
        {
            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                HeaderText = role,
                FillWeight = role == "Quản lý tòa nhà" ? 1.35f : 1f,
                FlatStyle = FlatStyle.Standard
            });
        }

        object[][] rows =
        {
            new object[] { "  ◉  Dashboard", true, true, true, true, true, true, true },
            new object[] { "  ▦  Căn hộ", true, true, true, true, false, false, false },
            new object[] { "  ●  Cư dân", true, true, true, true, true, false, false },
            new object[] { "  ▤  Hóa đơn / phí", true, true, true, true, false, false, false },
            new object[] { "  ■  Phản ánh", true, true, true, true, true, false, false },
            new object[] { "  ▣  Phương tiện", true, true, true, true, false, false, false },
            new object[] { "  ♙  Khách ra vào", true, true, false, false, false, false, false },
            new object[] { "  ◇  Tài sản", true, true, false, false, false, false, false },
            new object[] { "  ▥  Báo cáo", true, true, false, false, false, false, false },
            new object[] { "  ⚙  Cấu hình hệ thống", true, true, true, false, false, false, false }
        };

        foreach (var row in rows)
        {
            grid.Rows.Add(row);
        }

        return grid;
    }

    private static DataGridView CreateAccountLogGrid()
    {
        var grid = ModernUi.Grid();
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ColumnHeadersHeight = 34;
        grid.RowTemplate.Height = 30;
        grid.ScrollBars = ScrollBars.Vertical;

        AddGridColumn(grid, "Thời gian", 1.18f);
        AddGridColumn(grid, "Người dùng", 0.9f);
        AddGridColumn(grid, "Hành động", 1f);
        AddGridColumn(grid, "Nội dung", 2.2f);
        AddGridColumn(grid, "IP", 0.9f);

        grid.Rows.Add("17/05/2024 14:52:31", "superadmin", "Đăng nhập", "Đăng nhập thành công", "192.168.1.10");
        grid.Rows.Add("17/05/2024 14:48:12", "superadmin", "Cập nhật", "Cập nhật thông tin tài khoản: admin1", "192.168.1.10");
        grid.Rows.Add("17/05/2024 14:30:05", "admin1", "Đăng nhập", "Đăng nhập thành công", "192.168.1.23");
        grid.Rows.Add("17/05/2024 14:12:47", "superadmin", "Reset mật khẩu", "Reset mật khẩu cho tài khoản: baove01", "192.168.1.10");
        grid.Rows.Add("17/05/2024 13:55:21", "superadmin", "Thêm mới", "Thêm mới tài khoản: userdemo", "192.168.1.10");
        grid.Rows.Add("17/05/2024 11:03:17", "ketoan01", "Xuất báo cáo", "Xuất báo cáo doanh thu tháng 05/2024", "192.168.1.45");
        grid.Rows.Add("17/05/2024 09:15:34", "qttoanhaA", "Cập nhật", "Cập nhật thông tin căn hộ: A-1205", "192.168.1.32");
        grid.Rows.Add("16/05/2024 18:45:09", "baove01", "Đăng nhập", "Đăng nhập thành công", "192.168.1.28");

        return grid;
    }

    private static void AddGridColumn(DataGridView grid, string header, float fill)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            FillWeight = fill,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
    }

    private static void AddMiniPager(Control parent, int x, int y)
    {
        string[] labels = { "«", "‹", "1", "›", "»" };
        for (int i = 0; i < labels.Length; i++)
        {
            var button = labels[i] == "1"
                ? ModernUi.Button(labels[i], ModernUi.Blue, 32, 30)
                : ModernUi.OutlineButton(labels[i], 32, 30);
            button.Location = new Point(x + i * 38, y);
            parent.Controls.Add(button);
        }
    }

    private static void AddAccountInput(Control parent, string label, string value, int x, int y, int width)
    {
        var lbl = ModernUi.Label(label, 9f, FontStyle.Regular, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(100, 30);
        parent.Controls.Add(lbl);

        var input = ModernUi.TextBox("", width - 112);
        input.Text = value;
        input.Location = new Point(x + 112, y);
        input.Height = 30;
        parent.Controls.Add(input);
    }

    private static void AddAccountCombo(Control parent, string label, string[] values, int x, int y, int width)
    {
        var lbl = ModernUi.Label(label, 9f, FontStyle.Regular, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(100, 30);
        parent.Controls.Add(lbl);

        var combo = ModernUi.ComboBox(values, width - 112);
        combo.Location = new Point(x + 112, y);
        parent.Controls.Add(combo);
    }

    private void RenderPlaceholder(string pageKey)
    {
        string title = MenuItemsForRole().FirstOrDefault(m => m.Key == pageKey).Text ?? "Chức năng";
        var page = BeginPage(title, "Màn hình đang dùng giao diện shell mới");
        var card = ModernUi.Section(title, Math.Max(700, _content.ClientSize.Width - 80), 240);
        card.Location = new Point(18, 88);
        var text = ModernUi.Label("Khu vực này đã được đặt trong khung giao diện mới. Bạn có thể tiếp tục yêu cầu chi tiết để dựng form nghiệp vụ theo cùng hệ thiết kế.", 11f, FontStyle.Regular, ModernUi.Text);
        text.Location = new Point(24, 60);
        text.Size = new Size(card.Width - 48, 70);
        card.Controls.Add(text);
        page.Controls.Add(card);
    }

    private static void AddRow(Control parent, int y, int gap, params Control[] controls)
    {
        int x = 18;
        foreach (var control in controls)
        {
            control.Location = new Point(x, y);
            parent.Controls.Add(control);
            x += control.Width + gap;
        }
    }

    private static RoundedPanel ResidentCard(string title, string value, string detail, Color accent, string icon, string action, int width)
    {
        var card = ModernUi.CardPanel();
        card.Size = new Size(width, 126);
        var titleLabel = ModernUi.Label(title.ToUpperInvariant(), 8.5f, FontStyle.Bold, accent);
        titleLabel.Location = new Point(14, 12);
        titleLabel.Size = new Size(width - 28, 22);
        titleLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(titleLabel);
        var circle = new CircleLabel
        {
            Text = icon,
            CircleColor = accent,
            ForeColor = Color.White,
            Font = ModernUi.Font(22f, FontStyle.Bold),
            Size = new Size(58, 58),
            Location = new Point(18, 44)
        };
        card.Controls.Add(circle);
        var valueLabel = ModernUi.Label(value, 13f, FontStyle.Bold, ModernUi.Navy);
        valueLabel.Location = new Point(86, 40);
        valueLabel.Size = new Size(width - 98, 30);
        valueLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(valueLabel);
        var detailLabel = ModernUi.Label(detail, 8.4f, FontStyle.Regular, ModernUi.Text);
        detailLabel.Location = new Point(86, 70);
        detailLabel.Size = new Size(width - 98, 40);
        detailLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(detailLabel);
        var actionLabel = ModernUi.Badge(action, ModernUi.Blue);
        actionLabel.Location = new Point(14, 92);
        actionLabel.Size = new Size(width - 28, 24);
        card.Controls.Add(actionLabel);
        return card;
    }

    private static void AddFilter(Control parent, string label, string selected, int x, int y = 8)
    {
        var lbl = ModernUi.Label(label, 8.7f, FontStyle.Bold, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(170, 18);
        parent.Controls.Add(lbl);
        var combo = ModernUi.ComboBox(new[] { selected, "Tất cả", "Tòa A", "Tòa B", "Tòa C" }, 210);
        combo.Location = new Point(x, y + 22);
        parent.Controls.Add(combo);
    }

    private static void AddDetailField(Control parent, string label, string value, int y)
    {
        var lbl = ModernUi.Label(label, 8.8f, FontStyle.Regular, ModernUi.Text);
        lbl.Location = new Point(18, y);
        lbl.Size = new Size(120, 26);
        parent.Controls.Add(lbl);

        var input = ModernUi.TextBox("", parent.Width - 150);
        input.Text = value;
        input.Location = new Point(120, y);
        input.Height = 28;
        parent.Controls.Add(input);
    }

    private static void AddSmallField(Control parent, string label, string value, int x, int y)
    {
        var lbl = ModernUi.Label(label, 8.8f, FontStyle.Regular, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(95, 24);
        parent.Controls.Add(lbl);
        var input = ModernUi.TextBox("", parent.Width - x - 30);
        input.Text = value;
        input.Location = new Point(x + 108, y);
        input.Height = 28;
        parent.Controls.Add(input);
    }

    private static void AddFormActions(Control parent, int x, int y)
    {
        var add = ModernUi.Button("⊕  Thêm mới", ModernUi.Green, 116, 34);
        add.Location = new Point(x, y);
        parent.Controls.Add(add);
        var edit = ModernUi.Button("✎  Sửa", ModernUi.Orange, 110, 34);
        edit.Location = new Point(x + 126, y);
        parent.Controls.Add(edit);
        var delete = ModernUi.Button("×  Xóa", ModernUi.Red, 110, 34);
        delete.Location = new Point(x + 246, y);
        parent.Controls.Add(delete);
    }

    private static void AddActionTile(Control parent, string icon, string title, string subtitle, Color color, int x, int y, Color? textColor = null, int width = 178, int height = 72)
    {
        bool lightTile = color.GetBrightness() > 0.92f;
        var tile = ModernUi.CardPanel(5);
        tile.Location = new Point(x, y);
        tile.Size = new Size(width, height);
        tile.BackColor = color;
        tile.BorderColor = lightTile ? ModernUi.Border : color;
        tile.Cursor = Cursors.Hand;

        Color fg = textColor ?? Color.White;
        int iconSize = width <= 140 ? 26 : 30;
        var iconLabel = ModernUi.Label(icon, iconSize, FontStyle.Bold, fg);
        iconLabel.BackColor = Color.Transparent;
        iconLabel.Cursor = Cursors.Hand;

        var titleLabel = ModernUi.Label(title.Replace("\n", "\r\n"), width <= 140 ? 9.2f : 10f, FontStyle.Bold, fg);
        titleLabel.BackColor = Color.Transparent;
        titleLabel.Cursor = Cursors.Hand;

        var subLabel = ModernUi.Label(subtitle, 8.2f, FontStyle.Regular, lightTile ? ModernUi.Muted : Color.FromArgb(242, 247, 255));
        subLabel.BackColor = Color.Transparent;
        subLabel.Cursor = Cursors.Hand;

        if (width <= 140)
        {
            iconLabel.Location = new Point(0, 14);
            iconLabel.Size = new Size(width, 34);
            iconLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Location = new Point(8, 54);
            titleLabel.Size = new Size(width - 16, height - 62);
            titleLabel.TextAlign = ContentAlignment.TopCenter;
            tile.Controls.Add(iconLabel);
            tile.Controls.Add(titleLabel);
        }
        else
        {
            iconLabel.Location = new Point(14, 16);
            iconLabel.Size = new Size(42, height - 28);
            iconLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Location = new Point(66, 15);
            titleLabel.Size = new Size(width - 80, 25);
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            subLabel.Location = new Point(66, 40);
            subLabel.Size = new Size(width - 80, 22);
            subLabel.TextAlign = ContentAlignment.MiddleLeft;
            tile.Controls.Add(iconLabel);
            tile.Controls.Add(titleLabel);
            tile.Controls.Add(subLabel);
        }

        Color original = tile.BackColor;
        tile.MouseEnter += (_, _) => tile.BackColor = lightTile ? ModernUi.LightBlue : ControlPaint.Light(original, 0.08f);
        tile.MouseLeave += (_, _) => tile.BackColor = original;
        foreach (Control child in tile.Controls)
        {
            child.MouseEnter += (_, _) => tile.BackColor = lightTile ? ModernUi.LightBlue : ControlPaint.Light(original, 0.08f);
            child.MouseLeave += (_, _) => tile.BackColor = original;
        }

        parent.Controls.Add(tile);
    }

    private static DataGridView CreateGrid(string[] columns, object[][] rows)
    {
        var grid = ModernUi.Grid();
        var table = new DataTable();
        foreach (var column in columns)
        {
            table.Columns.Add(column);
        }

        foreach (var row in rows)
        {
            table.Rows.Add(row);
        }

        grid.DataSource = table;
        grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (column.HeaderText.Contains("Nội dung", StringComparison.OrdinalIgnoreCase) ||
                    column.HeaderText.Contains("Hạng mục", StringComparison.OrdinalIgnoreCase))
                {
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
            }
            grid.ClearSelection();
        };
        grid.CellFormatting += (_, e) => ApplyGridCellStyle(e);
        return grid;
    }

    private static void ApplyGridCellStyle(DataGridViewCellFormattingEventArgs e)
    {
        if (e.Value == null)
        {
            return;
        }

        string value = e.Value.ToString() ?? string.Empty;
        (Color fore, Color back)? style = value switch
        {
            "Cao" or "Quá hạn" => (ModernUi.Red, Color.FromArgb(255, 235, 235)),
            "Trung bình" or "Chưa thanh toán" => (ModernUi.Orange, Color.FromArgb(255, 244, 224)),
            "Thấp" or "Đã thanh toán" or "Đã lên lịch" or "Đang sử dụng" => (ModernUi.Green, Color.FromArgb(232, 248, 235)),
            "Đang xử lý" or "Đang cư trú" => (ModernUi.Blue, Color.FromArgb(232, 241, 255)),
            "Mới" => (ModernUi.Purple, Color.FromArgb(243, 232, 255)),
            "Đang trống" => (ModernUi.Teal, Color.FromArgb(229, 247, 250)),
            "Bảo trì" => (ModernUi.Orange, Color.FromArgb(255, 244, 224)),
            "Đang khóa" or "Chuyển ra" => (ModernUi.Muted, Color.FromArgb(241, 245, 249)),
            _ => null
        };

        if (style == null)
        {
            return;
        }

        e.CellStyle.ForeColor = style.Value.fore;
        e.CellStyle.BackColor = style.Value.back;
        e.CellStyle.SelectionForeColor = style.Value.fore;
        e.CellStyle.SelectionBackColor = ControlPaint.Light(style.Value.back, 0.12f);
        e.CellStyle.Font = GridBadgeFont;
    }

    private static void AddResidentProfile(Control parent)
    {
        var avatar = new CircleLabel
        {
            Text = "●",
            CircleColor = Color.FromArgb(152, 163, 179),
            ForeColor = Color.White,
            Font = ModernUi.Font(32f),
            Size = new Size(120, 120),
            Location = new Point(26, 56)
        };
        parent.Controls.Add(avatar);
        var choose = ModernUi.Button("▣  Chọn ảnh", ModernUi.Blue, 110, 30);
        choose.Location = new Point(32, 190);
        parent.Controls.Add(choose);
        var remove = ModernUi.OutlineButton("×  Xóa ảnh", 110, 30);
        remove.Location = new Point(32, 226);
        remove.ForeColor = ModernUi.Red;
        parent.Controls.Add(remove);

        int fx = 200;
        AddSmallField(parent, "Mã cư dân:", "CD0001", fx, 44);
        AddSmallField(parent, "Họ và tên:", "Nguyễn Văn An", fx, 82);
        AddSmallField(parent, "Ngày sinh:", "12/06/1990", fx, 120);
        AddSmallField(parent, "CCCD:", "001098012345", fx, 158);
        AddSmallField(parent, "Email:", "an.nguyen@gmail.com", fx, 196);
        AddSmallField(parent, "Số điện thoại:", "0901 234 567", fx, 234);
        AddSmallField(parent, "Địa chỉ thường trú:", "123 Nguyễn Trãi, Thanh Xuân, Hà Nội", fx, 272);
        AddSmallField(parent, "Căn hộ liên kết:", "A-1205 - Tòa A - Tầng 12", fx, 310);
        AddSmallField(parent, "Vai trò trong căn hộ:", "Chủ hộ", fx, 348);
        AddSmallField(parent, "Tình trạng cư trú:", "Đang cư trú", fx, 386);

        var historyTitle = ModernUi.Label("Lịch sử cư trú", 9.5f, FontStyle.Bold, ModernUi.Blue);
        historyTitle.Location = new Point(24, 430);
        historyTitle.Size = new Size(180, 24);
        parent.Controls.Add(historyTitle);
        var history = CreateGrid(
            new[] { "STT", "Căn hộ", "Vai trò", "Tình trạng", "Ngày bắt đầu", "Ngày kết thúc", "Ghi chú" },
            new object[][]
            {
                new object[] { 1, "A-1205", "Chủ hộ", "Đang cư trú", "12/03/2024", "-", "" },
                new object[] { 2, "A-0703", "Thành viên", "Chuyển ra", "01/06/2022", "11/03/2024", "Chuyển căn hộ" },
                new object[] { 3, "A-0501", "Thành viên", "Chuyển ra", "10/01/2020", "31/05/2022", "Chuyển căn hộ" }
            });
        history.Location = new Point(18, 458);
        history.Size = new Size(parent.Width - 36, 66);
        parent.Controls.Add(history);
    }

    private static void AddInvoiceDetail(Control parent)
    {
        var infoTitle = ModernUi.Label("Thông tin căn hộ & chủ hộ", 10f, FontStyle.Bold, ModernUi.Blue);
        infoTitle.Location = new Point(18, 42);
        infoTitle.Size = new Size(260, 24);
        parent.Controls.Add(infoTitle);

        string[] info =
        {
            "Mã hóa đơn:        HD2405-00128                 Ngày lập:        01/05/2024 08:30",
            "Căn hộ:            A-1205 - Tầng 12              Diện tích:       78.6 m²",
            "Chủ hộ:            Nguyễn Văn An                 SĐT:             0901 234 567",
            "Email:             nguyenvan@gmail.com           Số hộ khẩu:      04 người"
        };
        for (int i = 0; i < info.Length; i++)
        {
            var row = ModernUi.Label(info[i], 9.3f, FontStyle.Regular, ModernUi.Text);
            row.Location = new Point(18, 76 + i * 28);
            row.Size = new Size(parent.Width - 36, 26);
            parent.Controls.Add(row);
        }

        var detailTitle = ModernUi.Label("Chi tiết các khoản phí", 10f, FontStyle.Bold, ModernUi.Blue);
        detailTitle.Location = new Point(18, 196);
        detailTitle.Size = new Size(260, 24);
        parent.Controls.Add(detailTitle);
        var grid = CreateGrid(
            new[] { "STT", "Khoản phí", "Đơn giá (VNĐ)", "Số lượng/Diện tích", "Thành tiền (VNĐ)" },
            new object[][]
            {
                new object[] { 1, "Phí quản lý", "11,000", "78.6 m²", "864,600" },
                new object[] { 2, "Phí gửi xe", "120,000", "1 xe máy", "120,000" },
                new object[] { 3, "Phí vệ sinh", "10,000", "78.6 m²", "786,000" },
                new object[] { 4, "Phí điện", "3,500", "120 kWh", "420,000" },
                new object[] { 5, "Phí nước", "15,000", "25 m³", "375,000" },
                new object[] { 6, "Phí phát sinh khác", "-", "-", "-" }
            });
        grid.Location = new Point(18, 226);
        grid.Size = new Size(parent.Width - 36, 144);
        parent.Controls.Add(grid);
        var total = ModernUi.Label("TỔNG CỘNG                                                   2,565,600 VNĐ", 11f, FontStyle.Bold, ModernUi.Red);
        total.Location = new Point(18, 382);
        total.Size = new Size(parent.Width - 36, 28);
        parent.Controls.Add(total);
    }

    private static void AddComplaintDetail(Control parent)
    {
        var sender = ModernUi.Label("Người gửi\r\nNguyễn Văn An (Cư dân)\r\nCăn hộ A-1205 - Tòa A\r\n0987 654 321",
            9f, FontStyle.Regular, ModernUi.Text);
        sender.Location = new Point(18, 48);
        sender.Size = new Size(260, 90);
        parent.Controls.Add(sender);

        var owner = ModernUi.Label("Người phụ trách\r\nTrần Minh Tuấn (Kỹ thuật)\r\nPhòng Kỹ thuật\r\n0901 234 567",
            9f, FontStyle.Regular, ModernUi.Text);
        owner.Location = new Point(parent.Width / 2 + 16, 48);
        owner.Size = new Size(260, 90);
        parent.Controls.Add(owner);

        var contentLabel = ModernUi.Label("Nội dung phản ánh", 9f, FontStyle.Bold, ModernUi.Text);
        contentLabel.Location = new Point(18, 138);
        contentLabel.Size = new Size(200, 24);
        parent.Controls.Add(contentLabel);
        var content = new TextBox
        {
            Text = "Thang máy số 2 tại tòa A bị kẹt giữa tầng 8 và 9 khoảng 5 phút lúc 08:45 sáng nay, rất nguy hiểm cho người già và trẻ nhỏ.",
            Location = new Point(18, 164),
            Size = new Size(parent.Width / 2 - 36, 74),
            Multiline = true,
            Font = ModernUi.Font(9f)
        };
        parent.Controls.Add(content);

        var statusLabel = ModernUi.Label("Trạng thái xử lý", 9f, FontStyle.Bold, ModernUi.Text);
        statusLabel.Location = new Point(parent.Width / 2 + 16, 138);
        statusLabel.Size = new Size(200, 24);
        parent.Controls.Add(statusLabel);
        var status = ModernUi.ComboBox(new[] { "Đang xử lý", "Đã tiếp nhận", "Đã xử lý" }, parent.Width / 2 - 36);
        status.Location = new Point(parent.Width / 2 + 16, 164);
        parent.Controls.Add(status);

        var response = new TextBox
        {
            Text = "Đã kiểm tra và xử lý tạm thời. Sẽ thay cảm biến mới trong ngày hôm nay.",
            Location = new Point(parent.Width / 2 + 16, 214),
            Size = new Size(parent.Width / 2 - 36, 64),
            Multiline = true,
            Font = ModernUi.Font(9f)
        };
        parent.Controls.Add(response);

        var photo1 = new RoundedPanel { Location = new Point(18, 278), Size = new Size(90, 74), BackColor = Color.FromArgb(66, 73, 84) };
        parent.Controls.Add(photo1);
        var photo2 = new RoundedPanel { Location = new Point(124, 278), Size = new Size(90, 74), BackColor = Color.FromArgb(78, 84, 94) };
        parent.Controls.Add(photo2);
        var rating = ModernUi.Label("★ ★ ★ ★ ☆   (4/5)", 13f, FontStyle.Bold, ModernUi.Orange);
        rating.Location = new Point(parent.Width / 2 + 16, 308);
        rating.Size = new Size(240, 34);
        parent.Controls.Add(rating);
        var save = ModernUi.OutlineButton("Lưu đánh giá", 112, 34);
        save.Location = new Point(parent.Width - 140, 310);
        parent.Controls.Add(save);
    }

    private static void AddTimeline(Control parent)
    {
        int x = 42;
        int y = 76;
        var line = new Panel { BackColor = Color.FromArgb(202, 213, 228), Location = new Point(x + 19, y + 44), Size = new Size(3, 260) };
        parent.Controls.Add(line);
        AddTimelineItem(parent, x, y, ModernUi.Orange, "!", "#PA240515-001 - Đèn hành lang không sáng", "Đang xử lý\r\n16/05/2024 08:45");
        AddTimelineItem(parent, x, y + 110, ModernUi.Green, "✓", "Tiếp nhận phản ánh", "16/05/2024 08:45");
        AddTimelineItem(parent, x, y + 220, ModernUi.Blue, "⚙", "Đang xử lý", "16/05/2024 10:20");
        AddTimelineItem(parent, x, y + 330, Color.FromArgb(209, 213, 219), "○", "Hoàn tất", "Chưa hoàn thành");
    }

    private static void AddTimelineItem(Control parent, int x, int y, Color color, string icon, string title, string body)
    {
        var dot = new CircleLabel
        {
            Text = icon,
            CircleColor = color,
            ForeColor = Color.White,
            Font = ModernUi.Font(14f, FontStyle.Bold),
            Location = new Point(x, y),
            Size = new Size(42, 42)
        };
        parent.Controls.Add(dot);
        var text = ModernUi.Label(title + "\r\n" + body, 9.5f, FontStyle.Regular, ModernUi.Text);
        text.Location = new Point(x + 70, y - 2);
        text.Size = new Size(parent.Width - x - 95, 76);
        parent.Controls.Add(text);
    }

    private string GetDefaultPage() => "dashboard";
    private bool IsResident => RoleName().Contains("resident", StringComparison.OrdinalIgnoreCase) || RoleName().Contains("cư dân", StringComparison.OrdinalIgnoreCase) || CurrentUsername().StartsWith("resident", StringComparison.OrdinalIgnoreCase);
    private bool IsManager => !IsResident && (RoleName().Contains("manager", StringComparison.OrdinalIgnoreCase) || RoleName().Contains("quản lý", StringComparison.OrdinalIgnoreCase) || CurrentUsername().StartsWith("manager", StringComparison.OrdinalIgnoreCase));
    private string RoleName() => _session?.RoleName ?? "Super Admin";
    private string CurrentUsername() => _session?.Username ?? "superadmin";
    private string CurrentDisplayName() => _session?.FullName ?? (IsResident ? "Nguyễn Văn An" : CurrentUsername());
    private string RoleDisplay() => IsResident ? "Cư dân" : IsManager ? "Quản lý khu chung cư" : "Super Admin";
    private string RoleFooterLabel() => IsResident ? "Cư dân" : "Tên người dùng";
    private int NotificationCount() => IsResident ? 5 : IsManager ? 8 : 5;

    private void UpdateClock()
    {
        if (_clockLabel != null)
        {
            _clockLabel.Text = $"◷  Ngày giờ hệ thống: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
        }
    }

    private void InitSampleData()
    {
        _apartments.AddRange(new[]
        {
            new CanHo { MaCanHo = "A-1205", Tang = "12", LoaiCanHo = "2 PN - 2 WC", DienTich = 68.5, TrangThai = "Đang sử dụng", MaCuDan = "CD0001" },
            new CanHo { MaCanHo = "A-1206", Tang = "12", LoaiCanHo = "1 PN - 1 WC", DienTich = 54.2, TrangThai = "Đang sử dụng", MaCuDan = "CD0002" },
            new CanHo { MaCanHo = "A-1207", Tang = "12", LoaiCanHo = "3 PN - 2 WC", DienTich = 76.3, TrangThai = "Đang trống", MaCuDan = "" },
            new CanHo { MaCanHo = "A-1208", Tang = "12", LoaiCanHo = "3 PN - 2 WC", DienTich = 89.1, TrangThai = "Đang sử dụng", MaCuDan = "CD0003" }
        });

        _residents.AddRange(new[]
        {
            new CuDan { MaCuDan = "CD0001", HoTen = "Nguyễn Văn An", CCCD = "001098012345", SoDienThoai = "0901234567", MaCanHo = "A-1205", NgayVao = new DateTime(2024, 3, 12) },
            new CuDan { MaCuDan = "CD0002", HoTen = "Trần Thị Bình", CCCD = "001198023456", SoDienThoai = "0912345678", MaCanHo = "A-1205", NgayVao = new DateTime(2024, 3, 12) }
        });

        _fees.AddRange(new[]
        {
            new PhiDichVu { MaPhieu = "HD2405-00128", MaCanHo = "A-1205", Thang = 5, Nam = 2024, PhiQuanLy = 864600, PhiDienNuoc = 795000, PhiGuiXe = 120000, TrangThai = "Đã thanh toán" },
            new PhiDichVu { MaPhieu = "HD2405-00127", MaCanHo = "B-0803", Thang = 5, Nam = 2024, PhiQuanLy = 780000, PhiDienNuoc = 80000, PhiGuiXe = 120000, TrangThai = "Đã thanh toán" }
        });
    }
}

internal sealed class AccountAvatarControl : Control
{
    public AccountAvatarControl()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var bounds = new Rectangle(1, 1, Width - 3, Height - 3);
        using var bg = new SolidBrush(Color.FromArgb(228, 238, 251));
        using var border = new Pen(Color.FromArgb(198, 214, 236));
        e.Graphics.FillEllipse(bg, bounds);
        e.Graphics.DrawEllipse(border, bounds);

        using var hair = new SolidBrush(Color.FromArgb(37, 48, 66));
        using var skin = new SolidBrush(Color.FromArgb(255, 190, 139));
        using var shirt = new SolidBrush(ModernUi.Blue);
        using var neck = new SolidBrush(Color.FromArgb(238, 164, 116));

        int cx = Width / 2;
        e.Graphics.FillEllipse(hair, cx - 28, 26, 56, 44);
        e.Graphics.FillEllipse(skin, cx - 24, 34, 48, 52);
        e.Graphics.FillRectangle(neck, cx - 11, 78, 22, 18);
        e.Graphics.FillPie(shirt, cx - 46, 84, 92, 78, 200, 140);
        e.Graphics.FillRectangle(shirt, cx - 35, 95, 70, 34);

        using var eye = new SolidBrush(Color.FromArgb(35, 48, 65));
        e.Graphics.FillEllipse(eye, cx - 14, 56, 4, 4);
        e.Graphics.FillEllipse(eye, cx + 10, 56, 4, 4);
        using var mouth = new Pen(Color.FromArgb(140, 70, 68), 2);
        e.Graphics.DrawArc(mouth, cx - 10, 66, 20, 12, 15, 150);
    }
}

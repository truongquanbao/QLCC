using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
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
    private const int SidebarWidth = 238;
    private const int HeaderHeight = 64;
    private const int FooterHeight = 64;
    private static readonly Font GridBadgeFont = ModernUi.Font(8.4f, FontStyle.Bold);

    private readonly List<CanHo> _apartments = new();
    private readonly List<CuDan> _residents = new();
    private readonly List<PhiDichVu> _fees = new();
    private readonly Dictionary<string, Button> _navButtons = new();
    private UserSession? _session;

    private Panel _sidebar = null!;
    private Panel _content = null!;
    private Panel _footer = null!;
    private Label _clockLabel = null!;
    private string _activePage = "dashboard";
    private Timer _clockTimer = null!;
    private ContextMenuStrip? _quickActionMenu;
    private string? _quickActionMenuKey;
    private Control? _quickActionAnchor;

    public FrmMainDashboard()
    {
        _session = SessionManager.GetSession();
        InitSampleData();
        InitializeComponent();
        BuildShell();
        Shown += (_, _) =>
        {
            Navigate(GetDefaultPage());
        };
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
        if (_clockTimer != null)
        {
            _clockTimer.Stop();
            _clockTimer.Dispose();
        }

        if (_quickActionMenu is { IsDisposed: false })
        {
            _quickActionMenu.Close();
            _quickActionMenu = null;
            _quickActionMenuKey = null;
            _quickActionAnchor = null;
        }

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

        var logo = ModernUi.Label("▦", 24f, FontStyle.Bold, Color.White);
        logo.Location = new Point(14, 8);
        logo.Size = new Size(38, 46);
        logo.TextAlign = ContentAlignment.MiddleCenter;
        topBar.Controls.Add(logo);

        var appTitle = ModernUi.Label("PHẦN MỀM QUẢN LÝ KHU CHUNG CƯ", 15.8f, FontStyle.Bold, Color.White);
        appTitle.Location = new Point(64, 9);
        appTitle.Size = new Size(560, 44);
        topBar.Controls.Add(appTitle);

        var close = CreateWindowButton("×");
        close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        close.Location = new Point(ClientSize.Width - 48, 15);
        close.Click += (_, _) => Close();
        topBar.Controls.Add(close);

        var maximize = CreateWindowButton("□");
        maximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        maximize.Location = new Point(ClientSize.Width - 88, 15);
        maximize.Click += (_, _) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        topBar.Controls.Add(maximize);

        var minimize = CreateWindowButton("−");
        minimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        minimize.Location = new Point(ClientSize.Width - 128, 15);
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

        var user = ModernUi.Label($"●  {RoleFooterLabel()}: {FooterDisplayName()}", 10f, FontStyle.Regular, Color.White);
        user.Location = new Point(30, 15);
        user.Size = new Size(280, 34);
        _footer.Controls.Add(user);

        var role = ModernUi.Label($"◆  Vai trò: {RoleDisplay()}", 10f, FontStyle.Regular, Color.White);
        role.Location = new Point(345, 15);
        role.Size = new Size(300, 34);
        _footer.Controls.Add(role);

        _clockLabel = ModernUi.Label("", 10f, FontStyle.Regular, Color.White);
        _clockLabel.Location = new Point(645, 15);
        _clockLabel.Size = new Size(420, 34);
        _footer.Controls.Add(_clockLabel);

        var db = ModernUi.Label("▰  Trạng thái kết nối:  Đã kết nối SQL Server", 10f, FontStyle.Regular, Color.White);
        db.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        db.Location = new Point(Width - 435, 15);
        db.Size = new Size(410, 34);
        _footer.Controls.Add(db);
        _footer.Resize += (_, _) => db.Left = _footer.ClientSize.Width - 435;
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
            Padding = new Padding(12, 13, 12, 0),
            AutoScroll = true,
            BackColor = _sidebar.BackColor
        };
        _sidebar.Controls.Add(menu);

        string? currentGroup = null;
        foreach (var item in MenuItemsForRole())
        {
            // Add group header if group changed
            if (item.Group != currentGroup && !string.IsNullOrEmpty(item.Group))
            {
                currentGroup = item.Group;
                var groupHeader = new Label
                {
                    Text = item.Group,
                    Width = SidebarWidth - 24,
                    Height = 32,
                    Margin = new Padding(0, item.Group == MenuItemsForRole().First().Group ? 0 : 8, 0, 4),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 0, 0, 0),
                    Font = ModernUi.Font(8.4f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    BackColor = _sidebar.BackColor
                };
                menu.Controls.Add(groupHeader);
            }
            else if (string.IsNullOrEmpty(item.Group) && currentGroup != null)
            {
                currentGroup = null;
            }

            var button = new Button
            {
                Text = $"{item.Icon}   {item.Text}",
                Tag = item.Key,
                Width = SidebarWidth - 24,
                Height = 44,
                Margin = new Padding(0, 0, 0, 4),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                Font = ModernUi.Font(9.8f, FontStyle.Regular),
                BackColor = _sidebar.BackColor,
                ForeColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(15, 23, 42);
            button.Click += (_, _) => Navigate(item.Key);
            _navButtons[item.Key] = button;
            menu.Controls.Add(button);
        }
    }

    private IEnumerable<(string? Group, string Key, string Text, string Icon)> MenuItemsForRole()
    {
        if (IsResident)
        {
            return new[]
            {
                (null, "dashboard", "Dashboard cá nhân", "⌂"),
                (null, "profile", "Hồ sơ cá nhân", "●"),
                ("Thông tin", "apartment-info", "Thông tin căn hộ", "▦"),
                ("Thông tin", "my-invoices", "Hóa đơn của tôi", "▤"),
                ("Thông tin", "payment", "Thanh toán / Lịch sử", "▰"),
                ("Hỗ trợ", "send-complaint", "Gửi phản ánh", "■"),
                ("Hỗ trợ", "notifications", "Thông báo", "◆"),
                ("Hỗ trợ", "vehicles", "Xe của tôi", "▣"),
                ("Hỗ trợ", "visitors", "Khách của tôi", "♙"),
                ("Cài đặt", "password", "Đổi mật khẩu", "□")
            };
        }

        if (IsManager)
        {
            return new[]
            {
                (null, "dashboard", "Dashboard", "◉"),
                ("Quản lý", "apartments", "Căn hộ", "▦"),
                ("Quản lý", "residents", "Cư dân", "●"),
                ("Quản lý", "invoices", "Hóa đơn / phí", "▤"),
                ("Vận hành", "complaints", "Phản ánh", "■"),
                ("Vận hành", "vehicles", "Phương tiện", "▣"),
                ("Vận hành", "visitors", "Khách ra vào", "♙"),
                ("Vận hành", "assets", "Tài sản", "◇"),
                ("Hỗ trợ", "notifications", "Thông báo", "◆"),
                ("Hỗ trợ", "reports", "Báo cáo", "▥"),
                ("Cài đặt", "profile", "Hồ sơ cá nhân", "●")
            };
        }

        return new[]
        {
            (null, "dashboard", "Dashboard", "◉"),
            ("Quản lý", "accounts", "Quản lý tài khoản", "●"),
            ("Quản lý", "permissions", "Phân quyền", "□"),
            ("Quản lý", "apartments", "Tòa nhà / căn hộ", "▦"),
            ("Quản lý", "residents", "Cư dân", "●"),
            ("Quản lý", "invoices", "Hóa đơn / phí", "▤"),
            ("Vận hành", "complaints", "Phản ánh", "■"),
            ("Vận hành", "vehicles", "Phương tiện", "▣"),
            ("Vận hành", "visitors", "Khách ra vào", "♙"),
            ("Vận hành", "assets", "Tài sản", "◇"),
            ("Hệ thống", "reports", "Báo cáo", "▥"),
            ("Hệ thống", "logs", "Log hệ thống", "▤"),
            ("Hệ thống", "settings", "Cấu hình hệ thống", "⚙")
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
            case "notifications":
                RenderNotificationsPage();
                break;
            case "vehicles":
                RenderVehicles();
                break;
            case "visitors":
                RenderVisitors();
                break;
            case "profile":
                RenderProfilePage();
                break;
            case "password":
                ShowChangePasswordDialog();
                RenderProfilePage();
                break;
            case "assets":
                RenderAssets();
                break;
            case "reports":
                RenderReports();
                break;
            case "logs":
                RenderSystemLogs();
                break;
            case "settings":
                RenderSystemSettings();
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
            Font = ModernUi.Font(12f, FontStyle.Bold),
            Location = new Point(22, 17),
            Size = new Size(28, 28)
        };
        header.Controls.Add(headerIcon);

        var label = ModernUi.Label(title, 15f, FontStyle.Bold, ModernUi.Navy);
        label.AutoEllipsis = true;
        int rightReserved = IsResident ? 24 : 500;
        int maxTitleWidth = Math.Min(560, Math.Max(220, header.Width - 63 - rightReserved - 90));
        int titleWidth = Math.Min(maxTitleWidth, Math.Max(130, TextRenderer.MeasureText(title, label.Font).Width + 10));
        label.Location = new Point(63, 14);
        label.Size = new Size(titleWidth, 30);
        header.Controls.Add(label);

        var divider = new Panel
        {
            BackColor = ModernUi.Border,
            Location = new Point(label.Right + 16, 14),
            Size = new Size(1, 35)
        };
        header.Controls.Add(divider);

        var crumb = ModernUi.Label(breadcrumb, 8.8f, FontStyle.Regular, ModernUi.Muted);
        crumb.Location = new Point(divider.Right + 16, 18);
        crumb.AutoEllipsis = true;
        crumb.Size = new Size(Math.Max(0, header.Width - crumb.Left - rightReserved), 24);
        header.Controls.Add(crumb);

        if (!IsResident)
        {
            var search = ModernUi.SearchBox("Tìm kiếm nhanh...", 255, 36);
            search.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            search.Location = new Point(header.Width - 455, 12);
            header.Controls.Add(search);

            var bell = ModernUi.IconButton("🔔", 36);
            bell.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bell.Location = new Point(header.Width - 176, 12);
            bell.Cursor = Cursors.Hand;
            header.Controls.Add(bell);

            var badge = new CircleLabel
            {
                Text = NotificationCount().ToString(),
                CircleColor = ModernUi.Red,
                ForeColor = Color.White,
                Font = ModernUi.Font(8f, FontStyle.Bold),
                Location = new Point(header.Width - 160, 6),
                Size = new Size(19, 19)
            };
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            badge.Cursor = Cursors.Hand;
            header.Controls.Add(badge);

            var avatar = new CircleLabel
            {
                Text = "●",
                CircleColor = Color.FromArgb(226, 236, 248),
                ForeColor = ModernUi.Navy,
                Font = ModernUi.Font(13f, FontStyle.Bold),
                Location = new Point(header.Width - 121, 13),
                Size = new Size(34, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            avatar.Cursor = Cursors.Hand;
            header.Controls.Add(avatar);

            var userName = ModernUi.Label($"{CurrentUsername()} ▾", 9f, FontStyle.Bold, ModernUi.Text);
            userName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            userName.Location = new Point(header.Width - 83, 16);
            userName.Size = new Size(78, 28);
            userName.Cursor = Cursors.Hand;
            header.Controls.Add(userName);

            void ToggleNotificationDropdown()
            {
                try
                {
                    ShowNotificationMenu(bell);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        $"Không thể tải danh sách thông báo.\nChi tiết: {ex.Message}",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            void ToggleAccountDropdown()
            {
                try
                {
                    ShowAccountMenu(userName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        $"Không thể mở menu tài khoản.\nChi tiết: {ex.Message}",
                        "Tài khoản",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            bell.Click += (_, _) => ToggleNotificationDropdown();
            badge.Click += (_, _) => ToggleNotificationDropdown();
            avatar.Click += (_, _) => ToggleAccountDropdown();
            userName.Click += (_, _) => ToggleAccountDropdown();
        }

        return page;
    }

    private int PageWorkWidth(int minWidth = 980)
    {
        int contentWidth = _content.ClientSize.Width > 0 ? _content.ClientSize.Width : Math.Max(1024, Width - SidebarWidth);
        return Math.Max(minWidth, contentWidth - 36 - SystemInformation.VerticalScrollBarWidth);
    }

    private static object[][] RowsOrEmpty<T>(IEnumerable<T> items, int columnCount, Func<T, int, object[]> map, string message = "Không có dữ liệu")
    {
        var rows = items.Select(map).ToArray();
        return rows.Length > 0 ? rows : new[] { EmptyRow(columnCount, message) };
    }

    private static object[] EmptyRow(int columnCount, string message)
    {
        var row = Enumerable.Repeat<object>("", columnCount).ToArray();
        row[0] = message;
        return row;
    }

    private static string Display(string? value, string fallback = "-")
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string DateText(DateTime? value)
        => value.HasValue && value.Value > DateTime.MinValue ? value.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "-";

    private static string DateTimeText(DateTime? value)
        => value.HasValue && value.Value > DateTime.MinValue ? value.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : "-";

    private static string Money(decimal value)
        => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string MoneyShort(decimal value)
    {
        if (value >= 1_000_000_000m)
        {
            return $"{value / 1_000_000_000m:0.##}B";
        }

        if (value >= 1_000_000m)
        {
            return $"{value / 1_000_000m:0.##}M";
        }

        return Money(value);
    }

    private static int ChartValue(decimal value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value >= int.MaxValue ? int.MaxValue : (int)value;
    }

    private static string InvoiceCode(InvoiceDTO invoice)
        => $"HD{invoice.Year}{invoice.Month:00}-{invoice.InvoiceID:00000}";

    private static string ViStatus(string? status)
    {
        return (status ?? string.Empty).Trim() switch
        {
            "Active" => "Hoạt động",
            "Inactive" => "Tạm khóa",
            "Pending" => "Chờ duyệt",
            "Approved" => "Đã duyệt",
            "Rejected" => "Từ chối",
            "Paid" => "Đã thanh toán",
            "Partial" => "Thanh toán một phần",
            "Unpaid" => "Chưa thanh toán",
            "Overdue" => "Quá hạn",
            "New" => "Mới",
            "Open" => "Mới",
            "InProgress" or "In Progress" => "Đang xử lý",
            "Resolved" => "Đã xử lý",
            "Closed" => "Đã đóng",
            "High" => "Cao",
            "Medium" => "Trung bình",
            "Low" => "Thấp",
            "Occupied" or "Using" or "InUse" => "\u0110ang s\u1eed d\u1ee5ng",
            "Renting" => "Đang thuê",
            "Vacant" or "Empty" or "Available" => "\u0110ang tr\u1ed1ng",
            "Maintenance" => "Bảo trì",
            "Locked" => "Đang khóa",
            "Sent" => "Đã gửi",
            "Draft" => "Nháp",
            "Failed" => "Lỗi",
            "CheckedOut" => "Đã rời",
            "CheckedIn" => "Đã vào",
            "" => "-",
            var other => other
        };
    }

    private static string ResidentLivingStatus(ResidentDTO? resident)
    {
        string residentStatus = Display(resident?.ResidentStatus, "");
        if (residentStatus != "")
        {
            return residentStatus;
        }

        return (resident?.Status ?? string.Empty).Trim() switch
        {
            "Active" => "Đang cư trú",
            "MovedOut" or "Moved Out" => "Chuyển ra",
            "" => "-",
            var other => ViStatus(other)
        };
    }

    private static bool IsActiveStatus(string? status)
        => string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(status, "Hoạt động", StringComparison.OrdinalIgnoreCase);

    private static string ViVehicleType(string? type)
    {
        return (type ?? string.Empty).Trim() switch
        {
            "Car" or "Ô tô" => "Ô tô",
            "Motorbike" or "Motorcycle" or "Xe máy" => "Xe máy",
            "ElectricBike" or "Xe đạp điện" => "Xe đạp điện",
            "Bicycle" or "Xe đạp" => "Xe đạp",
            "" => "-",
            var other => other
        };
    }

    private static string UserRoleLabel(string? roleName)
    {
        return (roleName ?? string.Empty).Trim() switch
        {
            "Manager" => "Quản lý",
            "Resident" => "Cư dân",
            "" => "-",
            var other => other
        };
    }

    private static string DbUserStatus(string? status)
    {
        return (status ?? string.Empty).Trim() switch
        {
            "Hoạt động" => "Active",
            "Tạm khóa" => "Inactive",
            "Chờ duyệt" => "Pending",
            "Từ chối" => "Rejected",
            "Đã duyệt" => "Approved",
            "" => "Pending",
            var other => other
        };
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        Span<char> password = stackalloc char[12];
        for (int i = 0; i < password.Length; i++)
        {
            password[i] = chars[Random.Shared.Next(chars.Length)];
        }

        bool hasUpper = false;
        bool hasLower = false;
        bool hasDigit = false;
        bool hasSpecial = false;
        for (int i = 0; i < password.Length; i++)
        {
            char ch = password[i];
            hasUpper |= char.IsUpper(ch);
            hasLower |= char.IsLower(ch);
            hasDigit |= char.IsDigit(ch);
            hasSpecial |= "!@#$%^&*".Contains(ch);
        }

        if (!hasUpper)
        {
            password[0] = 'A';
        }

        if (!hasLower)
        {
            password[1] = 'b';
        }

        if (!hasDigit)
        {
            password[2] = '7';
        }

        if (!hasSpecial)
        {
            password[3] = '!';
        }

        return new string(password);
    }

    private static string AuditLevel(string? action)
    {
        string value = action ?? string.Empty;
        if (value.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Lỗi", StringComparison.OrdinalIgnoreCase))
        {
            return "Lỗi";
        }

        if (value.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Reset", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Reject", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Xóa", StringComparison.OrdinalIgnoreCase))
        {
            return "Cảnh báo";
        }

        return "Thông tin";
    }

    private static List<SystemConfigRow> GetSystemConfigs()
    {
        var rows = new List<SystemConfigRow>();
        try
        {
            const string query = @"
                SELECT c.ConfigKey, c.ConfigValue, c.Description, c.UpdatedAt, ISNULL(u.Username, '') AS UpdatedBy
                FROM SystemConfig c
                LEFT JOIN Users u ON c.UpdatedBy = u.UserID
                ORDER BY c.ConfigKey";

            using var connection = DatabaseHelper.CreateConnection();
            using var command = new SqlCommand(query, connection);
            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new SystemConfigRow
                {
                    ConfigKey = reader.GetString(0),
                    ConfigValue = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    UpdatedAt = reader.GetDateTime(3),
                    UpdatedBy = reader.GetString(4)
                });
            }
        }
        catch
        {
            // Keep the dashboard usable if the optional config table has not been migrated yet.
        }

        return rows;
    }

    private static string ConfigValue(List<SystemConfigRow> configs, string key, string fallback)
        => configs.FirstOrDefault(c => string.Equals(c.ConfigKey, key, StringComparison.OrdinalIgnoreCase))?.ConfigValue ?? fallback;

    private sealed class SystemConfigRow
    {
        public string ConfigKey { get; init; } = "";
        public string ConfigValue { get; init; } = "";
        public string Description { get; init; } = "";
        public DateTime UpdatedAt { get; init; }
        public string UpdatedBy { get; init; } = "";
    }

    private static DateTime? ParseDashboardDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] formats =
        {
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "MM/dd/yyyy HH:mm:ss"
        };

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var exact))
        {
            return exact;
        }

        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var local))
        {
            return local;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var invariant)
            ? invariant
            : null;
    }

    private static bool IsBackupOverdue(DateTime? lastBackupAt, int warningDays)
    {
        int threshold = warningDays > 0 ? warningDays : 7;
        return !lastBackupAt.HasValue || lastBackupAt.Value < DateTime.Now.AddDays(-threshold);
    }

    private static string BackupDisplayText(string rawValue)
        => ParseDashboardDate(rawValue) is DateTime parsed
            ? DateTimeText(parsed)
            : (string.IsNullOrWhiteSpace(rawValue) ? "Chưa có dữ liệu" : rawValue);

    private void ReloadCurrentPage()
    {
        Navigate(_activePage);
    }

    private bool TryToggleQuickMenu(Control anchor, string menuKey)
    {
        if (_quickActionMenu is { IsDisposed: false } &&
            _quickActionMenu.Visible &&
            ReferenceEquals(_quickActionAnchor, anchor) &&
            string.Equals(_quickActionMenuKey, menuKey, StringComparison.Ordinal))
        {
            _quickActionMenu.Close();
            return true;
        }

        return false;
    }

    private void ShowAccountMenu(Control anchor)
    {
        if (TryToggleQuickMenu(anchor, "account"))
        {
            return;
        }

        ShowQuickActionMenu(anchor, "account",
            ("Thông tin tài khoản / Hồ sơ cá nhân", () => Navigate("profile")),
            ("Đổi mật khẩu", () => ShowChangePasswordDialog()),
            ("Cài đặt", OpenAccountSettings),
            ("Đăng xuất", PerformLogout),
            ("Thoát chương trình", ConfirmExitApplication));
    }

    private void ShowNotificationMenu(Control anchor)
    {
        if (anchor.IsDisposed || !anchor.IsHandleCreated)
        {
            return;
        }

        if (TryToggleQuickMenu(anchor, "notification"))
        {
            return;
        }

        if (_quickActionMenu is { IsDisposed: false })
        {
            _quickActionMenu.Close();
            _quickActionMenu = null;
        }

        var notifications = GetHeaderNotifications().Take(6).ToList();
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Font = new Font("Segoe UI", 9.2f, FontStyle.Regular)
        };

        _quickActionMenu = menu;
        _quickActionMenuKey = "notification";
        _quickActionAnchor = anchor;

        var titleItem = new ToolStripMenuItem("Thông báo gần đây")
        {
            Enabled = false,
            Font = new Font("Segoe UI", 9.4f, FontStyle.Bold)
        };
        menu.Items.Add(titleItem);
        menu.Items.Add(new ToolStripSeparator());

        foreach (var notification in notifications)
        {
            string caption = BuildNotificationCaption(notification);
            var item = new ToolStripMenuItem(caption)
            {
                AutoToolTip = true,
                ToolTipText = BuildNotificationTooltip(notification)
            };

            if (!notification.IsRead)
            {
                item.Font = new Font("Segoe UI", 9.2f, FontStyle.Bold);
            }

            item.Click += (_, _) =>
            {
                try
                {
                    OpenNotificationDetail(notification);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        $"Không thể mở thông báo.\nChi tiết: {ex.Message}",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };
            menu.Items.Add(item);
        }

        menu.Items.Add(new ToolStripSeparator());

        var viewAllItem = new ToolStripMenuItem("Xem tất cả thông báo");
        viewAllItem.Click += (_, _) => Navigate("notifications");
        menu.Items.Add(viewAllItem);

        var markAllReadItem = new ToolStripMenuItem("Đánh dấu đã đọc");
        markAllReadItem.Click += (_, _) =>
        {
            try
            {
                MarkAllNotificationsAsRead();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Không thể cập nhật thông báo.\nChi tiết: {ex.Message}",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        };
        menu.Items.Add(markAllReadItem);

        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_quickActionMenu, menu))
            {
                _quickActionMenu = null;
                _quickActionMenuKey = null;
                _quickActionAnchor = null;
            }
        };

        menu.Show(anchor, new Point(0, anchor.Height));
    }

    private void ShowQuickActionMenu(Control anchor, params (string Text, Action Handler)[] items)
    {
        ShowQuickActionMenu(anchor, "quick-action", items);
    }

    private void ShowQuickActionMenu(Control anchor, string menuKey, params (string Text, Action Handler)[] items)
    {
        if (anchor.IsDisposed || !anchor.IsHandleCreated)
        {
            return;
        }

        if (_quickActionMenu is { IsDisposed: false })
        {
            _quickActionMenu.Close();
            _quickActionMenu = null;
        }

        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Font = new Font("Segoe UI", 9.2f, FontStyle.Regular)
        };
        _quickActionMenu = menu;
        _quickActionMenuKey = menuKey;
        _quickActionAnchor = anchor;

        foreach (var (text, handler) in items)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += (_, _) =>
            {
                if (IsDisposed)
                {
                    return;
                }

                BeginInvoke(new Action(() =>
                {
                    if (!IsDisposed)
                    {
                        handler();
                    }
                }));
            };
            menu.Items.Add(item);
        }

        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_quickActionMenu, menu))
            {
                _quickActionMenu = null;
                _quickActionMenuKey = null;
                _quickActionAnchor = null;
            }
        };
        menu.Show(anchor, new Point(0, anchor.Height));
    }

    private void OpenManagementDialog<T>() where T : Form, new()
    {
        using var form = new T();
        form.StartPosition = FormStartPosition.CenterParent;
        form.ShowDialog(this);
        ReloadCurrentPage();
    }

    private void SaveDashboardSnapshot()
    {
        var users = UserDAL.GetAllUsers();
        var residents = ResidentDAL.GetAllResidents();
        var apartments = ApartmentDAL.GetAllApartments();
        var invoices = InvoiceDAL.GetAllInvoices();
        var complaints = ComplaintDAL.GetAllComplaints();
        var configs = GetSystemConfigs();
        string lastBackupRaw = ConfigValue(configs, "LastBackupAt", "");

        using var dialog = new SaveFileDialog
        {
            Title = "Lưu ảnh chụp dữ liệu dashboard",
            FileName = $"dashboard_snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            Filter = "CSV (*.csv)|*.csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            RestoreDirectory = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var rows = new[]
        {
            "ThongTin;GiaTri",
            $"Thoi diem tao;{DateTime.Now:dd/MM/yyyy HH:mm:ss}",
            $"Tong tai khoan;{users.Count}",
            $"Tai khoan hoat dong;{users.Count(u => IsActiveStatus(u.Status))}",
            $"So cu dan;{residents.Count}",
            $"So can ho;{apartments.Count}",
            $"Can ho dang su dung;{apartments.Count(a => ViStatus(a.Status) == "Đang sử dụng")}",
            $"Hoa don chua thanh toan;{invoices.Count(i => ViStatus(i.PaymentStatus) != "Đã thanh toán")}",
            $"Phan anh chua dong;{complaints.Count(c => ViStatus(c.Status) is not ("Đã xử lý" or "Đã đóng"))}",
            $"Backup gan nhat;{BackupDisplayText(lastBackupRaw)}"
        };

        File.WriteAllLines(dialog.FileName, rows, new System.Text.UTF8Encoding(true));
        AuditLogDAL.LogAction(_session?.UserID, "DashboardSnapshot", "Dashboard", description: $"Saved dashboard snapshot: {dialog.FileName}");

        MessageBox.Show(this,
            $"Đã lưu dữ liệu dashboard vào:\n{dialog.FileName}",
            "Lưu dữ liệu",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void SaveGeneratedFile((bool Success, string Message, byte[] FileContent, string FileName) result, string filter, string auditAction)
    {
        if (!result.Success || result.FileContent == null || result.FileContent.Length == 0 || string.IsNullOrWhiteSpace(result.FileName))
        {
            MessageBox.Show(this,
                string.IsNullOrWhiteSpace(result.Message) ? "Không thể tạo file xuất." : result.Message,
                "Xuất dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Lưu file xuất",
            FileName = result.FileName,
            Filter = filter,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            RestoreDirectory = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        File.WriteAllBytes(dialog.FileName, result.FileContent);
        AuditLogDAL.LogAction(_session?.UserID, auditAction, "Report", description: $"Saved report: {dialog.FileName}");

        MessageBox.Show(this,
            $"Đã xuất file thành công:\n{dialog.FileName}",
            "Báo cáo nhanh",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void RunDatabaseBackup()
    {
        if (!ConfigurationHelper.GetAppSettingAsBool("EnableBackupRestore", true))
        {
            MessageBox.Show(this,
                "Tính năng backup/restore hiện đang bị tắt trong cấu hình ứng dụng.",
                "Backup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var configs = GetSystemConfigs();
            using var connection = DatabaseHelper.CreateConnection();
            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
            {
                throw new InvalidOperationException("Không xác định được tên cơ sở dữ liệu để sao lưu.");
            }

            string backupDirectory = ResolveBackupDirectory(ConfigValue(configs, "BackupPath", ".\\backups"));
            Directory.CreateDirectory(backupDirectory);

            string safeDatabaseName = new string(builder.InitialCatalog.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch).ToArray());
            string backupFilePath = Path.Combine(backupDirectory, $"{safeDatabaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

            using var command = connection.CreateCommand();
            command.CommandTimeout = 0;
            command.CommandText = @"
DECLARE @sql nvarchar(max) =
    N'BACKUP DATABASE ' + QUOTENAME(@DatabaseName) +
    N' TO DISK = N''' + REPLACE(@BackupPath, '''', '''''') + N''' WITH COPY_ONLY, INIT';
EXEC (@sql);";
            command.Parameters.AddWithValue("@DatabaseName", builder.InitialCatalog);
            command.Parameters.AddWithValue("@BackupPath", backupFilePath);

            connection.Open();
            command.ExecuteNonQuery();

            string backupTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            UpsertSystemConfig("LastBackupAt", backupTime, "Thời điểm backup dữ liệu gần nhất");
            UpsertSystemConfig("BackupPath", backupDirectory, "Thư mục chứa các bản sao lưu");

            AuditLogDAL.LogAction(_session?.UserID, "DatabaseBackup", "SystemConfig", description: $"Created backup: {backupFilePath}");

            MessageBox.Show(this,
                $"Đã sao lưu dữ liệu thành công:\n{backupFilePath}",
                "Backup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            ReloadCurrentPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Backup thất bại:\n{ex.Message}",
                "Backup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static string ResolveBackupDirectory(string configuredPath)
    {
        string fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ApartmentManager",
            "Backups");

        if (string.IsNullOrWhiteSpace(configuredPath) || !Path.IsPathRooted(configuredPath))
        {
            return fallback;
        }

        return configuredPath;
    }

    private void UpsertSystemConfig(string key, string value, string description)
    {
        try
        {
            const string query = @"
IF EXISTS (SELECT 1 FROM dbo.SystemConfig WHERE ConfigKey = @ConfigKey)
BEGIN
    UPDATE dbo.SystemConfig
    SET ConfigValue = @ConfigValue,
        Description = CASE WHEN NULLIF(@Description, N'') IS NULL THEN Description ELSE @Description END,
        UpdatedAt = GETDATE(),
        UpdatedBy = @UpdatedBy
    WHERE ConfigKey = @ConfigKey;
END
ELSE
BEGIN
    INSERT INTO dbo.SystemConfig (ConfigKey, ConfigValue, Description, UpdatedAt, UpdatedBy)
    VALUES (@ConfigKey, @ConfigValue, NULLIF(@Description, N''), GETDATE(), @UpdatedBy);
END";

            using var connection = DatabaseHelper.CreateConnection();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ConfigKey", key);
            command.Parameters.AddWithValue("@ConfigValue", value);
            command.Parameters.AddWithValue("@Description", description);
            object updatedBy = _session?.UserID is int userId ? userId : DBNull.Value;
            command.Parameters.AddWithValue("@UpdatedBy", updatedBy);
            connection.Open();
            command.ExecuteNonQuery();
        }
        catch
        {
            // Keep backup usable even if SystemConfig has not been initialized yet.
        }
    }

    private void RenderAdminDashboard()
    {
        var page = BeginPage("Dashboard", "Trang chủ / Tổng quan hệ thống");
        int w = PageWorkWidth(1150);
        int y = 76;
        int gap = 12;
        int cardW = (w - gap * 5) / 6;
        var users = UserDAL.GetAllUsers();
        var residents = ResidentDAL.GetAllResidents();
        var apartments = ApartmentDAL.GetAllApartments();
        var invoices = InvoiceDAL.GetAllInvoices();
        var complaintsData = ComplaintDAL.GetAllComplaints();
        var configs = GetSystemConfigs();
        int occupied = apartments.Count(a => ViStatus(a.Status) == "Đang sử dụng");
        int vacant = apartments.Count(a => ViStatus(a.Status) == "Đang trống");
        int occupancyRate = apartments.Count == 0 ? 0 : (int)Math.Round(occupied * 100m / apartments.Count);
        var latestInvoice = invoices.OrderByDescending(i => i.Year).ThenByDescending(i => i.Month).FirstOrDefault();
        var periodInvoices = latestInvoice == null
            ? new List<InvoiceDTO>()
            : invoices.Where(i => i.Year == latestInvoice.Year && i.Month == latestInvoice.Month).ToList();
        decimal monthRevenue = periodInvoices.Sum(i => i.PaidAmount);
        var unpaid = invoices.Where(i => ViStatus(i.PaymentStatus) != "Đã thanh toán").ToList();
        decimal unpaidAmount = unpaid.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount));
        int backupWarningDays = int.TryParse(ConfigValue(configs, "BackupWarningDays", "7"), out var parsedWarningDays) ? Math.Max(1, parsedWarningDays) : 7;
        string lastBackupRaw = ConfigValue(configs, "LastBackupAt", "");
        DateTime? lastBackupAt = ParseDashboardDate(lastBackupRaw);
        bool backupOverdue = IsBackupOverdue(lastBackupAt, backupWarningDays);
        string backupText = lastBackupAt.HasValue ? DateTimeText(lastBackupAt.Value) : "Chưa có dữ liệu";

        // Reorganized stat cards with emphasis on revenue
        AddRow(page, y, gap,
            ModernUi.StatCard("Số tài khoản", users.Count.ToString("N0"), "Tài khoản", ModernUi.Blue, "●", $"{users.Count(u => IsActiveStatus(u.Status))} hoạt động", cardW),
            ModernUi.StatCard("Số cư dân", residents.Count.ToString("N0"), "Người", ModernUi.Blue, "●●", $"{residents.Count(r => IsActiveStatus(r.Status))} đang cư trú", cardW),
            ModernUi.StatCard("Số căn hộ", apartments.Count.ToString("N0"), "Căn hộ", ModernUi.Blue, "▦", $"{occupied:N0} đang ở", cardW),
            ModernUi.StatCard("Lấp đầy", $"{occupancyRate}%", "Đơn vị", Color.FromArgb(6, 182, 212), "◔", $"{occupied:N0} / {apartments.Count:N0}", cardW),
            ModernUi.StatCard("Doanh thu", Money(monthRevenue), "VNĐ", Color.FromArgb(34, 197, 94), "$", latestInvoice == null ? "Chưa có" : $"Kỳ {latestInvoice.Month:00}/{latestInvoice.Year}", cardW),
            ModernUi.StatCard("Nợ chưa thu", unpaid.Count.ToString("N0"), "Hóa đơn", ModernUi.Red, "▤", $"{Money(unpaidAmount)} VNĐ", cardW));

        y += 144;

        int alertW = Math.Max(260, (int)(w * 0.18));
        int occupancyW = Math.Max(320, (int)(w * 0.24));
        int revenueW = Math.Max(520, w - occupancyW - alertW - gap * 2);

        var revenue = ModernUi.Section("Doanh thu theo tháng (VNĐ)", revenueW, 252);
        revenue.Location = new Point(18, y);
        var chart = new BarChartPanel
        {
            Location = new Point(12, 40),
            Size = new Size(revenue.Width - 24, 194),
            BarColor = ModernUi.Blue,
            AxisMax = 1_500_000_000,
            SeriesLabel = "Doanh thu (VNĐ)"
        };
        var monthlyRevenue = invoices
            .GroupBy(i => new DateTime(i.Year, i.Month, 1))
            .OrderBy(g => g.Key)
            .TakeLast(12)
            .Select(g => (Label: $"T{g.Key.Month}", Value: ChartValue(g.Sum(i => i.PaidAmount))))
            .ToList();
        chart.AxisMax = Math.Max(1, monthlyRevenue.Count == 0 ? 1 : (int)(monthlyRevenue.Max(m => m.Value) * 1.2m));
        chart.Bars.AddRange(monthlyRevenue);
        revenue.Controls.Add(chart);
        page.Controls.Add(revenue);

        var occupancy = ModernUi.Section("Tỷ lệ lấp đầy căn hộ", occupancyW, 252);
        occupancy.Location = new Point(revenue.Right + gap, y);
        occupancy.Controls.Add(new DonutChartPanel
        {
            Percent = occupancyRate,
            CenterText = $"{occupancyRate}%",
            SubText = "Đang ở",
            Location = new Point(12, 40),
            Size = new Size(occupancy.Width - 24, 184)
        });
        page.Controls.Add(occupancy);

        var alert = ModernUi.Section("Cảnh báo hệ thống", alertW, 252);
        alert.BackColor = backupOverdue ? Color.FromArgb(255, 249, 235) : Color.FromArgb(239, 251, 244);
        alert.BorderColor = backupOverdue ? Color.FromArgb(241, 213, 153) : Color.FromArgb(181, 223, 196);
        alert.Location = new Point(occupancy.Right + gap, y);
        var alertIcon = new WarningTriangleControl
        {
            TriangleColor = backupOverdue ? ModernUi.Orange : Color.FromArgb(34, 197, 94),
            Size = new Size(82, 72),
            Location = new Point((alert.Width - 82) / 2, 54)
        };
        alert.Controls.Add(alertIcon);
        string headline = backupOverdue ? $"CHƯA BACKUP QUÁ {backupWarningDays} NGÀY" : "TRẠNG THÁI BACKUP ỔN ĐỊNH";
        var alertText = ModernUi.Label(
            $"{headline}\r\nLần backup gần nhất:\r\n{backupText}",
            11f,
            FontStyle.Bold,
            backupOverdue ? Color.FromArgb(220, 85, 0) : Color.FromArgb(25, 111, 61));
        alertText.Location = new Point(20, 142);
        alertText.Size = new Size(alert.Width - 40, 70);
        alertText.TextAlign = ContentAlignment.MiddleCenter;
        alert.Controls.Add(alertText);
        var backup = ModernUi.Button("▤  Backup ngay", ModernUi.Orange, 160, 36);
        backup.Location = new Point((alert.Width - 160) / 2, 210);
        backup.Click += (_, _) => RunDatabaseBackup();
        alert.Controls.Add(backup);
        page.Controls.Add(alert);

        y += 260;

        int actionsW = Math.Max(360, (int)(w * 0.38));
        int complaintsW = w - actionsW - gap;

        var complaints = ModernUi.Section("Phản ánh đang xử lý", complaintsW, 272);
        complaints.Location = new Point(18, y);
        var openComplaints = complaintsData
            .Where(c => ViStatus(c.Status) is not ("Đã xử lý" or "Đã đóng"))
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .ToList();
        var complaintsGrid = CreateGrid(
            new[] { "STT", "Mã phản ánh", "Nội dung", "Cư dân", "Căn hộ", "Ngày tạo", "Ưu tiên", "Trạng thái" },
            RowsOrEmpty(openComplaints, 8, (c, i) => new object[]
            {
                i + 1,
                $"PA{c.CreatedAt:yyMMdd}-{c.ComplaintID:000}",
                c.Title,
                c.ResidentName,
                c.ApartmentCode,
                DateTimeText(c.CreatedAt),
                ViStatus(c.Priority),
                ViStatus(c.Status)
            }));
        complaintsGrid.Location = new Point(12, 42);
        complaintsGrid.Size = new Size(complaints.Width - 24, 182);
        complaints.Controls.Add(complaintsGrid);
        var allComplaints = ModernUi.OutlineButton("Xem tất cả phản ánh  →", 176, 30);
        allComplaints.Location = new Point(16, 228);
        allComplaints.Click += (_, _) => Navigate("complaints");
        complaints.Controls.Add(allComplaints);
        page.Controls.Add(complaints);

        var actions = ModernUi.Section("Thao tác nhanh", actionsW, 272);
        actions.Location = new Point(complaints.Right + gap, y);
        AddAdminQuickActions(actions);
        page.Controls.Add(actions);
    }

    private void AddAdminQuickActions(Control actions)
    {
        const int columns = 3;
        const int padX = 18;
        const int gapX = 12;
        const int tileH = 72;
        int tileW = Math.Max(1, (actions.Width - padX * 2 - gapX * (columns - 1)) / columns);
        int row1 = 54;
        int row2 = 150;

        var addTile = AddActionTile(actions, "⊕", "Thêm mới", "Tạo dữ liệu", Color.FromArgb(34, 197, 94), padX, row1, width: tileW, height: tileH);
        BindTileClick(addTile, (_, _) => ShowQuickActionMenu(addTile,
            ("Thêm tài khoản", () => Navigate("accounts")),
            ("Thêm cư dân", () => OpenManagementDialog<FrmResidentManagement>()),
            ("Thêm căn hộ", () => OpenManagementDialog<FrmApartmentManagement>()),
            ("Thêm hóa đơn", () => OpenManagementDialog<FrmInvoiceManagement>()),
            ("Thêm phản ánh", () => OpenManagementDialog<FrmComplaintManagement>())));

        var editTile = AddActionTile(actions, "✎", "Sửa dữ liệu", "Chỉnh sửa", Color.FromArgb(249, 115, 22), padX + (tileW + gapX), row1, width: tileW, height: tileH);
        BindTileClick(editTile, (_, _) => ShowQuickActionMenu(editTile,
            ("Sửa tài khoản", () => Navigate("accounts")),
            ("Sửa cư dân", () => OpenManagementDialog<FrmResidentManagement>()),
            ("Sửa căn hộ", () => OpenManagementDialog<FrmApartmentManagement>()),
            ("Sửa hóa đơn", () => OpenManagementDialog<FrmInvoiceManagement>()),
            ("Sửa phản ánh", () => OpenManagementDialog<FrmComplaintManagement>())));

        var deleteTile = AddActionTile(actions, "▥", "Xóa dữ liệu", "Xóa bỏ", Color.FromArgb(239, 68, 68), padX + (tileW + gapX) * 2, row1, width: tileW, height: tileH);
        BindTileClick(deleteTile, (_, _) => ShowQuickActionMenu(deleteTile,
            ("Xóa tài khoản", () => Navigate("accounts")),
            ("Xóa cư dân", () => OpenManagementDialog<FrmResidentManagement>()),
            ("Xóa căn hộ", () => OpenManagementDialog<FrmApartmentManagement>()),
            ("Xóa hóa đơn", () => OpenManagementDialog<FrmInvoiceManagement>()),
            ("Xóa phản ánh", () => OpenManagementDialog<FrmComplaintManagement>())));

        var saveTile = AddActionTile(actions, "▣", "Lưu dữ liệu", "Lưu thay đổi", ModernUi.Blue, padX, row2, width: tileW, height: tileH);
        BindTileClick(saveTile, (_, _) => SaveDashboardSnapshot());

        var cancelTile = AddActionTile(actions, "×", "Làm mới", "Tải lại dữ liệu", Color.FromArgb(100, 116, 139), padX + (tileW + gapX), row2, width: tileW, height: tileH);
        BindTileClick(cancelTile, (_, _) => ReloadCurrentPage());

        var reportTile = AddActionTile(actions, "▤", "Báo cáo", "Xuất dữ liệu", Color.FromArgb(51, 65, 85), padX + (tileW + gapX) * 2, row2, width: tileW, height: tileH);
        BindTileClick(reportTile, (_, _) => ShowQuickActionMenu(reportTile,
            ("Xuất báo cáo lấp đầy (.xlsx)", () => SaveGeneratedFile(
                ReportsBLL.GenerateOccupancyReport(),
                "Excel Workbook (*.xlsx)|*.xlsx",
                "QuickOccupancyReport")),
            ("Xuất danh sách cư dân (.csv)", () => SaveGeneratedFile(
                ReportsBLL.ExportDataToCSV("residents"),
                "CSV (*.csv)|*.csv",
                "QuickResidentsExport")),
            ("Xuất danh sách hóa đơn (.csv)", () => SaveGeneratedFile(
                ReportsBLL.ExportDataToCSV("invoices"),
                "CSV (*.csv)|*.csv",
                "QuickInvoicesExport")),
            ("Mở trang báo cáo", () => Navigate("reports"))));
    }

    private static void AddInvoiceQuickActions(Control actions)
    {
        const int columns = 3;
        const int padX = 18;
        const int gapX = 12;
        const int tileH = 52;
        int tileW = Math.Max(1, (actions.Width - padX * 2 - gapX * (columns - 1)) / columns);
        int row1 = 42;
        int row2 = 102;

        AddActionTile(actions, "▤", "Tạo hóa đơn", "Theo tháng", ModernUi.Blue, padX, row1, width: tileW, height: tileH);
        AddActionTile(actions, "▦", "Tính phí", "Tự động", ModernUi.Green, padX + tileW + gapX, row1, width: tileW, height: tileH);
        AddActionTile(actions, "▰", "Cập nhật", "Thanh toán", ModernUi.Orange, padX + (tileW + gapX) * 2, row1, width: tileW, height: tileH);
        AddActionTile(actions, "▣", "In hóa đơn", "Bản in", ModernUi.Purple, padX, row2, width: tileW, height: tileH);
        AddActionTile(actions, "▥", "Xuất Excel", "File bảng", ModernUi.Green, padX + tileW + gapX, row2, width: tileW, height: tileH);
        AddActionTile(actions, "PDF", "Xuất PDF", "File PDF", ModernUi.Red, padX + (tileW + gapX) * 2, row2, width: tileW, height: tileH);
    }

    private void RenderManagerDashboard()
    {
        var page = BeginPage("Dashboard", "Trang chủ / Dashboard");
        int x = 14;
        int w = Math.Max(1150, _content.ClientSize.Width - 28);
        int y = 83;
        int gap = 16;
        int cardW = (w - gap * 5) / 6;
        var residents = ResidentDAL.GetAllResidents();
        var apartments = ApartmentDAL.GetAllApartments();
        var invoices = InvoiceDAL.GetAllInvoices();
        var complaints = ComplaintDAL.GetAllComplaints();
        var visitors = VisitorDAL.GetAllVisitors();
        var schedules = AssetDAL.GetMaintenanceSchedules();
        var notifications = NotificationDAL.GetAllNotifications();
        int occupied = apartments.Count(a => ViStatus(a.Status) == "Đang sử dụng");
        int occupancyRate = apartments.Count == 0 ? 0 : (int)Math.Round(occupied * 100m / apartments.Count);
        int unpaidInvoices = invoices.Count(i => ViStatus(i.PaymentStatus) != "Đã thanh toán");
        int todayVisitors = visitors.Count(v => ((DateTime)v.ArrivalTime).Date == DateTime.Today);

        decimal debtAmount = invoices.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount));
        bool hasDebt = unpaidInvoices > 0 && debtAmount > 0;

        AddRowAt(page, x, y, gap,
            ModernUi.StatCard("Số cư dân hiện tại", residents.Count.ToString("N0"), "Người", ModernUi.Blue, "●●", $"{residents.Count(r => IsActiveStatus(r.Status))} đang cư trú", cardW, 162),
            ModernUi.StatCard("Phản ánh mới", complaints.Count(c => ViStatus(c.Status) == "Mới").ToString("N0"), "Phản ánh", Color.FromArgb(34, 197, 94), "▤", $"{complaints.Count:N0} tổng phiếu", cardW, 162),
            CreateDebtCard(unpaidInvoices, debtAmount, cardW, hasDebt),
            ModernUi.StatCard("Bảo trì sắp tới", schedules.Count(s => s.ScheduledDate <= DateTime.Today.AddDays(7)).ToString("N0"), "Lịch", Color.FromArgb(6, 182, 212), "⚒", "Trong 7 ngày tới", cardW, 162),
            ModernUi.StatCard("Khách hôm nay", todayVisitors.ToString("N0"), "Khách", ModernUi.Blue, "●", $"{visitors.Count:N0} tổng lượt", cardW, 162),
            ModernUi.StatCard("Lấp đầy căn hộ", $"{occupancyRate}%", "Đơn vị", Color.FromArgb(34, 197, 94), "◔", $"{occupied:N0}/{apartments.Count:N0}", cardW, 162));

        y += 176;

        int leftW = (int)((w - gap) * 0.42);
        int rightW = w - leftW - gap;
        int topPanelH = 272;

        var chartPanel = ModernUi.Section("Top loại phản ánh nhiều nhất", leftW, topPanelH);
        chartPanel.Location = new Point(x, y);
        AddSectionDots(chartPanel);
        var axisTitle = ModernUi.Label("Số lượng", 8.6f, FontStyle.Regular, ModernUi.Text);
        axisTitle.Location = new Point(16, 32);
        axisTitle.Size = new Size(90, 20);
        chartPanel.Controls.Add(axisTitle);
        var chart = new BarChartPanel
        {
            Location = new Point(12, 52),
            Size = new Size(chartPanel.Width - 24, 194),
            BarColor = ModernUi.Blue,
            AxisMax = 50,
            GridSteps = 5,
            ShowValueLabels = true,
            SeriesLabel = "Số lượng phản ánh"
        };
        var complaintGroups = complaints
            .GroupBy(c => (string)Display(c.Category, "Khác"))
            .OrderByDescending(g => g.Count())
            .Take(6)
            .Select(g => (Label: g.Key.Length > 10 ? g.Key[..10] : g.Key, Value: g.Count()))
            .ToList();
        chart.AxisMax = Math.Max(1, complaintGroups.Count == 0 ? 1 : (int)(complaintGroups.Max(g => g.Value) * 1.2m));
        chart.Bars.AddRange(complaintGroups);
        chartPanel.Controls.Add(chart);
        page.Controls.Add(chartPanel);

        var newComplaints = ModernUi.Section("Phản ánh mới cần xử lý", rightW, topPanelH);
        newComplaints.Location = new Point(chartPanel.Right + gap, y);
        AddSectionDots(newComplaints);
        var grid = CreateGrid(
            new[] { "STT", "Mã phản ánh", "Nội dung", "Căn hộ", "Người gửi", "Thời gian", "Ưu tiên", "Trạng thái" },
            RowsOrEmpty(complaints.Where(c => ViStatus(c.Status) == "Mới").Take(5), 8, (c, i) => new object[]
            {
                i + 1,
                $"PA{c.CreatedAt:yyMMdd}-{c.ComplaintID:000}",
                c.Title,
                c.ApartmentCode,
                c.ResidentName,
                DateTimeText(c.CreatedAt),
                ViStatus(c.Priority),
                ViStatus(c.Status)
            }));
        grid.Location = new Point(12, 38);
        grid.Size = new Size(newComplaints.Width - 24, 192);
        newComplaints.Controls.Add(grid);
        var link = ModernUi.OutlineButton("Xem tất cả phản ánh mới  →", 210, 30);
        link.Location = new Point(newComplaints.Width - 232, 231);
        link.Click += (_, _) => Navigate("complaints");
        newComplaints.Controls.Add(link);
        page.Controls.Add(newComplaints);

        y += 285;

        int bottomPanelH = 255;
        var schedule = ModernUi.Section("Lịch bảo trì 7 ngày tới", leftW, bottomPanelH);
        schedule.Location = new Point(x, y);
        AddSectionDots(schedule);
        var scheduleGrid = CreateGrid(
            new[] { "Ngày", "Hạng mục", "Khu vực", "Nội dung", "Trạng thái" },
            RowsOrEmpty(schedules.Take(5), 5, (s, _) => new object[]
            {
                DateText(s.ScheduledDate),
                s.Category,
                s.Location,
                Display(s.Note, s.AssetName),
                ViStatus(s.Status)
            }, "Không có lịch bảo trì"));
        scheduleGrid.Location = new Point(12, 42);
        scheduleGrid.Size = new Size(schedule.Width - 24, 194);
        schedule.Controls.Add(scheduleGrid);
        page.Controls.Add(schedule);

        var notice = ModernUi.Section("Thông báo nhanh", rightW, bottomPanelH);
        notice.Location = new Point(schedule.Right + gap, y);
        var latestNotices = notifications.Take(5).ToList();
        if (latestNotices.Count == 0)
        {
            AddNoticeRow(notice, 0, "Không có thông báo", "");
        }
        for (int i = 0; i < latestNotices.Count; i++)
        {
            AddNoticeRow(notice, i, Display(latestNotices[i].Title, Display(latestNotices[i].Message)), DateTimeText(latestNotices[i].CreatedAt));
        }
        page.Controls.Add(notice);

        static void AddRowAt(Control parent, int startX, int top, int spacing, params Control[] controls)
        {
            int cx = startX;
            foreach (var control in controls)
            {
                control.Location = new Point(cx, top);
                parent.Controls.Add(control);
                cx += control.Width + spacing;
            }
        }

        static void AddSectionDots(Control parent)
        {
            var dots = ModernUi.OutlineButton("...", 28, 24);
            dots.Location = new Point(parent.Width - 40, 11);
            parent.Controls.Add(dots);
        }

        static void AddNoticeRow(Control parent, int index, string message, string time)
        {
            int y = 44 + index * 37;
            var icon = ModernUi.Label("▰", 11f, FontStyle.Bold, ModernUi.Blue);
            icon.Location = new Point(24, y);
            icon.Size = new Size(20, 24);
            icon.TextAlign = ContentAlignment.MiddleCenter;
            parent.Controls.Add(icon);

            var text = ModernUi.Label(message, 9f, FontStyle.Regular, ModernUi.Text);
            text.Location = new Point(58, y);
            text.Size = new Size(parent.Width - 250, 24);
            parent.Controls.Add(text);

            var date = ModernUi.Label(time, 8.7f, FontStyle.Regular, Color.FromArgb(148, 163, 184));
            date.Location = new Point(parent.Width - 150, y);
            date.Size = new Size(130, 24);
            date.TextAlign = ContentAlignment.MiddleRight;
            parent.Controls.Add(date);

            if (index < 4)
            {
                var line = new Panel
                {
                    BackColor = Color.FromArgb(232, 238, 246),
                    Location = new Point(12, y + 30),
                    Size = new Size(parent.Width - 24, 1)
                };
                parent.Controls.Add(line);
            }
        }
    }

    private void RenderResidentDashboard()
    {
        var page = BeginPage("Dashboard cá nhân", $"Xin chào, {CurrentDisplayName()}! Chúc bạn một ngày tốt lành.");
        int w = Math.Max(1150, _content.ClientSize.Width - 58);
        int y = 76;
        int gap = 12;
        int cardW = (w - gap * 5) / 6;
        ResidentDTO? resident = _session?.UserID > 0 ? ResidentDAL.GetResidentByUserID(_session.UserID) : null;
        resident ??= ResidentDAL.GetAllResidents().FirstOrDefault(r => string.Equals(r.Username, CurrentUsername(), StringComparison.OrdinalIgnoreCase));
        var apartment = resident == null ? null : ApartmentDAL.GetApartmentByID(resident.ApartmentID);
        var residentInvoices = resident == null ? new List<InvoiceDTO>() : InvoiceDAL.GetInvoicesByResident(resident.ResidentID);
        var latestInvoice = residentInvoices.FirstOrDefault();
        var residentNotifications = _session?.UserID > 0 ? NotificationDAL.GetUserNotifications(_session.UserID) : new List<NotificationDTO>();
        var residentComplaints = resident == null ? new List<dynamic>() : ComplaintDAL.GetComplaintsByResident(resident.ResidentID);
        int unreadCount = residentNotifications.Count(n => !n.IsRead);
        int openComplaintCount = residentComplaints.Count(c => ViStatus(c.Status) is not ("Đã xử lý" or "Đã đóng"));

        AddRow(page, y, gap,
            ResidentCard("Thông tin cá nhân", Display(resident?.FullName, CurrentDisplayName()), $"{Display(resident?.Phone)}\r\n{Display(resident?.Email)}", ModernUi.Blue, "●", "Xem chi tiết", cardW),
            ResidentCard("Căn hộ đang ở", Display(resident?.ApartmentCode), $"{Display(apartment?.BuildingName)} - Tầng {apartment?.FloorNumber?.ToString() ?? "-"}\r\nDiện tích: {(apartment == null ? "-" : apartment.Area.ToString("N1", CultureInfo.InvariantCulture))} m²", Color.FromArgb(34, 197, 94), "⌂", "Xem chi tiết", cardW),
            ResidentCard("Hóa đơn mới nhất", latestInvoice == null ? "Chưa có" : $"Tháng {latestInvoice.Month:00}/{latestInvoice.Year}", latestInvoice == null ? "Không có dữ liệu\r\n-" : $"{Money(latestInvoice.TotalAmount)} VNĐ\r\nNgày phát hành: {DateText(latestInvoice.CreatedAt)}", Color.FromArgb(249, 115, 22), "▤", "Xem hóa đơn", cardW),
            ResidentCard("Trạng thái thanh toán", latestInvoice == null ? "-" : ViStatus(latestInvoice.PaymentStatus), latestInvoice == null ? "Không có hóa đơn\r\n-" : $"Đã thu: {Money(latestInvoice.PaidAmount)} VNĐ\r\nHạn: {DateText(latestInvoice.DueDate)}", Color.FromArgb(34, 197, 94), "✓", "Xem lịch sử", cardW),
            ResidentCard("Thông báo chưa đọc", unreadCount.ToString("N0"), "Thông báo mới", ModernUi.Blue, "◆", "Xem tất cả", cardW),
            ResidentCard("Phản ánh đang xử lý", openComplaintCount.ToString("N0"), "Phản ánh đang xử lý", Color.FromArgb(6, 182, 212), "■", "Xem chi tiết", cardW));

        y += 142;

        var invoices = ModernUi.Section("Hóa đơn gần đây", (int)(w * 0.40), 270);
        invoices.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "Kỳ hóa đơn", "Ngày phát hành", "Hạn thanh toán", "Số tiền (VNĐ)", "Trạng thái", "Hành động" },
            RowsOrEmpty(residentInvoices.Take(8), 6, (invoice, _) => new object[]
            {
                $"{invoice.Month:00}/{invoice.Year}",
                DateText(invoice.CreatedAt),
                DateText(invoice.DueDate),
                Money(invoice.TotalAmount),
                ViStatus(invoice.PaymentStatus),
                "Xem"
            }));
        grid.Location = new Point(12, 46);
        grid.Size = new Size(invoices.Width - 24, 190);
        invoices.Controls.Add(grid);
        page.Controls.Add(invoices);

        var notices = ModernUi.Section("Thông báo gần đây", (int)(w * 0.26), 270);
        notices.Location = new Point(invoices.Right + gap, y);
        var recentNotifications = residentNotifications.Take(5).ToList();
        if (recentNotifications.Count == 0)
        {
            recentNotifications.Add(new NotificationDTO { Title = "Không có thông báo", CreatedAt = DateTime.MinValue });
        }
        for (int i = 0; i < recentNotifications.Count; i++)
        {
            string rowText = $"{Display(recentNotifications[i].Title, Display(recentNotifications[i].Message))}        {DateTimeText(recentNotifications[i].CreatedAt)}";
            var row = ModernUi.Label("•  " + rowText, 9.3f, FontStyle.Regular, ModernUi.Text);
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
        string qrInvoiceText = latestInvoice == null
            ? "Quét QR để thanh toán hóa đơn mới nhất\r\n\r\nHóa đơn:   Chưa có hóa đơn\r\nSố tiền:   0 VNĐ\r\nNội dung CK: -"
            : $"Quét QR để thanh toán hóa đơn mới nhất\r\n\r\nHóa đơn:   Tháng {latestInvoice.Month:00}/{latestInvoice.Year}\r\nSố tiền:   {Money(Math.Max(0, latestInvoice.TotalAmount - latestInvoice.PaidAmount))} VNĐ\r\nNội dung CK: {Display(resident?.ApartmentCode)}-{latestInvoice.Month:00}{latestInvoice.Year}-{Display(resident?.FullName, CurrentUsername()).Replace(" ", "")}";
        var qrText = ModernUi.Label(qrInvoiceText, 10.2f, FontStyle.Regular, ModernUi.Text);
        qrText.Location = new Point(190, 54);
        qrText.Size = new Size(qr.Width - 220, 118);
        qr.Controls.Add(qrText);
        var note = ModernUi.Badge("Sau khi thanh toán, hệ thống sẽ tự động cập nhật trong vòng 5-10 phút.", ModernUi.Blue);
        note.Location = new Point(190, 174);
        note.Size = new Size(qr.Width - 220, 26);
        qr.Controls.Add(note);
        page.Controls.Add(qr);
    }

    private RoundedPanel CreateDebtCard(int unpaidCount, decimal debtAmount, int width, bool hasDebt)
    {
        var card = ModernUi.CardPanel();
        card.Size = new Size(width, 126);

        Color accentColor = hasDebt ? ModernUi.Red : Color.FromArgb(249, 115, 22);
        var titleLabel = ModernUi.Label("NỢ CHƯA THU", 8.5f, FontStyle.Bold, accentColor);
        titleLabel.Location = new Point(14, 12);
        titleLabel.Size = new Size(width - 28, 22);
        titleLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(titleLabel);

        var circle = new CircleLabel
        {
            Text = "!",
            CircleColor = accentColor,
            ForeColor = Color.White,
            Font = ModernUi.Font(22f, FontStyle.Bold),
            Size = new Size(58, 58),
            Location = new Point(18, 44)
        };
        card.Controls.Add(circle);

        // Add warning icon/badge if there's debt
        if (hasDebt)
        {
            var badge = new CircleLabel
            {
                Text = "⚠",
                CircleColor = ModernUi.Red,
                ForeColor = Color.White,
                Font = ModernUi.Font(12f, FontStyle.Bold),
                Size = new Size(28, 28),
                Location = new Point(62, 42)
            };
            card.Controls.Add(badge);
        }

        var valueLabel = ModernUi.Label(unpaidCount.ToString("N0"), 13f, FontStyle.Bold, ModernUi.Navy);
        valueLabel.Location = new Point(86, 40);
        valueLabel.Size = new Size(width - 98, 30);
        valueLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(valueLabel);

        var detailLabel = ModernUi.Label($"{Money(debtAmount)} VNĐ\r\n{unpaidCount} Hóa đơn", 8.4f, FontStyle.Regular, ModernUi.Text);
        detailLabel.Location = new Point(86, 70);
        detailLabel.Size = new Size(width - 98, 40);
        detailLabel.TextAlign = ContentAlignment.MiddleCenter;
        card.Controls.Add(detailLabel);

        Color badgeColor = hasDebt ? ModernUi.Red : Color.FromArgb(249, 115, 22);
        var actionLabel = ModernUi.Badge("Cần xử lý", badgeColor);
        actionLabel.Location = new Point(14, 92);
        actionLabel.Size = new Size(width - 28, 24);
        card.Controls.Add(actionLabel);

        return card;
    }

    private static ComboBox AddInvoiceStatusFilter(Control parent, string label, string selected, int x, int y = 8)
    {
        var lbl = ModernUi.Label(label, 8.7f, FontStyle.Bold, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(170, 18);
        parent.Controls.Add(lbl);
        var combo = ModernUi.ComboBox(new[] 
        { 
            selected, 
            "Tất cả",
            "Đã thanh toán",
            "Chưa thanh toán",
            "Thanh toán một phần",
            "Quá hạn"
        }, 210);
        combo.Location = new Point(x, y + 22);
        parent.Controls.Add(combo);
        return combo;
    }

    private void RenderApartments()
    {
        var page = BeginPage("Dashboard", "Quản lý tòa nhà / block / tầng / căn hộ");
        int w = PageWorkWidth(1180);

        List<ApartmentDTO> apartments = new();
        List<ApartmentDTO> displayApartments = new();
        List<(int Id, string Name)> buildingItems = new();
        List<(int Id, int BuildingID, string Name)> blockItems = new();
        List<(int Id, int BlockID, int Number)> floorItems = new();
        Dictionary<int, TreeNode> apartmentNodes = new();
        ApartmentDTO? selectedApartment = null;
        bool isCreateMode = false;
        bool syncingSelection = false;
        bool suppressFilterEvents = false;
        bool suppressLocationEvents = false;
        string[] apartmentColumns =
        {
            "Mã căn hộ", "Tòa nhà", "Block", "Tầng", "Diện tích (m²)", "Loại căn hộ", "Trạng thái", "Số người tối đa"
        };
        string[] apartmentStatuses = { "Empty", "Occupied", "Renting", "Maintenance", "Locked" };

        int filterY = 80;
        int filterInputY = 103;
        int gap = 17;
        int refreshW = 112;
        int searchW = 270;
        int[] filterWidths = { 250, 238, 230, 213 };
        int x = 18;
        var buildingFilter = AddApartmentFilter(page, "Tòa nhà", "Tất cả", x, filterY, filterWidths[0]);
        x += filterWidths[0] + gap;
        var blockFilter = AddApartmentFilter(page, "Block", "Tất cả", x, filterY, filterWidths[1]);
        x += filterWidths[1] + gap;
        var floorFilter = AddApartmentFilter(page, "Tầng", "Tất cả", x, filterY, filterWidths[2]);
        x += filterWidths[2] + gap;
        var statusFilter = AddApartmentFilter(page, "Trạng thái căn hộ", "Tất cả", x, filterY, filterWidths[3]);
        x += filterWidths[3] + gap;

        var apartmentSearch = ModernUi.SearchBox("Tìm kiếm mã căn hộ...", searchW, 38);
        apartmentSearch.Location = new Point(x, filterInputY);
        page.Controls.Add(apartmentSearch);

        var refresh = ModernUi.OutlineButton("⟳  Làm mới", refreshW, 38);
        refresh.Location = new Point(w - refreshW + 18, filterInputY);
        page.Controls.Add(refresh);

        var title = ModernUi.Label("QUẢN LÝ TÒA NHÀ / BLOCK / TẦNG / CĂN HỘ", 10.5f, FontStyle.Bold, ModernUi.Blue);
        title.Location = new Point(18, 160);
        title.Size = new Size(w, 28);
        page.Controls.Add(title);

        var content = ModernUi.CardPanel();
        content.Location = new Point(18, 200);
        content.Size = new Size(w, 610);
        content.Padding = Padding.Empty;
        page.Controls.Add(content);

        var tabStrip = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(w, 50),
            BackColor = ModernUi.Header
        };
        content.Controls.Add(tabStrip);

        string[] tabNames = { "Tòa nhà", "Block", "Tầng", "Căn hộ" };
        for (int i = 0; i < tabNames.Length; i++)
        {
            AddApartmentTab(tabStrip, tabNames[i], i == 3, 0 + i * 112);
        }

        const int innerX = 6;
        const int innerGap = 12;
        int sectionsY = 58;
        int sectionH = content.Height - sectionsY - 12;
        int treeW = Math.Min(306, Math.Max(286, (int)(w * 0.21)));
        int detailW = Math.Min(380, Math.Max(350, (int)(w * 0.27)));
        int leftW = w - innerX * 2 - detailW - treeW - innerGap * 2;
        if (leftW < 540)
        {
            leftW = 540;
            detailW = Math.Max(330, w - innerX * 2 - leftW - treeW - innerGap * 2);
        }

        var list = ModernUi.Section("Danh sách căn hộ", leftW, sectionH);
        list.Location = new Point(innerX, sectionsY);
        var grid = CreateGrid(apartmentColumns, new[] { EmptyRow(apartmentColumns.Length, "Không có căn hộ") });
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, sectionH - 102);
        grid.ColumnHeadersHeight = 38;
        grid.RowTemplate.Height = 37;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        int[] weights = { 88, 82, 62, 58, 96, 116, 110, 112 };
        for (int i = 0; i < grid.Columns.Count && i < weights.Length; i++)
        {
            grid.Columns[i].FillWeight = weights[i];
        }

        list.Controls.Add(grid);
        var apartmentTotalLabel = AddApartmentPager(list, sectionH, list.Width, 0);
        content.Controls.Add(list);

        var details = ModernUi.Section("Thông tin căn hộ", detailW, 438);
        details.Height = sectionH;
        details.Location = new Point(list.Right + innerGap, sectionsY);

        int labelX = 18;
        int inputX = 148;
        int inputW = details.Width - inputX - 18;
        int fieldY = 48;
        int fieldStep = 36;
        TextBox codeInput = AddApartmentTextField(details, "Mã căn hộ", labelX, inputX, fieldY, inputW, true);
        ComboBox buildingInput = AddApartmentComboField(details, "Tòa nhà", labelX, inputX, fieldY + fieldStep, inputW, true);
        ComboBox blockInput = AddApartmentComboField(details, "Block", labelX, inputX, fieldY + fieldStep * 2, inputW, true);
        ComboBox floorInput = AddApartmentComboField(details, "Tầng", labelX, inputX, fieldY + fieldStep * 3, inputW, true);
        TextBox areaInput = AddApartmentTextField(details, "Diện tích (m²)", labelX, inputX, fieldY + fieldStep * 4, inputW, true);
        ComboBox typeInput = AddApartmentComboField(details, "Loại căn hộ", labelX, inputX, fieldY + fieldStep * 5, inputW, true);
        ComboBox statusInput = AddApartmentComboField(details, "Trạng thái", labelX, inputX, fieldY + fieldStep * 6, inputW, true);

        var maxLabel = ModernUi.Label("Số người tối đa", 8.6f, FontStyle.Regular, ModernUi.Text);
        maxLabel.Location = new Point(labelX, fieldY + fieldStep * 7);
        maxLabel.Size = new Size(inputX - labelX - 28, 30);
        details.Controls.Add(maxLabel);
        var requiredMark = ModernUi.Label("*", 8.6f, FontStyle.Bold, ModernUi.Red);
        requiredMark.Location = new Point(inputX - 22, fieldY + fieldStep * 7);
        requiredMark.Size = new Size(14, 30);
        requiredMark.TextAlign = ContentAlignment.MiddleCenter;
        details.Controls.Add(requiredMark);
        var maxResidentsInput = new NumericUpDown
        {
            Location = new Point(inputX, fieldY + fieldStep * 7),
            Size = new Size(inputW, 28),
            Minimum = 1,
            Maximum = 20,
            Font = ModernUi.Font(9f),
            BorderStyle = BorderStyle.FixedSingle
        };
        details.Controls.Add(maxResidentsInput);

        var noteLabel = ModernUi.Label("Ghi chú", 8.6f, FontStyle.Regular, ModernUi.Text);
        noteLabel.Location = new Point(labelX, fieldY + fieldStep * 8);
        noteLabel.Size = new Size(inputX - labelX - 28, 30);
        details.Controls.Add(noteLabel);
        var noteBox = new TextBox
        {
            Location = new Point(inputX, fieldY + fieldStep * 8),
            Size = new Size(inputW, 66),
            Multiline = true,
            Font = ModernUi.Font(8.8f),
            BorderStyle = BorderStyle.FixedSingle
        };
        details.Controls.Add(noteBox);

        var noteCount = ModernUi.Label("0/255", 8f, FontStyle.Regular, ModernUi.Muted);
        noteCount.Location = new Point(details.Width - 66, fieldY + fieldStep * 8 + 68);
        noteCount.Size = new Size(48, 18);
        noteCount.TextAlign = ContentAlignment.MiddleRight;
        details.Controls.Add(noteCount);

        int actionY = Math.Min(sectionH - 92, fieldY + fieldStep * 8 + 104);
        int actionGap = 10;
        int actionW = (details.Width - 36 - actionGap * 2) / 3;
        var add = ModernUi.Button("⊕  Thêm", ModernUi.Green, actionW, 34);
        add.Font = ModernUi.Font(9f, FontStyle.Bold);
        add.Location = new Point(18, actionY);
        details.Controls.Add(add);
        var edit = ModernUi.Button("✎  Sửa", ModernUi.Orange, actionW, 34);
        edit.Font = ModernUi.Font(9f, FontStyle.Bold);
        edit.Location = new Point(add.Right + actionGap, actionY);
        details.Controls.Add(edit);
        var delete = ModernUi.Button("×  Xóa", ModernUi.Red, actionW, 34);
        delete.Font = ModernUi.Font(9f, FontStyle.Bold);
        delete.Location = new Point(edit.Right + actionGap, actionY);
        details.Controls.Add(delete);

        int saveW = (details.Width - 46) / 2;
        var save = ModernUi.Button("▣  Lưu", ModernUi.Blue, saveW, 36);
        save.Font = ModernUi.Font(9f, FontStyle.Bold);
        save.Location = new Point(18, actionY + 46);
        details.Controls.Add(save);
        var cancel = ModernUi.Button("×  Hủy", Color.FromArgb(107, 118, 132), saveW, 36);
        cancel.Font = ModernUi.Font(9f, FontStyle.Bold);
        cancel.Location = new Point(save.Right + 10, actionY + 46);
        details.Controls.Add(cancel);
        content.Controls.Add(details);

        var tree = ModernUi.Section("Sơ đồ block - tầng - căn hộ", treeW, sectionH);
        tree.Location = new Point(details.Right + innerGap, sectionsY);
        var treeView = new TreeView
        {
            Location = new Point(18, 44),
            Size = new Size(tree.Width - 36, sectionH - 62),
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = ModernUi.Text,
            Font = ModernUi.Font(9.2f),
            HideSelection = false,
            HotTracking = true,
            Indent = 20,
            ItemHeight = 24,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true
        };
        tree.Controls.Add(treeView);
        content.Controls.Add(tree);

        typeInput.Items.Clear();
        typeInput.AddOption("Studio", "Studio");
        typeInput.AddOption("1 PN - 1 WC", "1BR");
        typeInput.AddOption("2 PN - 1 WC", "2BR");
        typeInput.AddOption("3 PN - 2 WC", "3BR");
        typeInput.AddOption("4 PN - 2 WC", "4BR");
        typeInput.AddOption("Penthouse", "Penthouse");
        if (typeInput.Items.Count > 0)
        {
            typeInput.SelectedIndex = 0;
        }

        statusInput.Items.Clear();
        statusInput.AddOption("Đang trống", "Empty");
        statusInput.AddOption("Đang sử dụng", "Occupied");
        statusInput.AddOption("Đang thuê", "Renting");
        statusInput.AddOption("Bảo trì", "Maintenance");
        statusInput.AddOption("Đang khóa", "Locked");
        statusInput.SelectValue("Empty");
        codeInput.MaxLength = 20;
        noteBox.MaxLength = 255;

        string FloorLabel(int? floorNumber)
            => floorNumber.HasValue && floorNumber.Value > 0 ? $"Tầng {floorNumber.Value:00}" : "-";

        string DbApartmentStatus(string? status)
        {
            return (status ?? string.Empty).Trim() switch
            {
                "Đang trống" => "Empty",
                "Đang sử dụng" => "Occupied",
                "Đang thuê" => "Renting",
                "Bảo trì" => "Maintenance",
                "Đang khóa" => "Locked",
                "" => "Empty",
                var other => other
            };
        }

        bool TryParseArea(string text, out decimal area)
        {
            text = (text ?? string.Empty).Trim();
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out area) ||
                   decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out area) ||
                   decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out area);
        }

        void SetFilterItems(ComboBox combo, IEnumerable<string> items, string preferred = "Tất cả")
        {
            string[] options = items
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(item => item, StringComparer.CurrentCultureIgnoreCase)
                .Prepend("Tất cả")
                .ToArray();

            combo.BeginUpdate();
            combo.Items.Clear();
            combo.Items.AddRange(options.Cast<object>().ToArray());
            combo.EndUpdate();

            int index = Array.FindIndex(options, item => string.Equals(item, preferred, StringComparison.OrdinalIgnoreCase));
            combo.SelectedIndex = index >= 0 ? index : 0;
        }

        HashSet<int> GetMatchingBuildingIds(string? buildingText)
        {
            if (string.IsNullOrWhiteSpace(buildingText) || string.Equals(buildingText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                return buildingItems.Select(item => item.Id).ToHashSet();
            }

            return buildingItems
                .Where(item => string.Equals(BuildingShort(item.Name), buildingText, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Id)
                .ToHashSet();
        }

        HashSet<int> GetMatchingBlockIds(string? buildingText, string? blockText)
        {
            IEnumerable<(int Id, int BuildingID, string Name)> query = blockItems;
            if (!string.IsNullOrWhiteSpace(buildingText) && !string.Equals(buildingText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                HashSet<int> buildingIds = GetMatchingBuildingIds(buildingText);
                query = query.Where(item => buildingIds.Contains(item.BuildingID));
            }

            if (!string.IsNullOrWhiteSpace(blockText) && !string.Equals(blockText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(item => string.Equals(BlockShort(item.Name), blockText, StringComparison.OrdinalIgnoreCase));
            }

            return query.Select(item => item.Id).ToHashSet();
        }

        int? FindBuildingIdByLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label) || string.Equals(label, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var match = buildingItems.FirstOrDefault(item => string.Equals(BuildingShort(item.Name), label, StringComparison.OrdinalIgnoreCase));
            return match.Id > 0 ? match.Id : null;
        }

        int? FindBlockIdByLabel(string? label, int? buildingId = null)
        {
            if (string.IsNullOrWhiteSpace(label) || string.Equals(label, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            IEnumerable<(int Id, int BuildingID, string Name)> query = blockItems;
            if (buildingId.HasValue && buildingId.Value > 0)
            {
                query = query.Where(item => item.BuildingID == buildingId.Value);
            }

            var match = query.FirstOrDefault(item => string.Equals(BlockShort(item.Name), label, StringComparison.OrdinalIgnoreCase));
            return match.Id > 0 ? match.Id : null;
        }

        int? FindFloorIdByLabel(string? label, int? blockId = null)
        {
            if (string.IsNullOrWhiteSpace(label) || string.Equals(label, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            IEnumerable<(int Id, int BlockID, int Number)> query = floorItems;
            if (blockId.HasValue && blockId.Value > 0)
            {
                query = query.Where(item => item.BlockID == blockId.Value);
            }

            var match = query.FirstOrDefault(item => string.Equals(FloorLabel(item.Number), label, StringComparison.OrdinalIgnoreCase));
            return match.Id > 0 ? match.Id : null;
        }

        void PopulateBuildingInput(int? preferredBuildingId = null)
        {
            buildingInput.BeginUpdate();
            buildingInput.Items.Clear();
            foreach (var building in buildingItems.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                buildingInput.AddOption(BuildingShort(building.Name), building.Id);
            }

            buildingInput.EndUpdate();
            if (preferredBuildingId.HasValue)
            {
                buildingInput.SelectValue(preferredBuildingId.Value);
            }

            if (buildingInput.SelectedIndex < 0 && buildingInput.Items.Count > 0)
            {
                buildingInput.SelectedIndex = 0;
            }
        }

        void PopulateBlockInput(int? buildingId, int? preferredBlockId = null)
        {
            blockInput.BeginUpdate();
            blockInput.Items.Clear();
            IEnumerable<(int Id, int BuildingID, string Name)> query = blockItems;
            if (buildingId.HasValue && buildingId.Value > 0)
            {
                query = query.Where(item => item.BuildingID == buildingId.Value);
            }

            foreach (var block in query.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                blockInput.AddOption($"Block {BlockShort(block.Name)}", block.Id);
            }

            blockInput.EndUpdate();
            if (preferredBlockId.HasValue)
            {
                blockInput.SelectValue(preferredBlockId.Value);
            }

            if (blockInput.SelectedIndex < 0 && blockInput.Items.Count > 0)
            {
                blockInput.SelectedIndex = 0;
            }
        }

        void PopulateFloorInput(int? blockId, int? preferredFloorId = null)
        {
            floorInput.BeginUpdate();
            floorInput.Items.Clear();
            IEnumerable<(int Id, int BlockID, int Number)> query = floorItems;
            if (blockId.HasValue && blockId.Value > 0)
            {
                query = query.Where(item => item.BlockID == blockId.Value);
            }

            foreach (var floor in query.OrderBy(item => item.Number))
            {
                floorInput.AddOption(floor.Number.ToString("00", CultureInfo.InvariantCulture), floor.Id);
            }

            floorInput.EndUpdate();
            if (preferredFloorId.HasValue)
            {
                floorInput.SelectValue(preferredFloorId.Value);
            }

            if (floorInput.SelectedIndex < 0 && floorInput.Items.Count > 0)
            {
                floorInput.SelectedIndex = 0;
            }
        }

        void SetLocationSelection(int? buildingId, int? blockId, int? floorId)
        {
            suppressLocationEvents = true;
            try
            {
                PopulateBuildingInput(buildingId);
                int selectedBuildingId = buildingInput.GetSelectedValueInt();
                PopulateBlockInput(selectedBuildingId > 0 ? selectedBuildingId : buildingId, blockId);
                int selectedBlockId = blockInput.GetSelectedValueInt();
                PopulateFloorInput(selectedBlockId > 0 ? selectedBlockId : blockId, floorId);
            }
            finally
            {
                suppressLocationEvents = false;
            }
        }

        void LoadReferenceData()
        {
            apartments = ApartmentDAL.GetAllApartments();

            buildingItems = new List<(int Id, string Name)>();
            foreach (dynamic building in BuildingDAL.GetAllBuildings())
            {
                int id = Convert.ToInt32(building.BuildingID, CultureInfo.InvariantCulture);
                string name = Display(building.BuildingName, "");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    buildingItems.Add((id, name));
                }
            }

            blockItems = new List<(int Id, int BuildingID, string Name)>();
            foreach (var building in buildingItems)
            {
                foreach (dynamic block in BlockDAL.GetBlocksByBuilding(building.Id))
                {
                    int blockId = Convert.ToInt32(block.BlockID, CultureInfo.InvariantCulture);
                    int blockBuildingId = Convert.ToInt32(block.BuildingID, CultureInfo.InvariantCulture);
                    string blockName = Display(block.BlockName, "");
                    if (!string.IsNullOrWhiteSpace(blockName))
                    {
                        blockItems.Add((blockId, blockBuildingId, blockName));
                    }
                }
            }

            floorItems = new List<(int Id, int BlockID, int Number)>();
            foreach (var block in blockItems)
            {
                foreach (dynamic floor in FloorDAL.GetFloorsByBlock(block.Id))
                {
                    int floorId = Convert.ToInt32(floor.FloorID, CultureInfo.InvariantCulture);
                    int floorBlockId = Convert.ToInt32(floor.BlockID, CultureInfo.InvariantCulture);
                    int floorNumber = Convert.ToInt32(floor.FloorNumber, CultureInfo.InvariantCulture);
                    floorItems.Add((floorId, floorBlockId, floorNumber));
                }
            }
        }

        void RefreshFilterOptions(string? preferredBuilding = null, string? preferredBlock = null, string? preferredFloor = null, string? preferredStatus = null)
        {
            suppressFilterEvents = true;
            try
            {
                string buildingText = preferredBuilding ?? buildingFilter.SelectedItem?.ToString() ?? "Tất cả";
                string blockText = preferredBlock ?? blockFilter.SelectedItem?.ToString() ?? "Tất cả";
                string floorText = preferredFloor ?? floorFilter.SelectedItem?.ToString() ?? "Tất cả";
                string statusText = preferredStatus ?? statusFilter.SelectedItem?.ToString() ?? "Tất cả";

                SetFilterItems(buildingFilter, buildingItems.Select(item => BuildingShort(item.Name)), buildingText);
                buildingText = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";

                IEnumerable<(int Id, int BuildingID, string Name)> blockQuery = blockItems;
                if (!string.Equals(buildingText, "Tất cả", StringComparison.OrdinalIgnoreCase))
                {
                    HashSet<int> buildingIds = GetMatchingBuildingIds(buildingText);
                    blockQuery = blockQuery.Where(item => buildingIds.Contains(item.BuildingID));
                }

                SetFilterItems(blockFilter, blockQuery.Select(item => BlockShort(item.Name)), blockText);
                blockText = blockFilter.SelectedItem?.ToString() ?? "Tất cả";

                IEnumerable<(int Id, int BlockID, int Number)> floorQuery = floorItems;
                if (!string.Equals(buildingText, "Tất cả", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(blockText, "Tất cả", StringComparison.OrdinalIgnoreCase))
                {
                    HashSet<int> blockIds = GetMatchingBlockIds(buildingText, blockText);
                    floorQuery = floorQuery.Where(item => blockIds.Contains(item.BlockID));
                }

                SetFilterItems(floorFilter, floorQuery.Select(item => FloorLabel(item.Number)), floorText);
                SetFilterItems(statusFilter, apartmentStatuses.Select(ViStatus), statusText);
            }
            finally
            {
                suppressFilterEvents = false;
            }
        }

        List<ApartmentDTO> FilterApartments()
        {
            string buildingText = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";
            string blockText = blockFilter.SelectedItem?.ToString() ?? "Tất cả";
            string floorText = floorFilter.SelectedItem?.ToString() ?? "Tất cả";
            string statusText = statusFilter.SelectedItem?.ToString() ?? "Tất cả";
            string searchText = apartmentSearch.Text.Trim();

            IEnumerable<ApartmentDTO> filtered = apartments;
            if (!string.Equals(buildingText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(apartment => string.Equals(BuildingShort(apartment.BuildingName), buildingText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(blockText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(apartment => string.Equals(BlockShort(apartment.BlockName), blockText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(floorText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(apartment => string.Equals(FloorLabel(apartment.FloorNumber), floorText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(statusText, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(apartment => string.Equals(ViStatus(apartment.Status), statusText, StringComparison.OrdinalIgnoreCase));
            }

            if (searchText.Length > 0)
            {
                filtered = filtered.Where(apartment =>
                {
                    string haystack = string.Join(' ',
                        Display(apartment.ApartmentCode, string.Empty),
                        BuildingShort(apartment.BuildingName),
                        BlockShort(apartment.BlockName),
                        FloorLabel(apartment.FloorNumber),
                        ApartmentTypeText(apartment.ApartmentType),
                        ViStatus(apartment.Status),
                        Display(apartment.Note, string.Empty));

                    return haystack.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
                });
            }

            return filtered
                .OrderBy(apartment => Display(apartment.BuildingName, string.Empty), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(apartment => Display(apartment.BlockName, string.Empty), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(apartment => apartment.FloorNumber ?? 0)
                .ThenBy(apartment => Display(apartment.ApartmentCode, string.Empty), StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        void PopulateApartmentGrid(IReadOnlyList<ApartmentDTO> source)
        {
            grid.SuspendLayout();
            grid.DataSource = null;
            grid.Rows.Clear();

            // If the grid was originally created with a DataSource the auto-generated columns
            // may have been removed when DataSource was cleared. Ensure columns exist before
            // adding rows manually to avoid InvalidOperationException.
            if (grid.Columns.Count == 0)
            {
                foreach (var col in apartmentColumns)
                {
                    grid.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        HeaderText = col,
                        DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
                    });
                }
                grid.ColumnHeadersHeight = 38;
                grid.RowTemplate.Height = 37;
            }

            if (source.Count == 0)
            {
                grid.Rows.Add(EmptyRow(apartmentColumns.Length, "Không có căn hộ phù hợp"));
                grid.ClearSelection();
                grid.ResumeLayout();
                return;
            }

            foreach (var apartment in source)
            {
                int rowIndex = grid.Rows.Add(
                    Display(apartment.ApartmentCode),
                    BuildingShort(apartment.BuildingName),
                    BlockShort(apartment.BlockName),
                    apartment.FloorNumber?.ToString("00", CultureInfo.InvariantCulture) ?? "-",
                    apartment.Area.ToString("N2", CultureInfo.InvariantCulture),
                    ApartmentTypeText(apartment.ApartmentType),
                    ViStatus(apartment.Status),
                    apartment.MaxResidents);

                grid.Rows[rowIndex].Tag = apartment;
            }

            grid.ClearSelection();
            grid.ResumeLayout();
        }

        ApartmentDTO? FirstApartmentInNode(TreeNode node)
        {
            if (node.Tag is ApartmentDTO apartment)
            {
                return apartment;
            }

            foreach (TreeNode child in node.Nodes)
            {
                var found = FirstApartmentInNode(child);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        void PopulateApartmentTree(IReadOnlyList<ApartmentDTO> source)
        {
            apartmentNodes = new Dictionary<int, TreeNode>();
            treeView.BeginUpdate();
            treeView.Nodes.Clear();

            var root = new TreeNode("▣  Dự án khu chung cư");
            treeView.Nodes.Add(root);

            foreach (var buildingGroup in source.GroupBy(apartment => BuildingShort(apartment.BuildingName)))
            {
                var buildingNode = new TreeNode($"▰  {buildingGroup.Key}");
                root.Nodes.Add(buildingNode);

                foreach (var blockGroup in buildingGroup.GroupBy(apartment => BlockShort(apartment.BlockName)))
                {
                    var blockNode = new TreeNode($"▰  Block {blockGroup.Key}");
                    buildingNode.Nodes.Add(blockNode);

                    foreach (var floorGroup in blockGroup.GroupBy(apartment => apartment.FloorNumber ?? 0).OrderByDescending(group => group.Key))
                    {
                        var floorNode = new TreeNode($"▦  Tầng {floorGroup.Key:00}");
                        blockNode.Nodes.Add(floorNode);

                        foreach (var apartment in floorGroup.OrderBy(item => Display(item.ApartmentCode, string.Empty), StringComparer.CurrentCultureIgnoreCase))
                        {
                            var apartmentNode = new TreeNode($"▣  {Display(apartment.ApartmentCode)}")
                            {
                                Tag = apartment
                            };
                            floorNode.Nodes.Add(apartmentNode);
                            apartmentNodes[apartment.ApartmentID] = apartmentNode;
                        }
                    }
                }
            }

            root.Expand();
            if (root.Nodes.Count > 0)
            {
                root.Nodes[0].Expand();
                if (root.Nodes[0].Nodes.Count > 0)
                {
                    root.Nodes[0].Nodes[0].Expand();
                }
            }

            treeView.EndUpdate();
        }

        void PrepareCreateMode()
        {
            isCreateMode = true;
            codeInput.Clear();
            areaInput.Clear();
            noteBox.Clear();
            noteCount.Text = "0/255";
            maxResidentsInput.Value = 4;
            if (typeInput.Items.Count > 0)
            {
                typeInput.SelectedIndex = 0;
            }

            statusInput.SelectValue("Empty");

            int? preferredBuildingId = FindBuildingIdByLabel(buildingFilter.SelectedItem?.ToString()) ??
                                       selectedApartment?.BuildingID ??
                                       (buildingItems.Count > 0 ? buildingItems[0].Id : null);
            int? preferredBlockId = FindBlockIdByLabel(blockFilter.SelectedItem?.ToString(), preferredBuildingId) ??
                                    (selectedApartment?.BuildingID == preferredBuildingId ? selectedApartment?.BlockID : null);
            int? preferredFloorId = FindFloorIdByLabel(floorFilter.SelectedItem?.ToString(), preferredBlockId) ??
                                    (selectedApartment?.BlockID == preferredBlockId ? selectedApartment?.FloorID : null);
            SetLocationSelection(preferredBuildingId, preferredBlockId, preferredFloorId);

            syncingSelection = true;
            try
            {
                grid.ClearSelection();
                treeView.SelectedNode = null;
            }
            finally
            {
                syncingSelection = false;
            }

            codeInput.Focus();
        }

        void SelectApartment(ApartmentDTO? apartment, bool fromTree = false)
        {
            if (apartment == null)
            {
                return;
            }

            selectedApartment = apartment;
            isCreateMode = false;
            codeInput.Text = Display(apartment.ApartmentCode, "");
            SetLocationSelection(apartment.BuildingID, apartment.BlockID, apartment.FloorID);
            areaInput.Text = apartment.Area.ToString("N2", CultureInfo.InvariantCulture);
            typeInput.SelectValue(apartment.ApartmentType);
            statusInput.SelectValue(apartment.Status);
            maxResidentsInput.Value = Math.Min(maxResidentsInput.Maximum, Math.Max(maxResidentsInput.Minimum, apartment.MaxResidents <= 0 ? 1 : apartment.MaxResidents));
            noteBox.Text = Display(apartment.Note, "");
            noteCount.Text = $"{Math.Min(noteBox.TextLength, 255)}/255";

            syncingSelection = true;
            try
            {
                grid.ClearSelection();
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.Tag is ApartmentDTO rowApartment && rowApartment.ApartmentID == apartment.ApartmentID)
                    {
                        row.Selected = true;
                        grid.CurrentCell = row.Cells[0];
                        break;
                    }
                }

                if (!fromTree && apartmentNodes.TryGetValue(apartment.ApartmentID, out var node))
                {
                    treeView.SelectedNode = node;
                    node.EnsureVisible();
                }
            }
            finally
            {
                syncingSelection = false;
            }
        }

        bool ValidateApartmentForm(out int floorId, out decimal area, out string apartmentType, out string apartmentStatus, out string apartmentCode, out string? note)
        {
            floorId = 0;
            area = 0;
            apartmentType = string.Empty;
            apartmentStatus = string.Empty;
            apartmentCode = codeInput.Text.Trim();
            note = string.IsNullOrWhiteSpace(noteBox.Text) ? null : noteBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(apartmentCode))
            {
                MessageBox.Show(this, "Mã căn hộ không được để trống.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                codeInput.Focus();
                return false;
            }

            if (apartmentCode.Length > 20)
            {
                MessageBox.Show(this, "Mã căn hộ tối đa 20 ký tự.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                codeInput.Focus();
                return false;
            }

            floorId = floorInput.GetSelectedValueInt();
            if (floorId <= 0)
            {
                MessageBox.Show(this, "Bạn chưa chọn tầng hợp lệ.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!TryParseArea(areaInput.Text, out area) || area <= 0 || area > 1000)
            {
                MessageBox.Show(this, "Diện tích phải là số lớn hơn 0 và không vượt quá 1000 m².",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                areaInput.Focus();
                return false;
            }

            apartmentType = typeInput.GetSelectedValueString();
            if (string.IsNullOrWhiteSpace(apartmentType))
            {
                MessageBox.Show(this, "Bạn chưa chọn loại căn hộ.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            apartmentStatus = DbApartmentStatus(statusInput.GetSelectedText());
            return true;
        }

        void RefreshApartmentView(int? preferredApartmentId = null, bool keepCreateMode = false)
        {
            displayApartments = FilterApartments();
            apartmentTotalLabel.Text = $"Tổng số: {displayApartments.Count:N0} căn hộ";
            PopulateApartmentGrid(displayApartments);
            PopulateApartmentTree(displayApartments);

            if (keepCreateMode)
            {
                PrepareCreateMode();
                return;
            }

            if (preferredApartmentId.HasValue)
            {
                var preferred = displayApartments.FirstOrDefault(apartment => apartment.ApartmentID == preferredApartmentId.Value);
                if (preferred != null)
                {
                    SelectApartment(preferred);
                    return;
                }
            }

            if (selectedApartment != null)
            {
                var current = displayApartments.FirstOrDefault(apartment => apartment.ApartmentID == selectedApartment.ApartmentID);
                if (current != null)
                {
                    SelectApartment(current);
                    return;
                }
            }

            if (displayApartments.Count > 0)
            {
                SelectApartment(displayApartments[0]);
                return;
            }

            PrepareCreateMode();
        }

        void ReloadApartmentData(int? preferredApartmentId = null, bool keepCreateMode = false)
        {
            string currentBuilding = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";
            string currentBlock = blockFilter.SelectedItem?.ToString() ?? "Tất cả";
            string currentFloor = floorFilter.SelectedItem?.ToString() ?? "Tất cả";
            string currentStatus = statusFilter.SelectedItem?.ToString() ?? "Tất cả";

            LoadReferenceData();
            RefreshFilterOptions(currentBuilding, currentBlock, currentFloor, currentStatus);
            RefreshApartmentView(preferredApartmentId, keepCreateMode);
        }

        void SaveApartment()
        {
            if (!ValidateApartmentForm(out int floorId, out decimal area, out string apartmentType, out string apartmentStatus, out string apartmentCode, out string? note))
            {
                return;
            }

            if (isCreateMode || selectedApartment == null)
            {
                var createResult = ApartmentBLL.CreateApartment(apartmentCode, floorId, area, apartmentType, (int)maxResidentsInput.Value, note);
                if (!createResult.Success)
                {
                    MessageBox.Show(this, createResult.Message,
                        "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!string.Equals(apartmentStatus, "Empty", StringComparison.OrdinalIgnoreCase))
                {
                    var statusResult = ApartmentBLL.UpdateApartmentStatus(createResult.ApartmentID, apartmentStatus);
                    if (!statusResult.Success)
                    {
                        MessageBox.Show(this,
                            $"Đã tạo căn hộ nhưng chưa cập nhật được trạng thái.\n{statusResult.Message}",
                            "Quản lý căn hộ",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }

                AuditLogDAL.LogAction(_session?.UserID, "Create_Apartment", "Apartment", createResult.ApartmentID, $"Tạo căn hộ: {apartmentCode}");
                MessageBox.Show(this, "Đã thêm căn hộ thành công.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReloadApartmentData(createResult.ApartmentID);
                return;
            }

            var updateResult = ApartmentBLL.UpdateApartment(selectedApartment.ApartmentID, floorId, apartmentCode, area, apartmentType, (int)maxResidentsInput.Value, note);
            if (!updateResult.Success)
            {
                MessageBox.Show(this, updateResult.Message,
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!string.Equals(apartmentStatus, selectedApartment.Status, StringComparison.OrdinalIgnoreCase))
            {
                var statusResult = ApartmentBLL.UpdateApartmentStatus(selectedApartment.ApartmentID, apartmentStatus);
                if (!statusResult.Success)
                {
                    MessageBox.Show(this, statusResult.Message,
                        "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            AuditLogDAL.LogAction(_session?.UserID, "Update_Apartment", "Apartment", selectedApartment.ApartmentID, $"Cập nhật căn hộ: {apartmentCode}");
            MessageBox.Show(this, "Đã lưu thay đổi căn hộ.",
                "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReloadApartmentData(selectedApartment.ApartmentID);
        }

        void DeleteApartment()
        {
            if (selectedApartment == null || isCreateMode)
            {
                MessageBox.Show(this, "Bạn chưa chọn căn hộ để xóa.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(this,
                    $"Xóa căn hộ `{selectedApartment.ApartmentCode}`?",
                    "Quản lý căn hộ",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var result = ApartmentBLL.DeleteApartment(selectedApartment.ApartmentID);
            if (!result.Success)
            {
                MessageBox.Show(this, result.Message,
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AuditLogDAL.LogAction(_session?.UserID, "Delete_Apartment", "Apartment", selectedApartment.ApartmentID, $"Xóa căn hộ: {selectedApartment.ApartmentCode}");
            MessageBox.Show(this, "Đã xóa căn hộ.",
                "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            selectedApartment = null;
            ReloadApartmentData();
        }

        buildingInput.SelectedIndexChanged += (_, _) =>
        {
            if (suppressLocationEvents)
            {
                return;
            }

            suppressLocationEvents = true;
            try
            {
                PopulateBlockInput(buildingInput.GetSelectedValueInt());
                PopulateFloorInput(blockInput.GetSelectedValueInt());
            }
            finally
            {
                suppressLocationEvents = false;
            }
        };

        blockInput.SelectedIndexChanged += (_, _) =>
        {
            if (suppressLocationEvents)
            {
                return;
            }

            suppressLocationEvents = true;
            try
            {
                PopulateFloorInput(blockInput.GetSelectedValueInt());
            }
            finally
            {
                suppressLocationEvents = false;
            }
        };

        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Tag is ApartmentDTO apartment)
            {
                SelectApartment(apartment);
            }
        };

        treeView.AfterSelect += (_, e) =>
        {
            if (syncingSelection)
            {
                return;
            }

            SelectApartment(FirstApartmentInNode(e.Node), fromTree: true);
        };

        apartmentSearch.TextChanged += (_, _) => RefreshApartmentView(selectedApartment?.ApartmentID, isCreateMode);
        buildingFilter.SelectedIndexChanged += (_, _) =>
        {
            if (suppressFilterEvents)
            {
                return;
            }

            RefreshFilterOptions(buildingFilter.SelectedItem?.ToString(), blockFilter.SelectedItem?.ToString(), floorFilter.SelectedItem?.ToString(), statusFilter.SelectedItem?.ToString());
            RefreshApartmentView(selectedApartment?.ApartmentID, isCreateMode);
        };
        blockFilter.SelectedIndexChanged += (_, _) =>
        {
            if (suppressFilterEvents)
            {
                return;
            }

            RefreshFilterOptions(buildingFilter.SelectedItem?.ToString(), blockFilter.SelectedItem?.ToString(), floorFilter.SelectedItem?.ToString(), statusFilter.SelectedItem?.ToString());
            RefreshApartmentView(selectedApartment?.ApartmentID, isCreateMode);
        };
        floorFilter.SelectedIndexChanged += (_, _) =>
        {
            if (!suppressFilterEvents)
            {
                RefreshApartmentView(selectedApartment?.ApartmentID, isCreateMode);
            }
        };
        statusFilter.SelectedIndexChanged += (_, _) =>
        {
            if (!suppressFilterEvents)
            {
                RefreshApartmentView(selectedApartment?.ApartmentID, isCreateMode);
            }
        };

        refresh.Click += (_, _) => ReloadApartmentData(selectedApartment?.ApartmentID, isCreateMode);
        add.Click += (_, _) => PrepareCreateMode();
        edit.Click += (_, _) =>
        {
            if (selectedApartment == null)
            {
                MessageBox.Show(this, "Bạn chưa chọn căn hộ để sửa.",
                    "Quản lý căn hộ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var refreshedApartment = ApartmentDAL.GetApartmentByID(selectedApartment.ApartmentID) ?? selectedApartment;
            SelectApartment(refreshedApartment);
            codeInput.Focus();
            codeInput.SelectAll();
        };
        delete.Click += (_, _) => DeleteApartment();
        save.Click += (_, _) => SaveApartment();
        cancel.Click += (_, _) =>
        {
            if (selectedApartment != null)
            {
                ReloadApartmentData(selectedApartment.ApartmentID);
            }
            else
            {
                PrepareCreateMode();
            }
        };
        noteBox.TextChanged += (_, _) => noteCount.Text = $"{Math.Min(noteBox.TextLength, 255)}/255";

        ReloadApartmentData();
    }

    private static ComboBox AddApartmentFilter(Control parent, string label, string selected, int x, int y, int width)
    {
        var lbl = ModernUi.Label(label, 8.8f, FontStyle.Bold, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(width, 20);
        parent.Controls.Add(lbl);

        var combo = ModernUi.ComboBox(new[] { selected }, width);
        combo.Location = new Point(x, y + 22);
        combo.Height = 38;
        parent.Controls.Add(combo);
        return combo;
    }

    private static ComboBox AddResidentFilterCombo(Control parent, string label, int x, int y, int width)
    {
        var lbl = ModernUi.Label(label, 8.8f, FontStyle.Bold, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(width, 20);
        parent.Controls.Add(lbl);

        var combo = ModernUi.ComboBox(new[] { "Tất cả" }, width);
        combo.Location = new Point(x, y + 22);
        combo.Height = 32;
        parent.Controls.Add(combo);
        return combo;
    }

    private static void AddApartmentTab(Control parent, string text, bool active, int x)
    {
        var tab = ModernUi.Label(text, 9.2f, active ? FontStyle.Bold : FontStyle.Regular, active ? ModernUi.Blue : ModernUi.Text);
        tab.Location = new Point(x, 0);
        tab.Size = new Size(112, 50);
        tab.TextAlign = ContentAlignment.MiddleCenter;
        parent.Controls.Add(tab);

        if (active)
        {
            var line = new Panel
            {
                BackColor = ModernUi.Blue,
                Location = new Point(x, 48),
                Size = new Size(112, 2)
            };
            parent.Controls.Add(line);
        }
    }

    private static TextBox AddApartmentTextField(Control parent, string label, int labelX, int inputX, int y, int inputW, bool required)
    {
        AddApartmentFieldLabel(parent, label, labelX, inputX, y, required);
        var input = ModernUi.TextBox("", inputW);
        input.Location = new Point(inputX, y);
        input.Height = 28;
        input.Font = ModernUi.Font(9f);
        parent.Controls.Add(input);
        return input;
    }

    private static ComboBox AddApartmentComboField(Control parent, string label, int labelX, int inputX, int y, int inputW, bool required)
    {
        AddApartmentFieldLabel(parent, label, labelX, inputX, y, required);
        var input = ModernUi.ComboBox(Array.Empty<string>(), inputW);
        input.Location = new Point(inputX, y);
        input.Height = 28;
        input.Font = ModernUi.Font(9f);
        parent.Controls.Add(input);
        return input;
    }

    private static void AddApartmentFieldLabel(Control parent, string label, int x, int inputX, int y, bool required)
    {
        var lbl = ModernUi.Label(label, 8.6f, FontStyle.Regular, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(inputX - x - 28, 30);
        parent.Controls.Add(lbl);

        if (required)
        {
            var mark = ModernUi.Label("*", 8.6f, FontStyle.Bold, ModernUi.Red);
            mark.Location = new Point(inputX - 22, y);
            mark.Size = new Size(14, 30);
            mark.TextAlign = ContentAlignment.MiddleCenter;
            parent.Controls.Add(mark);
        }
    }

    private static Label AddApartmentPager(Control parent, int sectionH, int width, int total)
    {
        var totalLabel = ModernUi.Label($"Tổng số: {total:N0} căn hộ", 8.8f, FontStyle.Bold, ModernUi.Blue);
        totalLabel.Location = new Point(18, sectionH - 38);
        totalLabel.Size = new Size(180, 28);
        parent.Controls.Add(totalLabel);

        string[] pages = { "|<", "<", "1", "2", "3", "...", "13", ">", ">|" };
        int buttonW = 31;
        int startX = Math.Max(210, width - 402);
        for (int i = 0; i < pages.Length; i++)
        {
            var pageButton = ModernUi.OutlineButton(pages[i], buttonW, 30);
            pageButton.Font = ModernUi.Font(8.6f, i == 2 ? FontStyle.Bold : FontStyle.Regular);
            pageButton.Location = new Point(startX + i * (buttonW + 6), sectionH - 38);
            if (i == 2)
            {
                pageButton.BackColor = ModernUi.Blue;
                pageButton.ForeColor = Color.White;
            }
            parent.Controls.Add(pageButton);
        }

        var pageSize = ModernUi.ComboBox(new[] { "10", "20", "50" }, 66);
        pageSize.Location = new Point(width - 80, sectionH - 38);
        parent.Controls.Add(pageSize);
        return totalLabel;
    }

    private static string BuildingShort(string? value)
    {
        string text = Display(value);
        return text.Replace("Tòa nhà", "Tòa", StringComparison.OrdinalIgnoreCase)
                   .Replace("Toà nhà", "Toà", StringComparison.OrdinalIgnoreCase);
    }

    private static string BlockShort(string? value)
    {
        string text = Display(value);
        return text.Replace("Block", "", StringComparison.OrdinalIgnoreCase).Trim();
    }

    private static string ApartmentTypeText(string? type)
    {
        return (type ?? string.Empty).Trim() switch
        {
            "Studio" => "Studio",
            "1BR" or "1 phòng ngủ" => "1 PN - 1 WC",
            "2BR" or "2 phòng ngủ" => "2 PN - 1 WC",
            "3BR" or "3 phòng ngủ" => "3 PN - 2 WC",
            "4BR" or "4 phòng ngủ" => "4 PN - 2 WC",
            "Penthouse" => "Penthouse",
            "" => "-",
            var other => other
        };
    }

    private void RenderResidents_Legacy()
    {
        var page = BeginPage("Quản lý cư dân", "");
        int w = PageWorkWidth();
        int y = 72;
        var residents = ResidentDAL.GetAllResidents();

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 100);
        AddFilter(filters, "Tòa nhà:", "Tất cả", 16);
        AddFilter(filters, "Căn hộ:", "Tất cả", 276);
        AddFilter(filters, "Trạng thái cư trú:", "Tất cả", 536);
        AddFilter(filters, "Vai trò trong căn hộ:", "Tất cả", 796);
        var search = ModernUi.TextBox("Nhập thông tin cần tìm...", 300);
        search.Location = new Point(16, 66);
        filters.Controls.Add(search);
        page.Controls.Add(filters);

        y += 116;
        int leftW = (int)(w * 0.58);
        var list = ModernUi.Section("Danh sách cư dân", leftW, 536);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "STT", "Mã cư dân", "Họ tên", "CCCD", "SĐT", "Email", "Căn hộ", "Tình trạng cư trú", "Ngày vào ở" },
            RowsOrEmpty(residents.Take(50), 9, (r, i) => new object[]
            {
                i + 1,
                $"CD{r.ResidentID:0000}",
                Display(r.FullName),
                Display(r.CCCD),
                Display(r.Phone),
                Display(r.Email),
                Display(r.ApartmentCode),
                ViStatus(r.Status),
                DateText(r.StartDate ?? r.MoveInDate)
            }));
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
        AddResidentProfile(details, residents.FirstOrDefault());
        page.Controls.Add(details);
    }

    private void RenderResidents()
    {
        var page = BeginPage("Quản lý cư dân", "Dashboard / Cư dân");
        int w = PageWorkWidth();
        int y = 72;
        var allResidents = ResidentDAL.GetAllResidents();
        var apartments = ApartmentDAL.GetAllApartments();
        var apartmentById = apartments.ToDictionary(a => a.ApartmentID);
        List<ResidentDTO> displayResidents = new();

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 104);
        page.Controls.Add(filters);

        var buildingFilter = AddResidentFilterCombo(filters, "Tòa nhà", 16, 10, 210);
        var apartmentFilter = AddResidentFilterCombo(filters, "Căn hộ", 244, 10, 180);
        var statusFilter = AddResidentFilterCombo(filters, "Tình trạng cư trú", 442, 10, 192);
        var roleFilter = AddResidentFilterCombo(filters, "Vai trò trong căn hộ", 652, 10, 190);
        var search = ModernUi.TextBox("Nhập tên, CCCD, SĐT hoặc căn hộ...", Math.Min(310, w - 36));
        search.Location = new Point(16, 64);
        filters.Controls.Add(search);

        var filterSummary = ModernUi.Label("", 8.7f, FontStyle.Regular, ModernUi.Muted);
        filterSummary.Location = new Point(search.Right + 16, 66);
        filterSummary.Size = new Size(Math.Max(200, w - search.Right - 48), 24);
        filters.Controls.Add(filterSummary);

        y += 120;
        int leftW = Math.Max(640, (int)(w * 0.60));
        int rightW = w - leftW - 12;

        var list = ModernUi.Section("Danh sách cư dân theo căn hộ", leftW, 536);
        list.Location = new Point(18, y);
        var gridColumns = new[] { "Mã cư dân", "Họ tên", "Tòa", "Block", "Tầng", "Căn hộ", "Vai trò", "Tình trạng", "Ngày vào ở" };
        var grid = CreateGrid(gridColumns, new[] { EmptyRow(gridColumns.Length, "Không có cư dân") });
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 428);
        grid.ColumnHeadersHeight = 38;
        grid.RowTemplate.Height = 36;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        list.Controls.Add(grid);

        var listSummary = ModernUi.Label("", 8.8f, FontStyle.Bold, ModernUi.Blue);
        listSummary.Location = new Point(18, 482);
        listSummary.Size = new Size(list.Width - 36, 22);
        list.Controls.Add(listSummary);

        var listHint = ModernUi.Label("Chọn cư dân để xem căn hộ liên kết và các cư dân cùng căn.", 8.7f, FontStyle.Regular, ModernUi.Muted);
        listHint.Location = new Point(18, 504);
        listHint.Size = new Size(list.Width - 36, 22);
        list.Controls.Add(listHint);
        page.Controls.Add(list);

        var details = ModernUi.Section("Thông tin cư dân và căn hộ", rightW, 536);
        details.Location = new Point(list.Right + 12, y);
        var detailBody = new Panel
        {
            Location = new Point(0, 34),
            Size = new Size(details.Width, details.Height - 34),
            BackColor = Color.Transparent
        };
        details.Controls.Add(detailBody);
        page.Controls.Add(details);

        bool suppressFilterEvents = false;

        void SetFilterItems(ComboBox combo, IEnumerable<string> items, string preferred)
        {
            string[] options = items
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.CurrentCultureIgnoreCase)
                .Prepend("Tất cả")
                .ToArray();

            combo.BeginUpdate();
            combo.Items.Clear();
            combo.Items.AddRange(options.Cast<object>().ToArray());
            int selectedIndex = Array.FindIndex(options, option => string.Equals(option, preferred, StringComparison.OrdinalIgnoreCase));
            combo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            combo.EndUpdate();
        }

        void RefreshApartmentFilter()
        {
            suppressFilterEvents = true;
            try
            {
                string selectedBuilding = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";
                string currentApartment = apartmentFilter.SelectedItem?.ToString() ?? "Tất cả";
                IEnumerable<string> apartmentsForFilter = apartments
                    .Where(a => selectedBuilding == "Tất cả" || string.Equals(BuildingShort(a.BuildingName), selectedBuilding, StringComparison.OrdinalIgnoreCase))
                    .Select(a => Display(a.ApartmentCode));
                SetFilterItems(apartmentFilter, apartmentsForFilter, currentApartment);
            }
            finally
            {
                suppressFilterEvents = false;
            }
        }

        void BindResidentGrid(IReadOnlyList<ResidentDTO> residents)
        {
            SetGridData(
                grid,
                gridColumns,
                RowsOrEmpty(residents, gridColumns.Length, (resident, _) =>
                {
                    apartmentById.TryGetValue(resident.ApartmentID, out var apartment);
                    return new object[]
                    {
                        $"CD{resident.ResidentID:0000}",
                        Display(resident.FullName),
                        BuildingShort(apartment?.BuildingName),
                        BlockShort(apartment?.BlockName),
                        apartment?.FloorNumber?.ToString("00") ?? "-",
                        Display(resident.ApartmentCode),
                        Display(resident.RelationshipWithOwner),
                        ResidentLivingStatus(resident),
                        DateText(resident.StartDate ?? resident.MoveInDate)
                    };
                }, "Không có cư dân phù hợp"));

            int[] weights = { 88, 132, 84, 68, 56, 84, 106, 114, 90 };
            for (int i = 0; i < grid.Columns.Count && i < weights.Length; i++)
            {
                grid.Columns[i].FillWeight = weights[i];
            }

            grid.ClearSelection();
        }

        void SelectResident(ResidentDTO? resident)
        {
            ApartmentDTO? apartment = null;
            if (resident != null)
            {
                apartmentById.TryGetValue(resident.ApartmentID, out apartment);
            }

            var roommates = resident == null
                ? new List<ResidentDTO>()
                : allResidents
                    .Where(r => r.ApartmentID == resident.ApartmentID)
                    .OrderBy(r => Display(r.RelationshipWithOwner))
                    .ThenBy(r => Display(r.FullName))
                    .ToList();

            RenderResidentLinkedDetails(detailBody, resident, apartment, roommates);

            grid.ClearSelection();
            if (resident == null)
            {
                return;
            }

            for (int i = 0; i < displayResidents.Count && i < grid.Rows.Count; i++)
            {
                if (displayResidents[i].ResidentID == resident.ResidentID)
                {
                    grid.Rows[i].Selected = true;
                    grid.CurrentCell = grid.Rows[i].Cells[0];
                    break;
                }
            }
        }

        void ApplyFilters(ResidentDTO? preferredResident = null)
        {
            string selectedBuilding = buildingFilter.SelectedItem?.ToString() ?? "Tất cả";
            string selectedApartment = apartmentFilter.SelectedItem?.ToString() ?? "Tất cả";
            string selectedStatus = statusFilter.SelectedItem?.ToString() ?? "Tất cả";
            string selectedRole = roleFilter.SelectedItem?.ToString() ?? "Tất cả";
            string searchText = (search.Text ?? string.Empty).Trim();

            displayResidents = allResidents
                .Where(resident =>
                {
                    apartmentById.TryGetValue(resident.ApartmentID, out var apartment);
                    string buildingName = BuildingShort(apartment?.BuildingName);
                    string apartmentCode = Display(resident.ApartmentCode);
                    string residentStatus = ResidentLivingStatus(resident);
                    string relationship = Display(resident.RelationshipWithOwner);

                    if (selectedBuilding != "Tất cả" && !string.Equals(buildingName, selectedBuilding, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (selectedApartment != "Tất cả" && !string.Equals(apartmentCode, selectedApartment, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (selectedStatus != "Tất cả" && !string.Equals(residentStatus, selectedStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (selectedRole != "Tất cả" && !string.Equals(relationship, selectedRole, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (searchText.Length == 0)
                    {
                        return true;
                    }

                    string haystack = string.Join(" ", new[]
                    {
                        Display(resident.FullName, ""),
                        Display(resident.CCCD, ""),
                        Display(resident.Phone, ""),
                        Display(resident.Email, ""),
                        apartmentCode,
                        buildingName,
                        BlockShort(apartment?.BlockName)
                    });

                    return haystack.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
                })
                .OrderBy(resident => Display(resident.ApartmentCode, ""))
                .ThenBy(resident => Display(resident.RelationshipWithOwner, ""))
                .ThenBy(resident => Display(resident.FullName, ""))
                .ToList();

            BindResidentGrid(displayResidents);

            int apartmentCount = displayResidents
                .Select(resident => resident.ApartmentID)
                .Distinct()
                .Count();
            listSummary.Text = $"Hiển thị {displayResidents.Count:N0} / {allResidents.Count:N0} cư dân - {apartmentCount:N0} căn hộ có cư dân.";
            filterSummary.Text = selectedBuilding == "Tất cả"
                ? "Danh sách đang đồng bộ theo căn hộ thực tế trong database."
                : $"Đang lọc theo {selectedBuilding} và các căn hộ liên quan.";

            ResidentDTO? residentToSelect = preferredResident != null
                ? displayResidents.FirstOrDefault(r => r.ResidentID == preferredResident.ResidentID)
                : displayResidents.FirstOrDefault();
            SelectResident(residentToSelect);
        }

        var buildingOptions = apartments.Select(a => BuildingShort(a.BuildingName));
        var statusOptions = allResidents.Select(ResidentLivingStatus);
        var roleOptions = allResidents.Select(r => Display(r.RelationshipWithOwner));
        SetFilterItems(buildingFilter, buildingOptions, "Tất cả");
        SetFilterItems(statusFilter, statusOptions, "Tất cả");
        SetFilterItems(roleFilter, roleOptions, "Tất cả");
        RefreshApartmentFilter();

        buildingFilter.SelectedIndexChanged += (_, _) =>
        {
            if (suppressFilterEvents)
            {
                return;
            }

            RefreshApartmentFilter();
            ApplyFilters();
        };
        apartmentFilter.SelectedIndexChanged += (_, _) =>
        {
            if (!suppressFilterEvents)
            {
                ApplyFilters();
            }
        };
        statusFilter.SelectedIndexChanged += (_, _) =>
        {
            if (!suppressFilterEvents)
            {
                ApplyFilters();
            }
        };
        roleFilter.SelectedIndexChanged += (_, _) =>
        {
            if (!suppressFilterEvents)
            {
                ApplyFilters();
            }
        };
        search.TextChanged += (_, _) => ApplyFilters();
        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < displayResidents.Count)
            {
                SelectResident(displayResidents[e.RowIndex]);
            }
        };

        ApplyFilters(allResidents.FirstOrDefault());
    }

    private void RenderInvoices()
    {
        var page = BeginPage("Quản lý phí dịch vụ & hóa đơn", "");
        int w = PageWorkWidth();
        int y = 72;
        var invoices = InvoiceDAL.GetAllInvoices();
        var residentsByApartment = ResidentDAL.GetAllResidents()
            .GroupBy(r => r.ApartmentID)
            .ToDictionary(g => g.Key, g => g.First());

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 96);
        AddFilter(filters, "Tháng:", "05/2024", 14);
        AddFilter(filters, "Năm:", "2024", 236);
        AddFilter(filters, "Căn hộ:", "Tất cả", 458);
        var statusFilterCombo = AddInvoiceStatusFilter(filters, "Trạng thái thanh toán:", "Tất cả", 680);
        var search = ModernUi.TextBox("Tìm kiếm nhanh...", 300);
        search.Location = new Point(14, 60);
        filters.Controls.Add(search);
        page.Controls.Add(filters);

        y += 112;
        int leftW = (int)(w * 0.58);
        var list = ModernUi.Section("Danh sách hóa đơn", leftW, 430);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "", "Mã hóa đơn", "Tháng/Năm", "Căn hộ", "Chủ hộ", "Tổng tiền (VNĐ)", "Trạng thái thanh toán", "Ngày thanh toán" },
            RowsOrEmpty(invoices.Take(50), 8, (invoice, index) =>
            {
                residentsByApartment.TryGetValue(invoice.ApartmentID, out var resident);
                return new object[]
                {
                    index == 0 ? "›" : "",
                    InvoiceCode(invoice),
                    $"{invoice.Month:00}/{invoice.Year}",
                    Display(invoice.ApartmentCode),
                    Display(resident?.FullName),
                    Money(invoice.TotalAmount),
                    ViStatus(invoice.PaymentStatus),
                    invoice.PaidAmount > 0 ? DateText(invoice.UpdatedAt) : "-"
                };
            }));
        grid.Location = new Point(0, 44);
        grid.Size = new Size(list.Width, 336);
        list.Controls.Add(grid);
        var paging = ModernUi.Label($"Hiển thị 1 - {Math.Min(50, invoices.Count)} / {invoices.Count} hóa đơn", 9f, FontStyle.Regular, ModernUi.Text);
        paging.Location = new Point(16, 386);
        paging.Size = new Size(list.Width - 32, 28);
        list.Controls.Add(paging);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Chi tiết hóa đơn", w - leftW - 12, 430);
        detail.Location = new Point(list.Right + 12, y);
        var selectedInvoice = invoices.FirstOrDefault();
        residentsByApartment.TryGetValue(selectedInvoice?.ApartmentID ?? 0, out var selectedInvoiceResident);
        AddInvoiceDetail(detail, selectedInvoice, selectedInvoiceResident);
        page.Controls.Add(detail);

        y += 446;

        var latestInvoice = invoices.OrderByDescending(i => i.Year).ThenByDescending(i => i.Month).FirstOrDefault();
        var periodInvoices = latestInvoice == null ? new List<InvoiceDTO>() : invoices.Where(i => i.Year == latestInvoice.Year && i.Month == latestInvoice.Month).ToList();
        int paidCount = periodInvoices.Count(i => ViStatus(i.PaymentStatus) == "Đã thanh toán");
        int unpaidCount = periodInvoices.Count - paidCount;
        decimal totalAmount = periodInvoices.Sum(i => i.TotalAmount);
        decimal paidAmount = periodInvoices.Sum(i => i.PaidAmount);
        decimal debtAmount = periodInvoices.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount));
        int paidPercent = periodInvoices.Count == 0 ? 0 : (int)Math.Round(paidCount * 100m / periodInvoices.Count);
        var stats = ModernUi.Section(latestInvoice == null ? "Tình hình thanh toán" : $"Tình hình thanh toán tháng {latestInvoice.Month:00}/{latestInvoice.Year}", (int)(w * 0.50), 170);
        stats.Location = new Point(18, y);
        var donut = new DonutChartPanel
        {
            Percent = paidPercent,
            CenterText = periodInvoices.Count.ToString("N0"),
            SubText = "Tổng hóa đơn",
            AccentColor = ModernUi.Green,
            Location = new Point(20, 36),
            Size = new Size(360, 118)
        };
        stats.Controls.Add(donut);
        var summary = ModernUi.Label($"▣  Tổng số hóa đơn:        {periodInvoices.Count:N0}\r\n✓  Đã thanh toán:          {paidCount:N0} hóa đơn\r\n△  Chưa thanh toán:        {unpaidCount:N0} hóa đơn\r\n↗  Tổng phát sinh:         {Money(totalAmount)} VNĐ\r\n▣  Đã thu:                 {Money(paidAmount)} VNĐ ({paidPercent}%)\r\n◇  Còn phải thu:           {Money(debtAmount)} VNĐ",
            9.5f, FontStyle.Regular, ModernUi.Text);
        summary.Location = new Point(390, 34);
        summary.Size = new Size(stats.Width - 410, 124);
        stats.Controls.Add(summary);
        page.Controls.Add(stats);

        var actions = ModernUi.Section("", w - stats.Width - 12, 170);
        actions.Location = new Point(stats.Right + 12, y);
        AddInvoiceQuickActions(actions);
        page.Controls.Add(actions);
    }

    private void RenderComplaints()
    {
        var page = BeginPage("Phản ánh / Thông báo / Hợp đồng", "");
        int w = PageWorkWidth();
        int y = 72;
        var complaintsData = ComplaintDAL.GetAllComplaints();

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
        filters.Size = new Size(w, 138);
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
        AddFilter(filters, "Từ ngày", "01/05/2024", 12, 84);
        AddFilter(filters, "Đến ngày", "17/05/2024", 180, 84);
        page.Controls.Add(filters);

        y += 152;
        int leftW = (int)(w * 0.52);
        var list = ModernUi.Section($"Danh sách phản ánh ({complaintsData.Count:N0})", leftW, 400);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "", "Mã phản ánh", "Tiêu đề", "Căn hộ", "Loại phản ánh", "Ưu tiên", "Trạng thái", "Ngày gửi" },
            RowsOrEmpty(complaintsData.Take(50), 8, (c, i) => new object[]
            {
                i == 0 ? "☑" : (i + 1).ToString(),
                $"PA{c.CreatedAt:yyMMdd}-{c.ComplaintID:000}",
                c.Title,
                c.ApartmentCode,
                c.Category,
                ViStatus(c.Priority),
                ViStatus(c.Status),
                DateTimeText(c.CreatedAt)
            }));
        grid.Location = new Point(12, 42);
        grid.Size = new Size(list.Width - 24, 302);
        list.Controls.Add(grid);
        var paging = ModernUi.Label($"Hiển thị 1 - {Math.Min(50, complaintsData.Count)} / {complaintsData.Count} bản ghi",
            9f, FontStyle.Regular, ModernUi.Text);
        paging.Location = new Point(18, 354);
        paging.Size = new Size(list.Width - 36, 30);
        list.Controls.Add(paging);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Chi tiết phản ánh", w - leftW - 12, 400);
        detail.Location = new Point(list.Right + 12, y);
        AddComplaintDetail(detail, complaintsData.FirstOrDefault());
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

    private void RenderVehicles()
    {
        var page = BeginPage("Quản lý phương tiện", "Dashboard / Phương tiện");
        int w = PageWorkWidth();
        int y = 72;
        var vehicles = VehicleDAL.GetAllVehicles();
        var residentById = ResidentDAL.GetAllResidents().ToDictionary(r => r.ResidentID);

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 96);
        AddFilter(filters, "Tòa nhà", "Tất cả", 14);
        AddFilter(filters, "Căn hộ", "Tất cả", 238);
        AddFilter(filters, "Loại xe", "Tất cả", 462);
        AddFilter(filters, "Trạng thái thẻ", "Tất cả", 686);
        var search = ModernUi.TextBox("Tìm biển số, chủ sở hữu...", Math.Min(300, w - 220));
        search.Location = new Point(14, 62);
        filters.Controls.Add(search);
        var addButton = ModernUi.Button("+  Đăng ký xe", ModernUi.Blue, 132, 32);
        addButton.Location = new Point(w - 430, 60);
        filters.Controls.Add(addButton);
        var cardButton = ModernUi.Button("▣  Cấp thẻ", ModernUi.Green, 118, 32);
        cardButton.Location = new Point(w - 284, 60);
        filters.Controls.Add(cardButton);
        var lockButton = ModernUi.Button("×  Khóa thẻ", ModernUi.Red, 118, 32);
        lockButton.Location = new Point(w - 152, 60);
        filters.Controls.Add(lockButton);
        page.Controls.Add(filters);

        y += 112;
        int leftW = (int)(w * 0.65);
        int rightW = w - leftW - 12;

        var list = ModernUi.Section("Danh sách phương tiện", leftW, 438);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "Mã xe", "Loại xe", "Biển số", "Hãng xe", "Màu xe", "Chủ sở hữu", "Căn hộ", "Ngày đăng ký", "Trạng thái" },
            RowsOrEmpty(vehicles.Take(50), 9, (v, _) =>
            {
                residentById.TryGetValue((int)v.ResidentID, out ResidentDTO? resident);
                return new object[]
                {
                    $"XE{v.CreatedAt:yyMMdd}-{v.VehicleID:000}",
                    ViVehicleType(v.VehicleType),
                    v.LicensePlate,
                    Display(v.Brand),
                    Display(v.Color),
                    v.FullName,
                    Display(resident?.ApartmentCode),
                    DateText(v.CreatedAt),
                    ViStatus(v.Status)
                };
            }));
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 332);
        list.Controls.Add(grid);
        var hint = ModernUi.Label("Kiểm tra trùng biển số trước khi cấp thẻ gửi xe. Thẻ tạm khóa không được ghi nhận ra/vào bãi.", 9f, FontStyle.Regular, ModernUi.Muted);
        hint.Location = new Point(18, 390);
        hint.Size = new Size(list.Width - 36, 28);
        list.Controls.Add(hint);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Thông tin phương tiện", rightW, 438);
        detail.Location = new Point(list.Right + 12, y);
        dynamic? selectedVehicle = vehicles.FirstOrDefault();
        string selectedApartment = "-";
        if (selectedVehicle != null)
        {
            int selectedResidentId = (int)selectedVehicle.ResidentID;
            if (residentById.TryGetValue(selectedResidentId, out var selectedResident))
            {
                selectedApartment = Display(selectedResident.ApartmentCode);
            }
        }
        AddDetailField(detail, "Mã xe *", selectedVehicle == null ? "" : $"XE{selectedVehicle.CreatedAt:yyMMdd}-{selectedVehicle.VehicleID:000}", 44);
        AddDetailField(detail, "Loại xe *", selectedVehicle == null ? "" : ViVehicleType(selectedVehicle.VehicleType), 82);
        AddDetailField(detail, "Biển số *", selectedVehicle == null ? "" : Display(selectedVehicle.LicensePlate), 120);
        AddDetailField(detail, "Hãng xe", selectedVehicle == null ? "" : Display(selectedVehicle.Brand), 158);
        AddDetailField(detail, "Màu xe", selectedVehicle == null ? "" : Display(selectedVehicle.Color), 196);
        AddDetailField(detail, "Chủ sở hữu *", selectedVehicle == null ? "" : Display(selectedVehicle.FullName), 234);
        AddDetailField(detail, "Căn hộ *", selectedApartment, 272);
        AddDetailField(detail, "Trạng thái", selectedVehicle == null ? "" : ViStatus(selectedVehicle.Status), 310);
        var note = new TextBox
        {
            Text = selectedVehicle == null ? "" : Display(selectedVehicle.Note, ""),
            Location = new Point(120, 348),
            Size = new Size(detail.Width - 150, 42),
            Multiline = true,
            Font = ModernUi.Font(9f)
        };
        detail.Controls.Add(note);
        AddResponsiveFormActions(detail, 398);
        page.Controls.Add(detail);

        y += 454;
        var stats = ModernUi.Section("Thống kê phương tiện", w, 162);
        stats.Location = new Point(18, y);
        int cardW = (w - 36 - 36) / 4;
        int carCount = vehicles.Count(v => ViVehicleType(v.VehicleType) == "Ô tô");
        int motorbikeCount = vehicles.Count(v => ViVehicleType(v.VehicleType) == "Xe máy");
        int pendingCount = vehicles.Count(v => ViStatus(v.Status) == "Chờ duyệt");
        AddRow(stats, 42, 12,
            ModernUi.StatCard("Tổng phương tiện", vehicles.Count.ToString("N0"), "Đã đăng ký", ModernUi.Blue, "▣", $"{vehicles.Count(v => IsActiveStatus(v.Status))} đang hoạt động", cardW, 102),
            ModernUi.StatCard("Ô tô", carCount.ToString("N0"), "Xe", ModernUi.Orange, "▰", vehicles.Count == 0 ? "0%" : $"{carCount * 100 / vehicles.Count}% tổng số", cardW, 102),
            ModernUi.StatCard("Xe máy", motorbikeCount.ToString("N0"), "Xe", ModernUi.Green, "▦", vehicles.Count == 0 ? "0%" : $"{motorbikeCount * 100 / vehicles.Count}% tổng số", cardW, 102),
            ModernUi.StatCard("Chờ duyệt", pendingCount.ToString("N0"), "Cần xử lý", ModernUi.Red, "!", "Từ dữ liệu Vehicles", cardW, 102));
        page.Controls.Add(stats);
    }

    private void RenderVisitors()
    {
        var page = BeginPage("Quản lý khách ra vào", "Dashboard / Khách ra vào");
        int w = PageWorkWidth();
        int y = 72;
        var visitors = VisitorDAL.GetAllVisitors();
        var residentById = ResidentDAL.GetAllResidents().ToDictionary(r => r.ResidentID);

        var toolbar = new Panel { Location = new Point(18, y), Size = new Size(w, 42), BackColor = ModernUi.Surface };
        int actionX = Math.Max(0, w - 558);
        var approve = ModernUi.Button("✓  Duyệt khách", ModernUi.Green, 132, 36);
        approve.Location = new Point(actionX, 0);
        toolbar.Controls.Add(approve);
        var reject = ModernUi.Button("×  Từ chối", ModernUi.Red, 112, 36);
        reject.Location = new Point(actionX + 144, 0);
        toolbar.Controls.Add(reject);
        var checkIn = ModernUi.Button("↘  Ghi nhận vào", ModernUi.Blue, 140, 36);
        checkIn.Location = new Point(actionX + 268, 0);
        toolbar.Controls.Add(checkIn);
        var checkOut = ModernUi.Button("↗  Ghi nhận ra", ModernUi.Orange, 138, 36);
        checkOut.Location = new Point(actionX + 420, 0);
        toolbar.Controls.Add(checkOut);
        page.Controls.Add(toolbar);

        y += 50;
        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 96);
        AddFilter(filters, "Ngày đăng ký", "17/05/2024", 14);
        AddFilter(filters, "Căn hộ đến", "Tất cả", 238);
        AddFilter(filters, "Trạng thái duyệt", "Tất cả", 462);
        AddFilter(filters, "Loại khách", "Tất cả", 686);
        var search = ModernUi.TextBox("Tìm khách, SĐT, căn hộ...", Math.Min(360, w - 32));
        search.Location = new Point(14, 62);
        filters.Controls.Add(search);
        page.Controls.Add(filters);

        y += 112;
        int leftW = (int)(w * 0.64);
        int rightW = w - leftW - 12;

        var list = ModernUi.Section("Danh sách đăng ký khách", leftW, 422);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "Mã đăng ký", "Khách", "SĐT", "Căn hộ đến", "Người đăng ký", "Thời gian đến", "Thời gian rời", "Trạng thái", "Ghi chú" },
            RowsOrEmpty(visitors.Take(50), 9, (v, _) =>
            {
                residentById.TryGetValue((int)v.ResidentID, out ResidentDTO? resident);
                DateTime? departure = v.CheckOutTime as DateTime?;
                return new object[]
                {
                    $"KRA{v.CreatedAt:yyMMdd}-{v.VisitorID:000}",
                    Display(v.VisitorName),
                    Display(v.Phone),
                    Display(resident?.ApartmentCode),
                    Display(v.ResidentName),
                    DateTimeText(v.ArrivalTime),
                    DateTimeText(departure),
                    ViStatus(v.Status),
                    Display(v.Note, Display(v.Purpose))
                };
            }));
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 318);
        list.Controls.Add(grid);
        var paging = ModernUi.Label($"Hiển thị 1 - {Math.Min(50, visitors.Count)} / {visitors.Count} lượt đăng ký", 9f, FontStyle.Regular, ModernUi.Text);
        paging.Location = new Point(18, 374);
        paging.Size = new Size(list.Width - 36, 28);
        list.Controls.Add(paging);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Phiếu khách ra vào", rightW, 422);
        detail.Location = new Point(list.Right + 12, y);
        dynamic? selectedVisitor = visitors.FirstOrDefault();
        string visitorApartment = "-";
        if (selectedVisitor != null)
        {
            int residentId = (int)selectedVisitor.ResidentID;
            if (residentById.TryGetValue(residentId, out var resident))
            {
                visitorApartment = Display(resident.ApartmentCode);
            }
        }
        AddDetailField(detail, "Mã đăng ký", selectedVisitor == null ? "" : $"KRA{selectedVisitor.CreatedAt:yyMMdd}-{selectedVisitor.VisitorID:000}", 44);
        AddDetailField(detail, "Tên khách *", selectedVisitor == null ? "" : Display(selectedVisitor.VisitorName), 82);
        AddDetailField(detail, "SĐT *", selectedVisitor == null ? "" : Display(selectedVisitor.Phone), 120);
        AddDetailField(detail, "CCCD/Hộ chiếu", selectedVisitor == null ? "" : Display(selectedVisitor.IDNumber), 158);
        AddDetailField(detail, "Căn hộ đến *", visitorApartment, 196);
        AddDetailField(detail, "Người đăng ký", selectedVisitor == null ? "" : Display(selectedVisitor.ResidentName), 234);
        AddDetailField(detail, "Thời gian đến", selectedVisitor == null ? "" : DateTimeText(selectedVisitor.ArrivalTime), 272);
        AddDetailField(detail, "Trạng thái", selectedVisitor == null ? "" : ViStatus(selectedVisitor.Status), 310);
        var note = new TextBox
        {
            Text = selectedVisitor == null ? "" : Display(selectedVisitor.Note, Display(selectedVisitor.Purpose, "")),
            Location = new Point(120, 348),
            Size = new Size(detail.Width - 150, 42),
            Multiline = true,
            Font = ModernUi.Font(9f)
        };
        detail.Controls.Add(note);
        page.Controls.Add(detail);

        y += 438;
        var history = ModernUi.Section("Lịch sử vào / ra hôm nay", w, 194);
        history.Location = new Point(18, y);
        var todayVisitors = visitors
            .Where(v => ((DateTime)v.ArrivalTime).Date == DateTime.Today)
            .OrderByDescending(v => v.ArrivalTime)
            .Take(20)
            .ToList();
        var historyGrid = CreateGrid(
            new[] { "Thời gian", "Khách", "Căn hộ", "Cổng", "Bảo vệ", "Sự kiện", "Ghi chú" },
            RowsOrEmpty(todayVisitors, 7, (v, _) =>
            {
                residentById.TryGetValue((int)v.ResidentID, out ResidentDTO? resident);
                return new object[] { DateTimeText(v.ArrivalTime), Display(v.VisitorName), Display(resident?.ApartmentCode), "-", Display(v.ApprovedBy), ViStatus(v.Status), Display(v.Note, Display(v.Purpose)) };
            }, "Không có lượt khách hôm nay"));
        historyGrid.Location = new Point(12, 44);
        historyGrid.Size = new Size(history.Width - 24, 114);
        history.Controls.Add(historyGrid);
        page.Controls.Add(history);
    }

    private void RenderAssets()
    {
        var page = BeginPage("Quản lý tài sản chung & bảo trì", "Dashboard / Tài sản");
        int w = PageWorkWidth();
        int y = 72;
        var assets = AssetDAL.GetAllAssets();
        var maintenanceSchedules = AssetDAL.GetMaintenanceSchedules();

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 96);
        AddFilter(filters, "Loại tài sản", "Tất cả", 14);
        AddFilter(filters, "Khu vực", "Tất cả", 238);
        AddFilter(filters, "Tình trạng", "Tất cả", 462);
        AddFilter(filters, "Hạn bảo trì", "Tất cả", 686);
        var search = ModernUi.TextBox("Tìm mã, tên tài sản, vị trí...", Math.Min(340, w - 380));
        search.Location = new Point(14, 62);
        filters.Controls.Add(search);
        var addAsset = ModernUi.Button("+  Thêm tài sản", ModernUi.Blue, 142, 32);
        addAsset.Location = new Point(w - 448, 60);
        filters.Controls.Add(addAsset);
        var schedule = ModernUi.Button("⚒  Lập lịch", ModernUi.Orange, 128, 32);
        schedule.Location = new Point(w - 292, 60);
        filters.Controls.Add(schedule);
        var repair = ModernUi.Button("▤  Ghi sửa chữa", ModernUi.Green, 150, 32);
        repair.Location = new Point(w - 150, 60);
        filters.Controls.Add(repair);
        page.Controls.Add(filters);

        y += 112;
        int leftW = (int)(w * 0.64);
        int rightW = w - leftW - 12;

        var list = ModernUi.Section("Danh mục tài sản", leftW, 430);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "Mã tài sản", "Tên tài sản", "Loại", "Vị trí", "Ngày mua", "Tình trạng", "Bảo trì gần nhất", "Bảo trì tiếp theo", "Chi phí sửa chữa" },
            RowsOrEmpty(assets, 9, (a, _) => new object[]
            {
                a.AssetCode,
                a.AssetName,
                a.AssetType,
                a.Location,
                DateText(a.PurchaseDate),
                ViStatus(a.Condition),
                DateText(a.LastMaintenanceDate),
                DateText(a.NextMaintenanceDate),
                Money(a.RepairCost)
            }));
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 324);
        list.Controls.Add(grid);
        var note = ModernUi.Label("Tài sản đến hạn bảo trì trong 7 ngày sẽ được đưa vào danh sách cảnh báo.", 9f, FontStyle.Regular, ModernUi.Muted);
        note.Location = new Point(18, 382);
        note.Size = new Size(list.Width - 36, 28);
        list.Controls.Add(note);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Thông tin tài sản", rightW, 430);
        detail.Location = new Point(list.Right + 12, y);
        var selectedAsset = assets.FirstOrDefault();
        AddDetailField(detail, "Mã tài sản *", selectedAsset?.AssetCode ?? "", 44);
        AddDetailField(detail, "Tên tài sản *", selectedAsset?.AssetName ?? "", 82);
        AddDetailField(detail, "Loại *", selectedAsset?.AssetType ?? "", 120);
        AddDetailField(detail, "Vị trí *", selectedAsset?.Location ?? "", 158);
        AddDetailField(detail, "Ngày mua", DateText(selectedAsset?.PurchaseDate), 196);
        AddDetailField(detail, "Tình trạng", selectedAsset == null ? "" : ViStatus(selectedAsset.Condition), 234);
        AddDetailField(detail, "Bảo trì gần nhất", DateText(selectedAsset?.LastMaintenanceDate), 272);
        AddDetailField(detail, "Bảo trì tiếp theo", DateText(selectedAsset?.NextMaintenanceDate), 310);
        AddDetailField(detail, "Chi phí sửa chữa", selectedAsset == null ? "" : Money(selectedAsset.RepairCost), 348);
        AddResponsiveFormActions(detail, 388);
        page.Controls.Add(detail);

        y += 446;
        int dueW = (int)(w * 0.50);
        var due = ModernUi.Section("Cảnh báo bảo trì", dueW, 216);
        due.Location = new Point(18, y);
        var dueAssets = assets
            .Where(a => a.NextMaintenanceDate.HasValue && a.NextMaintenanceDate.Value <= DateTime.Today.AddDays(7))
            .OrderBy(a => a.NextMaintenanceDate)
            .Take(10)
            .ToList();
        var dueGrid = CreateGrid(
            new[] { "Tài sản", "Vị trí", "Hạn bảo trì", "Mức độ", "Người phụ trách" },
            RowsOrEmpty(dueAssets, 5, (a, _) => new object[]
            {
                a.AssetName,
                a.Location,
                DateText(a.NextMaintenanceDate),
                a.NextMaintenanceDate.HasValue && a.NextMaintenanceDate.Value.Date <= DateTime.Today ? "Cao" : "Trung bình",
                "-"
            }, "Không có tài sản đến hạn"));
        dueGrid.Location = new Point(12, 44);
        dueGrid.Size = new Size(due.Width - 24, 136);
        due.Controls.Add(dueGrid);
        page.Controls.Add(due);

        var plan = ModernUi.Section("Lịch bảo trì sắp tới", w - dueW - 12, 216);
        plan.Location = new Point(due.Right + 12, y);
        var planGrid = CreateGrid(
            new[] { "Ngày", "Hạng mục", "Khu vực", "Trạng thái" },
            RowsOrEmpty(maintenanceSchedules.Take(20), 4, (m, _) => new object[]
            {
                DateText(m.ScheduledDate),
                m.Category,
                m.Location,
                ViStatus(m.Status)
            }, "Không có lịch bảo trì"));
        planGrid.Location = new Point(12, 44);
        planGrid.Size = new Size(plan.Width - 24, 136);
        plan.Controls.Add(planGrid);
        page.Controls.Add(plan);
    }

    private void RenderReports()
    {
        var page = BeginPage("Báo cáo & thống kê", "Dashboard / Báo cáo");
        int w = PageWorkWidth();
        int y = 72;
        var residents = ResidentDAL.GetAllResidents();
        var apartments = ApartmentDAL.GetAllApartments();
        var invoices = InvoiceDAL.GetAllInvoices();
        var complaints = ComplaintDAL.GetAllComplaints();
        var vehicles = VehicleDAL.GetAllVehicles();
        var visitors = VisitorDAL.GetAllVisitors();
        int occupied = apartments.Count(a => ViStatus(a.Status) == "Đang sử dụng");
        int occupancyRate = apartments.Count == 0 ? 0 : (int)Math.Round(occupied * 100m / apartments.Count);
        decimal revenueTotal = invoices.Sum(i => i.PaidAmount);
        decimal debtTotal = invoices.Sum(i => Math.Max(0, i.TotalAmount - i.PaidAmount));
        int unpaidCount = invoices.Count(i => ViStatus(i.PaymentStatus) != "Đã thanh toán");

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 96);
        AddFilter(filters, "Kỳ báo cáo", "Tháng 05/2024", 14);
        AddFilter(filters, "Từ ngày", "01/05/2024", 238);
        AddFilter(filters, "Đến ngày", "17/05/2024", 462);
        AddFilter(filters, "Tòa nhà", "Tất cả", 686);
        var excel = ModernUi.Button("▥  Excel", ModernUi.Green, 100, 32);
        excel.Location = new Point(w - 330, 60);
        filters.Controls.Add(excel);
        var pdf = ModernUi.Button("PDF", ModernUi.Red, 88, 32);
        pdf.Location = new Point(w - 216, 60);
        filters.Controls.Add(pdf);
        var refresh = ModernUi.Button("⟳  Làm mới", ModernUi.Blue, 112, 32);
        refresh.Location = new Point(w - 116, 60);
        filters.Controls.Add(refresh);
        page.Controls.Add(filters);

        y += 112;
        int cardW = (w - 36 - 12 * 5) / 6;
        AddRow(page, y, 12,
            ModernUi.StatCard("Cư dân", residents.Count.ToString("N0"), "Người", ModernUi.Blue, "●●", $"{residents.Count(r => IsActiveStatus(r.Status))} hoạt động", cardW, 124),
            ModernUi.StatCard("Lấp đầy", $"{occupancyRate}%", "Căn hộ", ModernUi.Green, "◔", $"{occupied:N0}/{apartments.Count:N0}", cardW, 124),
            ModernUi.StatCard("Doanh thu", MoneyShort(revenueTotal), "VNĐ", ModernUi.Orange, "$", "Đã thu", cardW, 124),
            ModernUi.StatCard("Công nợ", MoneyShort(debtTotal), "VNĐ", ModernUi.Red, "!", $"{unpaidCount:N0} hóa đơn", cardW, 124),
            ModernUi.StatCard("Phản ánh", complaints.Count.ToString("N0"), "Phiếu", ModernUi.Purple, "▤", $"{complaints.Count(c => ViStatus(c.Status) == "Mới")} mới", cardW, 124),
            ModernUi.StatCard("Phương tiện", vehicles.Count.ToString("N0"), "Xe", ModernUi.Teal, "▣", $"{vehicles.Count(v => IsActiveStatus(v.Status))} hoạt động", cardW, 124));

        y += 140;
        int chartW = (w - 12) / 2;
        var revenue = ModernUi.Section("Doanh thu theo tháng (VNĐ)", chartW, 264);
        revenue.Location = new Point(18, y);
        var chart = new BarChartPanel
        {
            BarColor = ModernUi.Blue,
            AxisMax = 1,
            SeriesLabel = "Doanh thu (VNĐ)",
            Location = new Point(12, 42),
            Size = new Size(revenue.Width - 24, 188)
        };
        var monthly = invoices
            .GroupBy(i => new DateTime(i.Year, i.Month, 1))
            .OrderBy(g => g.Key)
            .TakeLast(12)
            .Select(g => (Label: $"T{g.Key.Month}", Value: ChartValue(g.Sum(i => i.PaidAmount))))
            .ToList();
        chart.AxisMax = Math.Max(1, monthly.Count == 0 ? 1 : (int)(monthly.Max(m => m.Value) * 1.2m));
        foreach (var item in monthly)
        {
            chart.Bars.Add(item);
        }
        revenue.Controls.Add(chart);
        page.Controls.Add(revenue);

        var operations = ModernUi.Section("Vận hành tòa nhà", w - chartW - 12, 264);
        operations.Location = new Point(revenue.Right + 12, y);
        int donutW = Math.Min(270, Math.Max(220, operations.Width - 280));
        var occupancy = new DonutChartPanel
        {
            Percent = occupancyRate,
            CenterText = $"{occupancyRate}%",
            SubText = "Lấp đầy",
            AccentColor = ModernUi.Green,
            Location = new Point(16, 48),
            Size = new Size(donutW, 168)
        };
        operations.Controls.Add(occupancy);
        var reportText = ModernUi.Label($"Phản ánh đã xử lý: {complaints.Count(c => ViStatus(c.Status) == "Đã xử lý"):N0} / {complaints.Count:N0}\r\nKhách ra vào hôm nay: {visitors.Count(v => ((DateTime)v.ArrivalTime).Date == DateTime.Today):N0} lượt\r\nCăn hộ bảo trì: {apartments.Count(a => ViStatus(a.Status) == "Bảo trì"):N0}\r\nPhương tiện hoạt động: {vehicles.Count(v => IsActiveStatus(v.Status)):N0} / {vehicles.Count:N0}",
            9.5f, FontStyle.Regular, ModernUi.Text);
        reportText.Location = new Point(donutW + 38, 64);
        reportText.Size = new Size(operations.Width - donutW - 56, 116);
        operations.Controls.Add(reportText);
        page.Controls.Add(operations);

        y += 280;
        int complaintW = (int)(w * 0.42);
        var complaintChart = ModernUi.Section("Phản ánh theo loại", complaintW, 234);
        complaintChart.Location = new Point(18, y);
        var complaintBars = new BarChartPanel
        {
            BarColor = ModernUi.Blue,
            AxisMax = 1,
            ShowValueLabels = true,
            SeriesLabel = "Số lượng phản ánh",
            Location = new Point(12, 42),
            Size = new Size(complaintChart.Width - 24, 160)
        };
        var complaintGroups = complaints
            .GroupBy(c => (string)Display(c.Category, "Khác"))
            .OrderByDescending(g => g.Count())
            .Take(6)
            .Select(g => (Label: g.Key.Length > 10 ? g.Key[..10] : g.Key, Value: g.Count()))
            .ToList();
        complaintBars.AxisMax = Math.Max(1, complaintGroups.Count == 0 ? 1 : (int)(complaintGroups.Max(g => g.Value) * 1.2m));
        foreach (var item in complaintGroups)
        {
            complaintBars.Bars.Add(item);
        }
        complaintChart.Controls.Add(complaintBars);
        page.Controls.Add(complaintChart);

        var saved = ModernUi.Section("Danh sách báo cáo đã tạo", w - complaintW - 12, 234);
        saved.Location = new Point(complaintChart.Right + 12, y);
        var savedGrid = CreateGrid(
            new[] { "Báo cáo", "Kỳ", "Người tạo", "Ngày tạo", "Định dạng", "Trạng thái" },
            new object[][]
            {
                new object[] { "Doanh thu tháng", "05/2024", "ketoan01", "17/05/2024 15:10", "Excel", "Thành công" },
                new object[] { "Công nợ cư dân", "05/2024", "manager1", "17/05/2024 14:40", "PDF", "Thành công" },
                new object[] { "Phản ánh vận hành", "Tuần 20", "manager1", "16/05/2024 18:20", "Excel", "Thành công" },
                new object[] { "Tài sản bảo trì", "05/2024", "admin1", "16/05/2024 16:05", "PDF", "Chờ xử lý" }
            });
        savedGrid.Location = new Point(12, 44);
        savedGrid.Size = new Size(saved.Width - 24, 150);
        saved.Controls.Add(savedGrid);
        page.Controls.Add(saved);
    }

    private void RenderSystemLogs()
    {
        var page = BeginPage("Log hệ thống", "Dashboard / Log hệ thống");
        int w = PageWorkWidth();
        int y = 72;
        var logs = AuditLogDAL.GetAuditLogs(limit: 100);
        var userByName = UserDAL.GetAllUsers()
            .Where(u => !string.IsNullOrWhiteSpace(u.Username))
            .GroupBy(u => u.Username!)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 96);
        AddFilter(filters, "Người dùng", "Tất cả", 14);
        AddFilter(filters, "Hành động", "Tất cả", 238);
        AddFilter(filters, "Mức độ", "Tất cả", 462);
        AddFilter(filters, "Từ ngày", "01/05/2024", 686);
        var search = ModernUi.TextBox("Tìm module, nội dung, IP...", Math.Min(340, w - 420));
        search.Location = new Point(14, 62);
        filters.Controls.Add(search);
        var export = ModernUi.Button("▥  Xuất log", ModernUi.Green, 120, 32);
        export.Location = new Point(w - 272, 60);
        filters.Controls.Add(export);
        var clear = ModernUi.Button("×  Xóa log cũ", ModernUi.Red, 132, 32);
        clear.Location = new Point(w - 138, 60);
        filters.Controls.Add(clear);
        page.Controls.Add(filters);

        y += 112;
        int leftW = (int)(w * 0.68);
        int rightW = w - leftW - 12;

        var list = ModernUi.Section("Nhật ký hoạt động", leftW, 460);
        list.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "Thời gian", "Người dùng", "Vai trò", "Hành động", "Module", "Nội dung", "IP", "Mức độ" },
            RowsOrEmpty(logs.Take(50), 8, (l, _) =>
            {
                string username = Display(l.Username, "system");
                userByName.TryGetValue(username, out var user);
                return new object[]
                {
                    DateTimeText(l.Timestamp),
                    username,
                    Display(user?.RoleName),
                    Display(l.Action),
                    Display(l.EntityName),
                    Display(l.Description, $"{l.Action} {l.EntityName}"),
                    Display(l.IPAddress),
                    AuditLevel(l.Action)
                };
            }));
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 356);
        list.Controls.Add(grid);
        var pager = ModernUi.Label($"Hiển thị 1 - {Math.Min(50, logs.Count)} / {logs.Count} dòng log", 9f, FontStyle.Regular, ModernUi.Text);
        pager.Location = new Point(18, 412);
        pager.Size = new Size(list.Width - 36, 28);
        list.Controls.Add(pager);
        page.Controls.Add(list);

        var detail = ModernUi.Section("Chi tiết log", rightW, 460);
        detail.Location = new Point(list.Right + 12, y);
        dynamic? selectedLog = logs.FirstOrDefault();
        string selectedUsername = selectedLog == null ? "" : Display(selectedLog.Username, "system");
        userByName.TryGetValue(selectedUsername, out var selectedUser);
        AddDetailField(detail, "Thời gian", selectedLog == null ? "" : DateTimeText(selectedLog.Timestamp), 48);
        AddDetailField(detail, "Người dùng", selectedUsername, 88);
        AddDetailField(detail, "Vai trò", Display(selectedUser?.RoleName), 128);
        AddDetailField(detail, "Module", selectedLog == null ? "" : Display(selectedLog.EntityName), 168);
        AddDetailField(detail, "Hành động", selectedLog == null ? "" : Display(selectedLog.Action), 208);
        AddDetailField(detail, "IP", selectedLog == null ? "" : Display(selectedLog.IPAddress), 248);
        AddDetailField(detail, "Mức độ", selectedLog == null ? "" : AuditLevel(selectedLog.Action), 288);
        var content = new TextBox
        {
            Text = selectedLog == null ? "" : Display(selectedLog.Description, $"{selectedLog.Action} {selectedLog.EntityName}"),
            Location = new Point(120, 328),
            Size = new Size(detail.Width - 150, 74),
            Multiline = true,
            Font = ModernUi.Font(9f)
        };
        detail.Controls.Add(content);
        page.Controls.Add(detail);

        y += 476;
        var security = ModernUi.Section("Cảnh báo bảo mật gần đây", w, 190);
        security.Location = new Point(18, y);
        var securityLogs = logs
            .Where(l => AuditLevel(l.Action) != "Thông tin")
            .Take(20)
            .ToList();
        var securityGrid = CreateGrid(
            new[] { "Thời gian", "Nguồn", "Sự kiện", "Mức độ", "Trạng thái" },
            RowsOrEmpty(securityLogs, 5, (l, _) => new object[]
            {
                DateTimeText(l.Timestamp),
                Display(l.IPAddress),
                Display(l.Description, $"{l.Action} {l.EntityName}"),
                AuditLevel(l.Action),
                "Đã ghi nhận"
            }, "Không có cảnh báo bảo mật"));
        securityGrid.Location = new Point(12, 44);
        securityGrid.Size = new Size(security.Width - 24, 108);
        security.Controls.Add(securityGrid);
        page.Controls.Add(security);
    }

    private void RenderSystemSettings()
    {
        var page = BeginPage("Cấu hình hệ thống", "Dashboard / Cấu hình hệ thống");
        int w = PageWorkWidth();
        int y = 72;
        int gap = 12;
        int colW = (w - gap) / 2;
        var systemConfigs = GetSystemConfigs();
        var connectionBuilder = new SqlConnectionStringBuilder(ConfigurationHelper.GetConnectionString("ApartmentManagerDB"));
        int backupWarningDays = int.TryParse(ConfigValue(systemConfigs, "BackupWarningDays", "7"), out var parsedWarningDays) ? Math.Max(1, parsedWarningDays) : 7;
        string lastBackupRaw = ConfigValue(systemConfigs, "LastBackupAt", "");
        DateTime? lastBackupAt = ParseDashboardDate(lastBackupRaw);
        bool backupOverdue = IsBackupOverdue(lastBackupAt, backupWarningDays);
        string backupStatusText = lastBackupAt.HasValue ? $"Đã backup: {DateTimeText(lastBackupAt.Value)}" : "Chưa có dữ liệu backup";

        var general = ModernUi.Section("Cấu hình chung", colW, 284);
        general.Location = new Point(18, y);
        AddDetailField(general, "Tên hệ thống", ConfigValue(systemConfigs, "AppName", ConfigurationHelper.GetAppSetting("AppName")), 46);
        AddDetailField(general, "Múi giờ", ConfigValue(systemConfigs, "TimeZone", TimeZoneInfo.Local.Id), 86);
        AddDetailField(general, "Ngôn ngữ", ConfigValue(systemConfigs, "Language", "Tiếng Việt"), 126);
        AddDetailField(general, "Định dạng ngày", ConfigValue(systemConfigs, "DateTimeFormat", "dd/MM/yyyy HH:mm"), 166);
        AddDetailField(general, "Phiên bản", ConfigValue(systemConfigs, "AppVersion", ConfigurationHelper.GetAppSetting("AppVersion")), 206);
        AddResponsiveFormActions(general, 244);
        page.Controls.Add(general);

        var security = ModernUi.Section("Bảo mật đăng nhập", colW, 284);
        security.Location = new Point(general.Right + gap, y);
        AddDetailField(security, "Mật khẩu mạnh", ConfigValue(systemConfigs, "RequireStrongPassword", "Bắt buộc"), 46);
        AddDetailField(security, "Sai tối đa", $"{ConfigValue(systemConfigs, "MaxLoginAttempts", ConfigurationHelper.GetAppSetting("MaxLoginAttempts"))} lần", 86);
        AddDetailField(security, "Khóa tài khoản", $"{ConfigValue(systemConfigs, "LockDurationMinutes", ConfigurationHelper.GetAppSetting("LockDurationMinutes"))} phút", 126);
        AddDetailField(security, "Hết hạn mật khẩu", $"{ConfigurationHelper.GetAppSetting("PasswordExpirationDays")} ngày", 166);
        AddDetailField(security, "Phiên làm việc", $"{ConfigurationHelper.GetAppSetting("SessionTimeoutMinutes")} phút", 206);
        var saveSecurity = ModernUi.Button("▣  Lưu bảo mật", ModernUi.Blue, 138, 32);
        saveSecurity.Location = new Point(18, 244);
        security.Controls.Add(saveSecurity);
        page.Controls.Add(security);

        y += 300;
        var database = ModernUi.Section("Kết nối & dữ liệu", colW, 300);
        database.Location = new Point(18, y);
        AddDetailField(database, "SQL Server", connectionBuilder.DataSource, 46);
        AddDetailField(database, "Database", connectionBuilder.InitialCatalog, 86);
        AddDetailField(database, "Xác thực", connectionBuilder.IntegratedSecurity ? "Windows" : "SQL Login", 126);
        AddDetailField(database, "Backup gần nhất", BackupDisplayText(lastBackupRaw), 166);
        AddDetailField(database, "Thư mục backup", ConfigValue(systemConfigs, "BackupPath", ".\\backups"), 206);
        var test = ModernUi.Button("✓  Test kết nối", ModernUi.Green, 138, 32);
        test.Location = new Point(18, 252);
        database.Controls.Add(test);
        var save = ModernUi.Button("▣  Lưu cấu hình", ModernUi.Blue, 138, 32);
        save.Location = new Point(168, 252);
        database.Controls.Add(save);
        page.Controls.Add(database);

        var backup = ModernUi.Section("Backup / Restore", colW, 300);
        backup.Location = new Point(database.Right + gap, y);
        var backupNow = ModernUi.Button("▤  Backup ngay", ModernUi.Orange, Math.Max(150, (backup.Width - 48) / 2), 48);
        backupNow.Location = new Point(18, 56);
        backupNow.Click += (_, _) => RunDatabaseBackup();
        backup.Controls.Add(backupNow);
        var restore = ModernUi.Button("↥  Restore từ file", ModernUi.Blue, Math.Max(150, (backup.Width - 48) / 2), 48);
        restore.Location = new Point(backupNow.Right + 12, 56);
        backup.Controls.Add(restore);
        var autoBackup = ModernUi.Label(
            $"Tự động backup: {ConfigValue(systemConfigs, "AutoBackupTime", "02:00")} hằng ngày\r\n" +
            $"Giữ bản sao lưu: {ConfigValue(systemConfigs, "BackupRetentionDays", "30")} ngày\r\n" +
            $"Kiểm tra toàn vẹn dữ liệu: {ConfigValue(systemConfigs, "VerifyBackup", "Đã bật")}\r\n" +
            $"Cảnh báo backup quá hạn: {ConfigValue(systemConfigs, "BackupWarningDays", "7")} ngày",
            9.5f, FontStyle.Regular, ModernUi.Text);
        autoBackup.Location = new Point(22, 126);
        autoBackup.Size = new Size(backup.Width - 44, 112);
        backup.Controls.Add(autoBackup);
        string backupState = backupOverdue ? $"Cần backup (quá {backupWarningDays} ngày)" : backupStatusText;
        var backupStatus = ModernUi.Badge($"Trạng thái: {backupState}", backupOverdue ? ModernUi.Orange : ModernUi.Green);
        backupStatus.Location = new Point(22, 244);
        backupStatus.Size = new Size(backup.Width - 44, 32);
        backup.Controls.Add(backupStatus);
        page.Controls.Add(backup);

        y += 316;
        var configs = ModernUi.Section("Bảng cấu hình hệ thống", w, 226);
        configs.Location = new Point(18, y);
        var grid = CreateGrid(
            new[] { "ConfigKey", "ConfigValue", "Mô tả", "Cập nhật lúc", "Cập nhật bởi" },
            RowsOrEmpty(systemConfigs, 5, (c, _) => new object[] { c.ConfigKey, c.ConfigValue, Display(c.Description), DateTimeText(c.UpdatedAt), Display(c.UpdatedBy) }));
        grid.Location = new Point(12, 44);
        grid.Size = new Size(configs.Width - 24, 146);
        configs.Controls.Add(grid);
        page.Controls.Add(configs);
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
        var headerUser = ModernUi.Label($"●  {CurrentUsername()} ▾", 9.5f, FontStyle.Bold, ModernUi.Navy);
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
        var role = ModernUi.ComboBox(new[] { "Tất cả", "Super Admin", "Quản lý", "Cư dân" }, 168);
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
        var addButton = AddToolbarButton(toolbar, "+  Thêm", ModernUi.Blue, buttonX, buttonY, 88);
        var editButton = AddToolbarButton(toolbar, "✎  Sửa", Color.FromArgb(241, 166, 0), buttonX + 102, buttonY, 86);
        var deleteButton = AddToolbarButton(toolbar, "×  Xóa", ModernUi.Red, buttonX + 202, buttonY, 86);
        var lockButton = AddToolbarButton(toolbar, "▣  Khóa/Mở khóa", ModernUi.Blue, buttonX + 302, buttonY, 130);
        var resetPasswordButton = AddToolbarButton(toolbar, "⚿  Reset MK", ModernUi.Purple, buttonX + 446, buttonY, 128);

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

        int totalUsers = UserDAL.GetAllUsers().Count;
        var paging = ModernUi.Label($"Hiển thị 1 - {Math.Min(8, totalUsers)} / {totalUsers} tài khoản", 9f, FontStyle.Regular, ModernUi.Text);
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
        var usernameInput = AddAccountInput(account, "Username *", "superadmin", fx, 42, colW);
        var fullNameInput = AddAccountInput(account, "Họ tên *", "Nguyễn Văn An", col2X, 42, colW);
        var emailInput = AddAccountInput(account, "Email *", "superadmin@chungcu.vn", fx, 84, colW);
        var phoneInput = AddAccountInput(account, "SĐT", "0909123456", col2X, 84, colW);
        var roleInput = AddAccountCombo(account, "Vai trò *", new[] { "Super Admin", "Quản lý", "Cư dân" }, fx, 126, colW);
        var statusInput = AddAccountCombo(account, "Trạng thái *", new[] { "Hoạt động", "Tạm khóa", "Chờ duyệt", "Từ chối" }, col2X, 126, colW);

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

        var roles = RolePermissionDAL.GetAllRoles();
        UserDTO? selectedUser = null;
        string? selectedAvatarPath = null;

        if (perPage.Items.Count > 0)
        {
            perPage.SelectedItem = "20";
        }

        bool IsResidentRole(string? roleName)
            => string.Equals(roleName, "Resident", StringComparison.OrdinalIgnoreCase);

        RoleDTO? FindRoleByLabel(string? label)
            => roles.FirstOrDefault(r =>
                string.Equals(UserRoleLabel(r.RoleName), label, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.RoleName, label, StringComparison.OrdinalIgnoreCase));

        void SetFormMode(bool creating)
        {
            accountTitle.Text = creating ? "THÊM TÀI KHOẢN MỚI" : "THÔNG TIN TÀI KHOẢN";
            save.Text = creating ? "▣  Tạo mới" : "▣  Lưu";
        }

        void PopulateAccountForm(UserDTO? user, bool clearSelection = false)
        {
            selectedUser = user;
            selectedAvatarPath = user?.AvatarPath;
            choose.Text = string.IsNullOrWhiteSpace(selectedAvatarPath)
                ? "▣ Chọn ảnh"
                : $"▣ {Path.GetFileName(selectedAvatarPath)}";

            if (user == null)
            {
                usernameInput.Text = string.Empty;
                fullNameInput.Text = string.Empty;
                emailInput.Text = string.Empty;
                phoneInput.Text = string.Empty;
                if (roleInput.Items.Count > 0)
                {
                    roleInput.SelectedIndex = 0;
                }

                statusInput.SelectedItem = "Hoạt động";
                approved.Checked = true;
                forceChange.Checked = true;
                if (clearSelection)
                {
                    userGrid.ClearSelection();
                }

                SetFormMode(true);
                return;
            }

            usernameInput.Text = Display(user.Username, "");
            fullNameInput.Text = Display(user.FullName, "");
            emailInput.Text = Display(user.Email, "");
            phoneInput.Text = Display(user.Phone, "");
            roleInput.SelectedItem = UserRoleLabel(user.RoleName);
            statusInput.SelectedItem = ViStatus(user.Status);
            approved.Checked = user.IsApproved;
            forceChange.Checked = false;
            SetFormMode(false);
        }

        List<UserDTO> FilterUsers()
        {
            var filtered = UserDAL.GetAllUsers().AsEnumerable();
            string keyword = search.Text.Trim();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(u =>
                    Display(u.Username).Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    Display(u.Email).Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    Display(u.FullName).Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            string roleFilter = role.Text.Trim();
            if (!string.IsNullOrWhiteSpace(roleFilter) && !string.Equals(roleFilter, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(u => string.Equals(UserRoleLabel(u.RoleName), roleFilter, StringComparison.OrdinalIgnoreCase));
            }

            string statusFilter = status.Text.Trim();
            if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(u => string.Equals(ViStatus(u.Status), statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.ToList();
        }

        void RefreshAuditLog()
        {
            var logs = AuditLogDAL.GetAuditLogs(limit: 50)
                .Where(l =>
                    string.Equals(Display(l.EntityName), "User", StringComparison.OrdinalIgnoreCase) ||
                    Display(l.Action).Contains("Login", StringComparison.OrdinalIgnoreCase) ||
                    Display(l.Action).Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                    Display(l.Action).Contains("Account", StringComparison.OrdinalIgnoreCase))
                .Take(50)
                .ToList();

            PopulateAccountLogGrid(logGrid, logs);
        }

        void RefreshUsers(int? selectUserId = null)
        {
            var filteredUsers = FilterUsers();
            PopulateAccountUsersGrid(userGrid, filteredUsers);
            paging.Text = filteredUsers.Count == 0
                ? "Không có tài khoản phù hợp"
                : $"Hiển thị 1 - {filteredUsers.Count} / {UserDAL.GetAllUsers().Count} tài khoản";

            if (selectUserId.HasValue)
            {
                foreach (DataGridViewRow row in userGrid.Rows)
                {
                    if (row.Tag is UserDTO rowUser && rowUser.UserID == selectUserId.Value)
                    {
                        row.Selected = true;
                        userGrid.CurrentCell = row.Cells[0];
                        PopulateAccountForm(rowUser);
                        RefreshAuditLog();
                        return;
                    }
                }
            }

            if (filteredUsers.Count > 0)
            {
                userGrid.Rows[0].Selected = true;
                userGrid.CurrentCell = userGrid.Rows[0].Cells[0];
                PopulateAccountForm(filteredUsers[0]);
            }
            else
            {
                PopulateAccountForm(null);
            }

            RefreshAuditLog();
        }

        bool ValidateAccountForm(out RoleDTO? selectedRole, out string dbStatus)
        {
            selectedRole = FindRoleByLabel(roleInput.Text);
            dbStatus = DbUserStatus(statusInput.Text);

            if (!ValidationHelper.IsValidUsername(usernameInput.Text.Trim()))
            {
                MessageBox.Show(this, "Username phải từ 3-50 ký tự và chỉ gồm chữ, số, dấu gạch dưới.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                usernameInput.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(fullNameInput.Text))
            {
                MessageBox.Show(this, "Họ tên không được để trống.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                fullNameInput.Focus();
                return false;
            }

            if (!ValidationHelper.IsValidEmail(emailInput.Text.Trim()))
            {
                MessageBox.Show(this, "Email không hợp lệ.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                emailInput.Focus();
                return false;
            }

            if (!ValidationHelper.IsValidPhone(phoneInput.Text.Trim()))
            {
                MessageBox.Show(this, "Số điện thoại không hợp lệ.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                phoneInput.Focus();
                return false;
            }

            if (selectedRole == null)
            {
                MessageBox.Show(this, "Bạn chưa chọn vai trò hợp lệ.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            bool approvedValue = approved.Checked;
            if (!IsResidentRole(selectedRole.RoleName))
            {
                approvedValue = true;
                approved.Checked = true;
                if (dbStatus is "Pending" or "Rejected")
                {
                    dbStatus = "Active";
                    statusInput.SelectedItem = "Hoạt động";
                }
            }
            else if (approvedValue && dbStatus == "Pending")
            {
                dbStatus = "Active";
                statusInput.SelectedItem = "Hoạt động";
            }
            else if (!approvedValue && dbStatus == "Active")
            {
                dbStatus = "Pending";
                statusInput.SelectedItem = "Chờ duyệt";
            }

            return true;
        }

        void SaveAccountChanges()
        {
            if (!ValidateAccountForm(out var selectedRole, out var dbStatus) || selectedRole == null)
            {
                return;
            }

            string username = usernameInput.Text.Trim();
            string fullName = fullNameInput.Text.Trim();
            string email = emailInput.Text.Trim();
            string phone = phoneInput.Text.Trim();
            bool isApproved = approved.Checked || !IsResidentRole(selectedRole.RoleName);

            if (selectedUser == null)
            {
                if (UserDAL.UsernameExists(username))
                {
                    MessageBox.Show(this, "Username đã tồn tại.",
                        "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (UserDAL.EmailExists(email))
                {
                    MessageBox.Show(this, "Email đã tồn tại.",
                        "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string tempPassword = GenerateTemporaryPassword();
                int newUserId = UserDAL.CreateUser(
                    username,
                    PasswordHasher.HashPassword(tempPassword),
                    fullName,
                    email,
                    phone,
                    selectedRole.RoleID,
                    dbStatus);

                bool updated = UserDAL.UpdateUser(
                    newUserId,
                    username,
                    fullName,
                    email,
                    phone,
                    selectedRole.RoleID,
                    dbStatus,
                    isApproved,
                    selectedAvatarPath,
                    _session?.UserID);

                if (!updated)
                {
                    MessageBox.Show(this, "Tạo tài khoản thất bại ở bước cập nhật thông tin.",
                        "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                AuditLogDAL.LogAction(_session?.UserID, "Create_User", "User", newUserId, $"Tạo tài khoản: {username}");
                MessageBox.Show(this,
                    $"Đã tạo tài khoản thành công.\nMật khẩu tạm thời: {tempPassword}",
                    "Quản lý tài khoản",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                RefreshUsers(newUserId);
                return;
            }

            if (UserDAL.UsernameExists(username, selectedUser.UserID))
            {
                MessageBox.Show(this, "Username đã tồn tại.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (UserDAL.EmailExists(email, selectedUser.UserID))
            {
                MessageBox.Show(this, "Email đã tồn tại.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool success = UserDAL.UpdateUser(
                selectedUser.UserID,
                username,
                fullName,
                email,
                phone,
                selectedRole.RoleID,
                dbStatus,
                isApproved,
                string.IsNullOrWhiteSpace(selectedAvatarPath) ? selectedUser.AvatarPath : selectedAvatarPath,
                _session?.UserID);

            if (!success)
            {
                MessageBox.Show(this, "Không thể lưu thay đổi tài khoản.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AuditLogDAL.LogAction(_session?.UserID, "Update_User", "User", selectedUser.UserID, $"Cập nhật tài khoản: {username}");
            MessageBox.Show(this, "Đã lưu thay đổi tài khoản.",
                "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshUsers(selectedUser.UserID);
        }

        void DeleteSelectedUser()
        {
            if (selectedUser == null)
            {
                MessageBox.Show(this, "Bạn chưa chọn tài khoản để xóa.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedUser.UserID == _session?.UserID)
            {
                MessageBox.Show(this, "Không thể xóa tài khoản đang đăng nhập.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(this,
                    $"Xóa tài khoản `{selectedUser.Username}`?",
                    "Quản lý tài khoản",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            if (!UserDAL.DeleteUser(selectedUser.UserID))
            {
                MessageBox.Show(this,
                    "Không thể xóa tài khoản. Tài khoản có thể đang được tham chiếu ở dữ liệu khác.",
                    "Quản lý tài khoản",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            AuditLogDAL.LogAction(_session?.UserID, "Delete_User", "User", selectedUser.UserID, $"Xóa tài khoản: {selectedUser.Username}");
            RefreshUsers();
        }

        void ToggleSelectedUserLock()
        {
            if (selectedUser == null)
            {
                MessageBox.Show(this, "Bạn chưa chọn tài khoản.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedUser.UserID == _session?.UserID)
            {
                MessageBox.Show(this, "Không thể khóa tài khoản đang đăng nhập.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isActive = string.Equals(selectedUser.Status, "Active", StringComparison.OrdinalIgnoreCase);
            string targetStatus;
            DateTime? lockedUntil;
            string actionName;

            if (isActive)
            {
                targetStatus = "Inactive";
                lockedUntil = DateTime.Now.AddYears(10);
                actionName = "Lock_Account";
            }
            else
            {
                targetStatus = selectedUser.IsApproved || !IsResidentRole(selectedUser.RoleName) ? "Active" : "Pending";
                lockedUntil = null;
                actionName = "Unlock_Account";
            }

            if (!UserDAL.UpdateUserStatus(selectedUser.UserID, targetStatus, lockedUntil))
            {
                MessageBox.Show(this, "Không thể cập nhật trạng thái khóa/mở khóa.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AuditLogDAL.LogAction(_session?.UserID, actionName, "User", selectedUser.UserID, $"{actionName}: {selectedUser.Username}");
            RefreshUsers(selectedUser.UserID);
        }

        void ResetSelectedUserPassword()
        {
            if (selectedUser == null)
            {
                MessageBox.Show(this, "Bạn chưa chọn tài khoản.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tempPassword = GenerateTemporaryPassword();
            var result = AuthenticationBLL.ResetPassword(selectedUser.UserID, tempPassword, tempPassword);
            if (!result.success)
            {
                MessageBox.Show(this, result.message,
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AuditLogDAL.LogAction(_session?.UserID, "Reset_Password_Admin", "User", selectedUser.UserID, $"Reset mật khẩu: {selectedUser.Username}");
            MessageBox.Show(this,
                $"Đã đặt lại mật khẩu.\nMật khẩu tạm thời: {tempPassword}",
                "Quản lý tài khoản",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            RefreshAuditLog();
        }

        search.TextChanged += (_, _) => RefreshUsers(selectedUser?.UserID);
        role.SelectedIndexChanged += (_, _) => RefreshUsers(selectedUser?.UserID);
        status.SelectedIndexChanged += (_, _) => RefreshUsers(selectedUser?.UserID);

        userGrid.SelectionChanged += (_, _) =>
        {
            if (userGrid.CurrentRow?.Tag is UserDTO rowUser)
            {
                PopulateAccountForm(rowUser);
            }
        };

        addButton.Click += (_, _) => PopulateAccountForm(null, clearSelection: true);
        editButton.Click += (_, _) =>
        {
            if (selectedUser == null)
            {
                MessageBox.Show(this, "Bạn chưa chọn tài khoản để sửa.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PopulateAccountForm(UserDAL.GetUserByID(selectedUser.UserID) ?? selectedUser);
        };
        deleteButton.Click += (_, _) => DeleteSelectedUser();
        lockButton.Click += (_, _) => ToggleSelectedUserLock();
        resetPasswordButton.Click += (_, _) => ResetSelectedUserPassword();
        save.Click += (_, _) => SaveAccountChanges();
        cancel.Click += (_, _) => RefreshUsers(selectedUser?.UserID);
        choose.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Chọn ảnh đại diện",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                selectedAvatarPath = dialog.FileName;
                choose.Text = $"▣ {Path.GetFileName(dialog.FileName)}";
            }
        };

        RefreshUsers();

        static Button AddToolbarButton(Control parent, string text, Color color, int x, int y, int width)
        {
            var button = ModernUi.Button(text, color, width, 36);
            button.Location = new Point(x, y);
            parent.Controls.Add(button);
            return button;
        }
    }

    private static DataGridView CreateAccountUsersGrid()
    {
        var grid = ModernUi.Grid();
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ColumnHeadersHeight = 36;
        grid.RowTemplate.Height = 34;
        grid.ScrollBars = ScrollBars.Vertical;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        AddGridColumn(grid, "Username", 1.05f);
        AddGridColumn(grid, "Họ tên", 1.18f);
        AddGridColumn(grid, "Email", 1.52f);
        AddGridColumn(grid, "SĐT", 1f);
        AddGridColumn(grid, "Vai trò", 1.05f);
        AddGridColumn(grid, "Trạng thái", 0.9f);
        AddGridColumn(grid, "Đã duyệt", 0.82f);
        AddGridColumn(grid, "Lần đăng nhập cuối", 1.18f);
        return grid;
    }

    private static void PopulateAccountUsersGrid(DataGridView grid, IReadOnlyList<UserDTO> users)
    {
        grid.Rows.Clear();

        if (users.Count == 0)
        {
            int rowIndex = grid.Rows.Add("Không có dữ liệu", "", "", "", "", "", "", "");
            grid.Rows[rowIndex].Tag = null;
            return;
        }

        for (int i = 0; i < users.Count; i++)
        {
            var user = users[i];
            int rowIndex = grid.Rows.Add(
                Display(user.Username),
                Display(user.FullName),
                Display(user.Email),
                Display(user.Phone),
                UserRoleLabel(user.RoleName),
                ViStatus(user.Status),
                user.IsApproved ? "✓" : "×",
                DateTimeText(user.LastLoginAt));

            var row = grid.Rows[rowIndex];
            row.Tag = user;
            if (i == 0)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(222, 237, 255);
            }

            string status = row.Cells[5].Value?.ToString() ?? "";
            Color color = status == "Hoạt động" ? ModernUi.Green : status == "Tạm khóa" ? ModernUi.Red : ModernUi.Blue;
            row.Cells[5].Style.ForeColor = color;
            row.Cells[5].Style.BackColor = status == "Hoạt động"
                ? Color.FromArgb(224, 247, 231)
                : status == "Tạm khóa"
                    ? Color.FromArgb(255, 232, 232)
                    : Color.FromArgb(230, 241, 255);
            row.Cells[6].Style.ForeColor = row.Cells[6].Value?.ToString() == "✓" ? ModernUi.Green : ModernUi.Red;
            row.Cells[6].Style.Font = ModernUi.Font(10f, FontStyle.Bold);
        }
    }

    private static DataGridView CreatePermissionMatrixGrid()
    {
        var roles = RolePermissionDAL.GetAllRoles();
        var permissions = RolePermissionDAL.GetAllPermissions();
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

        foreach (var role in roles.Take(7))
        {
            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                HeaderText = Display(role.RoleName),
                FillWeight = (role.RoleName ?? "").Length > 14 ? 1.35f : 1f,
                FlatStyle = FlatStyle.Standard
            });
        }

        if (roles.Count == 0 || permissions.Count == 0)
        {
            grid.Rows.Add("Không có dữ liệu phân quyền");
            return grid;
        }

        foreach (var permission in permissions.Take(12))
        {
            object[] row = new object[roles.Take(7).Count() + 1];
            row[0] = $"  ▣  {Display(permission.Description, Display(permission.PermissionName))}";
            int index = 1;
            foreach (var role in roles.Take(7))
            {
                row[index++] = role.PermissionIDs.Contains(permission.PermissionID);
            }
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

        return grid;
    }

    private static void PopulateAccountLogGrid(DataGridView grid, IReadOnlyList<dynamic> logs)
    {
        grid.Rows.Clear();

        if (logs.Count == 0)
        {
            grid.Rows.Add("Không có dữ liệu", "", "", "", "");
            return;
        }

        foreach (var log in logs)
        {
            grid.Rows.Add(
                DateTimeText(log.Timestamp),
                Display(log.Username, "system"),
                Display(log.Action),
                Display(log.Description, $"{log.Action} {log.EntityName}"),
                Display(log.IPAddress));
        }
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

    private static TextBox AddAccountInput(Control parent, string label, string value, int x, int y, int width)
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
        return input;
    }

    private static ComboBox AddAccountCombo(Control parent, string label, string[] values, int x, int y, int width)
    {
        var lbl = ModernUi.Label(label, 9f, FontStyle.Regular, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(100, 30);
        parent.Controls.Add(lbl);

        var combo = ModernUi.ComboBox(values, width - 112);
        combo.Location = new Point(x + 112, y);
        parent.Controls.Add(combo);
        return combo;
    }

    private void RenderProfilePage()
    {
        var page = BeginPage("Hồ sơ cá nhân", "Dashboard / Tài khoản");
        int width = Math.Min(PageWorkWidth(760), 900);

        var profileCard = ModernUi.Section("Thông tin tài khoản", width, 290);
        profileCard.Location = new Point(18, 88);
        page.Controls.Add(profileCard);

        var avatar = new CircleLabel
        {
            // Dùng ký tự đầu nếu chưa có ảnh đại diện để không phụ thuộc dữ liệu file.
            Text = GetUserInitials(),
            CircleColor = ModernUi.Blue,
            ForeColor = Color.White,
            Font = ModernUi.Font(26f, FontStyle.Bold),
            Location = new Point(28, 66),
            Size = new Size(110, 110)
        };
        profileCard.Controls.Add(avatar);

        AddProfileField(profileCard, "Tên đăng nhập", CurrentUsername(), 180, 58, 300);
        AddProfileField(profileCard, "Họ và tên", CurrentDisplayName(), 180, 96, 300);
        AddProfileField(profileCard, "Email", Display(_session?.Email, "-"), 180, 134, 300);
        AddProfileField(profileCard, "Số điện thoại", Display(_session?.Phone, "-"), 180, 172, 300);
        AddProfileField(profileCard, "Vai trò", RoleDisplay(), 520, 58, 280);
        AddProfileField(profileCard, "Mã người dùng", (_session?.UserID ?? 0).ToString(), 520, 96, 280);
        AddProfileField(profileCard, "Trạng thái", Display(_session?.Status, "Đang hoạt động"), 520, 134, 280);
        AddProfileField(profileCard, "Đăng nhập lúc", _session?.LoginTime.ToString("dd/MM/yyyy HH:mm:ss") ?? "-", 520, 172, 280);

        var changePasswordButton = ModernUi.Button("Đổi mật khẩu", ModernUi.Blue, 150, 36);
        changePasswordButton.Location = new Point(180, 228);
        changePasswordButton.Click += (_, _) => ShowChangePasswordDialog();
        profileCard.Controls.Add(changePasswordButton);

        var settingsButton = ModernUi.OutlineButton("Mở cài đặt", 150, 36);
        settingsButton.Location = new Point(344, 228);
        settingsButton.Click += (_, _) => OpenAccountSettings();
        profileCard.Controls.Add(settingsButton);
    }

    private void RenderNotificationsPage()
    {
        var page = BeginPage("Thông báo", "Dashboard / Thông báo");
        int width = Math.Min(PageWorkWidth(760), 960);

        var card = ModernUi.Section("Danh sách thông báo", width, 380);
        card.Location = new Point(18, 88);
        page.Controls.Add(card);

        var notifications = GetHeaderNotifications();
        if (notifications.Count == 0)
        {
            notifications = CreateSampleNotifications();
        }

        for (int i = 0; i < notifications.Count && i < 8; i++)
        {
            var notification = notifications[i];
            var row = new RoundedPanel
            {
                Radius = 6,
                BorderColor = Color.FromArgb(226, 232, 240),
                BackColor = notification.IsRead ? Color.White : Color.FromArgb(239, 246, 255),
                Location = new Point(18, 48 + i * 40),
                Size = new Size(card.Width - 36, 34)
            };

            var text = ModernUi.Label(BuildNotificationCaption(notification), 9f,
                notification.IsRead ? FontStyle.Regular : FontStyle.Bold,
                ModernUi.Text);
            text.Location = new Point(12, 7);
            text.Size = new Size(row.Width - 24, 20);
            row.Controls.Add(text);

            row.Cursor = Cursors.Hand;
            text.Cursor = Cursors.Hand;
            row.Click += (_, _) => OpenNotificationDetail(notification);
            text.Click += (_, _) => OpenNotificationDetail(notification);
            card.Controls.Add(row);
        }

        var markAllReadButton = ModernUi.Button("Đánh dấu tất cả đã đọc", ModernUi.Blue, 180, 34);
        markAllReadButton.Location = new Point(18, card.Bottom + 12);
        markAllReadButton.Click += (_, _) => MarkAllNotificationsAsRead();
        page.Controls.Add(markAllReadButton);
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
        int labelW = Math.Min(126, Math.Max(104, parent.Width / 3));
        int inputX = 18 + labelW + 12;
        int inputW = Math.Max(120, parent.Width - inputX - 18);

        var lbl = ModernUi.Label(label, 8.8f, FontStyle.Regular, ModernUi.Text);
        lbl.Location = new Point(18, y);
        lbl.Size = new Size(labelW, 26);
        parent.Controls.Add(lbl);

        var input = ModernUi.TextBox("", inputW);
        input.Text = value;
        input.Location = new Point(inputX, y);
        input.Height = 28;
        parent.Controls.Add(input);
    }

    private static void AddSmallField(Control parent, string label, string value, int x, int y)
    {
        var lbl = ModernUi.Label(label, 8.8f, FontStyle.Regular, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(95, 24);
        parent.Controls.Add(lbl);
        int inputX = x + 108;
        int inputW = Math.Max(120, parent.Width - inputX - 18);
        var input = ModernUi.TextBox("", inputW);
        input.Text = value;
        input.Location = new Point(inputX, y);
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

    private static void AddResponsiveFormActions(Control parent, int y)
    {
        const int x = 18;
        const int gap = 8;
        int buttonW = Math.Max(82, (parent.Width - x * 2 - gap * 2) / 3);
        string addText = buttonW < 104 ? "⊕ Thêm" : "⊕  Thêm";
        string editText = buttonW < 104 ? "✎ Sửa" : "✎  Sửa";
        string deleteText = buttonW < 104 ? "× Xóa" : "×  Xóa";

        var add = ModernUi.Button(addText, ModernUi.Green, buttonW, 32);
        add.Location = new Point(x, y);
        parent.Controls.Add(add);

        var edit = ModernUi.Button(editText, ModernUi.Orange, buttonW, 32);
        edit.Location = new Point(x + buttonW + gap, y);
        parent.Controls.Add(edit);

        var delete = ModernUi.Button(deleteText, ModernUi.Red, buttonW, 32);
        delete.Location = new Point(x + (buttonW + gap) * 2, y);
        parent.Controls.Add(delete);
    }

    private static void BindTileClick(Control tile, EventHandler handler)
    {
        tile.Click += handler;
        foreach (Control child in tile.Controls)
        {
            child.Click += handler;
        }
    }

    private static bool HeaderContains(string header, params string[] keywords)
        => keywords.Any(keyword => header.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    private static bool ShouldLeftAlignColumn(string header)
        => HeaderContains(header, "Nội dung", "Hạng mục", "Mô tả", "Ghi chú", "Địa chỉ", "Họ tên", "Người gửi", "Người phụ trách", "Tiêu đề");

    private static float GridFillWeight(string header)
    {
        if (HeaderContains(header, "STT", "No."))
        {
            return 42f;
        }

        if (HeaderContains(header, "ID", "Mã"))
        {
            return 78f;
        }

        if (HeaderContains(header, "Ngày", "Thời gian", "Kỳ"))
        {
            return 96f;
        }

        if (HeaderContains(header, "Nội dung", "Mô tả", "Ghi chú", "Địa chỉ"))
        {
            return 180f;
        }

        if (HeaderContains(header, "Hạng mục", "Tiêu đề", "Loại"))
        {
            return 126f;
        }

        if (HeaderContains(header, "Cư dân", "Người", "Chủ hộ", "Tài khoản", "Email", "Điện thoại"))
        {
            return 116f;
        }

        if (HeaderContains(header, "Căn hộ", "Tòa nhà", "Block", "Phương tiện"))
        {
            return 92f;
        }

        if (HeaderContains(header, "Trạng thái", "Ưu tiên", "Mức độ"))
        {
            return 86f;
        }

        if (HeaderContains(header, "VNĐ", "Tiền", "Doanh thu", "Phí"))
        {
            return 108f;
        }

        return 100f;
    }

    private static int GridMinimumWidth(string header)
    {
        if (HeaderContains(header, "STT", "No."))
        {
            return 52;
        }

        if (HeaderContains(header, "ID", "Mã"))
        {
            return 84;
        }

        if (HeaderContains(header, "Ngày", "Thời gian", "Kỳ"))
        {
            return 118;
        }

        if (HeaderContains(header, "Nội dung", "Mô tả", "Ghi chú", "Địa chỉ"))
        {
            return 180;
        }

        if (HeaderContains(header, "Cư dân", "Người", "Email", "Điện thoại"))
        {
            return 116;
        }

        if (HeaderContains(header, "Trạng thái", "Ưu tiên", "Mức độ"))
        {
            return 96;
        }

        return 90;
    }

    private static void ConfigureGridColumns(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.FillWeight = GridFillWeight(column.HeaderText);
            column.MinimumWidth = GridMinimumWidth(column.HeaderText);
            column.DefaultCellStyle.Alignment = ShouldLeftAlignColumn(column.HeaderText)
                ? DataGridViewContentAlignment.MiddleLeft
                : DataGridViewContentAlignment.MiddleCenter;
        }

        grid.ClearSelection();
    }

    private static RoundedPanel AddActionTile(Control parent, string icon, string title, string subtitle, Color color, int x, int y, Color? textColor = null, int width = 178, int height = 72)
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
        return tile;
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
            ConfigureGridColumns(grid);
        };
        grid.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                e.ToolTipText = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
            }
        };
        grid.CellFormatting += (_, e) => ApplyGridCellStyle(e);
        return grid;
    }

    private static void SetGridData(DataGridView grid, string[] columns, object[][] rows)
    {
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
            "Từ chối" or "Tạm khóa" or "Hết hạn" or "Hỏng" or "Lỗi" => (ModernUi.Red, Color.FromArgb(255, 235, 235)),
            "Trung bình" or "Chưa thanh toán" or "Cần bảo trì" or "Sắp đến hạn" or "Cảnh báo" => (ModernUi.Orange, Color.FromArgb(255, 244, 224)),
            "Thấp" or "Đã thanh toán" or "Đã lên lịch" or "Đang sử dụng" or "Đang thuê" or "Hoạt động" or "Đã duyệt" or "Đã vào" or "Đã rời" or "Tốt" or "Thành công" or "Đã sao lưu" => (ModernUi.Green, Color.FromArgb(232, 248, 235)),
            "Đang xử lý" or "Đang cư trú" or "Chờ duyệt" or "Chờ xử lý" or "Thông tin" => (ModernUi.Blue, Color.FromArgb(232, 241, 255)),
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

    private static void RenderResidentLinkedDetails(Control parent, ResidentDTO? resident, ApartmentDTO? apartment, IReadOnlyList<ResidentDTO> apartmentResidents)
    {
        parent.SuspendLayout();
        parent.Controls.Clear();

        if (resident == null)
        {
            var empty = ModernUi.Label("Không có cư dân phù hợp với bộ lọc hiện tại.", 10f, FontStyle.Regular, ModernUi.Muted);
            empty.Location = new Point(16, 18);
            empty.Size = new Size(parent.Width - 32, 28);
            parent.Controls.Add(empty);
            parent.ResumeLayout();
            return;
        }

        var title = ModernUi.Label(Display(resident.FullName), 11.5f, FontStyle.Bold, ModernUi.Navy);
        title.Location = new Point(16, 12);
        title.Size = new Size(parent.Width - 32, 26);
        parent.Controls.Add(title);

        string apartmentText = apartment == null
            ? $"Căn hộ liên kết: {Display(resident.ApartmentCode)}"
            : $"Căn hộ liên kết: {Display(resident.ApartmentCode)} · {BuildingShort(apartment.BuildingName)} / {BlockShort(apartment.BlockName)} / Tầng {apartment.FloorNumber?.ToString("00") ?? "-"}";
        var subtitle = ModernUi.Label(apartmentText, 8.9f, FontStyle.Regular, ModernUi.Muted);
        subtitle.Location = new Point(16, 40);
        subtitle.Size = new Size(parent.Width - 32, 22);
        parent.Controls.Add(subtitle);

        int infoWidth = (parent.Width - 40) / 2;
        AddResidentInfoBlock(parent, "Mã cư dân", $"CD{resident.ResidentID:0000}", 16, 76, infoWidth);
        AddResidentInfoBlock(parent, "Căn hộ", Display(resident.ApartmentCode), 24 + infoWidth, 76, infoWidth);
        AddResidentInfoBlock(parent, "Số điện thoại", Display(resident.Phone), 16, 118, infoWidth);
        AddResidentInfoBlock(parent, "Tình trạng", ResidentLivingStatus(resident), 24 + infoWidth, 118, infoWidth);
        AddResidentInfoBlock(parent, "CCCD", Display(resident.CCCD), 16, 160, infoWidth);
        AddResidentInfoBlock(parent, "Vai trò", Display(resident.RelationshipWithOwner), 24 + infoWidth, 160, infoWidth);
        AddResidentInfoBlock(parent, "Ngày vào ở", DateText(resident.StartDate ?? resident.MoveInDate), 16, 202, infoWidth);
        AddResidentInfoBlock(parent, "Số người cùng căn", apartmentResidents.Count.ToString("N0"), 24 + infoWidth, 202, infoWidth);

        var apartmentCard = ModernUi.CardPanel(6);
        apartmentCard.Location = new Point(16, 248);
        apartmentCard.Size = new Size(parent.Width - 32, 98);
        parent.Controls.Add(apartmentCard);

        var apartmentTitle = ModernUi.Label("THÔNG TIN CĂN HỘ", 8.7f, FontStyle.Bold, ModernUi.Blue);
        apartmentTitle.Location = new Point(12, 10);
        apartmentTitle.Size = new Size(190, 20);
        apartmentCard.Controls.Add(apartmentTitle);

        string apartmentLine1 = apartment == null
            ? "Chưa tìm thấy thông tin căn hộ."
            : $"{BuildingShort(apartment.BuildingName)} / {BlockShort(apartment.BlockName)} / Tầng {apartment.FloorNumber?.ToString("00") ?? "-"}";
        var apartmentInfo1 = ModernUi.Label(apartmentLine1, 9.1f, FontStyle.Bold, ModernUi.Text);
        apartmentInfo1.Location = new Point(12, 34);
        apartmentInfo1.Size = new Size(apartmentCard.Width - 24, 22);
        apartmentCard.Controls.Add(apartmentInfo1);

        string apartmentLine2 = apartment == null
            ? "-"
            : $"Loại căn: {ApartmentTypeText(apartment.ApartmentType)} · Diện tích: {apartment.Area.ToString("N2", CultureInfo.InvariantCulture)} m²";
        var apartmentInfo2 = ModernUi.Label(apartmentLine2, 8.8f, FontStyle.Regular, ModernUi.Text);
        apartmentInfo2.Location = new Point(12, 56);
        apartmentInfo2.Size = new Size(apartmentCard.Width - 24, 18);
        apartmentCard.Controls.Add(apartmentInfo2);

        string apartmentLine3 = apartment == null
            ? "-"
            : $"Trạng thái căn: {ViStatus(apartment.Status)} · Sức chứa: {apartmentResidents.Count}/{Math.Max(1, apartment.MaxResidents)} người";
        var apartmentInfo3 = ModernUi.Label(apartmentLine3, 8.8f, FontStyle.Regular, ModernUi.Text);
        apartmentInfo3.Location = new Point(12, 74);
        apartmentInfo3.Size = new Size(apartmentCard.Width - 24, 18);
        apartmentCard.Controls.Add(apartmentInfo3);

        var roommatesTitle = ModernUi.Label($"CƯ DÂN CÙNG CĂN HỘ ({apartmentResidents.Count:N0})", 9f, FontStyle.Bold, ModernUi.Blue);
        roommatesTitle.Location = new Point(16, 358);
        roommatesTitle.Size = new Size(parent.Width - 32, 22);
        parent.Controls.Add(roommatesTitle);

        var roommatesGrid = CreateGrid(
            new[] { "Mã cư dân", "Họ tên", "Vai trò", "Tình trạng", "Ngày vào ở" },
            RowsOrEmpty(apartmentResidents, 5, (mate, _) => new object[]
            {
                $"CD{mate.ResidentID:0000}",
                Display(mate.FullName),
                Display(mate.RelationshipWithOwner),
                ResidentLivingStatus(mate),
                DateText(mate.StartDate ?? mate.MoveInDate)
            }, "Không có cư dân cùng căn"));
        roommatesGrid.Location = new Point(16, 384);
        roommatesGrid.Size = new Size(parent.Width - 32, 92);
        roommatesGrid.ColumnHeadersHeight = 34;
        roommatesGrid.RowTemplate.Height = 34;
        int[] roommateWeights = { 84, 130, 100, 100, 88 };
        for (int i = 0; i < roommatesGrid.Columns.Count && i < roommateWeights.Length; i++)
        {
            roommatesGrid.Columns[i].FillWeight = roommateWeights[i];
        }
        parent.Controls.Add(roommatesGrid);

        parent.ResumeLayout();
    }

    private static void AddResidentInfoBlock(Control parent, string label, string value, int x, int y, int width)
    {
        var caption = ModernUi.Label(label.ToUpperInvariant(), 8.1f, FontStyle.Bold, ModernUi.Muted);
        caption.Location = new Point(x, y);
        caption.Size = new Size(width, 16);
        parent.Controls.Add(caption);

        var text = ModernUi.Label(value, 9.4f, FontStyle.Bold, ModernUi.Text);
        text.Location = new Point(x, y + 16);
        text.Size = new Size(width, 22);
        parent.Controls.Add(text);
    }

    private static void AddResidentProfile(Control parent, ResidentDTO? resident)
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
        AddSmallField(parent, "Mã cư dân:", resident == null ? "" : $"CD{resident.ResidentID:0000}", fx, 44);
        AddSmallField(parent, "Họ và tên:", Display(resident?.FullName, ""), fx, 82);
        AddSmallField(parent, "Ngày sinh:", DateText(resident?.DOB), fx, 120);
        AddSmallField(parent, "CCCD:", Display(resident?.CCCD, ""), fx, 158);
        AddSmallField(parent, "Email:", Display(resident?.Email, ""), fx, 196);
        AddSmallField(parent, "Số điện thoại:", Display(resident?.Phone, ""), fx, 234);
        AddSmallField(parent, "Địa chỉ thường trú:", Display(resident?.AddressRegistration, ""), fx, 272);
        AddSmallField(parent, "Căn hộ liên kết:", Display(resident?.ApartmentCode, ""), fx, 310);
        AddSmallField(parent, "Vai trò trong căn hộ:", Display(resident?.RelationshipWithOwner, ""), fx, 348);
        AddSmallField(parent, "Tình trạng cư trú:", resident == null ? "" : ViStatus(resident.Status), fx, 386);

        var historyTitle = ModernUi.Label("Lịch sử cư trú", 9.5f, FontStyle.Bold, ModernUi.Blue);
        historyTitle.Location = new Point(24, 430);
        historyTitle.Size = new Size(180, 24);
        parent.Controls.Add(historyTitle);
        var history = CreateGrid(
            new[] { "STT", "Căn hộ", "Vai trò", "Tình trạng", "Ngày bắt đầu", "Ngày kết thúc", "Ghi chú" },
            resident == null
                ? new[] { EmptyRow(7, "Không có dữ liệu") }
                : new object[][]
                {
                    new object[]
                    {
                        1,
                        Display(resident.ApartmentCode),
                        Display(resident.RelationshipWithOwner),
                        ViStatus(resident.Status),
                        DateText(resident.StartDate ?? resident.MoveInDate),
                        DateText(resident.EndDate ?? resident.MoveOutDate),
                        Display(resident.Note, "")
                    }
                });
        history.Location = new Point(18, 458);
        history.Size = new Size(parent.Width - 36, 66);
        parent.Controls.Add(history);
    }

    private static void AddInvoiceDetail(Control parent, InvoiceDTO? invoice, ResidentDTO? resident)
    {
        var infoTitle = ModernUi.Label("Thông tin căn hộ & chủ hộ", 10f, FontStyle.Bold, ModernUi.Blue);
        infoTitle.Location = new Point(18, 42);
        infoTitle.Size = new Size(260, 24);
        parent.Controls.Add(infoTitle);

        string[] info = invoice == null
            ? new[] { "Không có dữ liệu hóa đơn", "", "", "" }
            : new[]
            {
                $"Mã hóa đơn:        {InvoiceCode(invoice)}                 Ngày lập:        {DateTimeText(invoice.CreatedAt)}",
                $"Căn hộ:            {Display(invoice.ApartmentCode)}              Kỳ phí:          {invoice.Month:00}/{invoice.Year}",
                $"Chủ hộ:            {Display(resident?.FullName)}                 SĐT:             {Display(resident?.Phone)}",
                $"Email:             {Display(resident?.Email)}           Trạng thái:      {ViStatus(invoice.PaymentStatus)}"
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
            invoice == null
                ? new[] { EmptyRow(5, "Không có dữ liệu") }
                : new object[][]
                {
                    new object[] { 1, "Tổng phí kỳ này", Money(invoice.TotalAmount), "1 kỳ", Money(invoice.TotalAmount) },
                    new object[] { 2, "Đã thanh toán", Money(invoice.PaidAmount), "-", Money(invoice.PaidAmount) },
                    new object[] { 3, "Còn phải thu", Money(Math.Max(0, invoice.TotalAmount - invoice.PaidAmount)), "-", Money(Math.Max(0, invoice.TotalAmount - invoice.PaidAmount)) }
                });
        grid.Location = new Point(18, 226);
        grid.Size = new Size(parent.Width - 36, 144);
        parent.Controls.Add(grid);
        var total = ModernUi.Label($"TỔNG CỘNG                                                   {Money(invoice?.TotalAmount ?? 0)} VNĐ", 11f, FontStyle.Bold, ModernUi.Red);
        total.Location = new Point(18, 382);
        total.Size = new Size(parent.Width - 36, 28);
        parent.Controls.Add(total);
    }

    private static void AddComplaintDetail(Control parent, dynamic? complaint)
    {
        var sender = ModernUi.Label(complaint == null
                ? "Người gửi\r\nKhông có dữ liệu"
                : $"Người gửi\r\n{Display(complaint.ResidentName)} (Cư dân)\r\nCăn hộ {Display(complaint.ApartmentCode)}\r\nMã phản ánh PA{complaint.CreatedAt:yyMMdd}-{complaint.ComplaintID:000}",
            9f, FontStyle.Regular, ModernUi.Text);
        sender.Location = new Point(18, 48);
        sender.Size = new Size(260, 90);
        parent.Controls.Add(sender);

        var owner = ModernUi.Label(complaint == null
                ? "Người phụ trách\r\n-"
                : $"Người phụ trách\r\n{Display(complaint.AssignedTo)}\r\nLoại: {Display(complaint.Category)}\r\nƯu tiên: {ViStatus(complaint.Priority)}",
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
            Text = complaint == null ? "" : Display(complaint.Description, Display(complaint.Title, "")),
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
        string currentStatus = complaint == null ? "Không có dữ liệu" : ViStatus((string?)complaint.Status);
        var status = ModernUi.ComboBox(new[] { currentStatus, "Mới", "Đang xử lý", "Đã xử lý", "Đã đóng" }, parent.Width / 2 - 36);
        status.Location = new Point(parent.Width / 2 + 16, 164);
        parent.Controls.Add(status);

        var response = new TextBox
        {
            Text = complaint == null ? "" : $"Cập nhật gần nhất: {DateTimeText(complaint.UpdatedAt)}",
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
        var rating = ModernUi.Label(complaint == null ? "Chưa có đánh giá" : $"Trạng thái: {ViStatus(complaint.Status)}", 13f, FontStyle.Bold, ModernUi.Orange);
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

    private static void AddProfileField(Control parent, string label, string value, int x, int y, int width)
    {
        var caption = ModernUi.Label(label.ToUpperInvariant(), 8.1f, FontStyle.Bold, ModernUi.Muted);
        caption.Location = new Point(x, y);
        caption.Size = new Size(width, 16);
        parent.Controls.Add(caption);

        var text = ModernUi.Label(value, 9.4f, FontStyle.Bold, ModernUi.Text);
        text.Location = new Point(x, y + 16);
        text.Size = new Size(width, 22);
        parent.Controls.Add(text);
    }

    private string GetUserInitials()
    {
        string source = CurrentDisplayName();
        if (string.IsNullOrWhiteSpace(source))
        {
            return "U";
        }

        var letters = source
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]))
            .ToArray();

        return letters.Length == 0 ? "U" : new string(letters);
    }

    private List<NotificationDTO> GetHeaderNotifications()
    {
        try
        {
            List<NotificationDTO> notifications;
            if (_session?.UserID > 0)
            {
                notifications = NotificationDAL.GetUserNotifications(_session.UserID);
                if (notifications.Count > 0)
                {
                    return notifications;
                }
            }

            notifications = NotificationDAL.GetAllNotifications();
            if (notifications.Count > 0)
            {
                return notifications;
            }
        }
        catch
        {
            // Fallback về dữ liệu mẫu nếu database chưa sẵn sàng.
        }

        return CreateSampleNotifications();
    }

    private List<NotificationDTO> CreateSampleNotifications()
    {
        return new List<NotificationDTO>
        {
            new NotificationDTO
            {
                NotificationID = 0,
                Title = "Hệ thống",
                Message = "Đã tải dashboard thành công.",
                CreatedAt = DateTime.Now.AddMinutes(-10),
                IsRead = false
            },
            new NotificationDTO
            {
                NotificationID = 0,
                Title = "Nhắc việc",
                Message = "Có hóa đơn cần kiểm tra trạng thái thanh toán.",
                CreatedAt = DateTime.Now.AddHours(-2),
                IsRead = false
            },
            new NotificationDTO
            {
                NotificationID = 0,
                Title = "Thông báo chung",
                Message = "Chức năng thông báo đang dùng dữ liệu mẫu.",
                CreatedAt = DateTime.Now.AddDays(-1),
                IsRead = true
            }
        };
    }

    private string BuildNotificationCaption(NotificationDTO notification)
    {
        string title = Display(notification.Title, Display(notification.Subject, "Thông báo"));
        string body = Display(notification.Message, Display(notification.Body, ""));
        string time = notification.CreatedAt == DateTime.MinValue ? "Mới" : notification.CreatedAt.ToString("dd/MM HH:mm");
        string prefix = notification.IsRead ? "" : "[Mới] ";
        return $"{prefix}{title} - {body} ({time})";
    }

    private string BuildNotificationTooltip(NotificationDTO notification)
    {
        return $"{Display(notification.Title, "Thông báo")}\n{Display(notification.Message, Display(notification.Body, ""))}";
    }

    private void OpenNotificationDetail(NotificationDTO notification)
    {
        if (notification.NotificationID > 0 && !notification.IsRead)
        {
            NotificationDAL.MarkAsRead(notification.NotificationID);
        }

        MessageBox.Show(this,
            BuildNotificationTooltip(notification),
            Display(notification.Title, "Thông báo"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        ReloadCurrentPage();
    }

    private void MarkAllNotificationsAsRead()
    {
        if (_session?.UserID > 0)
        {
            NotificationDAL.MarkAllAsRead(_session.UserID);
        }

        ReloadCurrentPage();
    }

    private void OpenAccountSettings()
    {
        if (!IsResident)
        {
            Navigate("settings");
            return;
        }

        MessageBox.Show(this,
            "Tài khoản cư dân hiện chưa có màn hình cài đặt riêng.",
            "Cài đặt",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ConfirmExitApplication()
    {
        if (MessageBox.Show(this,
                "Bạn có chắc muốn thoát chương trình?",
                "Thoát chương trình",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
        {
            Close();
        }
    }

    private void PerformLogout()
    {
        if (MessageBox.Show(this,
                "Bạn có chắc muốn đăng xuất khỏi hệ thống?",
                "Đăng xuất",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            AuthenticationBLL.Logout(); // Clear session trước khi quay về đăng nhập.
            Hide();

            using var loginForm = new FrmLogin();
            if (loginForm.ShowDialog() == DialogResult.OK && SessionManager.GetSession() != null)
            {
                _session = SessionManager.GetSession();
                BuildShell();
                Navigate(GetDefaultPage());
                Show();
                Activate();
                return;
            }

            Close();
        }
        catch (Exception ex)
        {
            Show();
            MessageBox.Show(this,
                $"Đăng xuất không thành công.\nChi tiết: {ex.Message}",
                "Đăng xuất",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private bool ShowChangePasswordDialog()
    {
        if (_session?.UserID <= 0)
        {
            MessageBox.Show(this,
                "Không tìm thấy thông tin người dùng hiện tại.",
                "Đổi mật khẩu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        using var dialog = new Form
        {
            Text = "Đổi mật khẩu",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(420, 250)
        };

        var lblCurrent = ModernUi.Label("Mật khẩu hiện tại", 9f, FontStyle.Bold, ModernUi.Text);
        lblCurrent.Location = new Point(24, 24);
        lblCurrent.Size = new Size(160, 24);
        dialog.Controls.Add(lblCurrent);

        var txtCurrent = new TextBox
        {
            Location = new Point(24, 48),
            Size = new Size(372, 27),
            UseSystemPasswordChar = true
        };
        dialog.Controls.Add(txtCurrent);

        var lblNew = ModernUi.Label("Mật khẩu mới", 9f, FontStyle.Bold, ModernUi.Text);
        lblNew.Location = new Point(24, 84);
        lblNew.Size = new Size(160, 24);
        dialog.Controls.Add(lblNew);

        var txtNew = new TextBox
        {
            Location = new Point(24, 108),
            Size = new Size(372, 27),
            UseSystemPasswordChar = true
        };
        dialog.Controls.Add(txtNew);

        var lblConfirm = ModernUi.Label("Xác nhận mật khẩu mới", 9f, FontStyle.Bold, ModernUi.Text);
        lblConfirm.Location = new Point(24, 144);
        lblConfirm.Size = new Size(180, 24);
        dialog.Controls.Add(lblConfirm);

        var txtConfirm = new TextBox
        {
            Location = new Point(24, 168),
            Size = new Size(372, 27),
            UseSystemPasswordChar = true
        };
        dialog.Controls.Add(txtConfirm);

        var btnSave = ModernUi.Button("Lưu", ModernUi.Blue, 100, 34);
        btnSave.Location = new Point(192, 206);
        dialog.Controls.Add(btnSave);

        var btnCancel = ModernUi.OutlineButton("Hủy", 100, 34);
        btnCancel.Location = new Point(296, 206);
        btnCancel.Click += (_, _) => dialog.Close();
        dialog.Controls.Add(btnCancel);

        bool isSuccess = false;
        btnSave.Click += (_, _) =>
        {
            try
            {
                var result = AuthenticationBLL.ChangePassword(_session.UserID, txtCurrent.Text, txtNew.Text, txtConfirm.Text);
                if (!result.success)
                {
                    MessageBox.Show(dialog, result.message, "Đổi mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show(dialog, result.message, "Đổi mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                isSuccess = true;
                dialog.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(dialog,
                    $"Không thể đổi mật khẩu.\nChi tiết: {ex.Message}",
                    "Đổi mật khẩu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };

        dialog.ShowDialog(this);
        return isSuccess;
    }

    private string GetDefaultPage() => "dashboard";
    private bool IsResident => RoleName().Contains("resident", StringComparison.OrdinalIgnoreCase) || RoleName().Contains("cư dân", StringComparison.OrdinalIgnoreCase) || CurrentUsername().StartsWith("resident", StringComparison.OrdinalIgnoreCase);
    private bool IsManager => !IsResident && (RoleName().Contains("manager", StringComparison.OrdinalIgnoreCase) || RoleName().Contains("quản lý", StringComparison.OrdinalIgnoreCase) || CurrentUsername().StartsWith("manager", StringComparison.OrdinalIgnoreCase));
    private string RoleName() => _session?.RoleName ?? "Super Admin";
    private string CurrentUsername() => _session?.Username ?? "superadmin";
    private string CurrentDisplayName() => _session?.FullName ?? (IsResident ? "Nguyễn Văn An" : CurrentUsername());
    private string FooterDisplayName() => IsManager ? CurrentUsername() : CurrentDisplayName();
    private string RoleDisplay() => IsResident ? "Cư dân" : IsManager ? "Quản lý khu chung cư" : "Super Admin";
    private string RoleFooterLabel() => IsResident ? "Cư dân" : IsManager ? "Người dùng" : "Tên người dùng";
    private int NotificationCount()
    {
        try
        {
            if (_session?.UserID > 0)
            {
                int unread = NotificationDAL.GetUnreadNotificationCount(_session.UserID);
                if (unread > 0)
                {
                    return unread;
                }

                int total = NotificationDAL.GetUserNotifications(_session.UserID).Count;
                if (total > 0)
                {
                    return total;
                }
            }

            int fallbackTotal = NotificationDAL.GetAllNotifications().Count;
            if (fallbackTotal > 0)
            {
                return fallbackTotal;
            }
        }
        catch
        {
            // Giữ fallback an toàn nếu DB chưa sẵn sàng.
        }

        return CreateSampleNotifications().Count;
    }

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

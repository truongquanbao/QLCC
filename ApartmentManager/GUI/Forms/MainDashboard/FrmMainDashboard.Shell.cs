using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmMainDashboard
{
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
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, FooterHeight));
        Controls.Add(shell);

        var topBar = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 0,
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

        _sidebar = new GradientPanel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernUi.SidebarTop
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

    private static PaginationControls AddPaginationControls(Control parent, int summaryX, int summaryY, int buttonsX, int buttonsY, int pageSizeX, int pageSizeY, int summaryWidth)
    {
        var summaryLabel = ModernUi.Label("", 9f, FontStyle.Regular, ModernUi.Text);
        summaryLabel.Location = new Point(summaryX, summaryY);
        summaryLabel.Size = new Size(summaryWidth, 26);
        parent.Controls.Add(summaryLabel);

        var firstButton = ModernUi.OutlineButton("«", 32, 30);
        firstButton.Location = new Point(buttonsX, buttonsY);
        parent.Controls.Add(firstButton);

        var previousButton = ModernUi.OutlineButton("‹", 32, 30);
        previousButton.Location = new Point(buttonsX + 38, buttonsY);
        parent.Controls.Add(previousButton);

        var pageButton = ModernUi.Button("1", ModernUi.Blue, 32, 30);
        pageButton.Location = new Point(buttonsX + 76, buttonsY);
        pageButton.Enabled = false; // Nút giữa chỉ hiển thị trang hiện tại.
        parent.Controls.Add(pageButton);

        var nextButton = ModernUi.OutlineButton("›", 32, 30);
        nextButton.Location = new Point(buttonsX + 114, buttonsY);
        parent.Controls.Add(nextButton);

        var lastButton = ModernUi.OutlineButton("»", 32, 30);
        lastButton.Location = new Point(buttonsX + 152, buttonsY);
        parent.Controls.Add(lastButton);

        var pageSizeCombo = ModernUi.ComboBox(new[] { "10", "20", "50" }, 66);
        pageSizeCombo.Location = new Point(pageSizeX, pageSizeY);
        pageSizeCombo.SelectedItem = "10";
        parent.Controls.Add(pageSizeCombo);

        return new PaginationControls
        {
            SummaryLabel = summaryLabel,
            FirstButton = firstButton,
            PreviousButton = previousButton,
            PageButton = pageButton,
            NextButton = nextButton,
            LastButton = lastButton,
            PageSizeCombo = pageSizeCombo
        };
    }

    private static IReadOnlyList<T> Paginate<T>(IReadOnlyList<T> source, PaginationState state)
    {
        state.TotalRecords = source.Count;
        state.ClampCurrentPage();
        return source
            .Skip(state.StartIndex)
            .Take(state.PageSize)
            .ToList();
    }

    private static int FindPageForIndex(int index, int pageSize)
    {
        if (index < 0 || pageSize <= 0)
        {
            return 1;
        }

        return (index / pageSize) + 1;
    }

    private static int ParsePageSize(object? selectedItem, int fallback = 10)
    {
        return int.TryParse(selectedItem?.ToString(), out int pageSize) && pageSize > 0
            ? pageSize
            : fallback;
    }

    private static string BuildPaginationSummary(PaginationState state, string itemLabel)
    {
        if (state.TotalRecords == 0)
        {
            return $"Hiển thị 0 - 0 / 0 {itemLabel}";
        }

        int start = state.StartIndex + 1;
        int end = Math.Min(state.StartIndex + state.PageSize, state.TotalRecords);
        return $"Hiển thị {start} - {end} / {state.TotalRecords:N0} {itemLabel}";
    }

    private static void UpdatePaginationControls(PaginationState state, PaginationControls controls, string itemLabel)
    {
        controls.SummaryLabel.Text = BuildPaginationSummary(state, itemLabel);
        controls.PageButton.Text = state.TotalRecords == 0 ? "0" : state.CurrentPage.ToString(CultureInfo.InvariantCulture);
        controls.FirstButton.Enabled = state.CurrentPage > 1;
        controls.PreviousButton.Enabled = state.CurrentPage > 1;
        controls.LastButton.Enabled = state.CurrentPage < state.TotalPages && state.TotalRecords > 0;
        controls.NextButton.Enabled = state.CurrentPage < state.TotalPages && state.TotalRecords > 0;
    }

    private void BuildFooter()
    {
        _footer.Controls.Clear();
        _footer.Padding = new Padding(18, 8, 18, 8);

        var user = ModernUi.Label($"●  {RoleFooterLabel()}: {FooterDisplayName()}", 10f, FontStyle.Regular, Color.White);
        user.Size = new Size(300, 28);
        user.AutoEllipsis = true;
        _footer.Controls.Add(user);

        var role = ModernUi.Label($"◆  Vai trò: {RoleDisplay()}", 10f, FontStyle.Regular, Color.White);
        role.Size = new Size(300, 28);
        role.AutoEllipsis = true;
        _footer.Controls.Add(role);

        _clockLabel = ModernUi.Label("", 10f, FontStyle.Regular, Color.White);
        _clockLabel.Size = new Size(420, 28);
        _clockLabel.AutoEllipsis = true;
        _footer.Controls.Add(_clockLabel);

        var db = ModernUi.Label("▰  Trạng thái kết nối:  Đã kết nối SQL Server", 10f, FontStyle.Regular, Color.White);
        db.Size = new Size(390, 28);
        db.AutoEllipsis = true;
        _footer.Controls.Add(db);

        void LayoutFooter()
        {
            int top = Math.Max(10, (_footer.ClientSize.Height - 28) / 2);
            int left = 24;
            int gap = 18;
            int available = Math.Max(320, _footer.ClientSize.Width - left * 2 - gap * 3);

            int userWidth = Math.Max(170, (int)(available * 0.22f));
            int roleWidth = Math.Max(150, (int)(available * 0.18f));
            int clockWidth = Math.Max(230, (int)(available * 0.28f));
            int dbWidth = Math.Max(180, available - userWidth - roleWidth - clockWidth);

            user.Location = new Point(left, top);
            user.Size = new Size(userWidth, 28);

            role.Location = new Point(user.Right + gap, top);
            role.Size = new Size(roleWidth, 28);

            _clockLabel.Location = new Point(role.Right + gap, top);
            _clockLabel.Size = new Size(clockWidth, 28);

            db.Location = new Point(_clockLabel.Right + gap, top);
            db.Size = new Size(dbWidth, 28);
        }

        _footer.Resize += (_, _) => LayoutFooter();
        LayoutFooter();
    }

    private void BuildSidebar()
    {
        _sidebar.Controls.Clear();
        _navButtons.Clear();

        var brand = new Panel
        {
            Size = new Size(SidebarWidth, 84),
            BackColor = Color.Transparent,
            Padding = new Padding(14, 14, 12, 10)
        };
        _sidebar.Controls.Add(brand);

        var brandIcon = ModernUi.Label("\u25A6", 24f, FontStyle.Bold, Color.White);
        brandIcon.BackColor = Color.Transparent;
        brandIcon.TextAlign = ContentAlignment.MiddleCenter;
        brandIcon.SetBounds(10, 16, 42, 42);
        brand.Controls.Add(brandIcon);

        var brandTitle = ModernUi.Label("PH\u1EA6N M\u1EC0M QU\u1EA2N L\u00DD\r\nKHU CHUNG C\u01AF", 9.2f, FontStyle.Bold, Color.White);
        brandTitle.BackColor = Color.Transparent;
        brandTitle.TextAlign = ContentAlignment.MiddleLeft;
        brandTitle.SetBounds(58, 17, SidebarWidth - 76, 44);
        brand.Controls.Add(brandTitle);

        var brandLine = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Color.FromArgb(42, 83, 135)
        };
        brand.Controls.Add(brandLine);

        var menu = new FlowLayoutPanel
        {
            Size = new Size(SidebarWidth, Math.Max(0, _sidebar.Height - brand.Height)),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12, 12, 12, 0),
            AutoScroll = true,
            BackColor = Color.Transparent
        };
        _sidebar.Controls.Add(menu);

        static string SidebarGlyph(string key) => key switch
        {
            "dashboard" => "\u25C9",
            "accounts" => "\u25CF",
            "permissions" => "\u25A1",
            "apartments" or "apartment-info" => "\u25A6",
            "residents" or "profile" => "\u25CF",
            "invoices" or "my-invoices" or "payment" => "\u25A4",
            "complaints" or "send-complaint" => "\u25A0",
            "vehicles" => "\u25A3",
            "visitors" => "\u25CF",
            "assets" => "\u25C7",
            "reports" => "\u25A5",
            "logs" => "\u25A4",
            "settings" or "notifications" or "password" => "\u25CE",
            _ => "\u25CF"
        };

        string? currentGroup = null;
        foreach (var item in MenuItemsForRole())
        {
            if (item.Key == "permissions")
            {
                continue;
            }

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
                Text = string.Empty,
                Tag = item.Key,
                Width = SidebarWidth - 24,
                Height = 44,
                Margin = new Padding(0, 0, 0, 4),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = Padding.Empty,
                Font = ModernUi.Font(9.8f, FontStyle.Regular),
                BackColor = _sidebar.BackColor,
                ForeColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                AccessibleName = item.Text
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(15, 23, 42);
            string iconText = SidebarGlyph(item.Key);
            string menuText = item.Text;
            button.Paint += (_, e) =>
            {
                const int iconWidth = 28;
                int iconLeft = 16;
                int textLeft = iconLeft + iconWidth + 10;
                var iconRect = new Rectangle(iconLeft, 0, iconWidth, button.Height);
                var textRect = new Rectangle(textLeft, 0, Math.Max(40, button.Width - textLeft - 12), button.Height);

                TextRenderer.DrawText(
                    e.Graphics,
                    iconText,
                    ModernUi.Font(10.2f, FontStyle.Regular),
                    iconRect,
                    button.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                TextRenderer.DrawText(
                    e.Graphics,
                    menuText,
                    button.Font,
                    textRect,
                    button.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            };
            button.Click += (_, _) => Navigate(item.Key);
            _navButtons[item.Key] = button;
            menu.Controls.Add(button);
        }

        void LayoutSidebar()
        {
            int width = Math.Max(160, _sidebar.ClientSize.Width);
            brand.SetBounds(0, 0, width, 84);
            brandIcon.SetBounds(12, 17, 40, 40);
            brandTitle.SetBounds(60, 16, Math.Max(80, width - 76), 46);
            menu.SetBounds(0, brand.Bottom, width, Math.Max(0, _sidebar.ClientSize.Height - brand.Height));
            int itemWidth = Math.Max(120, width - 24 - SystemInformation.VerticalScrollBarWidth);
            foreach (Control control in menu.Controls)
            {
                control.Width = itemWidth;
            }
        }

        _sidebar.Resize += (_, _) => LayoutSidebar();
        LayoutSidebar();
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

    private void UpdateClock()
    {
        if (_clockLabel != null)
        {
            _clockLabel.Text = $"◷  Ngày giờ hệ thống: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
        }
    }
}

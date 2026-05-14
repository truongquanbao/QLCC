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
    private void Navigate(string page)
    {
        _activePage = page;
        foreach (var pair in _navButtons)
        {
            bool active = pair.Key == page;
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
                RenderCustomerComplaints();
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
        page.HorizontalScroll.Enabled = false;
        page.HorizontalScroll.Visible = false;
        _content.Controls.Add(page);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 74,
            BackColor = Color.White,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        page.Controls.Add(header);

        bool isResidentsPage = title.IndexOf("cư dân", StringComparison.CurrentCultureIgnoreCase) >= 0;

        var headerIcon = new CircleLabel
        {
            Text = "◉",
            CircleColor = ModernUi.Blue,
            ForeColor = Color.White,
            Font = ModernUi.Font(12f, FontStyle.Bold),
            Location = new Point(22, 17),
            Size = new Size(28, 28)
        };
        headerIcon.Text = isResidentsPage ? "👥" : "●";
        headerIcon.Font = isResidentsPage
            ? new Font("Segoe UI Emoji", 11.5f, FontStyle.Regular)
            : ModernUi.Font(11f, FontStyle.Bold);
        headerIcon.Size = new Size(36, 36);
        isResidentsPage =
            title.IndexOf("cư dân", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
            title.IndexOf("resident", StringComparison.CurrentCultureIgnoreCase) >= 0;
        headerIcon.Text = isResidentsPage ? "●" : "◉";
        headerIcon.Font = ModernUi.Font(isResidentsPage ? 12f : 11f, FontStyle.Bold);
        header.Controls.Add(headerIcon);

        var label = ModernUi.Label(title, 15f, FontStyle.Bold, ModernUi.Navy);
        label.AutoEllipsis = true;
        int rightReserved = IsResident ? 24 : 500;
        int maxTitleWidth = Math.Min(560, Math.Max(220, header.Width - 72 - rightReserved - 90));
        int titleWidth = Math.Min(maxTitleWidth, Math.Max(130, TextRenderer.MeasureText(title, label.Font).Width + 10));
        label.Location = new Point(72, 14);
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

        void LayoutHeaderBase(int rightEdge)
        {
            headerIcon.SetBounds(22, 19, 36, 36);

            int titleLeft = 72;
            int titleMaxWidth = Math.Max(150, rightEdge - titleLeft - 24);
            int desiredTitleWidth = Math.Max(130, TextRenderer.MeasureText(title, label.Font).Width + 10);
            label.SetBounds(titleLeft, 20, Math.Min(titleMaxWidth, desiredTitleWidth), 30);

            divider.Visible = !string.IsNullOrWhiteSpace(breadcrumb);
            crumb.Visible = divider.Visible;
            divider.Location = new Point(label.Right + 16, 19);
            crumb.Location = new Point(divider.Right + 16, 22);
            crumb.Size = new Size(Math.Max(0, rightEdge - crumb.Left), 22);
        }

        if (!IsResident)
        {
            bool hideTopSearch =
                 title.IndexOf("hóa đơn", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 title.IndexOf("hoá đơn", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 title.IndexOf("phí dịch vụ", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 title.IndexOf("phản ánh", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 title.IndexOf("thông báo", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 title.IndexOf("hợp đồng", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 breadcrumb.IndexOf("hóa đơn", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 breadcrumb.IndexOf("hoá đơn", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 breadcrumb.IndexOf("phí dịch vụ", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 breadcrumb.IndexOf("phản ánh", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 breadcrumb.IndexOf("thông báo", StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                 breadcrumb.IndexOf("hợp đồng", StringComparison.CurrentCultureIgnoreCase) >= 0;

            Panel search = null;

            if (!hideTopSearch)
            {
                search = ModernUi.SearchBox("Tìm kiếm nhanh...", 270, 42);
                search.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                search.Location = new Point(header.Width - 455, 12);
                header.Controls.Add(search);

                var searchInput = search.Controls.OfType<TextBox>().FirstOrDefault();
                if (searchInput != null)
                {
                    searchInput.PlaceholderText = "Tìm kiếm nhanh...";
                }
            }

            var bell = ModernUi.IconButton("!", 36);
            bell.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bell.Location = new Point(header.Width - 176, 18);
            bell.Cursor = Cursors.Hand;
            // Use a safe, non-emoji font for the symbol and center it
            bell.Font = ModernUi.Font(12f, FontStyle.Bold);
            bell.TextAlign = ContentAlignment.MiddleCenter;
            bell.Padding = Padding.Empty;
            header.Controls.Add(bell);

            var badge = new CircleLabel
            {
                Text = NotificationCount().ToString(),
                CircleColor = ModernUi.Red,
                ForeColor = Color.White,
                Font = ModernUi.Font(7.2f, FontStyle.Bold),
                Size = new Size(16, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            header.Controls.Add(badge);

            var avatar = new CircleLabel
            {
                Text = "SA",
                CircleColor = Color.FromArgb(226, 236, 248),
                ForeColor = ModernUi.Navy,
                Font = ModernUi.Font(9f, FontStyle.Bold),
                Size = new Size(34, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            header.Controls.Add(avatar);

            var userName = ModernUi.Label($"{CurrentUsername()} ▾", 9f, FontStyle.Bold, ModernUi.Text);
            userName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            userName.Size = new Size(92, 26);
            userName.AutoEllipsis = true;
            userName.Cursor = Cursors.Hand;
            userName.TextAlign = ContentAlignment.MiddleLeft;
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

            void LayoutInteractiveHeader()
            {
                int rightEdge = Math.Max(0, header.ClientSize.Width - 18);

                // Username: fixed width, vertically centered at y = 23
                userName.SetBounds(Math.Max(0, rightEdge - 112), 23, 112, 24);
                rightEdge = userName.Left - 10;

                // Avatar: 34x34, y = 18 (slightly higher to vertically center with username)
                avatar.SetBounds(Math.Max(0, rightEdge - 34), 18, 34, 34);
                rightEdge = avatar.Left - 12;

                // Notification button: 36x36, y = 17
                bell.SetBounds(Math.Max(0, rightEdge - 36), 17, 36, 36);

                // Badge: corner top-right of bell, shift up by 4px
                badge.SetBounds(
                    bell.Right - 7,
                    bell.Top - 4,
                    16,
                    16);

                rightEdge = bell.Left - 18;

                if (search != null)
                {
                    int searchWidth = Math.Min(300, Math.Max(240, header.ClientSize.Width / 5));

                    // place search to the left of bell with a minimum gap
                    int searchRight = rightEdge - 18;
                    search.SetBounds(
                        Math.Max(0, searchRight - searchWidth),
                        14,
                        searchWidth,
                        42);

                    LayoutHeaderBase(search.Left - 18);
                }
                else
                {
                    LayoutHeaderBase(rightEdge - 18);
                }
            }

            header.Resize += (_, _) => LayoutInteractiveHeader();
            LayoutInteractiveHeader();
        }

        if (IsResident)
        {
            header.Resize += (_, _) => LayoutHeaderBase(header.ClientSize.Width - 24);
            LayoutHeaderBase(header.ClientSize.Width - 24);
        }

        return page;
    }

    private int PageWorkWidth(int minWidth = 980)
    {
        int contentWidth = _content.ClientSize.Width > 0 ? _content.ClientSize.Width : Math.Max(1024, Width - SidebarWidth);
        return Math.Max(minWidth, contentWidth - 36 - SystemInformation.VerticalScrollBarWidth);
    }
}

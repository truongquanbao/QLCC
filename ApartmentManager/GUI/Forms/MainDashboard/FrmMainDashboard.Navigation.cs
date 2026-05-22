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
        if (!CanAccessPage(page))
        {
            MessageBox.Show(this,
                "Bạn không có quyền thực hiện chức năng này.",
                "Phân quyền",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            page = "dashboard";
        }

        _activePage = page;
        foreach (var pair in _navButtons)
        {
            bool active = pair.Key == page;
            pair.Value.BackColor = active ? Color.FromArgb(29, 105, 220) : Color.Transparent;
            pair.Value.ForeColor = active ? Color.White : Color.FromArgb(226, 232, 240);
            pair.Value.FlatAppearance.MouseOverBackColor = active
                ? Color.FromArgb(29, 105, 220)
                : Color.FromArgb(5, 45, 96);
            pair.Value.Invalidate();
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
            else if (!HasAnyPermission(PermissionUserManagement, PermissionSystemConfiguration, "ManageApartments", "ManageResidents"))
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
                if (IsResident)
                {
                    RenderResidentInvoices(page == "payment");
                }
                else
                {
                    RenderInvoices();
                }
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
                RenderReportsDashboard();
                break;
            case "logs":
                RenderSystemLogs();
                break;
            case "settings":
                RenderSystemSettings();
                break;
            default:
                Navigate("dashboard");
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

        var header = new GradientPanel
        {
            Dock = DockStyle.Top,
            Height = HeaderHeight,
            BackColor = ModernUi.Navy,
            StartColor = ModernUi.SidebarTop,
            EndColor = ModernUi.Navy,
            GradientMode = LinearGradientMode.Horizontal,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        page.Controls.Add(header);

        var headerIcon = ModernUi.Label("\u2630", 18f, FontStyle.Bold, Color.White);
        headerIcon.BackColor = Color.Transparent;
        headerIcon.TextAlign = ContentAlignment.MiddleCenter;
        headerIcon.Location = new Point(18, (HeaderHeight - 36) / 2);
        headerIcon.Size = new Size(36, 36);
        header.Controls.Add(headerIcon);

        var label = ModernUi.Label(title, 14.4f, FontStyle.Bold, Color.White);
        label.BackColor = Color.Transparent;
        label.AutoEllipsis = true;
        label.Location = new Point(62, (HeaderHeight - 30) / 2);
        label.Size = new Size(320, 30);
        header.Controls.Add(label);

        var divider = new Panel
        {
            BackColor = Color.FromArgb(120, 226, 232, 240),
            Location = new Point(label.Right + 14, (HeaderHeight - 28) / 2),
            Size = new Size(1, 28)
        };
        header.Controls.Add(divider);

        var crumb = ModernUi.Label(breadcrumb, 8.5f, FontStyle.Regular, Color.FromArgb(220, 235, 244, 255));
        crumb.BackColor = Color.Transparent;
        crumb.Location = new Point(divider.Right + 14, (HeaderHeight - 22) / 2);
        crumb.AutoEllipsis = true;
        crumb.Size = new Size(320, 22);
        header.Controls.Add(crumb);

        void LayoutHeaderBase(int rightEdge)
        {
            int centerY = Math.Max(HeaderHeight / 2, header.ClientSize.Height / 2);
            headerIcon.SetBounds(18, centerY - 18, 36, 36);

            int titleLeft = 62;
            int titleMaxWidth = Math.Max(120, rightEdge - titleLeft - 24);
            int desiredTitleWidth = Math.Max(130, TextRenderer.MeasureText(title, label.Font).Width + 10);
            label.SetBounds(titleLeft, centerY - 15, Math.Min(titleMaxWidth, desiredTitleWidth), 30);

            divider.Visible = !string.IsNullOrWhiteSpace(breadcrumb);
            crumb.Visible = divider.Visible;
            divider.Location = new Point(label.Right + 14, centerY - 14);
            divider.Height = 28;
            crumb.Location = new Point(divider.Right + 14, centerY - 11);
            crumb.Size = new Size(Math.Max(0, rightEdge - crumb.Left), 22);
        }

        if (!IsResident)
        {
            var bell = new Label
            {
                Text = "!",
                AutoSize = false,
                Size = new Size(34, 34),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = ModernUi.Font(13f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
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
            _headerNotificationBadge = badge;
            _headerNotificationToolTip?.Dispose();
            _headerNotificationToolTip = new ToolTip
            {
                AutomaticDelay = 200,
                AutoPopDelay = 5000,
                ReshowDelay = 100
            };
            _headerNotificationToolTip.SetToolTip(bell, "Mở danh sách thông báo chưa đọc");
            _headerNotificationToolTip.SetToolTip(badge, "Mở danh sách thông báo chưa đọc");
            UpdateHeaderNotificationBadge();

            var help = new Label
            {
                Text = "?",
                AutoSize = false,
                Size = new Size(34, 34),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = ModernUi.Font(13f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            header.Controls.Add(help);

            var avatar = new CircleLabel
            {
                Text = GetUserInitials(),
                CircleColor = Color.FromArgb(226, 236, 248),
                ForeColor = ModernUi.Navy,
                Font = ModernUi.Font(9f, FontStyle.Bold),
                Size = new Size(38, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            header.Controls.Add(avatar);

            var userName = ModernUi.Label(CurrentUsername(), 9f, FontStyle.Bold, Color.White);
            userName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            userName.BackColor = Color.Transparent;
            userName.Size = new Size(104, 18);
            userName.AutoEllipsis = true;
            userName.Cursor = Cursors.Hand;
            userName.TextAlign = ContentAlignment.MiddleLeft;
            header.Controls.Add(userName);

            var userRole = ModernUi.Label(HeaderRoleText(), 8f, FontStyle.Regular, Color.FromArgb(220, 235, 244, 255));
            userRole.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            userRole.BackColor = Color.Transparent;
            userRole.Size = new Size(104, 16);
            userRole.AutoEllipsis = true;
            userRole.Cursor = Cursors.Hand;
            userRole.TextAlign = ContentAlignment.MiddleLeft;
            header.Controls.Add(userRole);

            var arrow = ModernUi.Label("\u25BE", 10f, FontStyle.Bold, Color.White);
            arrow.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            arrow.BackColor = Color.Transparent;
            arrow.TextAlign = ContentAlignment.MiddleCenter;
            arrow.Size = new Size(18, 20);
            arrow.Cursor = Cursors.Hand;
            header.Controls.Add(arrow);

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
            help.Click += (_, _) => Navigate("notifications");
            avatar.Click += (_, _) => ToggleAccountDropdown();
            userName.Click += (_, _) => ToggleAccountDropdown();
            userRole.Click += (_, _) => ToggleAccountDropdown();
            arrow.Click += (_, _) => ToggleAccountDropdown();

            void LayoutInteractiveHeader()
            {
                int headerWidth = header.ClientSize.Width > 80
                    ? header.ClientSize.Width
                    : Math.Max(760, _content.ClientSize.Width);
                int rightEdge = Math.Max(0, headerWidth - 22);
                int centerY = header.Height / 2;

                arrow.SetBounds(rightEdge - 18, centerY - 10, 18, 20);
                rightEdge = arrow.Left - 8;

                int userWidth = 118;
                int userTop = centerY - 18;
                userName.SetBounds(Math.Max(0, rightEdge - userWidth), userTop, userWidth, 18);
                userRole.SetBounds(userName.Left, userTop + 19, userWidth, 16);
                rightEdge = userName.Left - 10;

                avatar.SetBounds(Math.Max(0, rightEdge - 38), centerY - 19, 38, 38);
                rightEdge = avatar.Left - 18;

                help.SetBounds(Math.Max(0, rightEdge - 34), centerY - 17, 34, 34);
                rightEdge = help.Left - 12;

                bell.SetBounds(Math.Max(0, rightEdge - 34), centerY - 17, 34, 34);
                badge.SetBounds(bell.Right - 10, bell.Top + 2, 16, 16);

                LayoutHeaderBase(bell.Left - 24);
            }

            header.Resize += (_, _) => LayoutInteractiveHeader();
            LayoutInteractiveHeader();
            if (header.IsHandleCreated)
            {
                header.BeginInvoke(new Action(LayoutInteractiveHeader));
            }
            else
            {
                header.HandleCreated += (_, _) => header.BeginInvoke(new Action(LayoutInteractiveHeader));
            }
        }

        if (IsResident)
        {
            header.Resize += (_, _) => LayoutHeaderBase(header.ClientSize.Width - 24);
            LayoutHeaderBase(header.ClientSize.Width - 24);
            if (header.IsHandleCreated)
            {
                header.BeginInvoke(new Action(() => LayoutHeaderBase(header.ClientSize.Width - 24)));
            }
            else
            {
                header.HandleCreated += (_, _) => header.BeginInvoke(new Action(() => LayoutHeaderBase(header.ClientSize.Width - 24)));
            }
        }

        return page;
    }

    private static string PageHeaderIcon(string title)
    {
        string value = title ?? string.Empty;
        if (value.Contains("Báo cáo", StringComparison.OrdinalIgnoreCase))
        {
            return "☰";
        }

        if (value.Contains("Dashboard", StringComparison.OrdinalIgnoreCase))
        {
            return "⌂";
        }

        if (value.Contains("hóa đơn", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("phí", StringComparison.OrdinalIgnoreCase))
        {
            return "▤";
        }

        if (value.Contains("cư dân", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("tài khoản", StringComparison.OrdinalIgnoreCase))
        {
            return "●";
        }

        if (value.Contains("phương tiện", StringComparison.OrdinalIgnoreCase))
        {
            return "▣";
        }

        return "☰";
    }

    private string HeaderRoleText()
    {
        return RoleDisplay();
    }

    private int PageWorkWidth(int minWidth = 980)
    {
        int contentWidth = _content.ClientSize.Width > 0 ? _content.ClientSize.Width : Math.Max(1024, Width - SidebarWidth);
        return Math.Max(minWidth, contentWidth - 36 - SystemInformation.VerticalScrollBarWidth);
    }
}

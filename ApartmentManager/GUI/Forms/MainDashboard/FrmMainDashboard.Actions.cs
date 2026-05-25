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
    private void ReloadCurrentPage()
    {
        Navigate(_activePage);
    }

    private void NavigateWithQuickAction(string page, string mode)
    {
        _pendingQuickActionPage = page;
        _pendingQuickActionMode = mode;
        Navigate(page);
    }

    private bool ConsumeQuickAction(string page, string mode)
    {
        bool matches =
            string.Equals(_pendingQuickActionPage, page, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_pendingQuickActionMode, mode, StringComparison.OrdinalIgnoreCase);

        if (matches)
        {
            _pendingQuickActionPage = null;
            _pendingQuickActionMode = null;
        }

        return matches;
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
            ("Hồ sơ cá nhân", () => Navigate("profile")),
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
        titleItem.Text = "Thông báo gần đây";
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
        viewAllItem.Text = "Xem tất cả thông báo";
        viewAllItem.Click += (_, _) => ShowNotificationListPopup();
        menu.Items.Add(viewAllItem);

        var markAllReadItem = new ToolStripMenuItem("Đánh dấu đã đọc");
        markAllReadItem.Text = "Đánh dấu đã đọc";
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

    private void ShowNotificationListPopup()
    {
        var notifications = GetHeaderNotifications();
        using var dialog = new Form
        {
            Text = "Thông báo",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(560, 430),
            BackColor = ModernUi.Surface,
            Font = ModernUi.Font(9.5f)
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14),
            BackColor = ModernUi.Surface
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        dialog.Controls.Add(root);

        var title = ModernUi.Label($"Thông báo ({notifications.Count})", 12f, FontStyle.Bold, ModernUi.Blue);
        title.Dock = DockStyle.Fill;
        root.Controls.Add(title, 0, 0);

        var list = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(10)
        };
        list.HorizontalScroll.Enabled = false;
        list.HorizontalScroll.Visible = false;
        root.Controls.Add(list, 0, 1);

        if (notifications.Count == 0)
        {
            var empty = ModernUi.Label("Không có thông báo.", 10f, FontStyle.Regular, ModernUi.Muted);
            empty.TextAlign = ContentAlignment.MiddleCenter;
            empty.Size = new Size(510, 80);
            list.Controls.Add(empty);
        }
        else
        {
            foreach (var notification in notifications.Take(50))
            {
                var row = new RoundedPanel
                {
                    Radius = 8,
                    BorderColor = Color.FromArgb(226, 232, 240),
                    BackColor = notification.IsRead ? Color.White : Color.FromArgb(239, 246, 255),
                    Size = new Size(506, 58),
                    Margin = new Padding(0, 0, 0, 8),
                    Cursor = Cursors.Hand
                };

                var label = ModernUi.Label(BuildNotificationCaption(notification), 9f,
                    notification.IsRead ? FontStyle.Regular : FontStyle.Bold,
                    ModernUi.Text);
                label.SetBounds(12, 8, 480, 40);
                label.AutoEllipsis = true;
                label.Cursor = Cursors.Hand;
                row.Controls.Add(label);

                void Open()
                {
                    OpenNotificationDetail(notification);
                    dialog.Close();
                }

                row.Click += (_, _) => Open();
                label.Click += (_, _) => Open();
                list.Controls.Add(row);
            }
        }

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = ModernUi.Surface,
            Padding = new Padding(0, 10, 0, 0)
        };
        root.Controls.Add(actions, 0, 2);

        var closeButton = ModernUi.OutlineButton("Đóng", 90, 32);
        closeButton.Click += (_, _) => dialog.Close();
        actions.Controls.Add(closeButton);

        var markAllButton = ModernUi.Button("Đánh dấu đã đọc", ModernUi.Blue, 150, 32);
        markAllButton.Margin = new Padding(0, 0, 10, 0);
        markAllButton.Click += (_, _) =>
        {
            MarkAllNotificationsAsRead();
            dialog.Close();
        };
        actions.Controls.Add(markAllButton);

        dialog.ShowDialog(this);
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
        if (!RequirePermission(PermissionExportData, "lưu dữ liệu dashboard"))
        {
            return;
        }

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

    private bool SaveGeneratedFile((bool Success, string Message, byte[] FileContent, string FileName) result, string filter, string auditAction)
    {
        if (!RequirePermission(PermissionExportData, "xuất Excel/PDF"))
        {
            return false;
        }

        if (!result.Success || result.FileContent == null || result.FileContent.Length == 0 || string.IsNullOrWhiteSpace(result.FileName))
        {
            MessageBox.Show(this,
                string.IsNullOrWhiteSpace(result.Message) ? "Không thể tạo file xuất." : result.Message,
                "Xuất dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
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
            return false;
        }

        File.WriteAllBytes(dialog.FileName, result.FileContent);
        AuditLogDAL.LogAction(_session?.UserID, auditAction, "Report", description: $"Saved report: {dialog.FileName}");

        MessageBox.Show(this,
            $"Đã xuất file thành công:\n{dialog.FileName}",
            "Báo cáo nhanh",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        return true;
    }

    private void RunDatabaseBackup()
    {
        if (!RequirePermission(PermissionSystemConfiguration, "backup dữ liệu"))
        {
            return;
        }

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
            if (_session?.UserID > 0)
            {
                var userNotifications = NotificationDAL.GetUserNotifications(_session.UserID);
                if (userNotifications.Count > 0)
                {
                    return userNotifications;
                }
            }

            if (!IsResident)
            {
                return NotificationDAL.GetAllNotifications();
            }
        }
        catch
        {
            // Keep the notification popup lightweight and safe if the database is unavailable.
        }

        return new List<NotificationDTO>();
    }

    private string BuildNotificationCaption(NotificationDTO notification)
    {
        string title = Display(notification.Title, Display(notification.Subject, "Thông báo"));
        string body = Display(notification.Message, Display(notification.Body, ""));
        if (body.Length > 72)
        {
            body = body[..69] + "...";
        }

        string time = notification.CreatedAt == DateTime.MinValue
            ? "Mới"
            : notification.CreatedAt.ToString("dd/MM HH:mm");
        string prefix = notification.IsRead ? "" : "[Mới] ";
        return $"{prefix}{title} - {body} ({time})";
    }

    private string BuildNotificationTooltip(NotificationDTO notification)
    {
        return $"{Display(notification.Title, Display(notification.Subject, "Thông báo"))}\n{Display(notification.Message, Display(notification.Body, ""))}";
    }

    private void OpenNotificationDetail(NotificationDTO notification)
    {
        if (notification.NotificationID > 0 && !notification.IsRead)
        {
            NotificationDAL.MarkAsRead(notification.NotificationID);
            notification.IsRead = true;
        }

        MessageBox.Show(this,
            BuildNotificationTooltip(notification),
            Display(notification.Title, "Thông báo"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        UpdateHeaderNotificationBadge();
    }

    private void MarkAllNotificationsAsRead()
    {
        if (_session?.UserID > 0)
        {
            NotificationDAL.MarkAllAsRead(_session.UserID);
        }
        else
        {
            foreach (var notification in GetHeaderNotifications().Where(n => n.NotificationID > 0))
            {
                NotificationDAL.MarkAsRead(notification.NotificationID);
            }
        }

        UpdateHeaderNotificationBadge();
    }

    private void UpdateHeaderNotificationBadge()
    {
        if (_headerNotificationBadge == null || _headerNotificationBadge.IsDisposed)
        {
            return;
        }

        int count = NotificationCount();
        _headerNotificationBadge.Text = count.ToString(CultureInfo.InvariantCulture);
        _headerNotificationBadge.Visible = count > 0;
        _headerNotificationBadge.Invalidate();
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
            _session = null;
            _activePage = "dashboard";
            _navButtons.Clear();
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

    private const string PermissionUserManagement = "UserManagement";
    private const string PermissionManageRoles = "ManageRoles";
    private const string PermissionChangeUserRole = "ChangeUserRole";
    private const string PermissionLockUsers = "LockUsers";
    private const string PermissionResetPassword = "ResetPassword";
    private const string PermissionDeleteUsers = "DeleteUsers";
    private const string PermissionExportData = "ExportData";
    private const string PermissionSystemConfiguration = "SystemConfiguration";

    private bool IsSuperAdminRole()
        => _session != null && string.Equals(RoleName(), "Super Admin", StringComparison.OrdinalIgnoreCase);

    private bool HasPermission(string permissionName)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
        {
            return true;
        }

        if (IsSuperAdminRole())
        {
            return true;
        }

        return _session?.HasPermission(permissionName) == true;
    }

    private bool HasAnyPermission(params string[] permissionNames)
        => permissionNames == null ||
           permissionNames.Length == 0 ||
           permissionNames.Any(HasPermission);

    private bool RequirePermission(string permissionName, string actionName)
    {
        if (HasPermission(permissionName))
        {
            return true;
        }

        MessageBox.Show(this,
            $"Bạn không có quyền thực hiện chức năng này{(string.IsNullOrWhiteSpace(actionName) ? "." : $": {actionName}.")}",
            "Phân quyền",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
        return false;
    }

    private bool RequireAnyPermission(string actionName, params string[] permissionNames)
    {
        if (HasAnyPermission(permissionNames))
        {
            return true;
        }

        MessageBox.Show(this,
            $"Bạn không có quyền thực hiện chức năng này{(string.IsNullOrWhiteSpace(actionName) ? "." : $": {actionName}.")}",
            "Phân quyền",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
        return false;
    }

    private void ApplyActionPermission(Button button, string permissionName)
    {
        button.Enabled = HasPermission(permissionName);
        if (!button.Enabled)
        {
            button.Cursor = Cursors.No;
        }
    }

    private bool CanAccessPage(string pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey))
        {
            return false;
        }

        if (IsResident)
        {
            return pageKey is "dashboard" or "profile" or "password" or "apartment-info" or
                "my-invoices" or "payment" or "send-complaint" or "notifications" or
                "vehicles" or "visitors";
        }

        return pageKey switch
        {
            "dashboard" or "profile" or "password" or "notifications" => true,
            "accounts" => HasAnyPermission(PermissionUserManagement, PermissionManageRoles),
            "permissions" => HasPermission(PermissionManageRoles),
            "apartments" => HasPermission("ManageApartments"),
            "residents" => HasPermission("ManageResidents"),
            "invoices" => HasPermission("ManageInvoices"),
            "complaints" => HasPermission("ManageComplaints"),
            "vehicles" => HasPermission("ManageVehicles"),
            "visitors" => HasPermission("ManageVisitors"),
            "assets" => HasPermission("ManageAssets"),
            "reports" => HasAnyPermission("ReportGeneration", "ViewReports"),
            "logs" => HasPermission("ViewLogs"),
            "settings" => HasPermission(PermissionSystemConfiguration),
            _ => false
        };
    }

    private void RefreshCurrentSessionFromDatabase()
    {
        if (_session?.UserID <= 0)
        {
            return;
        }

        var user = UserDAL.GetUserByID(_session.UserID);
        if (user == null)
        {
            return;
        }

        _session.Username = user.Username;
        _session.FullName = user.FullName;
        _session.Email = user.Email;
        _session.Phone = user.Phone;
        _session.RoleID = user.RoleID;
        _session.RoleName = user.RoleName;
        _session.Status = user.Status;
        _session.AvatarPath = user.AvatarPath;
        _session.CurrentUser = user;
        _session.Permissions = RolePermissionDAL.GetPermissionNamesForRole(user.RoleID);
        SessionManager.SetSession(_session);
    }

    private string GetDefaultPage() => "dashboard";
    private bool HasCurrentRole(string roleName)
    {
        string currentRole = RoleName();
        if (string.IsNullOrWhiteSpace(currentRole))
        {
            return false;
        }

        return string.Equals(currentRole, roleName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(UserRoleLabel(currentRole), roleName, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsResident => HasCurrentRole("Resident") || HasCurrentRole("Cư dân");
    private bool IsManager => !IsResident && (HasCurrentRole("Manager") || HasCurrentRole("Quản lý"));
    private string RoleName() => _session?.RoleName ?? string.Empty;
    private string CurrentUsername() => _session?.Username ?? "superadmin";
    private string CurrentDisplayName() => _session?.FullName ?? (IsResident ? "Nguyễn Văn An" : CurrentUsername());
    private string FooterDisplayName() => CurrentDisplayName();
    private string RoleDisplay() => UserRoleLabel(RoleName());
    private string RoleFooterLabel() => "Tên người dùng";
    private int NotificationCount()
    {
        return GetHeaderNotifications().Count(notification => !notification.IsRead);
    }

}

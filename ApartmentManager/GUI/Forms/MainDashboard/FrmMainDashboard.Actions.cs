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
        viewAllItem.Click += (_, _) => Navigate("notifications");
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

}

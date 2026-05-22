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
    private void RenderAccounts()
    {
        if (!RequireAnyPermission("quản lý tài khoản", PermissionUserManagement, PermissionManageRoles))
        {
            Navigate("dashboard");
            return;
        }

        RolePermissionDAL.EnsureRbacDefaults();
        var roles = RolePermissionDAL.GetAllRoles();
        var permissions = RolePermissionDAL.GetAllPermissions();
        string[] roleFilterItems = new[] { "Tất cả" }
            .Concat(roles.Select(r => UserRoleLabel(r.RoleName)).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.CurrentCultureIgnoreCase))
            .ToArray();
        string[] roleInputItems = roles
            .Select(r => UserRoleLabel(r.RoleName))
            .Where(v => !string.IsNullOrWhiteSpace(v) && v != "-")
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        if (roleInputItems.Length == 0)
        {
            roleInputItems = new[] { "Super Admin", "Admin", "Manager", "Kế toán", "Lễ tân", "Kỹ thuật", "Bảo vệ", "Cư dân" };
        }

        var page = BeginPage("Quản lý tài khoản & phân quyền", "Hệ thống / Tài khoản & phân quyền");

        int margin = 18;
        int contentWidth = _content.ClientSize.Width > 0 ? _content.ClientSize.Width : _content.Width;
        int w = Math.Max(980, contentWidth - margin * 2 - SystemInformation.VerticalScrollBarWidth);

        var toolbar = new Panel
        {
            Location = new Point(margin, 88),
            Size = new Size(w, 50),
            BackColor = ModernUi.Surface
        };
        page.Controls.Add(toolbar);

        int toolbarPadding = 0;
        int filterY = 8;
        int buttonY = 50;
        int filterGap = 16;
        int searchW = Math.Min(390, Math.Max(250, w - 520));
        var search = ModernUi.TextBox("Tìm kiếm nhanh (username/email)...", searchW);
        search.Location = new Point(toolbarPadding, filterY);
        search.Size = new Size(searchW, 38);
        toolbar.Controls.Add(search);

        var roleLabel = ModernUi.Label("Vai trò:", 9.2f, FontStyle.Bold, ModernUi.Text);
        roleLabel.Location = new Point(search.Right + filterGap, filterY + 6);
        roleLabel.Size = new Size(58, 26);
        toolbar.Controls.Add(roleLabel);
        var role = ModernUi.ComboBox(roleFilterItems, 168);
        role.Location = new Point(roleLabel.Right + 6, filterY + 2);
        toolbar.Controls.Add(role);

        var statusLabel = ModernUi.Label("Trạng thái:", 9.2f, FontStyle.Bold, ModernUi.Text);
        statusLabel.Location = new Point(role.Right + filterGap, filterY + 6);
        statusLabel.Size = new Size(78, 26);
        toolbar.Controls.Add(statusLabel);
        var status = ModernUi.ComboBox(new[] { "Tất cả", "Hoạt động", "Tạm khóa", "Chờ duyệt" }, 168);
        status.Location = new Point(statusLabel.Right + 8, filterY + 2);
        toolbar.Controls.Add(status);

        toolbar.Height = 90;

        int toolbarButtonsWidth = 704;
        int buttonX = Math.Max(toolbarPadding, w - toolbarButtonsWidth);
        var addButton = AddToolbarButton(toolbar, "+  Thêm", ModernUi.Blue, buttonX, buttonY, 88);
        var editButton = AddToolbarButton(toolbar, "✎  Sửa", Color.FromArgb(241, 166, 0), buttonX + 102, buttonY, 86);
        var deleteButton = AddToolbarButton(toolbar, "×  Xóa", ModernUi.Red, buttonX + 202, buttonY, 86);
        var lockButton = AddToolbarButton(toolbar, "▣  Khóa/Mở khóa", ModernUi.Blue, buttonX + 302, buttonY, 130);
        var resetPasswordButton = AddToolbarButton(toolbar, "⚿  Reset MK", ModernUi.Purple, buttonX + 446, buttonY, 128);
        var viewPermissionsButton = AddToolbarButton(toolbar, "☑  Xem quyền", ModernUi.Teal, buttonX + 588, buttonY, 116);
        ApplyActionPermission(addButton, PermissionUserManagement);
        ApplyActionPermission(editButton, PermissionUserManagement);
        ApplyActionPermission(deleteButton, PermissionDeleteUsers);
        ApplyActionPermission(lockButton, PermissionLockUsers);
        ApplyActionPermission(resetPasswordButton, PermissionResetPassword);
        ApplyActionPermission(viewPermissionsButton, PermissionUserManagement);

        int topY = toolbar.Bottom + 12;
        int listW = w;
        int topH = 392;

        var users = ModernUi.CardPanel(5);
        users.Location = new Point(margin, topY);
        users.Size = new Size(listW, topH);
        users.Padding = new Padding(0);
        page.Controls.Add(users);

        var userGrid = CreateAccountUsersGrid();
        userGrid.Location = new Point(12, 12);
        userGrid.Size = new Size(users.Width - 24, 314);
        userGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        users.Controls.Add(userGrid);

        int pagerY = userGrid.Bottom + 14;
        int pagerButtonGroupWidth = 184;
        int pageSizeWidth = 66;
        int pageSizeTextWidth = 76;
        int pageSizeX = users.Width - 12 - pageSizeWidth;
        int pageSizeTextX = pageSizeX - pageSizeTextWidth - 8;
        int pagerButtonsX = pageSizeTextX - 14 - pagerButtonGroupWidth;
        var accountPager = AddPaginationControls(users, 16, pagerY + 2, pagerButtonsX, pagerY, pageSizeX, pagerY, Math.Max(280, pagerButtonsX - 32));
        var perPage = accountPager.PageSizeCombo;
        var perPageText = ModernUi.Label("mục/trang", 9f, FontStyle.Regular, ModernUi.Text);
        perPageText.Location = new Point(pageSizeTextX, pagerY + 2);
        perPageText.Size = new Size(pageSizeTextWidth, 26);
        users.Controls.Add(perPageText);

        var account = ModernUi.CardPanel(5);
        account.Location = new Point(margin, users.Bottom + 14);
        account.Size = new Size(w, 242);
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
        var roleInput = AddAccountCombo(account, "Vai trò *", roleInputItems, fx, 126, colW);
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
        ApplyActionPermission(save, PermissionUserManagement);
        var cancel = ModernUi.OutlineButton("⊘  Hủy", actionW, 34);
        cancel.Location = new Point(save.Right + 10, 190);
        account.Controls.Add(cancel);

        int bottomY = account.Bottom + 16;
        int bottomH = Math.Max(332, _content.ClientSize.Height - bottomY - 20);
        var bottom = ModernUi.CardPanel(5);
        bottom.Location = new Point(margin, bottomY);
        bottom.Size = new Size(w, bottomH);
        bottom.Padding = new Padding(0);
        page.Controls.Add(bottom);
        page.AutoScrollMinSize = new Size(0, bottom.Bottom + 24);

        var permissionTab = new Label
        {
            Text = "◆  Phân quyền vai trò",
            Location = new Point(0, 0),
            Size = new Size(190, 44),
            BackColor = Color.White,
            ForeColor = ModernUi.Navy,
            Font = ModernUi.Font(9.3f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        bottom.Controls.Add(permissionTab);
        var activityTab = new Label
        {
            Text = "▤  Nhật ký hoạt động",
            Location = new Point(190, 0),
            Size = new Size(180, 44),
            BackColor = ModernUi.Header,
            ForeColor = ModernUi.Text,
            Font = ModernUi.Font(9.3f, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        bottom.Controls.Add(activityTab);
        var activeLine = new Panel { BackColor = ModernUi.Blue, Location = new Point(12, 42), Size = new Size(166, 2) };
        bottom.Controls.Add(activeLine);
        var divider = new Panel { BackColor = ModernUi.Border, Location = new Point(0, 44), Size = new Size(bottom.Width, 1) };
        bottom.Controls.Add(divider);

        var savePermissionsButton = ModernUi.Button("Lưu phân quyền", ModernUi.Blue, 144, 34);
        savePermissionsButton.Location = new Point(bottom.Width - 156, 6);
        savePermissionsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        savePermissionsButton.Enabled = false;
        savePermissionsButton.Visible = HasPermission(PermissionManageRoles);
        bottom.Controls.Add(savePermissionsButton);

        var tabContent = new Panel
        {
            Location = new Point(12, 58),
            Size = new Size(bottom.Width - 24, bottom.Height - 70),
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };
        bottom.Controls.Add(tabContent);

        var matrix = CreatePermissionMatrixGrid(roles, permissions);
        matrix.Dock = DockStyle.Fill;
        tabContent.Controls.Add(matrix);

        var logGrid = CreateAccountLogGrid();
        logGrid.Dock = DockStyle.Fill;
        tabContent.Controls.Add(logGrid);
        logGrid.Visible = false;
        UserDTO? selectedUser = null;
        int? lastFocusedUserId = null;
        string? selectedAvatarPath = null;
        bool suppressAccountSelectionChanged = false;
        bool suppressPermissionMatrixEvents = false;

        var accountPagination = new PaginationState();

        bool IsResidentRole(string? roleName)
            => string.Equals(roleName, "Resident", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(roleName, "Cư dân", StringComparison.OrdinalIgnoreCase);

        RoleDTO? FindRoleByLabel(string? label)
            => roles.FirstOrDefault(r =>
                string.Equals(UserRoleLabel(r.RoleName), label, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.RoleName, label, StringComparison.OrdinalIgnoreCase));

        UserDTO? GetSelectedUserFromGrid()
        {
            if (userGrid.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in userGrid.SelectedRows)
                {
                    if (row.Tag is UserDTO selectedRowUser)
                    {
                        return selectedRowUser;
                    }
                }
            }

            return userGrid.CurrentRow?.Tag as UserDTO;
        }

        string AccountAvatarInitials(UserDTO? user)
        {
            string source = Display(user?.FullName, Display(user?.Username, "U")).Trim();
            if (string.IsNullOrWhiteSpace(source))
            {
                return "U";
            }

            string[] parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
            }

            return char.ToUpperInvariant(parts[0][0]).ToString();
        }

        void RefreshAccountAvatarPreview(UserDTO? user, string? avatarPathOverride = null)
        {
            avatar.SetAvatar(avatarPathOverride ?? user?.AvatarPath, AccountAvatarInitials(user));
        }

        void SetFormMode(bool creating)
        {
            accountTitle.Text = creating ? "THÊM TÀI KHOẢN MỚI" : "THÔNG TIN TÀI KHOẢN";
            save.Text = creating ? "▣  Tạo mới" : "▣  Lưu";
        }

        void PopulateAccountForm(UserDTO? user, bool clearSelection = false)
        {
            selectedUser = user;
            if (user != null)
            {
                lastFocusedUserId = user.UserID;
            }
            selectedAvatarPath = user?.AvatarPath;
            choose.Text = string.IsNullOrWhiteSpace(selectedAvatarPath)
                ? "▣ Chọn ảnh"
                : $"▣ {Path.GetFileName(selectedAvatarPath)}";
            RefreshAccountAvatarPreview(user, selectedAvatarPath);

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
                    suppressAccountSelectionChanged = true;
                    try
                    {
                        userGrid.ClearSelection();
                        userGrid.CurrentCell = null;
                    }
                    finally
                    {
                        suppressAccountSelectionChanged = false;
                    }
                }

                SetFormMode(true);
                usernameInput.Focus();
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
            fullNameInput.Focus();
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

        void ReloadPermissionMatrix()
        {
            roles = RolePermissionDAL.GetAllRoles();
            permissions = RolePermissionDAL.GetAllPermissions();

            suppressPermissionMatrixEvents = true;
            try
            {
                PopulatePermissionMatrixGrid(matrix, roles, permissions);
                savePermissionsButton.Enabled = false;
            }
            finally
            {
                suppressPermissionMatrixEvents = false;
            }
        }

        void SavePermissionChanges()
        {
            if (!RequirePermission(PermissionManageRoles, "lưu phân quyền"))
            {
                return;
            }

            if (matrix.IsCurrentCellDirty)
            {
                matrix.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }

            matrix.EndEdit();

            var roleColumns = matrix.Columns
                .Cast<DataGridViewColumn>()
                .Skip(1)
                .Where(column => column.Tag is RoleDTO)
                .Select(column => (Column: column, Role: (RoleDTO)column.Tag))
                .ToList();

            foreach (var (_, roleDto) in roleColumns)
            {
                var selectedPermissionIds = new List<int>();
                foreach (DataGridViewRow row in matrix.Rows)
                {
                    if (row.Tag is not PermissionDTO permissionDto)
                    {
                        continue;
                    }

                    var column = roleColumns.First(item => item.Role.RoleID == roleDto.RoleID).Column;
                    bool isGranted = row.Cells[column.Index].Value is bool granted && granted;
                    if (isGranted)
                    {
                        selectedPermissionIds.Add(permissionDto.PermissionID);
                    }
                }

                if (!RolePermissionDAL.UpdateRolePermissions(roleDto.RoleID, selectedPermissionIds))
                {
                    MessageBox.Show(this,
                        $"Không thể lưu phân quyền cho vai trò `{Display(roleDto.RoleName)}`.",
                        "Phân quyền",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }

            AuditLogDAL.LogAction(_session?.UserID, "Update_RolePermissions", "RolePermission", description: "Cập nhật phân quyền vai trò");
            ReloadPermissionMatrix();
            RefreshCurrentSessionFromDatabase();
            BuildShell();
            Navigate(CanAccessPage(_activePage) ? _activePage : "dashboard");
            MessageBox.Show(this,
                "Đã lưu thay đổi phân quyền.",
                "Phân quyền",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        void SetBottomTab(bool showPermissions)
        {
            permissionTab.BackColor = showPermissions ? Color.White : ModernUi.Header;
            permissionTab.ForeColor = showPermissions ? ModernUi.Navy : ModernUi.Text;
            permissionTab.Font = ModernUi.Font(9.3f, showPermissions ? FontStyle.Bold : FontStyle.Regular);

            activityTab.BackColor = showPermissions ? ModernUi.Header : Color.White;
            activityTab.ForeColor = showPermissions ? ModernUi.Text : ModernUi.Navy;
            activityTab.Font = ModernUi.Font(9.3f, showPermissions ? FontStyle.Regular : FontStyle.Bold);

            activeLine.Location = showPermissions ? new Point(12, 42) : new Point(activityTab.Left + 12, 42);
            activeLine.Width = showPermissions ? permissionTab.Width - 24 : activityTab.Width - 24;

            savePermissionsButton.Visible = showPermissions && HasPermission(PermissionManageRoles);
            matrix.Visible = showPermissions;
            logGrid.Visible = !showPermissions;
            if (showPermissions)
            {
                matrix.BringToFront();
            }
            else
            {
                logGrid.BringToFront();
            }
        }

        void RefreshUsersWithPaging(int? selectUserId = null)
        {
            var filteredUsers = FilterUsers();
            if (selectUserId.HasValue)
            {
                int selectedIndex = filteredUsers.FindIndex(user => user.UserID == selectUserId.Value);
                if (selectedIndex >= 0)
                {
                    accountPagination.CurrentPage = FindPageForIndex(selectedIndex, accountPagination.PageSize);
                }
            }

            IReadOnlyList<UserDTO> pageUsers = Paginate(filteredUsers, accountPagination);
            PopulateAccountUsersGrid(userGrid, pageUsers);
            UpdatePaginationControls(accountPagination, accountPager, "tài khoản");

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

            if (pageUsers.Count > 0)
            {
                userGrid.Rows[0].Selected = true;
                userGrid.CurrentCell = userGrid.Rows[0].Cells[0];
                PopulateAccountForm(pageUsers[0]);
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
            if (!RequirePermission(PermissionUserManagement, selectedUser == null ? "thêm tài khoản" : "sửa tài khoản"))
            {
                return;
            }

            if (!ValidateAccountForm(out var selectedRole, out var dbStatus) || selectedRole == null)
            {
                return;
            }

            string username = usernameInput.Text.Trim();
            string fullName = fullNameInput.Text.Trim();
            string email = emailInput.Text.Trim();
            string phone = phoneInput.Text.Trim();
            bool isApproved = approved.Checked || !IsResidentRole(selectedRole.RoleName);
            string oldRoleName = Display(selectedUser?.RoleName, "");
            string newRoleName = Display(selectedRole.RoleName, "");
            bool roleChanged = selectedUser != null &&
                !string.Equals(oldRoleName, newRoleName, StringComparison.OrdinalIgnoreCase);

            if ((selectedUser == null || roleChanged) &&
                !RequirePermission(PermissionChangeUserRole, selectedUser == null ? "gán vai trò tài khoản" : "đổi vai trò tài khoản"))
            {
                return;
            }

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

                RefreshUsersWithPaging(newUserId);
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

            if (string.Equals(dbStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                var reject = UserBLL.RejectUser(selectedUser.UserID, _session?.UserID, "Từ chối từ màn hình quản lý tài khoản");
                if (!reject.success)
                {
                    MessageBox.Show(this, reject.message,
                        "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                AuditLogDAL.LogAction(_session?.UserID, "Update_User", "User", selectedUser.UserID, $"Cập nhật tài khoản: {username}");
            }

            if (roleChanged)
            {
                AuditLogDAL.LogAction(
                    _session?.UserID,
                    "Change_UserRole",
                    "User",
                    selectedUser.UserID,
                    $"Đổi quyền user {selectedUser.Username}: {oldRoleName} -> {newRoleName}");

                if (selectedUser.UserID == _session?.UserID)
                {
                    RefreshCurrentSessionFromDatabase();
                    BuildShell();
                    Navigate(CanAccessPage(_activePage) ? _activePage : "dashboard");
                    return;
                }
            }

            MessageBox.Show(this, "Đã lưu thay đổi tài khoản.",
                "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshUsersWithPaging(selectedUser.UserID);
        }

        void DeleteSelectedUser()
        {
            if (!RequirePermission(PermissionDeleteUsers, "xóa tài khoản"))
            {
                return;
            }

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
            RefreshUsersWithPaging();
        }

        void ToggleSelectedUserLock()
        {
            if (!RequirePermission(PermissionLockUsers, "khóa/mở khóa tài khoản"))
            {
                return;
            }

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

            bool isInactive = string.Equals(selectedUser.Status, "Inactive", StringComparison.OrdinalIgnoreCase);
            var result = isInactive
                ? UserBLL.UnlockUserAccount(selectedUser.UserID, _session?.UserID, "Mở khóa từ màn hình quản lý tài khoản")
                : UserBLL.LockUserAccount(selectedUser.UserID, _session?.UserID, "Khóa từ màn hình quản lý tài khoản");

            if (!result.success)
            {
                MessageBox.Show(this, result.message,
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            RefreshUsersWithPaging(selectedUser.UserID);
        }

        void ResetSelectedUserPassword()
        {
            if (!RequirePermission(PermissionResetPassword, "reset mật khẩu"))
            {
                return;
            }

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

        void ShowSelectedUserPermissions()
        {
            var user = GetSelectedUserFromGrid() ?? selectedUser;
            if (user == null)
            {
                MessageBox.Show(this, "Bạn chưa chọn tài khoản để xem quyền.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var granted = RolePermissionDAL.GetPermissionNamesForRole(user.RoleID)
                .OrderBy(p => p, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            string permissionsText = granted.Count == 0
                ? "Chưa có quyền nào được gán."
                : string.Join(Environment.NewLine, granted.Select(p => $"• {p}"));

            MessageBox.Show(this,
                $"User: {Display(user.Username)}\nVai trò: {UserRoleLabel(user.RoleName)}\n\nQuyền hiện tại:\n{permissionsText}",
                "Quyền hiện tại",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        accountPager.FirstButton.Click += (_, _) =>
        {
            accountPagination.MoveToFirstPage();
            RefreshUsersWithPaging();
        };
        accountPager.PreviousButton.Click += (_, _) =>
        {
            accountPagination.MoveToPreviousPage();
            RefreshUsersWithPaging();
        };
        accountPager.NextButton.Click += (_, _) =>
        {
            accountPagination.MoveToNextPage();
            RefreshUsersWithPaging();
        };
        accountPager.LastButton.Click += (_, _) =>
        {
            accountPagination.MoveToLastPage();
            RefreshUsersWithPaging();
        };
        perPage.SelectedIndexChanged += (_, _) =>
        {
            accountPagination.SetPageSize(ParsePageSize(perPage.SelectedItem, accountPagination.PageSize));
            RefreshUsersWithPaging();
        };
        search.TextChanged += (_, _) =>
        {
            accountPagination.MoveToFirstPage();
            RefreshUsersWithPaging();
        };
        role.SelectedIndexChanged += (_, _) =>
        {
            accountPagination.MoveToFirstPage();
            RefreshUsersWithPaging();
        };
        status.SelectedIndexChanged += (_, _) =>
        {
            accountPagination.MoveToFirstPage();
            RefreshUsersWithPaging();
        };

        matrix.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (matrix.IsCurrentCellDirty)
            {
                matrix.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        matrix.CellValueChanged += (_, e) =>
        {
            if (suppressPermissionMatrixEvents || e.RowIndex < 0 || e.ColumnIndex <= 0)
            {
                return;
            }

            savePermissionsButton.Enabled = HasPermission(PermissionManageRoles);
        };

        permissionTab.Click += (_, _) => SetBottomTab(true);
        activityTab.Click += (_, _) => SetBottomTab(false);
        savePermissionsButton.Click += (_, _) => SavePermissionChanges();

        userGrid.SelectionChanged += (_, _) =>
        {
            if (suppressAccountSelectionChanged)
            {
                return;
            }

            if (GetSelectedUserFromGrid() is UserDTO rowUser)
            {
                PopulateAccountForm(rowUser);
            }
        };

        addButton.Click += (_, _) => PopulateAccountForm(null, clearSelection: true);
        editButton.Click += (_, _) =>
        {
            var userToEdit = GetSelectedUserFromGrid() ?? selectedUser;
            if (userToEdit == null)
            {
                MessageBox.Show(this, "Bạn chưa chọn tài khoản để sửa.",
                    "Quản lý tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PopulateAccountForm(UserDAL.GetUserByID(userToEdit.UserID) ?? userToEdit);
        };
        deleteButton.Click += (_, _) => DeleteSelectedUser();
        lockButton.Click += (_, _) => ToggleSelectedUserLock();
        resetPasswordButton.Click += (_, _) => ResetSelectedUserPassword();
        viewPermissionsButton.Click += (_, _) => ShowSelectedUserPermissions();
        save.Click += (_, _) => SaveAccountChanges();
        cancel.Click += (_, _) =>
        {
            if (selectedUser != null)
            {
                PopulateAccountForm(UserDAL.GetUserByID(selectedUser.UserID) ?? selectedUser);
                return;
            }

            if (lastFocusedUserId.HasValue)
            {
                RefreshUsersWithPaging(lastFocusedUserId.Value);
                return;
            }

            PopulateAccountForm(null, clearSelection: true);
        };
        choose.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Chọn ảnh đại diện",
                Filter = "Image Files|*.png;*.jpg;*.jpeg",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                selectedAvatarPath = dialog.FileName;
                choose.Text = $"▣ {Path.GetFileName(dialog.FileName)}";
                RefreshAccountAvatarPreview(selectedUser, selectedAvatarPath);
            }
        };

        RefreshUsersWithPaging();
        ReloadPermissionMatrix();
        if (ConsumeQuickAction("accounts", "add"))
        {
            PopulateAccountForm(null, clearSelection: true);
        }
        SetBottomTab(true);

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

    private static DataGridView CreatePermissionMatrixGrid(IReadOnlyList<RoleDTO> roles, IReadOnlyList<PermissionDTO> permissions)
    {
        var grid = ModernUi.Grid();
        grid.ReadOnly = false;
        grid.AllowUserToAddRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ColumnHeadersHeight = 34;
        grid.RowTemplate.Height = 30;
        grid.ScrollBars = ScrollBars.Both;
        PopulatePermissionMatrixGrid(grid, roles, permissions);
        return grid;
    }

    private static void PopulatePermissionMatrixGrid(DataGridView grid, IReadOnlyList<RoleDTO> roles, IReadOnlyList<PermissionDTO> permissions)
    {
        grid.Columns.Clear();
        grid.Rows.Clear();
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Module / Chức năng",
            Width = 280,
            ReadOnly = true
        });

        foreach (var role in roles)
        {
            var column = new DataGridViewCheckBoxColumn
            {
                HeaderText = Display(role.RoleName),
                Width = Math.Max(108, Math.Min(160, TextRenderer.MeasureText(Display(role.RoleName), ModernUi.Font(8.2f, FontStyle.Bold)).Width + 28)),
                FlatStyle = FlatStyle.Standard
            };
            column.Tag = role;
            grid.Columns.Add(column);
        }

        if (roles.Count == 0 || permissions.Count == 0)
        {
            grid.Rows.Add("Không có dữ liệu phân quyền");
            return;
        }

        foreach (var permission in permissions)
        {
            object[] row = new object[roles.Count + 1];
            row[0] = $"  ▣  {Display(permission.Description, Display(permission.PermissionName))}";
            int index = 1;
            foreach (var role in roles)
            {
                row[index++] = role.PermissionIDs.Contains(permission.PermissionID);
            }
            int rowIndex = grid.Rows.Add(row);
            grid.Rows[rowIndex].Tag = permission;
        }
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
}

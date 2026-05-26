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
        var page = BeginPage("Hồ sơ cá nhân", "Dashboard / Hồ sơ cá nhân");
        page.BackColor = ModernUi.Surface;
        page.AutoScroll = true;
        page.HorizontalScroll.Enabled = false;
        page.HorizontalScroll.Visible = false;

        string displayName = Display(_session?.FullName, "Super Administrator");
        string username = Display(_session?.Username, "superadmin");
        string email = Display(_session?.Email, "superadmin@chungcu.vn");
        string phone = Display(_session?.Phone, "0901 234 567");
        string role = RoleDisplay();
        if (string.IsNullOrWhiteSpace(role) || role == "-" || role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
        {
            role = "Quản trị viên";
        }

        var summaryCard = CreateProfileCard();
        var nameLabel = ModernUi.Label(displayName, 20f, FontStyle.Bold, ModernUi.Navy);
        nameLabel.AutoEllipsis = true;
        summaryCard.Controls.Add(nameLabel);

        var usernameLabel = ModernUi.Label(username, 10.5f, FontStyle.Regular, ModernUi.Text);
        usernameLabel.AutoEllipsis = true;
        summaryCard.Controls.Add(usernameLabel);

        var separator = new Panel { BackColor = ModernUi.Border };
        summaryCard.Controls.Add(separator);

        var roleLabel = ModernUi.Label(role, 10.5f, FontStyle.Regular, ModernUi.Text);
        roleLabel.AutoEllipsis = true;
        summaryCard.Controls.Add(roleLabel);

        var statusBadge = new RoundedPanel
        {
            Radius = 6,
            BackColor = Color.FromArgb(232, 248, 237),
            BorderColor = Color.FromArgb(169, 220, 184),
            Padding = Padding.Empty
        };
        var statusDot = ModernUi.Label("●", 9f, FontStyle.Bold, ModernUi.Green);
        statusDot.TextAlign = ContentAlignment.MiddleCenter;
        statusBadge.Controls.Add(statusDot);
        var statusText = ModernUi.Label("Đang hoạt động", 9.5f, FontStyle.Regular, ModernUi.Green);
        statusText.AutoEllipsis = true;
        statusBadge.Controls.Add(statusText);
        statusBadge.Resize += (_, _) =>
        {
            statusDot.SetBounds(10, 0, 16, statusBadge.Height);
            statusText.SetBounds(30, 0, Math.Max(60, statusBadge.Width - 38), statusBadge.Height);
        };
        summaryCard.Controls.Add(statusBadge);

        var personalCard = CreateProfileRowsCard(
            "Thông tin cá nhân",
            new[]
            {
                ("Họ và tên", displayName),
                ("Email", email),
                ("Số điện thoại", phone),
                ("Chức vụ", "Quản trị hệ thống")
            });

        var accountCard = CreateProfileRowsCard(
            "Cài đặt tài khoản",
            new[]
            {
                ("Tên đăng nhập", username),
                ("Email đăng nhập", email),
                ("Ngôn ngữ", "Tiếng Việt"),
                ("Múi giờ", "(GMT+07:00) Bangkok, Hà Nội, Jakarta")
            });

        page.Controls.Add(summaryCard);
        page.Controls.Add(personalCard);
        page.Controls.Add(accountCard);

        void LayoutProfilePage()
        {
            int padding = 20;
            int gap = 20;
            int contentWidth = Math.Max(320, page.ClientSize.Width - padding * 2 - SystemInformation.VerticalScrollBarWidth);
            int top = HeaderHeight + padding;

            summaryCard.SetBounds(padding, top, contentWidth, 150);
            LayoutProfileSummary(summaryCard, nameLabel, usernameLabel, separator, roleLabel, statusBadge);

            int cardTop = summaryCard.Bottom + gap;
            int cardHeight = 330;
            if (contentWidth >= 820)
            {
                int cardWidth = (contentWidth - gap) / 2;
                personalCard.SetBounds(padding, cardTop, cardWidth, cardHeight);
                accountCard.SetBounds(padding + cardWidth + gap, cardTop, contentWidth - cardWidth - gap, cardHeight);
                page.AutoScrollMinSize = new Size(0, accountCard.Bottom + padding);
            }
            else
            {
                personalCard.SetBounds(padding, cardTop, contentWidth, cardHeight);
                accountCard.SetBounds(padding, personalCard.Bottom + gap, contentWidth, cardHeight);
                page.AutoScrollMinSize = new Size(0, accountCard.Bottom + padding);
            }

            page.HorizontalScroll.Enabled = false;
            page.HorizontalScroll.Visible = false;
        }

        page.Resize += (_, _) => LayoutProfilePage();
        LayoutProfilePage();
    }

    private static RoundedPanel CreateProfileCard()
    {
        var card = ModernUi.CardPanel(12);
        card.BorderColor = Color.FromArgb(223, 231, 242);
        card.BackColor = Color.White;
        card.Padding = Padding.Empty;
        return card;
    }

    private static void LayoutProfileSummary(
        Control card,
        Label nameLabel,
        Label usernameLabel,
        Panel separator,
        Label roleLabel,
        Control statusBadge)
    {
        int left = 32;
        int top = 28;
        int width = Math.Max(160, card.Width - 64);

        nameLabel.SetBounds(left, top, width, 42);

        int lineTop = nameLabel.Bottom + 14;
        int usernameWidth = Math.Min(200, Math.Max(120, TextRenderer.MeasureText(usernameLabel.Text, usernameLabel.Font).Width + 12));
        usernameLabel.SetBounds(left, lineTop, usernameWidth, 28);

        separator.SetBounds(usernameLabel.Right + 16, lineTop + 5, 1, 18);

        int roleWidth = Math.Min(190, Math.Max(120, TextRenderer.MeasureText(roleLabel.Text, roleLabel.Font).Width + 16));
        roleLabel.SetBounds(separator.Right + 16, lineTop, roleWidth, 28);

        int badgeWidth = 164;
        int badgeLeft = roleLabel.Right + 16;
        if (badgeLeft + badgeWidth > card.Width - 32)
        {
            badgeLeft = left;
            lineTop += 34;
        }

        statusBadge.SetBounds(badgeLeft, lineTop - 1, badgeWidth, 32);
    }

    private static RoundedPanel CreateProfileRowsCard(string title, IReadOnlyList<(string Label, string Value)> rows)
    {
        var card = CreateProfileCard();

        var titleLabel = ModernUi.Label(title, 13f, FontStyle.Bold, ModernUi.Navy);
        titleLabel.AutoEllipsis = true;
        card.Controls.Add(titleLabel);

        var rowControls = new List<(Label Label, Label Value, Panel Divider)>();
        foreach (var row in rows)
        {
            var label = ModernUi.Label(row.Label, 10f, FontStyle.Regular, ModernUi.Text);
            label.AutoEllipsis = true;
            card.Controls.Add(label);

            var value = ModernUi.Label(row.Value, 10f, FontStyle.Regular, ModernUi.Navy);
            value.AutoEllipsis = false;
            card.Controls.Add(value);

            var divider = new Panel { BackColor = Color.FromArgb(226, 232, 240) };
            card.Controls.Add(divider);
            rowControls.Add((label, value, divider));
        }

        void LayoutRows()
        {
            int padding = 32;
            titleLabel.SetBounds(padding, 26, Math.Max(120, card.Width - padding * 2), 34);

            int y = 82;
            int rowHeight = 58;
            int labelWidth = Math.Min(220, Math.Max(130, (card.Width - padding * 2) / 2 - 18));
            int valueLeft = padding + labelWidth + 28;
            int valueWidth = Math.Max(120, card.Width - valueLeft - padding);

            foreach (var row in rowControls)
            {
                row.Label.SetBounds(padding, y, labelWidth, rowHeight - 10);
                row.Value.SetBounds(valueLeft, y, valueWidth, rowHeight - 10);
                row.Divider.SetBounds(padding, y + rowHeight - 1, Math.Max(80, card.Width - padding * 2), 1);
                y += rowHeight;
            }
        }

        card.Resize += (_, _) => LayoutRows();
        LayoutRows();
        return card;
    }

    private void RenderNotificationsPage()
    {
        var page = BeginPage("Thông báo của tôi", "Cư dân / Thông báo");
        page.AutoScroll = true;

        int w = PageWorkWidth();
        int x = 18;
        int y = 86;
        int gap = 14;

        int currentUserId = _session?.UserID ?? 0;

        List<NotificationDTO> allNotifications = currentUserId > 0
            ? NotificationDAL.GetUserNotifications(currentUserId)
            : new List<NotificationDTO>();

        allNotifications = allNotifications
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        List<NotificationDTO> filteredNotifications = new List<NotificationDTO>();
        NotificationDTO selectedNotification = null;
        string activeFilter = "All";

        List<NotificationDTO> demoNotifications = CreateDemoNotifications();

        List<NotificationDTO> CreateDemoNotifications()
        {
            DateTime now = DateTime.Now;

            return new List<NotificationDTO>
    {
        new NotificationDTO
        {
            NotificationID = -1,
            UserID = currentUserId,
            Title = "Hóa đơn tháng 05/2026 đã được phát hành",
            Message = "Hóa đơn tháng 05/2026 của căn A-0101 đã được phát hành. Vui lòng kiểm tra và thanh toán đúng hạn.",
            NotificationType = "Payment",
            Priority = "High",
            IsRead = false,
            CreatedAt = now.AddMinutes(-15)
        },
        new NotificationDTO
        {
            NotificationID = -2,
            UserID = currentUserId,
            Title = "Nhắc hạn thanh toán hóa đơn",
            Message = "Hóa đơn tháng 05/2026 còn 6 ngày đến hạn thanh toán. Vui lòng thanh toán để tránh bị quá hạn.",
            NotificationType = "Payment",
            Priority = "Critical",
            IsRead = false,
            CreatedAt = now.AddHours(-2)
        },
        new NotificationDTO
        {
            NotificationID = -3,
            UserID = currentUserId,
            Title = "Phản ánh của bạn đã được tiếp nhận",
            Message = "Ban quản lý đã tiếp nhận phản ánh về đèn hành lang không sáng. Bộ phận kỹ thuật sẽ kiểm tra trong thời gian sớm nhất.",
            NotificationType = "Complaint",
            Priority = "Medium",
            IsRead = false,
            CreatedAt = now.AddHours(-5)
        },
        new NotificationDTO
        {
            NotificationID = -4,
            UserID = currentUserId,
            Title = "Phản ánh đã chuyển sang trạng thái đang xử lý",
            Message = "Phản ánh PA260515-001 của bạn đang được xử lý bởi bộ phận kỹ thuật.",
            NotificationType = "Complaint",
            Priority = "Medium",
            IsRead = true,
            CreatedAt = now.AddDays(-1)
        },
        new NotificationDTO
        {
            NotificationID = -5,
            UserID = currentUserId,
            Title = "Bảo trì thang máy tòa A",
            Message = "Thang máy tòa A sẽ được bảo trì từ 09:00 đến 11:00 ngày 28/05/2026. Cư dân vui lòng sử dụng thang máy còn lại.",
            NotificationType = "Maintenance",
            Priority = "High",
            IsRead = false,
            CreatedAt = now.AddDays(-2)
        },
        new NotificationDTO
        {
            NotificationID = -6,
            UserID = currentUserId,
            Title = "Thông báo cắt nước tạm thời",
            Message = "Khu căn hộ A-0101 có thể bị gián đoạn nước trong khoảng 14:00 - 16:00 để bảo trì đường ống.",
            NotificationType = "Warning",
            Priority = "Critical",
            IsRead = false,
            CreatedAt = now.AddDays(-3)
        },
        new NotificationDTO
        {
            NotificationID = -7,
            UserID = currentUserId,
            Title = "Cập nhật tiện ích sân thể thao",
            Message = "Sân thể thao mở cửa từ 06:00 đến 22:00 hằng ngày. Cư dân có thể đăng ký sử dụng tại quầy lễ tân.",
            NotificationType = "Announcement",
            Priority = "Low",
            IsRead = true,
            CreatedAt = now.AddDays(-4)
        },
        new NotificationDTO
        {
            NotificationID = -8,
            UserID = currentUserId,
            Title = "Thông báo phí gửi xe tháng 05/2026",
            Message = "Phí gửi xe tháng 05/2026 đã được cập nhật. Vui lòng kiểm tra trong mục Hóa đơn của tôi.",
            NotificationType = "Payment",
            Priority = "Medium",
            IsRead = false,
            CreatedAt = now.AddDays(-5)
        },
        new NotificationDTO
        {
            NotificationID = -9,
            UserID = currentUserId,
            Title = "Thông báo vệ sinh khu vực chung",
            Message = "Ban quản lý sẽ tổng vệ sinh khu vực hành lang và thang bộ vào cuối tuần này.",
            NotificationType = "Announcement",
            Priority = "Low",
            IsRead = true,
            CreatedAt = now.AddDays(-6)
        },
        new NotificationDTO
        {
            NotificationID = -10,
            UserID = currentUserId,
            Title = "Cảnh báo an toàn phòng cháy chữa cháy",
            Message = "Cư dân vui lòng không để vật dụng cá nhân tại hành lang, cầu thang thoát hiểm và khu vực kỹ thuật.",
            NotificationType = "Warning",
            Priority = "Critical",
            IsRead = false,
            CreatedAt = now.AddDays(-7)
        }
    };
        }

        List<NotificationDTO> BuildDisplayNotifications()
        {
            return allNotifications
                .Concat(demoNotifications)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        int statW = Math.Max(200, (w - gap * 3) / 4);
        int statH = 104;

        var totalCard = ModernUi.Section("TỔNG THÔNG BÁO", statW, statH);
        totalCard.Location = new Point(x, y);
        page.Controls.Add(totalCard);

        var unreadCard = ModernUi.Section("CHƯA ĐỌC", statW, statH);
        unreadCard.Location = new Point(totalCard.Right + gap, y);
        page.Controls.Add(unreadCard);

        var readCard = ModernUi.Section("ĐÃ ĐỌC", statW, statH);
        readCard.Location = new Point(unreadCard.Right + gap, y);
        page.Controls.Add(readCard);

        var importantCard = ModernUi.Section("QUAN TRỌNG", statW, statH);
        importantCard.Location = new Point(readCard.Right + gap, y);
        page.Controls.Add(importantCard);

        int listW = Math.Max(620, (int)(w * 0.60));
        int detailW = Math.Max(360, w - listW - gap);

        var listPanel = ModernUi.Section("DANH SÁCH THÔNG BÁO", listW, 520);
        listPanel.Location = new Point(x, totalCard.Bottom + 16);
        page.Controls.Add(listPanel);

        var detailPanel = ModernUi.Section("CHI TIẾT THÔNG BÁO", detailW, 520);
        detailPanel.Location = new Point(listPanel.Right + gap, listPanel.Top);
        page.Controls.Add(detailPanel);

        Button filterAll = ModernUi.Button("Tất cả", ModernUi.Blue, 86, 32);
        Button filterUnread = ModernUi.OutlineButton("Chưa đọc", 108, 32);
        Button filterRead = ModernUi.OutlineButton("Đã đọc", 92, 32);
        Button filterPayment = ModernUi.OutlineButton("Hóa đơn", 98, 32);
        Button filterComplaint = ModernUi.OutlineButton("Phản ánh", 104, 32);
        Button filterSystem = ModernUi.OutlineButton("Hệ thống", 104, 32);

        Button[] filterButtons = { filterAll, filterUnread, filterRead, filterPayment, filterComplaint, filterSystem };
        foreach (Button button in filterButtons)
        {
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Font = ModernUi.Font(8.8f, FontStyle.Bold);
        }

        filterAll.Location = new Point(16, 42);
        filterUnread.Location = new Point(filterAll.Right + 8, 42);
        filterRead.Location = new Point(filterUnread.Right + 8, 42);
        filterPayment.Location = new Point(filterRead.Right + 8, 42);
        filterComplaint.Location = new Point(filterPayment.Right + 8, 42);
        filterSystem.Location = new Point(filterComplaint.Right + 8, 42);

        listPanel.Controls.Add(filterAll);
        listPanel.Controls.Add(filterUnread);
        listPanel.Controls.Add(filterRead);
        listPanel.Controls.Add(filterPayment);
        listPanel.Controls.Add(filterComplaint);
        listPanel.Controls.Add(filterSystem);

        string[] gridColumns = { "Mã", "Tiêu đề", "Loại", "Ngày gửi", "Trạng thái" };
        var grid = CreateGrid(gridColumns, new[] { EmptyRow(gridColumns.Length, "Chưa có thông báo") });
        grid.SetBounds(16, 88, listPanel.Width - 32, 330);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ScrollBars = ScrollBars.Both;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        listPanel.Controls.Add(grid);

        Button markAllRead = ModernUi.Button("Đánh dấu toàn bộ đã đọc", ModernUi.Blue, 214, 36);
        markAllRead.Location = new Point(16, listPanel.Height - 54);
        markAllRead.TextAlign = ContentAlignment.MiddleCenter;
        markAllRead.Font = ModernUi.Font(8.8f, FontStyle.Bold);
        listPanel.Controls.Add(markAllRead);

        Button refresh = ModernUi.OutlineButton("Làm mới", 104, 36);
        refresh.Location = new Point(markAllRead.Right + 10, listPanel.Height - 54);
        refresh.TextAlign = ContentAlignment.MiddleCenter;
        refresh.Font = ModernUi.Font(8.8f, FontStyle.Bold);
        listPanel.Controls.Add(refresh);

        Button deleteSelected = ModernUi.OutlineButton("Xóa thông báo", 138, 36);
        deleteSelected.Location = new Point(refresh.Right + 10, listPanel.Height - 54);
        deleteSelected.TextAlign = ContentAlignment.MiddleCenter;
        deleteSelected.Font = ModernUi.Font(8.8f, FontStyle.Bold);
        listPanel.Controls.Add(deleteSelected);

        string GetTitle(NotificationDTO n)
        {
            return Display(n.Title, Display(n.Subject, "Thông báo"));
        }

        string GetBody(NotificationDTO n)
        {
            return Display(n.Message, Display(n.Body, "Không có nội dung chi tiết."));
        }

        string GetTypeValue(NotificationDTO n)
        {
            return Display(n.NotificationType, Display(n.Type, "Other"));
        }

        string GetTypeText(NotificationDTO n)
        {
            string type = GetTypeValue(n);

            if (type.Equals("Payment", StringComparison.OrdinalIgnoreCase))
                return "Hóa đơn";

            if (type.Equals("Complaint", StringComparison.OrdinalIgnoreCase))
                return "Phản ánh";

            if (type.Equals("Maintenance", StringComparison.OrdinalIgnoreCase))
                return "Bảo trì";

            if (type.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                return "Cảnh báo";

            if (type.Equals("Announcement", StringComparison.OrdinalIgnoreCase))
                return "Thông báo";

            return "Hệ thống";
        }

        Color GetTypeColor(NotificationDTO n)
        {
            string type = GetTypeValue(n);

            if (type.Equals("Payment", StringComparison.OrdinalIgnoreCase))
                return ModernUi.Orange;

            if (type.Equals("Complaint", StringComparison.OrdinalIgnoreCase))
                return ModernUi.Teal;

            if (type.Equals("Maintenance", StringComparison.OrdinalIgnoreCase))
                return ModernUi.Blue;

            if (type.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                return ModernUi.Red;

            if (type.Equals("Announcement", StringComparison.OrdinalIgnoreCase))
                return ModernUi.Green;

            return Color.FromArgb(100, 116, 139);
        }

        bool IsImportant(NotificationDTO n)
        {
            string priority = Display(n.Priority, "");
            string type = GetTypeValue(n);

            return priority.Equals("High", StringComparison.OrdinalIgnoreCase)
                || priority.Equals("Critical", StringComparison.OrdinalIgnoreCase)
                || type.Equals("Warning", StringComparison.OrdinalIgnoreCase);
        }

        bool MatchFilter(NotificationDTO n)
        {
            string type = GetTypeValue(n);
            string title = GetTitle(n);
            string body = GetBody(n);

            if (activeFilter == "Unread")
                return !n.IsRead;

            if (activeFilter == "Read")
                return n.IsRead;

            if (activeFilter == "Payment")
                return type.Equals("Payment", StringComparison.OrdinalIgnoreCase)
                    || title.Contains("hóa đơn", StringComparison.OrdinalIgnoreCase)
                    || title.Contains("thanh toán", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("hóa đơn", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("thanh toán", StringComparison.OrdinalIgnoreCase);

            if (activeFilter == "Complaint")
                return type.Equals("Complaint", StringComparison.OrdinalIgnoreCase)
                    || title.Contains("phản ánh", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("phản ánh", StringComparison.OrdinalIgnoreCase);

            if (activeFilter == "System")
                return type.Equals("Other", StringComparison.OrdinalIgnoreCase)
                    || type.Equals("Announcement", StringComparison.OrdinalIgnoreCase)
                    || type.Equals("Maintenance", StringComparison.OrdinalIgnoreCase)
                    || type.Equals("Warning", StringComparison.OrdinalIgnoreCase);

            return true;
        }

        void AddStatValue(Control card, string value, string subtitle, Color color)
        {
            var valueLabel = ModernUi.Label(value, 20f, FontStyle.Bold, color);
            valueLabel.SetBounds(22, 42, card.Width - 44, 32);
            valueLabel.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(valueLabel);

            var subtitleLabel = ModernUi.Label(subtitle, 8.8f, FontStyle.Regular, ModernUi.Muted);
            subtitleLabel.SetBounds(18, 74, card.Width - 36, 22);
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter;
            subtitleLabel.AutoEllipsis = true;
            card.Controls.Add(subtitleLabel);
        }

        void RenderStats()
        {
            totalCard.Controls.Clear();
            unreadCard.Controls.Clear();
            readCard.Controls.Clear();
            importantCard.Controls.Clear();

            var totalTitle = ModernUi.Label("TỔNG THÔNG BÁO", 10f, FontStyle.Bold, ModernUi.Blue);
            totalTitle.SetBounds(18, 12, totalCard.Width - 36, 24);
            totalCard.Controls.Add(totalTitle);

            var unreadTitle = ModernUi.Label("CHƯA ĐỌC", 10f, FontStyle.Bold, ModernUi.Orange);
            unreadTitle.SetBounds(18, 12, unreadCard.Width - 36, 24);
            unreadCard.Controls.Add(unreadTitle);

            var readTitle = ModernUi.Label("ĐÃ ĐỌC", 10f, FontStyle.Bold, ModernUi.Green);
            readTitle.SetBounds(18, 12, readCard.Width - 36, 24);
            readCard.Controls.Add(readTitle);

            var importantTitle = ModernUi.Label("QUAN TRỌNG", 10f, FontStyle.Bold, ModernUi.Red);
            importantTitle.SetBounds(18, 12, importantCard.Width - 36, 24);
            importantCard.Controls.Add(importantTitle);

            AddStatValue(totalCard, allNotifications.Count.ToString("N0"), "Tất cả thông báo", ModernUi.Blue);
            AddStatValue(unreadCard, allNotifications.Count(n => !n.IsRead).ToString("N0"), "Cần xem", ModernUi.Orange);
            AddStatValue(readCard, allNotifications.Count(n => n.IsRead).ToString("N0"), "Đã xử lý", ModernUi.Green);
            AddStatValue(importantCard, allNotifications.Count(IsImportant).ToString("N0"), "Ưu tiên / cảnh báo", ModernUi.Red);
        }

        void SetFilterButtons()
        {
            void Apply(Button button, string key)
            {
                bool active = activeFilter == key;
                button.BackColor = active ? ModernUi.Blue : Color.White;
                button.ForeColor = active ? Color.White : ModernUi.Blue;
                button.FlatAppearance.BorderColor = active ? ModernUi.Blue : Color.FromArgb(203, 213, 225);
            }

            Apply(filterAll, "All");
            Apply(filterUnread, "Unread");
            Apply(filterRead, "Read");
            Apply(filterPayment, "Payment");
            Apply(filterComplaint, "Complaint");
            Apply(filterSystem, "System");
        }

        void RenderDetail(NotificationDTO notification)
        {
            detailPanel.Controls.Clear();

            var header = ModernUi.Label("CHI TIẾT THÔNG BÁO", 10f, FontStyle.Bold, ModernUi.Blue);
            header.SetBounds(18, 16, detailPanel.Width - 36, 24);
            detailPanel.Controls.Add(header);

            if (notification == null)
            {
                var empty = ModernUi.Label("Chọn một thông báo để xem nội dung đầy đủ.", 9.2f, FontStyle.Regular, ModernUi.Muted);
                empty.SetBounds(18, 90, detailPanel.Width - 36, 32);
                empty.TextAlign = ContentAlignment.MiddleCenter;
                detailPanel.Controls.Add(empty);
                return;
            }

            var typeBadge = ModernUi.Badge(GetTypeText(notification), GetTypeColor(notification));
            typeBadge.Location = new Point(18, 52);
            typeBadge.Size = new Size(96, 26);
            detailPanel.Controls.Add(typeBadge);

            var readBadge = ModernUi.Badge(notification.IsRead ? "Đã đọc" : "Chưa đọc", notification.IsRead ? ModernUi.Green : ModernUi.Orange);
            readBadge.Location = new Point(typeBadge.Right + 8, 52);
            readBadge.Size = new Size(96, 26);
            detailPanel.Controls.Add(readBadge);

            var title = ModernUi.Label(GetTitle(notification), 11.2f, FontStyle.Bold, ModernUi.Navy);
            title.SetBounds(18, 92, detailPanel.Width - 36, 52);
            title.AutoEllipsis = true;
            detailPanel.Controls.Add(title);

            var date = ModernUi.Label($"Ngày gửi: {DateTimeText(notification.CreatedAt)}", 8.8f, FontStyle.Regular, ModernUi.Muted);
            date.SetBounds(18, 148, detailPanel.Width - 36, 22);
            detailPanel.Controls.Add(date);

            var bodyTitle = ModernUi.Label("Nội dung đầy đủ", 8.8f, FontStyle.Bold, ModernUi.Text);
            bodyTitle.SetBounds(18, 184, detailPanel.Width - 36, 20);
            detailPanel.Controls.Add(bodyTitle);

            var body = new TextBox
            {
                Text = GetBody(notification),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = ModernUi.Font(9f),
                Location = new Point(18, 208),
                Size = new Size(detailPanel.Width - 36, 150)
            };
            detailPanel.Controls.Add(body);

            Button markRead = notification.IsRead
                ? ModernUi.OutlineButton("Đã đọc", 112, 36)
                : ModernUi.Button("Đánh dấu đã đọc", ModernUi.Blue, 154, 36);

            markRead.Location = new Point(18, 380);
            markRead.Enabled = !notification.IsRead;
            markRead.TextAlign = ContentAlignment.MiddleCenter;
            markRead.Font = ModernUi.Font(8.8f, FontStyle.Bold);

            if (notification.IsRead)
            {
                markRead.ForeColor = ModernUi.Green;
            }

            detailPanel.Controls.Add(markRead);

            var deleteCurrent = ModernUi.OutlineButton("Xóa thông báo", 132, 36);
            deleteCurrent.Location = new Point(markRead.Right + 10, 380);
            deleteCurrent.TextAlign = ContentAlignment.MiddleCenter;
            deleteCurrent.Font = ModernUi.Font(8.8f, FontStyle.Bold);
            detailPanel.Controls.Add(deleteCurrent);

            var clear = ModernUi.OutlineButton("Bỏ chọn", 92, 36);
            clear.Location = new Point(deleteCurrent.Right + 10, 380);
            clear.TextAlign = ContentAlignment.MiddleCenter;
            clear.Font = ModernUi.Font(8.8f, FontStyle.Bold);
            detailPanel.Controls.Add(clear);

            markRead.Click += (_, _) =>
            {
                if (notification.NotificationID > 0)
                {
                    NotificationDAL.MarkAsRead(notification.NotificationID);
                }
                else
                {
                    var demo = demoNotifications.FirstOrDefault(n => n.NotificationID == notification.NotificationID);
                    if (demo != null)
                    {
                        demo.IsRead = true;
                    }
                }

                MessageBox.Show(
                    this,
                    "Đã đánh dấu thông báo này là đã đọc.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                ReloadNotifications(notification.NotificationID);
            };

            deleteCurrent.Click += (_, _) =>
            {
                DialogResult confirm = MessageBox.Show(
                    this,
                    "Bạn có chắc muốn xóa thông báo này không?",
                    "Xóa thông báo",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                {
                    return;
                }

                if (notification.NotificationID > 0)
                {
                    NotificationDAL.DeleteNotification(notification.NotificationID);
                }
                else
                {
                    demoNotifications.RemoveAll(n => n.NotificationID == notification.NotificationID);
                }

                selectedNotification = null;

                MessageBox.Show(
                    this,
                    "Đã xóa thông báo.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                ReloadNotifications();
            };

            clear.Click += (_, _) =>
            {
                selectedNotification = null;
                grid.ClearSelection();
                RenderDetail(null);
            };
        }

        void ReloadNotifications(int? selectedId = null)
        {
            allNotifications = currentUserId > 0
                ? NotificationDAL.GetUserNotifications(currentUserId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList()
                : new List<NotificationDTO>();

            allNotifications = BuildDisplayNotifications();

            filteredNotifications = allNotifications
                .Where(MatchFilter)
                .ToList();

            SetGridData(grid, gridColumns, RowsOrEmpty(filteredNotifications, gridColumns.Length, (n, _) => new object[]
            {
            $"TB{n.NotificationID:00000}",
            GetTitle(n),
            GetTypeText(n),
            DateTimeText(n.CreatedAt),
            n.IsRead ? "Đã đọc" : "Chưa đọc"
            }, "Không có thông báo phù hợp"));

            int[] widths = { 90, 280, 110, 150, 100 };
            for (int i = 0; i < grid.Columns.Count && i < widths.Length; i++)
            {
                grid.Columns[i].Width = widths[i];
                grid.Columns[i].MinimumWidth = widths[i];
                grid.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            if (grid.Columns.Count > 1)
            {
                grid.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            RenderStats();
            SetFilterButtons();

            selectedNotification = selectedId.HasValue
                ? filteredNotifications.FirstOrDefault(n => n.NotificationID == selectedId.Value)
                : filteredNotifications.FirstOrDefault();

            RenderDetail(selectedNotification);

            grid.ClearSelection();

            if (selectedNotification != null)
            {
                int rowIndex = filteredNotifications.FindIndex(n => n.NotificationID == selectedNotification.NotificationID);

                if (rowIndex >= 0 && rowIndex < grid.Rows.Count)
                {
                    grid.Rows[rowIndex].Selected = true;
                    grid.CurrentCell = grid.Rows[rowIndex].Cells[0];
                }
            }
        }

        void ChangeFilter(string filter)
        {
            activeFilter = filter;
            ReloadNotifications();
        }

        filterAll.Click += (_, _) => ChangeFilter("All");
        filterUnread.Click += (_, _) => ChangeFilter("Unread");
        filterRead.Click += (_, _) => ChangeFilter("Read");
        filterPayment.Click += (_, _) => ChangeFilter("Payment");
        filterComplaint.Click += (_, _) => ChangeFilter("Complaint");
        filterSystem.Click += (_, _) => ChangeFilter("System");

        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= filteredNotifications.Count)
            {
                return;
            }

            selectedNotification = filteredNotifications[e.RowIndex];
            RenderDetail(selectedNotification);
        };

        markAllRead.Click += (_, _) =>
        {
            if (currentUserId <= 0)
            {
                return;
            }

            DialogResult confirm = MessageBox.Show(
                this,
                "Bạn có muốn đánh dấu toàn bộ thông báo là đã đọc không?",
                "Đánh dấu toàn bộ đã đọc",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            NotificationDAL.MarkAllAsRead(currentUserId);

            foreach (var demo in demoNotifications)
            {
                demo.IsRead = true;
            }

            MessageBox.Show(
                this,
                "Đã đánh dấu toàn bộ thông báo là đã đọc.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            ReloadNotifications(selectedNotification?.NotificationID);
        };

        refresh.Click += (_, _) =>
        {
            ReloadNotifications(selectedNotification?.NotificationID);
        };

        deleteSelected.Click += (_, _) =>
        {
            if (selectedNotification == null)
            {
                MessageBox.Show(this, "Vui lòng chọn thông báo cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                this,
                "Bạn có chắc muốn xóa thông báo này không?",
                "Xóa thông báo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            if (selectedNotification.NotificationID > 0)
            {
                NotificationDAL.DeleteNotification(selectedNotification.NotificationID);
            }
            else
            {
                demoNotifications.RemoveAll(n => n.NotificationID == selectedNotification.NotificationID);
            }

            selectedNotification = null;

            MessageBox.Show(
                this,
                "Đã xóa thông báo.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            ReloadNotifications();
        };

        page.AutoScrollMinSize = new Size(0, listPanel.Bottom + 90);
        ReloadNotifications();
    }
}

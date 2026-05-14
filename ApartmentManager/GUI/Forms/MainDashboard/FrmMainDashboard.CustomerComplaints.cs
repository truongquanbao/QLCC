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
    private void RenderCustomerComplaints()
    {
        if (IsResident)
        {
            RenderResidentComplaintPortal();
            return;
        }

        RenderStaffComplaintPortal();
    }

    private void RenderStaffComplaintPortal()
    {
        var page = BeginPage("Phản ánh khách hàng", "Vận hành / Phản ánh");
        int w = PageWorkWidth();
        int y = 86;
        const int gap = 12;

        var allComplaints = ComplaintDAL.GetAllComplaints();
        var staffUsers = UserDAL.GetAllUsers()
            .Where(u => u.IsActive && !Display(u.RoleName, "").Contains("Resident", StringComparison.OrdinalIgnoreCase))
            .OrderBy(u => Display(u.FullName, Display(u.Username)))
            .ToList();

        List<dynamic> filteredComplaints = new();
        dynamic selectedComplaint = null;
        var pagination = new PaginationState();

        var actionBar = new Panel
        {
            Location = new Point(18, y - 2),
            Size = new Size(w, 44),
            BackColor = ModernUi.Surface
        };
        page.Controls.Add(actionBar);

        var receiveButton = ModernUi.Button("Tiếp nhận", ModernUi.Blue, 130, 36);
        var assignButton = ModernUi.Button("Phân công cho tôi", ModernUi.Orange, 160, 36);
        var completeButton = ModernUi.Button("Hoàn tất", ModernUi.Green, 120, 36);
        var fullFormButton = ModernUi.OutlineButton("Form đầy đủ", 120, 36);
        actionBar.Controls.Add(receiveButton);
        actionBar.Controls.Add(assignButton);
        actionBar.Controls.Add(completeButton);
        actionBar.Controls.Add(fullFormButton);

        void LayoutActions()
        {
            int right = actionBar.Width;
            fullFormButton.Location = new Point(right - fullFormButton.Width, 4);
            completeButton.Location = new Point(fullFormButton.Left - gap - completeButton.Width, 4);
            assignButton.Location = new Point(completeButton.Left - gap - assignButton.Width, 4);
            receiveButton.Location = new Point(assignButton.Left - gap - receiveButton.Width, 4);
        }

        actionBar.Resize += (_, _) => LayoutActions();
        LayoutActions();

        y += 54;

        var stats = ModernUi.Section("Tổng quan phản ánh", w, 124);
        stats.Location = new Point(18, y);
        page.Controls.Add(stats);

        y += 138;

        var filters = ModernUi.CardPanel();
        filters.Location = new Point(18, y);
        filters.Size = new Size(w, 88);
        page.Controls.Add(filters);

        var statusFilter = AddComplaintCombo(filters, "Trạng thái", ComplaintStatusOptions(includeAll: true), 16, 148);
        var priorityFilter = AddComplaintCombo(filters, "Ưu tiên", ComplaintPriorityOptions(includeAll: true), 178, 130);
        var categoryFilter = AddComplaintCombo(filters, "Loại phản ánh", ComplaintCategoryOptions(includeAll: true), 322, 170);
        var search = ModernUi.SearchBox("Tìm mã, tiêu đề, căn hộ, cư dân...", Math.Max(260, w - 690), 34);
        search.Location = new Point(508, 38);
        filters.Controls.Add(search);
        var searchInput = search.Controls.OfType<TextBox>().FirstOrDefault();
        var resetButton = ModernUi.OutlineButton("Làm mới", 104, 34);
        resetButton.Location = new Point(w - 122, 38);
        filters.Controls.Add(resetButton);

        y += 102;

        int leftW = Math.Max(720, (int)(w * 0.62));
        int rightW = w - leftW - gap;

        var list = ModernUi.Section("Danh sách phản ánh", leftW, 506);
        list.Location = new Point(18, y);
        page.Controls.Add(list);

        var listTitle = list.Controls.OfType<Label>().FirstOrDefault();
        string[] gridColumns = { "Mã phản ánh", "Tiêu đề", "Căn hộ", "Cư dân", "Loại", "Ưu tiên", "Trạng thái", "Ngày gửi" };
        var grid = CreateGrid(gridColumns, new[] { EmptyRow(gridColumns.Length, "Không có phản ánh") });
        grid.Location = new Point(12, 44);
        grid.Size = new Size(list.Width - 24, 372);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.ScrollBars = ScrollBars.Both;
        grid.AllowUserToResizeColumns = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        list.Controls.Add(grid);

        var pager = AddPaginationControls(list, 18, 432, list.Width - 270, 430, list.Width - 80, 430, list.Width - 300);

        var detail = ModernUi.Section("Chi tiết xử lý", rightW, 506);
        detail.Location = new Point(list.Right + gap, y);
        detail.AutoScroll = true;
        page.Controls.Add(detail);

        ComboBox AddComplaintCombo(Control parent, string label, (string Text, string Value)[] options, int x, int width)
        {
            var lbl = ModernUi.Label(label, 8.4f, FontStyle.Bold, ModernUi.Text);
            lbl.SetBounds(x, 14, width, 18);
            parent.Controls.Add(lbl);

            var combo = ModernUi.ComboBox(Array.Empty<string>(), width);
            combo.Items.Clear();
            foreach (var option in options)
            {
                combo.Items.Add(new UiComboItem(option.Text, option.Value));
            }
            combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
            combo.SetBounds(x, 38, width, 30);
            parent.Controls.Add(combo);
            return combo;
        }

        void RefreshStats()
        {
            foreach (Control child in stats.Controls.Cast<Control>().Skip(1).ToList())
            {
                stats.Controls.Remove(child);
                child.Dispose();
            }

            int cardW = Math.Max(150, (stats.Width - 72) / 5);
            AddMiniComplaintStat(stats, "Tổng phản ánh", allComplaints.Count, ModernUi.Blue, 16, 44, cardW);
            AddMiniComplaintStat(stats, "Mới", allComplaints.Count(c => ComplaintStatusValue(c) is "New" or "Open"), ModernUi.Orange, 28 + cardW, 44, cardW);
            AddMiniComplaintStat(stats, "Đang xử lý", allComplaints.Count(c => ComplaintStatusValue(c) == "InProgress"), ModernUi.Blue, 40 + cardW * 2, 44, cardW);
            AddMiniComplaintStat(stats, "Đã xử lý", allComplaints.Count(c => ComplaintStatusValue(c) is "Resolved" or "Closed"), ModernUi.Green, 52 + cardW * 3, 44, cardW);
            AddMiniComplaintStat(stats, "Ưu tiên cao", allComplaints.Count(c => ComplaintPriorityValue(c) is "High" or "Critical"), ModernUi.Red, 64 + cardW * 4, 44, cardW);
        }

        void BindGrid()
        {
            var pageRows = Paginate(filteredComplaints, pagination);
            SetGridData(
                grid,
                gridColumns,
                RowsOrEmpty(pageRows, gridColumns.Length, (complaint, _) => new object[]
                {
                    ComplaintCode(complaint),
                    GetDynamicString(complaint, "Title"),
                    GetDynamicString(complaint, "ApartmentCode"),
                    GetDynamicString(complaint, "ResidentName", "FullName"),
                    ComplaintCategoryText(GetDynamicString(complaint, "Category")),
                    ViStatus(ComplaintPriorityValue(complaint)),
                    ViStatus(ComplaintStatusValue(complaint)),
                    DateTimeText(GetDynamicDate(complaint, "CreatedAt"))
                }, "Không có phản ánh phù hợp"));

            int[] widths = { 118, 210, 86, 150, 128, 92, 112, 132 };
            for (int i = 0; i < grid.Columns.Count && i < widths.Length; i++)
            {
                grid.Columns[i].Width = widths[i];
                grid.Columns[i].MinimumWidth = widths[i];
                grid.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            if (grid.Columns.Count > 3)
            {
                grid.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                grid.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            UpdatePaginationControls(pagination, pager, "phản ánh");
            if (listTitle != null)
            {
                listTitle.Text = $"DANH SÁCH PHẢN ÁNH ({filteredComplaints.Count:N0})";
            }
        }

        bool MatchesFilter(dynamic complaint)
        {
            string status = ComboBoxHelper.GetSelectedValueString(statusFilter);
            string priority = ComboBoxHelper.GetSelectedValueString(priorityFilter);
            string category = ComboBoxHelper.GetSelectedValueString(categoryFilter);
            string keyword = searchInput?.Text.Trim() ?? string.Empty;

            if (status != "All" && !string.Equals(NormalizeComplaintStatus(ComplaintStatusValue(complaint)), status, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (priority != "All" && !string.Equals(ComplaintPriorityValue(complaint), priority, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (category != "All" && !string.Equals(GetDynamicString(complaint, "Category"), category, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (keyword.Length == 0)
            {
                return true;
            }

            string haystack = string.Join(" ", new[]
            {
                ComplaintCode(complaint),
                GetDynamicString(complaint, "Title"),
                GetDynamicString(complaint, "Description"),
                GetDynamicString(complaint, "ApartmentCode"),
                GetDynamicString(complaint, "ResidentName", "FullName"),
                ComplaintCategoryText(GetDynamicString(complaint, "Category"))
            });

            return haystack.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        void ApplyFilters(int? preferredComplaintId = null)
        {
            filteredComplaints = allComplaints
                .Where(MatchesFilter)
                .OrderByDescending(c => ComplaintPriorityRank(ComplaintPriorityValue(c)))
                .ThenByDescending(c => GetDynamicDate(c, "CreatedAt") ?? DateTime.MinValue)
                .ToList();

            if (preferredComplaintId.HasValue)
            {
                int index = filteredComplaints.FindIndex(c => GetDynamicInt(c, "ComplaintID") == preferredComplaintId.Value);
                pagination.CurrentPage = FindPageForIndex(index, pagination.PageSize);
            }

            BindGrid();

            dynamic nextSelected = null;
            if (preferredComplaintId.HasValue)
            {
                nextSelected = filteredComplaints.FirstOrDefault(c => GetDynamicInt(c, "ComplaintID") == preferredComplaintId.Value);
            }

            nextSelected ??= filteredComplaints.Skip(pagination.StartIndex).FirstOrDefault();
            SelectComplaint(nextSelected);
            RefreshStats();
        }

        void SelectComplaint(dynamic complaint)
        {
            selectedComplaint = complaint;
            RenderStaffComplaintDetail(detail, (object)selectedComplaint, staffUsers, (Action)(() =>
            {
                int id = selectedComplaint == null ? 0 : GetDynamicInt(selectedComplaint, "ComplaintID");
                allComplaints = ComplaintDAL.GetAllComplaints();
                ApplyFilters(id > 0 ? id : null);
            }));

            grid.ClearSelection();
            if (complaint == null)
            {
                return;
            }

            int selectedId = GetDynamicInt(complaint, "ComplaintID");
            int rowIndex = filteredComplaints.Skip(pagination.StartIndex).Take(pagination.PageSize).ToList()
                .FindIndex(c => GetDynamicInt(c, "ComplaintID") == selectedId);
            if (rowIndex >= 0 && rowIndex < grid.Rows.Count)
            {
                grid.Rows[rowIndex].Selected = true;
                grid.CurrentCell = grid.Rows[rowIndex].Cells[0];
            }
        }

        void UpdateSelected(string status, bool assignToCurrentUser = false)
        {
            if (selectedComplaint == null)
            {
                MessageBox.Show(this, "Vui lòng chọn một phản ánh.", "Phản ánh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int complaintId = GetDynamicInt(selectedComplaint, "ComplaintID");
            if (assignToCurrentUser && _session?.UserID > 0)
            {
                ComplaintBLL.AssignComplaint(complaintId, _session.UserID);
            }

            var result = ComplaintBLL.UpdateComplaintStatus(complaintId, status);
            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, "Phản ánh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            allComplaints = ComplaintDAL.GetAllComplaints();
            ApplyFilters(complaintId);
        }

        statusFilter.SelectedIndexChanged += (_, _) => { pagination.MoveToFirstPage(); ApplyFilters(); };
        priorityFilter.SelectedIndexChanged += (_, _) => { pagination.MoveToFirstPage(); ApplyFilters(); };
        categoryFilter.SelectedIndexChanged += (_, _) => { pagination.MoveToFirstPage(); ApplyFilters(); };
        if (searchInput != null)
        {
            searchInput.TextChanged += (_, _) => { pagination.MoveToFirstPage(); ApplyFilters(); };
        }
        resetButton.Click += (_, _) =>
        {
            statusFilter.SelectedIndex = 0;
            priorityFilter.SelectedIndex = 0;
            categoryFilter.SelectedIndex = 0;
            searchInput?.Clear();
            pagination.MoveToFirstPage();
            allComplaints = ComplaintDAL.GetAllComplaints();
            ApplyFilters();
        };

        grid.CellClick += (_, e) =>
        {
            int absoluteIndex = pagination.StartIndex + e.RowIndex;
            if (e.RowIndex >= 0 && absoluteIndex >= 0 && absoluteIndex < filteredComplaints.Count)
            {
                SelectComplaint(filteredComplaints[absoluteIndex]);
            }
        };

        pager.FirstButton.Click += (_, _) => { pagination.MoveToFirstPage(); ApplyFilters(); };
        pager.PreviousButton.Click += (_, _) => { pagination.MoveToPreviousPage(); ApplyFilters(); };
        pager.NextButton.Click += (_, _) => { pagination.MoveToNextPage(); ApplyFilters(); };
        pager.LastButton.Click += (_, _) => { pagination.MoveToLastPage(); ApplyFilters(); };
        pager.PageSizeCombo.SelectedIndexChanged += (_, _) =>
        {
            pagination.SetPageSize(ParsePageSize(pager.PageSizeCombo.SelectedItem));
            ApplyFilters();
        };

        receiveButton.Click += (_, _) => UpdateSelected("InProgress", assignToCurrentUser: true);
        assignButton.Click += (_, _) =>
        {
            if (selectedComplaint == null || _session?.UserID <= 0)
            {
                MessageBox.Show(this, "Vui lòng chọn phản ánh cần phân công.", "Phân công", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int complaintId = GetDynamicInt(selectedComplaint, "ComplaintID");
            var result = ComplaintBLL.AssignComplaint(complaintId, _session.UserID);
            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, "Phân công", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            allComplaints = ComplaintDAL.GetAllComplaints();
            ApplyFilters(complaintId);
        };
        completeButton.Click += (_, _) => UpdateSelected("Resolved");
        fullFormButton.Click += (_, _) => OpenManagementDialog<FrmComplaintManagement>();

        page.AutoScroll = true;
        page.AutoScrollMinSize = new Size(0, y + 560);
        ApplyFilters();
    }

    private void RenderStaffComplaintDetail(Control parent, object complaint, IReadOnlyList<UserDTO> staffUsers, Action refresh)
    {
        parent.Controls.Clear();
        if (complaint == null)
        {
            var empty = ModernUi.Label("Chưa có phản ánh để hiển thị.", 10f, FontStyle.Regular, ModernUi.Muted);
            empty.SetBounds(18, 48, parent.Width - 36, 28);
            parent.Controls.Add(empty);
            return;
        }

        int complaintId = GetDynamicInt(complaint, "ComplaintID");
        var code = ModernUi.Label(ComplaintCode(complaint), 10.5f, FontStyle.Bold, ModernUi.Blue);
        code.SetBounds(18, 42, parent.Width - 36, 24);
        parent.Controls.Add(code);

        var title = ModernUi.Label(GetDynamicString(complaint, "Title"), 11.4f, FontStyle.Bold, ModernUi.Navy);
        title.SetBounds(18, 70, parent.Width - 36, 48);
        title.AutoEllipsis = true;
        parent.Controls.Add(title);

        AddComplaintInfo(parent, "Cư dân", GetDynamicString(complaint, "ResidentName", "FullName"), 18, 124, parent.Width / 2 - 28);
        AddComplaintInfo(parent, "Căn hộ", GetDynamicString(complaint, "ApartmentCode"), parent.Width / 2 + 8, 124, parent.Width / 2 - 26);
        AddComplaintInfo(parent, "Loại", ComplaintCategoryText(GetDynamicString(complaint, "Category")), 18, 174, parent.Width / 2 - 28);
        AddComplaintInfo(parent, "Ưu tiên", ViStatus(ComplaintPriorityValue(complaint)), parent.Width / 2 + 8, 174, parent.Width / 2 - 26);

        var statusBadge = ModernUi.Badge(ViStatus(ComplaintStatusValue(complaint)), ComplaintStatusColor(ComplaintStatusValue(complaint)));
        statusBadge.SetBounds(18, 224, Math.Min(150, parent.Width - 36), 28);
        parent.Controls.Add(statusBadge);

        AddComplaintInfo(parent, "Ngày gửi", DateTimeText(GetDynamicDate(complaint, "CreatedAt")), parent.Width / 2 + 8, 222, parent.Width / 2 - 26);

        var descLabel = ModernUi.Label("Nội dung", 8.6f, FontStyle.Bold, ModernUi.Text);
        descLabel.SetBounds(18, 266, parent.Width - 36, 18);
        parent.Controls.Add(descLabel);

        var description = new TextBox
        {
            Text = GetDynamicString(complaint, "Description"),
            Location = new Point(18, 288),
            Size = new Size(parent.Width - 36, 72),
            Multiline = true,
            ReadOnly = true,
            Font = ModernUi.Font(8.7f),
            ScrollBars = ScrollBars.Vertical
        };
        parent.Controls.Add(description);

        var statusCombo = AddComplaintEditCombo(parent, "Trạng thái", ComplaintStatusOptions(includeAll: false), NormalizeComplaintStatus(ComplaintStatusValue(complaint)), 18, 374, parent.Width / 2 - 28);
        var staffCombo = AddComplaintEditCombo(parent, "Người xử lý", staffUsers
            .Select(u => (Display(u.FullName, Display(u.Username)), u.UserID.ToString(CultureInfo.InvariantCulture)))
            .Prepend(("Chưa phân công", "0"))
            .ToArray(), (GetDynamicInt(complaint, "AssignedToUserID")).ToString(CultureInfo.InvariantCulture), parent.Width / 2 + 8, 374, parent.Width / 2 - 26);

        var noteLabel = ModernUi.Label("Phản hồi xử lý", 8.6f, FontStyle.Bold, ModernUi.Text);
        noteLabel.SetBounds(18, 430, parent.Width - 36, 18);
        parent.Controls.Add(noteLabel);

        var note = new TextBox
        {
            Text = GetDynamicString(complaint, "ResolutionNotes", "ResolutionNote", "Note"),
            Location = new Point(18, 452),
            Size = new Size(parent.Width - 36, 74),
            Multiline = true,
            Font = ModernUi.Font(8.7f),
            ScrollBars = ScrollBars.Vertical
        };
        parent.Controls.Add(note);

        int buttonY = 540;
        var save = ModernUi.Button("Lưu xử lý", ModernUi.Blue, 112, 32);
        save.Location = new Point(18, buttonY);
        parent.Controls.Add(save);

        var resolve = ModernUi.Button("Hoàn tất", ModernUi.Green, 104, 32);
        resolve.Location = new Point(save.Right + 10, buttonY);
        parent.Controls.Add(resolve);

        var attachment = GetDynamicString(complaint, "ImageAttachmentPath");
        if (attachment != "-")
        {
            var attachmentLabel = ModernUi.Label($"Ảnh: {Path.GetFileName(attachment)}", 8.3f, FontStyle.Regular, ModernUi.Muted);
            attachmentLabel.SetBounds(18, buttonY + 42, parent.Width - 36, 22);
            parent.Controls.Add(attachmentLabel);
        }

        save.Click += (_, _) =>
        {
            int parsedUserId;
            int assignedTo = int.TryParse(ComboBoxHelper.GetSelectedValueString(staffCombo), out parsedUserId) ? parsedUserId : 0;
            string status = ComboBoxHelper.GetSelectedValueString(statusCombo);
            if (assignedTo > 0)
            {
                ComplaintBLL.AssignComplaint(complaintId, assignedTo);
            }

            var result = status == "Resolved"
                ? ComplaintBLL.ResolveComplaint(complaintId, note.Text.Trim())
                : ComplaintBLL.UpdateComplaintStatus(complaintId, status);

            if (!result.Success)
            {
                MessageBox.Show(parent.FindForm(), result.Message, "Phản ánh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            refresh();
        };

        resolve.Click += (_, _) =>
        {
            var result = ComplaintBLL.ResolveComplaint(complaintId, note.Text.Trim());
            if (!result.Success)
            {
                MessageBox.Show(parent.FindForm(), result.Message, "Hoàn tất phản ánh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            refresh();
        };
    }

    private void RenderResidentComplaintPortal()
    {
        var page = BeginPage("Gửi phản ánh", "Cư dân / Phản ánh");
        int w = PageWorkWidth();
        int y = 86;
        const int gap = 12;

        var resident = GetCurrentResident();
        if (resident == null)
        {
            var empty = ModernUi.Section("Không tìm thấy hồ sơ cư dân", w, 180);
            empty.Location = new Point(18, y);
            var message = ModernUi.Label("Tài khoản hiện tại chưa liên kết với hồ sơ cư dân nên chưa thể gửi phản ánh.", 10f, FontStyle.Regular, ModernUi.Text);
            message.SetBounds(22, 54, empty.Width - 44, 32);
            empty.Controls.Add(message);
            page.Controls.Add(empty);
            return;
        }

        int formW = Math.Max(430, (int)(w * 0.40));
        int listW = w - formW - gap;

        var form = ModernUi.Section("Gửi phản ánh mới", formW, 506);
        form.Location = new Point(18, y);
        page.Controls.Add(form);

        var list = ModernUi.Section("Phản ánh của tôi", listW, 506);
        list.Location = new Point(form.Right + gap, y);
        page.Controls.Add(list);

        var category = AddComplaintEditCombo(form, "Loại phản ánh", ComplaintCategoryOptions(includeAll: false), "General", 18, 48, form.Width - 36);
        var priority = AddComplaintEditCombo(form, "Mức ưu tiên", ComplaintPriorityOptions(includeAll: false), "Medium", 18, 104, form.Width - 36);
        var title = AddComplaintTextBox(form, "Tiêu đề", "", 18, 160, form.Width - 36, multiline: false);
        var description = AddComplaintTextBox(form, "Nội dung chi tiết", "", 18, 216, form.Width - 36, multiline: true);

        var attachmentLabel = ModernUi.Label("Ảnh đính kèm", 8.6f, FontStyle.Bold, ModernUi.Text);
        attachmentLabel.SetBounds(18, 348, form.Width - 36, 18);
        form.Controls.Add(attachmentLabel);

        var attachment = ModernUi.TextBox("Đường dẫn ảnh minh chứng...", form.Width - 154);
        attachment.SetBounds(18, 370, form.Width - 154, 30);
        form.Controls.Add(attachment);

        var browse = ModernUi.OutlineButton("Chọn ảnh", 104, 30);
        browse.Location = new Point(attachment.Right + 8, 370);
        form.Controls.Add(browse);

        var submit = ModernUi.Button("Gửi phản ánh", ModernUi.Blue, 132, 34);
        submit.Location = new Point(18, 426);
        form.Controls.Add(submit);

        var clear = ModernUi.OutlineButton("Xóa nhập liệu", 124, 34);
        clear.Location = new Point(submit.Right + 10, 426);
        form.Controls.Add(clear);

        var mine = ComplaintDAL.GetComplaintsByResident(resident.ResidentID);
        var minePagination = new PaginationState();
        var myGridColumns = new[] { "Mã phản ánh", "Tiêu đề", "Loại", "Ưu tiên", "Trạng thái", "Ngày gửi" };
        var myGrid = CreateGrid(myGridColumns, new[] { EmptyRow(myGridColumns.Length, "Bạn chưa có phản ánh") });
        myGrid.Location = new Point(12, 44);
        myGrid.Size = new Size(list.Width - 24, 320);
        myGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        myGrid.ScrollBars = ScrollBars.Both;
        list.Controls.Add(myGrid);
        var pager = AddPaginationControls(list, 18, 382, list.Width - 270, 380, list.Width - 80, 380, list.Width - 300);

        var detail = ModernUi.Section("Theo dõi xử lý", w, 256);
        detail.Location = new Point(18, form.Bottom + 14);
        page.Controls.Add(detail);

        void ReloadMine(int? selectedId = null)
        {
            mine = ComplaintDAL.GetComplaintsByResident(resident.ResidentID);
            if (selectedId.HasValue)
            {
                int index = mine.FindIndex(c => GetDynamicInt(c, "ComplaintID") == selectedId.Value);
                minePagination.CurrentPage = FindPageForIndex(index, minePagination.PageSize);
            }

            var pageRows = Paginate(mine, minePagination);
            SetGridData(myGrid, myGridColumns, RowsOrEmpty(pageRows, myGridColumns.Length, (complaint, _) => new object[]
            {
                ComplaintCode(complaint),
                GetDynamicString(complaint, "Title"),
                ComplaintCategoryText(GetDynamicString(complaint, "Category")),
                ViStatus(ComplaintPriorityValue(complaint)),
                ViStatus(ComplaintStatusValue(complaint)),
                DateTimeText(GetDynamicDate(complaint, "CreatedAt"))
            }, "Bạn chưa có phản ánh"));

            int[] widths = { 120, 250, 130, 96, 112, 136 };
            for (int i = 0; i < myGrid.Columns.Count && i < widths.Length; i++)
            {
                myGrid.Columns[i].Width = widths[i];
                myGrid.Columns[i].MinimumWidth = widths[i];
            }

            UpdatePaginationControls(minePagination, pager, "phản ánh");
            dynamic selected = selectedId.HasValue
                ? mine.FirstOrDefault(c => GetDynamicInt(c, "ComplaintID") == selectedId.Value)
                : mine.Skip(minePagination.StartIndex).FirstOrDefault();
            RenderResidentComplaintDetail(detail, selected);
        }

        browse.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Chọn ảnh phản ánh",
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                attachment.Text = dialog.FileName;
            }
        };

        clear.Click += (_, _) =>
        {
            title.Clear();
            description.Clear();
            attachment.Clear();
            category.SelectedIndex = 0;
            priority.SelectedIndex = 1;
        };

        submit.Click += (_, _) =>
        {
            var result = ComplaintBLL.CreateComplaint(
                resident.ResidentID,
                title.Text.Trim(),
                description.Text.Trim(),
                ComboBoxHelper.GetSelectedValueString(category),
                ComboBoxHelper.GetSelectedValueString(priority),
                attachment.Text.Trim());

            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, "Gửi phản ánh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(this, "Đã gửi phản ánh thành công.", "Gửi phản ánh", MessageBoxButtons.OK, MessageBoxIcon.Information);
            title.Clear();
            description.Clear();
            attachment.Clear();
            ReloadMine(result.ComplaintID);
        };

        myGrid.CellClick += (_, e) =>
        {
            int absoluteIndex = minePagination.StartIndex + e.RowIndex;
            if (e.RowIndex >= 0 && absoluteIndex >= 0 && absoluteIndex < mine.Count)
            {
                RenderResidentComplaintDetail(detail, mine[absoluteIndex]);
            }
        };

        pager.FirstButton.Click += (_, _) => { minePagination.MoveToFirstPage(); ReloadMine(); };
        pager.PreviousButton.Click += (_, _) => { minePagination.MoveToPreviousPage(); ReloadMine(); };
        pager.NextButton.Click += (_, _) => { minePagination.MoveToNextPage(); ReloadMine(); };
        pager.LastButton.Click += (_, _) => { minePagination.MoveToLastPage(); ReloadMine(); };
        pager.PageSizeCombo.SelectedIndexChanged += (_, _) =>
        {
            minePagination.SetPageSize(ParsePageSize(pager.PageSizeCombo.SelectedItem));
            ReloadMine();
        };

        page.AutoScroll = true;
        page.AutoScrollMinSize = new Size(0, detail.Bottom + 80);
        ReloadMine();
    }

    private void RenderResidentComplaintDetail(Control parent, dynamic complaint)
    {
        parent.Controls.Clear();
        if (complaint == null)
        {
            var empty = ModernUi.Label("Chọn một phản ánh để theo dõi tình trạng xử lý.", 10f, FontStyle.Regular, ModernUi.Muted);
            empty.SetBounds(18, 50, parent.Width - 36, 28);
            parent.Controls.Add(empty);
            return;
        }

        int colW = (parent.Width - 60) / 3;
        AddComplaintInfo(parent, "Mã phản ánh", ComplaintCode(complaint), 18, 48, colW);
        AddComplaintInfo(parent, "Trạng thái", ViStatus(ComplaintStatusValue(complaint)), 30 + colW, 48, colW);
        AddComplaintInfo(parent, "Người xử lý", GetDynamicString(complaint, "AssignedToName", "AssignedTo"), 42 + colW * 2, 48, colW);

        var content = new TextBox
        {
            Text = GetDynamicString(complaint, "Description"),
            Location = new Point(18, 106),
            Size = new Size((parent.Width - 54) / 2, 96),
            Multiline = true,
            ReadOnly = true,
            Font = ModernUi.Font(8.7f),
            ScrollBars = ScrollBars.Vertical
        };
        parent.Controls.Add(content);

        var reply = new TextBox
        {
            Text = GetDynamicString(complaint, "ResolutionNotes", "ResolutionNote"),
            Location = new Point(content.Right + 18, 106),
            Size = new Size(parent.Width - content.Right - 36, 96),
            Multiline = true,
            ReadOnly = true,
            Font = ModernUi.Font(8.7f),
            ScrollBars = ScrollBars.Vertical
        };
        parent.Controls.Add(reply);

        var leftLabel = ModernUi.Label("Nội dung đã gửi", 8.6f, FontStyle.Bold, ModernUi.Text);
        leftLabel.SetBounds(content.Left, 84, content.Width, 18);
        parent.Controls.Add(leftLabel);

        var rightLabel = ModernUi.Label("Phản hồi từ ban quản lý", 8.6f, FontStyle.Bold, ModernUi.Text);
        rightLabel.SetBounds(reply.Left, 84, reply.Width, 18);
        parent.Controls.Add(rightLabel);
    }

    private static ComboBox AddComplaintEditCombo(Control parent, string label, (string Text, string Value)[] options, string selectedValue, int x, int y, int width)
    {
        var lbl = ModernUi.Label(label, 8.6f, FontStyle.Bold, ModernUi.Text);
        lbl.SetBounds(x, y, width, 18);
        parent.Controls.Add(lbl);

        var combo = ModernUi.ComboBox(Array.Empty<string>(), width);
        combo.Items.Clear();
        foreach (var option in options)
        {
            combo.Items.Add(new UiComboItem(option.Text, option.Value));
        }
        combo.SetBounds(x, y + 22, width, 30);
        parent.Controls.Add(combo);
        ComboBoxHelper.SelectValue(combo, selectedValue);
        if (combo.SelectedIndex < 0 && combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }

        return combo;
    }

    private static TextBox AddComplaintTextBox(Control parent, string label, string value, int x, int y, int width, bool multiline)
    {
        var lbl = ModernUi.Label(label, 8.6f, FontStyle.Bold, ModernUi.Text);
        lbl.SetBounds(x, y, width, 18);
        parent.Controls.Add(lbl);

        var input = ModernUi.TextBox("", width);
        input.Text = value;
        input.Multiline = multiline;
        input.SetBounds(x, y + 22, width, multiline ? 108 : 30);
        parent.Controls.Add(input);
        return input;
    }

    private static void AddComplaintInfo(Control parent, string label, string value, int x, int y, int width)
    {
        var caption = ModernUi.Label(label, 8.2f, FontStyle.Bold, ModernUi.Muted);
        caption.SetBounds(x, y, width, 16);
        parent.Controls.Add(caption);

        var text = ModernUi.Label(Display(value), 9.2f, FontStyle.Bold, ModernUi.Text);
        text.SetBounds(x, y + 18, width, 24);
        text.AutoEllipsis = true;
        parent.Controls.Add(text);
    }

    private static void AddMiniComplaintStat(Control parent, string title, int value, Color color, int x, int y, int width)
    {
        var card = ModernUi.CardPanel(6);
        card.SetBounds(x, y, width, 58);

        var valueLabel = ModernUi.Label(value.ToString("N0"), 14f, FontStyle.Bold, color);
        valueLabel.SetBounds(10, 6, width - 20, 22);
        card.Controls.Add(valueLabel);

        var titleLabel = ModernUi.Label(title, 8.2f, FontStyle.Bold, ModernUi.Text);
        titleLabel.SetBounds(10, 30, width - 20, 20);
        titleLabel.AutoEllipsis = true;
        card.Controls.Add(titleLabel);

        parent.Controls.Add(card);
    }

    private ResidentDTO GetCurrentResident()
    {
        if (_session?.UserID > 0)
        {
            var resident = ResidentDAL.GetResidentByUserID(_session.UserID);
            if (resident != null)
            {
                return resident;
            }
        }

        string displayName = CurrentDisplayName();
        return ResidentDAL.GetAllResidents()
            .FirstOrDefault(r =>
                string.Equals(Display(r.Username, ""), CurrentUsername(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Display(r.FullName, ""), displayName, StringComparison.OrdinalIgnoreCase));
    }

    private static string ComplaintCode(dynamic complaint)
    {
        DateTime createdAt = GetDynamicDate(complaint, "CreatedAt") ?? DateTime.Today;
        return $"PA{createdAt:yyMMdd}-{GetDynamicInt(complaint, "ComplaintID"):000}";
    }

    private static string ComplaintStatusValue(dynamic complaint)
        => NormalizeComplaintStatus(GetDynamicString(complaint, "Status"));

    private static string ComplaintPriorityValue(dynamic complaint)
    {
        string priority = GetDynamicString(complaint, "Priority");
        return priority == "-" ? "Medium" : priority;
    }

    private static string NormalizeComplaintStatus(string status)
    {
        return (status ?? string.Empty).Trim() switch
        {
            "Open" => "New",
            "In Progress" => "InProgress",
            "" or "-" => "New",
            var other => other
        };
    }

    private static int ComplaintPriorityRank(string priority)
    {
        return priority switch
        {
            "Critical" => 4,
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => 0
        };
    }

    private static string ComplaintCategoryText(string category)
    {
        return (category ?? string.Empty).Trim() switch
        {
            "Elevator" => "Thang máy",
            "Water" => "Nước / ống",
            "Electrical" => "Điện chiếu sáng",
            "Security" => "An ninh",
            "Cleaning" => "Vệ sinh",
            "Parking" => "Gửi xe",
            "Facility" => "Cơ sở vật chất",
            "Garden" => "Cây xanh",
            "Internet" => "Internet",
            "AccessCard" => "Thẻ ra vào",
            "Billing" or "Payment" => "Hóa đơn / phí",
            "Maintenance" => "Bảo trì",
            "Noise" => "Tiếng ồn",
            "General" or "" or "-" => "Chung",
            var other => other
        };
    }

    private static (string Text, string Value)[] ComplaintStatusOptions(bool includeAll)
    {
        var options = new List<(string Text, string Value)>();
        if (includeAll)
        {
            options.Add(("Tất cả", "All"));
        }
        options.AddRange(new[]
        {
            ("Mới", "New"),
            ("Đang xử lý", "InProgress"),
            ("Đã xử lý", "Resolved"),
            ("Đã đóng", "Closed")
        });
        return options.ToArray();
    }

    private static (string Text, string Value)[] ComplaintPriorityOptions(bool includeAll)
    {
        var options = new List<(string Text, string Value)>();
        if (includeAll)
        {
            options.Add(("Tất cả", "All"));
        }
        options.AddRange(new[]
        {
            ("Thấp", "Low"),
            ("Trung bình", "Medium"),
            ("Cao", "High"),
            ("Khẩn cấp", "Critical")
        });
        return options.ToArray();
    }

    private static (string Text, string Value)[] ComplaintCategoryOptions(bool includeAll)
    {
        var options = new List<(string Text, string Value)>();
        if (includeAll)
        {
            options.Add(("Tất cả", "All"));
        }
        options.AddRange(new[]
        {
            ("Chung", "General"),
            ("Bảo trì", "Maintenance"),
            ("Thang máy", "Elevator"),
            ("Nước / ống", "Water"),
            ("Điện chiếu sáng", "Electrical"),
            ("An ninh", "Security"),
            ("Vệ sinh", "Cleaning"),
            ("Gửi xe", "Parking"),
            ("Hóa đơn / phí", "Billing"),
            ("Cơ sở vật chất", "Facility")
        });
        return options.ToArray();
    }

    private static Color ComplaintStatusColor(string status)
    {
        return NormalizeComplaintStatus(status) switch
        {
            "New" => ModernUi.Orange,
            "InProgress" => ModernUi.Blue,
            "Resolved" => ModernUi.Green,
            "Closed" => ModernUi.Muted,
            _ => ModernUi.Muted
        };
    }

    private static int GetDynamicInt(dynamic obj, string name, int fallback = 0)
    {
        if (obj == null) return fallback;

        var prop = obj.GetType().GetProperty(name);
        if (prop == null) return fallback;

        object value = prop.GetValue(obj);
        if (value == null) return fallback;
        if (value is int intValue) return intValue;

        return int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : fallback;
    }

    private static DateTime? GetDynamicDate(dynamic obj, string name)
    {
        if (obj == null) return null;

        var prop = obj.GetType().GetProperty(name);
        if (prop == null) return null;

        object value = prop.GetValue(obj);
        if (value is DateTime date) return date;

        return DateTime.TryParse(value?.ToString(), CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed
            : null;
    }
}

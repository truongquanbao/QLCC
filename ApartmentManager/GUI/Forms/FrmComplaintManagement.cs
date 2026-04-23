using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public class FrmComplaintManagement : Form
{
    private const int PRIMARY_COLOR = 0x215689;
    private const int SPACING = 10;
    private const int LABEL_WIDTH = 120;
    private const int CONTROL_WIDTH = 200;

    private readonly UserSession? _session;

    private readonly ComboBox cmbFilterStatus = new ComboBox();
    private readonly ComboBox cmbFilterPriority = new ComboBox();
    private readonly Button btnSearch = new Button();
    private readonly Button btnShowAll = new Button();

    private readonly ComboBox cmbResident = new ComboBox();
    private readonly TextBox txtTitle = new TextBox();
    private readonly TextBox txtDescription = new TextBox();
    private readonly ComboBox cmbPriority = new ComboBox();
    private readonly ComboBox cmbStatus = new ComboBox();
    private readonly ComboBox cmbAssignedTo = new ComboBox();
    private readonly TextBox txtResolutionNote = new TextBox();
    private readonly DateTimePicker dtpReportDate = new DateTimePicker();
    private readonly DateTimePicker dtpCompletionDate = new DateTimePicker();

    private readonly Label lblResident = new Label();
    private readonly Label lblTitle = new Label();
    private readonly Label lblDescription = new Label();
    private readonly Label lblPriority = new Label();
    private readonly Label lblStatus = new Label();
    private readonly Label lblAssignedTo = new Label();
    private readonly Label lblResolutionNote = new Label();
    private readonly Label lblReportDate = new Label();
    private readonly Label lblCompletionDate = new Label();

    private readonly DataGridView dgvComplaints = new DataGridView();
    private readonly Button btnCreate = new Button();
    private readonly Button btnEdit = new Button();
    private readonly Button btnAssign = new Button();
    private readonly Button btnResolve = new Button();
    private readonly Button btnDelete = new Button();
    private readonly Button btnStatistics = new Button();
    private readonly Button btnClose = new Button();

    private readonly Label lblStatusBar = new Label();
    private readonly Label lblComplaintInfo = new Label();

    private int _selectedComplaintID;
    private List<dynamic> _staffList = new List<dynamic>();

    public FrmComplaintManagement()
    {
        _session = SessionManager.GetSession();

        if (_session == null || !SessionManager.HasPermission("ManageComplaints"))
        {
            MessageBox.Show("Bạn không có quyền truy cập màn hình này.", "Từ chối truy cập",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
            return;
        }

        InitializeComponent();
        LoadData();
        AuditLogDAL.LogAction(_session.UserID, "Opened complaint management form", "FrmComplaintManagement");
    }

    private void InitializeComponent()
    {
        Text = "Quản lý khiếu nại";
        Size = new Size(1300, 950);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        Controls.Add(CreateFilterPanel());
        Controls.Add(CreateDetailsPanel());
        Controls.Add(CreateGridPanel());
        Controls.Add(CreateButtonPanel());
        Controls.Add(CreateStatusPanel());
    }

    private Panel CreateFilterPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, SPACING),
            Size = new Size(ClientSize.Width - 2 * SPACING, 50),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        pnl.Controls.Add(new Label { Text = "Trạng thái:", Location = new Point(SPACING, SPACING), Size = new Size(70, 20) });

        cmbFilterStatus.Location = new Point(SPACING + 70, SPACING);
        cmbFilterStatus.Size = new Size(120, 25);
        cmbFilterStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbFilterStatus.Items.AddRange(new object[]
        {
            new UiComboItem("Tất cả", "All"),
            new UiComboItem("Mới", "New"),
            new UiComboItem("Đã phân công", "Assigned"),
            new UiComboItem("Đang xử lý", "In-Progress"),
            new UiComboItem("Đã giải quyết", "Resolved"),
            new UiComboItem("Đã đóng", "Closed")
        });
        cmbFilterStatus.SelectedIndex = 0;
        pnl.Controls.Add(cmbFilterStatus);

        pnl.Controls.Add(new Label { Text = "Ưu tiên:", Location = new Point(SPACING + 200, SPACING), Size = new Size(60, 20) });

        cmbFilterPriority.Location = new Point(SPACING + 280, SPACING);
        cmbFilterPriority.Size = new Size(120, 25);
        cmbFilterPriority.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbFilterPriority.Items.AddRange(new object[]
        {
            new UiComboItem("Tất cả", "All"),
            new UiComboItem("Thấp", "Low"),
            new UiComboItem("Trung bình", "Medium"),
            new UiComboItem("Cao", "High"),
            new UiComboItem("Khẩn cấp", "Critical")
        });
        cmbFilterPriority.SelectedIndex = 0;
        pnl.Controls.Add(cmbFilterPriority);

        btnSearch.Text = "Tìm kiếm";
        btnSearch.Location = new Point(SPACING + 410, SPACING);
        btnSearch.Size = new Size(80, 30);
        btnSearch.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnSearch.ForeColor = Color.White;
        btnSearch.Click += BtnSearch_Click;
        pnl.Controls.Add(btnSearch);

        btnShowAll.Text = "Hiển thị tất cả";
        btnShowAll.Location = new Point(SPACING + 500, SPACING);
        btnShowAll.Size = new Size(100, 30);
        btnShowAll.BackColor = Color.Gray;
        btnShowAll.ForeColor = Color.White;
        btnShowAll.Click += (s, e) => LoadData();
        pnl.Controls.Add(btnShowAll);

        return pnl;
    }

    private Panel CreateDetailsPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 70),
            Size = new Size(ClientSize.Width - 2 * SPACING, 220),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        int y = SPACING;
        int x = SPACING;

        lblResident.Text = "Cư dân:";
        lblResident.Location = new Point(x, y);
        lblResident.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblResident);

        cmbResident.Location = new Point(x + LABEL_WIDTH + SPACING, y);
        cmbResident.Size = new Size(CONTROL_WIDTH, 25);
        cmbResident.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbResident);

        lblPriority.Text = "Ưu tiên:";
        lblPriority.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y);
        lblPriority.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblPriority);

        cmbPriority.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y);
        cmbPriority.Size = new Size(CONTROL_WIDTH, 25);
        cmbPriority.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPriority.Items.AddRange(new object[]
        {
            new UiComboItem("Thấp", "Low"),
            new UiComboItem("Trung bình", "Medium"),
            new UiComboItem("Cao", "High"),
            new UiComboItem("Khẩn cấp", "Critical")
        });
        pnl.Controls.Add(cmbPriority);

        lblTitle.Text = "Tiêu đề:";
        lblTitle.Location = new Point(x, y + 35);
        lblTitle.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblTitle);

        txtTitle.Location = new Point(x + LABEL_WIDTH + SPACING, y + 35);
        txtTitle.Size = new Size(CONTROL_WIDTH * 2 + SPACING + 80, 25);
        pnl.Controls.Add(txtTitle);

        lblDescription.Text = "Mô tả:";
        lblDescription.Location = new Point(x, y + 70);
        lblDescription.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblDescription);

        txtDescription.Location = new Point(x + LABEL_WIDTH + SPACING, y + 70);
        txtDescription.Size = new Size(CONTROL_WIDTH * 2 + SPACING + 80, 60);
        txtDescription.Multiline = true;
        pnl.Controls.Add(txtDescription);

        lblStatus.Text = "Trạng thái:";
        lblStatus.Location = new Point(x + LABEL_WIDTH * 3 + CONTROL_WIDTH * 2 + SPACING * 5, y + 35);
        lblStatus.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblStatus);

        cmbStatus.Location = new Point(x + LABEL_WIDTH * 4 + CONTROL_WIDTH * 2 + SPACING * 6, y + 35);
        cmbStatus.Size = new Size(CONTROL_WIDTH, 25);
        cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbStatus.Items.AddRange(new object[]
        {
            new UiComboItem("Mới", "New"),
            new UiComboItem("Đã phân công", "Assigned"),
            new UiComboItem("Đang xử lý", "In-Progress"),
            new UiComboItem("Đã giải quyết", "Resolved"),
            new UiComboItem("Đã đóng", "Closed")
        });
        pnl.Controls.Add(cmbStatus);

        lblAssignedTo.Text = "Người xử lý:";
        lblAssignedTo.Location = new Point(x + LABEL_WIDTH * 3 + CONTROL_WIDTH * 2 + SPACING * 5, y + 70);
        lblAssignedTo.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblAssignedTo);

        cmbAssignedTo.Location = new Point(x + LABEL_WIDTH * 4 + CONTROL_WIDTH * 2 + SPACING * 6, y + 70);
        cmbAssignedTo.Size = new Size(CONTROL_WIDTH, 25);
        cmbAssignedTo.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbAssignedTo);

        lblReportDate.Text = "Ngày báo cáo:";
        lblReportDate.Location = new Point(x + LABEL_WIDTH * 3 + CONTROL_WIDTH * 2 + SPACING * 5, y + 105);
        lblReportDate.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblReportDate);

        dtpReportDate.Location = new Point(x + LABEL_WIDTH * 4 + CONTROL_WIDTH * 2 + SPACING * 6, y + 105);
        dtpReportDate.Size = new Size(CONTROL_WIDTH, 25);
        dtpReportDate.Format = DateTimePickerFormat.Short;
        pnl.Controls.Add(dtpReportDate);

        lblCompletionDate.Text = "Ngày hoàn thành:";
        lblCompletionDate.Location = new Point(x + LABEL_WIDTH * 3 + CONTROL_WIDTH * 2 + SPACING * 5, y + 140);
        lblCompletionDate.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblCompletionDate);

        dtpCompletionDate.Location = new Point(x + LABEL_WIDTH * 4 + CONTROL_WIDTH * 2 + SPACING * 6, y + 140);
        dtpCompletionDate.Size = new Size(CONTROL_WIDTH, 25);
        dtpCompletionDate.Format = DateTimePickerFormat.Short;
        pnl.Controls.Add(dtpCompletionDate);

        lblResolutionNote.Text = "Ghi chú xử lý:";
        lblResolutionNote.Location = new Point(x, y + 140);
        lblResolutionNote.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblResolutionNote);

        txtResolutionNote.Location = new Point(x + LABEL_WIDTH + SPACING, y + 140);
        txtResolutionNote.Size = new Size(CONTROL_WIDTH * 2 + SPACING + 80, 50);
        txtResolutionNote.Multiline = true;
        pnl.Controls.Add(txtResolutionNote);

        return pnl;
    }

    private Panel CreateGridPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 300),
            Size = new Size(ClientSize.Width - 2 * SPACING, 340),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        dgvComplaints.Location = new Point(0, 0);
        dgvComplaints.Size = new Size(pnl.ClientSize.Width, pnl.ClientSize.Height);
        dgvComplaints.Dock = DockStyle.Fill;
        dgvComplaints.AllowUserToAddRows = false;
        dgvComplaints.AllowUserToDeleteRows = false;
        dgvComplaints.ReadOnly = true;
        dgvComplaints.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvComplaints.MultiSelect = false;
        dgvComplaints.RowHeadersVisible = false;
        dgvComplaints.CellClick += DgvComplaints_CellClick;

        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "ComplaintID", Width = 50, Visible = false });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tiêu đề", DataPropertyName = "Title", Width = 180 });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cư dân", DataPropertyName = "ResidentName", Width = 120 });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ưu tiên", DataPropertyName = "Priority", Width = 80 });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", DataPropertyName = "Status", Width = 100 });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Người xử lý", DataPropertyName = "AssignedToName", Width = 120 });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ngày báo cáo", DataPropertyName = "ReportDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hoàn thành", DataPropertyName = "CompletionDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });

        pnl.Controls.Add(dgvComplaints);
        return pnl;
    }

    private Panel CreateButtonPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 650),
            Size = new Size(ClientSize.Width - 2 * SPACING, 50),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        int x = SPACING;
        int y = SPACING;

        btnCreate.Text = "Thêm";
        btnCreate.Location = new Point(x, y);
        btnCreate.Size = new Size(100, 30);
        btnCreate.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnCreate.ForeColor = Color.White;
        btnCreate.Click += BtnCreate_Click;
        pnl.Controls.Add(btnCreate);

        btnEdit.Text = "Sửa";
        btnEdit.Location = new Point(x + 110, y);
        btnEdit.Size = new Size(100, 30);
        btnEdit.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnEdit.ForeColor = Color.White;
        btnEdit.Click += BtnEdit_Click;
        pnl.Controls.Add(btnEdit);

        btnAssign.Text = "Phân công";
        btnAssign.Location = new Point(x + 220, y);
        btnAssign.Size = new Size(100, 30);
        btnAssign.BackColor = Color.FromArgb(0, 100, 100);
        btnAssign.ForeColor = Color.White;
        btnAssign.Click += BtnAssign_Click;
        pnl.Controls.Add(btnAssign);

        btnResolve.Text = "Giải quyết";
        btnResolve.Location = new Point(x + 330, y);
        btnResolve.Size = new Size(100, 30);
        btnResolve.BackColor = Color.FromArgb(0, 100, 0);
        btnResolve.ForeColor = Color.White;
        btnResolve.Click += BtnResolve_Click;
        pnl.Controls.Add(btnResolve);

        btnDelete.Text = "Xóa";
        btnDelete.Location = new Point(x + 440, y);
        btnDelete.Size = new Size(100, 30);
        btnDelete.BackColor = Color.FromArgb(0xC00000);
        btnDelete.ForeColor = Color.White;
        btnDelete.Click += BtnDelete_Click;
        pnl.Controls.Add(btnDelete);

        btnStatistics.Text = "Thống kê";
        btnStatistics.Location = new Point(x + 550, y);
        btnStatistics.Size = new Size(100, 30);
        btnStatistics.BackColor = Color.FromArgb(255, 140, 0);
        btnStatistics.ForeColor = Color.White;
        btnStatistics.Click += BtnStatistics_Click;
        pnl.Controls.Add(btnStatistics);

        btnClose.Text = "Đóng";
        btnClose.Location = new Point(pnl.ClientSize.Width - 110, y);
        btnClose.Size = new Size(100, 30);
        btnClose.BackColor = Color.Gray;
        btnClose.ForeColor = Color.White;
        btnClose.Click += (s, e) => Close();
        pnl.Controls.Add(btnClose);

        return pnl;
    }

    private Panel CreateStatusPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 710),
            Size = new Size(ClientSize.Width - 2 * SPACING, 70),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        lblStatusBar.Text = "Sẵn sàng";
        lblStatusBar.Location = new Point(SPACING, SPACING);
        lblStatusBar.Size = new Size(400, 20);
        lblStatusBar.ForeColor = Color.Green;
        pnl.Controls.Add(lblStatusBar);

        lblComplaintInfo.Text = "Tổng khiếu nại: 0 | Đang xử lý: 0 | Khẩn cấp: 0";
        lblComplaintInfo.Location = new Point(SPACING, SPACING + 25);
        lblComplaintInfo.Size = new Size(600, 20);
        pnl.Controls.Add(lblComplaintInfo);

        return pnl;
    }

    private void LoadData()
    {
        try
        {
            var complaints = ComplaintDAL.GetAllComplaints();
            dgvComplaints.DataSource = complaints.Cast<dynamic>().ToList();
            lblStatusBar.Text = $"Đã tải {complaints.Count} khiếu nại";
            LoadResidents();
            LoadStaff();
            CalculateComplaintInfo();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải khiếu nại: {ex.Message}", "Lỗi");
        }
    }

    private void LoadResidents()
    {
        try
        {
            var residents = ResidentDAL.GetAllResidents();
            cmbResident.Items.Clear();

            foreach (var res in residents)
            {
                cmbResident.Items.Add(new UiComboItem(res.FullName, res.ResidentID));
            }

            cmbResident.DisplayMember = "Text";
            cmbResident.ValueMember = "Value";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải cư dân: {ex.Message}", "Lỗi");
        }
    }

    private void LoadStaff()
    {
        try
        {
            var users = UserDAL.GetAllUsers();
            _staffList = users.Where(u => u.IsActive).Cast<dynamic>().ToList();
            cmbAssignedTo.Items.Clear();
            cmbAssignedTo.Items.Add(new UiComboItem("Chưa phân công", 0));

            foreach (var user in _staffList)
            {
                cmbAssignedTo.Items.Add(new UiComboItem(user.FullName, user.UserID));
            }

            cmbAssignedTo.DisplayMember = "Text";
            cmbAssignedTo.ValueMember = "Value";
            cmbAssignedTo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải nhân sự: {ex.Message}", "Lỗi");
        }
    }

    private void CalculateComplaintInfo()
    {
        try
        {
            var complaints = ComplaintDAL.GetAllComplaints();
            int pending = complaints.Count(c => c.Status != "Resolved" && c.Status != "Closed");
            int critical = complaints.Count(c => c.Priority == "Critical");
            lblComplaintInfo.Text = $"Tổng khiếu nại: {complaints.Count} | Đang xử lý: {pending} | Khẩn cấp: {critical}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tính thống kê khiếu nại: {ex.Message}", "Lỗi");
        }
    }

    private void DgvComplaints_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        try
        {
            _selectedComplaintID = (int)dgvComplaints[0, e.RowIndex].Value;
            var complaint = ComplaintDAL.GetComplaintByID(_selectedComplaintID);
            if (complaint == null) return;

            txtTitle.Text = complaint.Title;
            txtDescription.Text = complaint.Description;
            ComboBoxHelper.SelectValue(cmbPriority, complaint.Priority);
            ComboBoxHelper.SelectValue(cmbStatus, complaint.Status);
            txtResolutionNote.Text = complaint.ResolutionNote ?? string.Empty;
            dtpReportDate.Value = complaint.ReportDate;
            if (complaint.CompletionDate.HasValue)
            {
                dtpCompletionDate.Value = complaint.CompletionDate.Value;
            }

            ComboBoxHelper.SelectValue(cmbResident, complaint.ResidentID);
            ComboBoxHelper.SelectValue(cmbAssignedTo, complaint.AssignedToUserID ?? 0);
            lblStatusBar.Text = $"Đã chọn: {complaint.Title} - {complaint.Status}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải chi tiết khiếu nại: {ex.Message}", "Lỗi");
        }
    }

    private void BtnCreate_Click(object sender, EventArgs e)
    {
        if (cmbResident.SelectedItem == null)
        {
            MessageBox.Show("Vui lòng chọn cư dân.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            int residentID = ComboBoxHelper.GetSelectedValueInt(cmbResident);
            var result = ComplaintBLL.CreateComplaint(
                residentID,
                txtTitle.Text,
                txtDescription.Text,
                ComboBoxHelper.GetSelectedValueString(cmbPriority));

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(_session!.UserID, $"Created complaint: {txtTitle.Text}", "FrmComplaintManagement");
                ClearForm();
                LoadData();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tạo khiếu nại: {ex.Message}", "Lỗi");
        }
    }

    private void BtnEdit_Click(object sender, EventArgs e)
    {
        if (_selectedComplaintID == 0)
        {
            MessageBox.Show("Vui lòng chọn khiếu nại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var result = ComplaintBLL.UpdateComplaint(
                _selectedComplaintID,
                txtTitle.Text,
                txtDescription.Text,
                ComboBoxHelper.GetSelectedValueString(cmbPriority));

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(_session!.UserID, $"Updated complaint {_selectedComplaintID}", "FrmComplaintManagement");
                LoadData();
                ClearForm();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi cập nhật khiếu nại: {ex.Message}", "Lỗi");
        }
    }

    private void BtnAssign_Click(object sender, EventArgs e)
    {
        if (_selectedComplaintID == 0)
        {
            MessageBox.Show("Vui lòng chọn khiếu nại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            int staffID = ComboBoxHelper.GetSelectedValueInt(cmbAssignedTo);
            var result = ComplaintBLL.AssignComplaint(_selectedComplaintID, staffID > 0 ? staffID : null);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(_session!.UserID, $"Assigned complaint {_selectedComplaintID}", "FrmComplaintManagement");
                LoadData();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi phân công khiếu nại: {ex.Message}", "Lỗi");
        }
    }

    private void BtnResolve_Click(object sender, EventArgs e)
    {
        if (_selectedComplaintID == 0)
        {
            MessageBox.Show("Vui lòng chọn khiếu nại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var result = ComplaintBLL.ResolveComplaint(_selectedComplaintID, txtResolutionNote.Text);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(_session!.UserID, $"Resolved complaint {_selectedComplaintID}", "FrmComplaintManagement");
                LoadData();
                ClearForm();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi giải quyết khiếu nại: {ex.Message}", "Lỗi");
        }
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_selectedComplaintID == 0)
        {
            MessageBox.Show("Vui lòng chọn khiếu nại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (MessageBox.Show("Bạn có chắc muốn xóa khiếu nại này không?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            var result = ComplaintBLL.DeleteComplaint(_selectedComplaintID);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(_session!.UserID, $"Deleted complaint {_selectedComplaintID}", "FrmComplaintManagement");
                LoadData();
                ClearForm();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xóa khiếu nại: {ex.Message}", "Lỗi");
        }
    }

    private void BtnSearch_Click(object sender, EventArgs e)
    {
        try
        {
            var filtered = ComplaintDAL.GetAllComplaints().AsEnumerable();

            if (cmbFilterStatus.SelectedIndex > 0)
            {
                filtered = filtered.Where(c => c.Status == ComboBoxHelper.GetSelectedValueString(cmbFilterStatus));
            }

            if (cmbFilterPriority.SelectedIndex > 0)
            {
                filtered = filtered.Where(c => c.Priority == ComboBoxHelper.GetSelectedValueString(cmbFilterPriority));
            }

            dgvComplaints.DataSource = filtered.ToList();
            lblStatusBar.Text = $"Tìm thấy {filtered.Count()} khiếu nại";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tìm kiếm: {ex.Message}", "Lỗi");
        }
    }

    private void BtnStatistics_Click(object sender, EventArgs e)
    {
        try
        {
            var stats = ComplaintBLL.GetComplaintStatistics();
            if (stats != null)
            {
                string message = $"Tổng khiếu nại: {stats.TotalComplaints}\n\n" +
                                 $"Theo mức độ ưu tiên:\n" +
                                 $"  Thấp: {stats.LowPriority}\n" +
                                 $"  Trung bình: {stats.MediumPriority}\n" +
                                 $"  Cao: {stats.HighPriority}\n" +
                                 $"  Khẩn cấp: {stats.CriticalPriority}\n\n" +
                                 $"Theo trạng thái:\n" +
                                 $"  Đã giải quyết: {stats.ResolvedCount}\n" +
                                 $"  Đang xử lý: {stats.PendingCount}";

                MessageBox.Show(message, "Thống kê khiếu nại", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lấy thống kê: {ex.Message}", "Lỗi");
        }
    }

    private void ClearForm()
    {
        txtTitle.Clear();
        txtDescription.Clear();
        cmbPriority.SelectedIndex = -1;
        cmbStatus.SelectedIndex = -1;
        cmbResident.SelectedIndex = -1;
        cmbAssignedTo.SelectedIndex = 0;
        txtResolutionNote.Clear();
        dtpReportDate.Value = DateTime.Now;
        dtpCompletionDate.Value = DateTime.Now;
        _selectedComplaintID = 0;
    }
}

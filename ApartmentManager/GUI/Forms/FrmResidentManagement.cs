using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

/// <summary>
/// Form for managing residents with comprehensive profile management
/// </summary>
public class FrmResidentManagement : Form
{
    private const int PRIMARY_COLOR = 0x215689;
    private const int SPACING = 10;
    private const int LABEL_WIDTH = 120;
    private const int CONTROL_WIDTH = 200;

    // Filter controls
    private ComboBox cmbFilterStatus = new ComboBox();
    private TextBox txtSearchName = new TextBox();
    private Button btnSearch = new Button();
    private Button btnShowAll = new Button();

    // Resident details controls
    private TextBox txtFullName = new TextBox();
    private TextBox txtPhone = new TextBox();
    private TextBox txtEmail = new TextBox();
    private TextBox txtCCCD = new TextBox();
    private DateTimePicker dtpDOB = new DateTimePicker();
    private ComboBox cmbApartment = new ComboBox();
    private ComboBox cmbRelationship = new ComboBox();
    private DateTimePicker dtpStartDate = new DateTimePicker();
    private TextBox txtNote = new TextBox();

    // Labels
    private Label lblFullName = new Label();
    private Label lblPhone = new Label();
    private Label lblEmail = new Label();
    private Label lblCCCD = new Label();
    private Label lblDOB = new Label();
    private Label lblApartment = new Label();
    private Label lblRelationship = new Label();
    private Label lblStartDate = new Label();
    private Label lblNote = new Label();

    // Grid and buttons
    private DataGridView dgvResidents = new DataGridView();
    private Button btnCreate = new Button();
    private Button btnEdit = new Button();
    private Button btnDelete = new Button();
    private Button btnMoveOut = new Button();
    private Button btnStatistics = new Button();
    private Button btnClose = new Button();

    // Status
    private Label lblStatusBar = new Label();
    private Label lblResidentInfo = new Label();

    private int _selectedResidentID = 0;

    public FrmResidentManagement()
    {
        if (SessionManager.GetSession() == null || !SessionManager.HasPermission("ManageResidents"))
        {
            MessageBox.Show("Bạn không có quyền truy cập màn hình này.", "Từ chối truy cập",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.Close();
            return;
        }

        InitializeComponent();
        LoadData();
        AuditLogDAL.LogAction(SessionManager.GetSession().UserID, "Opened resident management form", "FrmResidentManagement");
    }

    private void InitializeComponent()
    {
        this.Text = "Quản lý cư dân";
        this.Size = new Size(1200, 850);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.White;

        var pnlFilter = CreateFilterPanel();
        var pnlDetails = CreateDetailsPanel();
        var pnlGrid = CreateGridPanel();
        var pnlButtons = CreateButtonPanel();
        var pnlStatus = CreateStatusPanel();

        this.Controls.Add(pnlFilter);
        this.Controls.Add(pnlDetails);
        this.Controls.Add(pnlGrid);
        this.Controls.Add(pnlButtons);
        this.Controls.Add(pnlStatus);
    }

    private Panel CreateFilterPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, SPACING),
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 50),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblStatus = new Label { Text = "Trạng thái:", Location = new Point(SPACING, SPACING), Size = new Size(80, 20) };
        pnl.Controls.Add(lblStatus);

        cmbFilterStatus.Location = new Point(SPACING + 90, SPACING);
        cmbFilterStatus.Size = new Size(150, 25);
        cmbFilterStatus.Items.AddRange(new object[]
        {
            new UiComboItem("Tất cả", "All"),
            new UiComboItem("Đang hoạt động", "Active"),
            new UiComboItem("Ngừng hoạt động", "Inactive")
        });
        cmbFilterStatus.SelectedIndex = 0;
        pnl.Controls.Add(cmbFilterStatus);

        var lblSearch = new Label { Text = "Tên:", Location = new Point(SPACING + 260, SPACING), Size = new Size(50, 20) };
        pnl.Controls.Add(lblSearch);

        txtSearchName.Location = new Point(SPACING + 320, SPACING);
        txtSearchName.Size = new Size(200, 25);
        pnl.Controls.Add(txtSearchName);

        btnSearch.Text = "Tìm kiếm";
        btnSearch.Location = new Point(SPACING + 530, SPACING);
        btnSearch.Size = new Size(80, 30);
        btnSearch.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnSearch.ForeColor = Color.White;
        btnSearch.Click += BtnSearch_Click;
        pnl.Controls.Add(btnSearch);

        btnShowAll.Text = "Hiển thị tất cả";
        btnShowAll.Location = new Point(SPACING + 620, SPACING);
        btnShowAll.Size = new Size(80, 30);
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
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 160),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        int y = SPACING;
        int x = SPACING;

        // Row 1
        lblFullName.Text = "Họ tên:";
        lblFullName.Location = new Point(x, y);
        lblFullName.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblFullName);

        txtFullName.Location = new Point(x + LABEL_WIDTH + SPACING, y);
        txtFullName.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtFullName);

        lblPhone.Text = "Số điện thoại:";
        lblPhone.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y);
        lblPhone.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblPhone);

        txtPhone.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y);
        txtPhone.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtPhone);

        // Row 2
        lblEmail.Text = "Email:";
        lblEmail.Location = new Point(x, y + 35);
        lblEmail.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblEmail);

        txtEmail.Location = new Point(x + LABEL_WIDTH + SPACING, y + 35);
        txtEmail.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtEmail);

        lblCCCD.Text = "CCCD:";
        lblCCCD.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 35);
        lblCCCD.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblCCCD);

        txtCCCD.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 35);
        txtCCCD.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtCCCD);

        // Row 3
        lblDOB.Text = "Ngày sinh:";
        lblDOB.Location = new Point(x, y + 70);
        lblDOB.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblDOB);

        dtpDOB.Location = new Point(x + LABEL_WIDTH + SPACING, y + 70);
        dtpDOB.Size = new Size(CONTROL_WIDTH, 25);
        dtpDOB.Format = DateTimePickerFormat.Short;
        pnl.Controls.Add(dtpDOB);

        lblApartment.Text = "Căn hộ:";
        lblApartment.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 70);
        lblApartment.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblApartment);

        cmbApartment.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 70);
        cmbApartment.Size = new Size(CONTROL_WIDTH, 25);
        cmbApartment.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbApartment);

        // Row 4
        lblRelationship.Text = "Quan hệ:";
        lblRelationship.Location = new Point(x, y + 105);
        lblRelationship.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblRelationship);

        cmbRelationship.Location = new Point(x + LABEL_WIDTH + SPACING, y + 105);
        cmbRelationship.Size = new Size(CONTROL_WIDTH, 25);
        cmbRelationship.Items.AddRange(new object[]
        {
            new UiComboItem("Chủ hộ", "Owner"),
            new UiComboItem("Người thân", "Family"),
            new UiComboItem("Bạn bè", "Friend"),
            new UiComboItem("Người thuê", "Tenant"),
            new UiComboItem("Khác", "Other")
        });
        cmbRelationship.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbRelationship);

        lblStartDate.Text = "Ngày vào:";
        lblStartDate.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 105);
        lblStartDate.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblStartDate);

        dtpStartDate.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 105);
        dtpStartDate.Size = new Size(CONTROL_WIDTH, 25);
        dtpStartDate.Format = DateTimePickerFormat.Short;
        pnl.Controls.Add(dtpStartDate);

        return pnl;
    }

    private Panel CreateGridPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 240),
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 330),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        dgvResidents.Location = new Point(0, 0);
        dgvResidents.Size = new Size(pnl.ClientSize.Width, pnl.ClientSize.Height);
        dgvResidents.Dock = DockStyle.Fill;
        dgvResidents.AllowUserToAddRows = false;
        dgvResidents.AllowUserToDeleteRows = false;
        dgvResidents.ReadOnly = true;
        dgvResidents.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvResidents.MultiSelect = false;
        dgvResidents.RowHeadersVisible = false;
        dgvResidents.CellClick += DgvResidents_CellClick;

        dgvResidents.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "ResidentID", Width = 50, Visible = false });
        dgvResidents.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ tên", DataPropertyName = "FullName", Width = 150 });
        dgvResidents.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Điện thoại", DataPropertyName = "Phone", Width = 120 });
        dgvResidents.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Email", DataPropertyName = "Email", Width = 150 });
        dgvResidents.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CCCD", DataPropertyName = "CCCD", Width = 120 });
        dgvResidents.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Căn hộ", DataPropertyName = "ApartmentCode", Width = 100 });
        dgvResidents.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", DataPropertyName = "Status", Width = 100 });

        pnl.Controls.Add(dgvResidents);
        return pnl;
    }

    private Panel CreateButtonPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 580),
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 50),
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

        btnDelete.Text = "Xóa";
        btnDelete.Location = new Point(x + 220, y);
        btnDelete.Size = new Size(100, 30);
        btnDelete.BackColor = Color.FromArgb(0xC00000);
        btnDelete.ForeColor = Color.White;
        btnDelete.Click += BtnDelete_Click;
        pnl.Controls.Add(btnDelete);

        btnMoveOut.Text = "Rời đi";
        btnMoveOut.Location = new Point(x + 330, y);
        btnMoveOut.Size = new Size(100, 30);
        btnMoveOut.BackColor = Color.FromArgb(255, 140, 0);
        btnMoveOut.ForeColor = Color.White;
        btnMoveOut.Click += BtnMoveOut_Click;
        pnl.Controls.Add(btnMoveOut);

        btnStatistics.Text = "Thống kê";
        btnStatistics.Location = new Point(x + 440, y);
        btnStatistics.Size = new Size(100, 30);
        btnStatistics.BackColor = Color.FromArgb(0, 100, 0);
        btnStatistics.ForeColor = Color.White;
        btnStatistics.Click += BtnStatistics_Click;
        pnl.Controls.Add(btnStatistics);

        btnClose.Text = "Đóng";
        btnClose.Location = new Point(pnl.ClientSize.Width - 110, y);
        btnClose.Size = new Size(100, 30);
        btnClose.BackColor = Color.Gray;
        btnClose.ForeColor = Color.White;
        btnClose.Click += (s, e) => this.Close();
        pnl.Controls.Add(btnClose);

        return pnl;
    }

    private Panel CreateStatusPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 640),
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 60),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        lblStatusBar.Text = "Sẵn sàng";
        lblStatusBar.Location = new Point(SPACING, SPACING);
        lblStatusBar.Size = new Size(400, 20);
        lblStatusBar.ForeColor = Color.Green;
        pnl.Controls.Add(lblStatusBar);

        lblResidentInfo.Text = "Tổng cư dân: 0";
        lblResidentInfo.Location = new Point(SPACING, SPACING + 25);
        lblResidentInfo.Size = new Size(400, 20);
        pnl.Controls.Add(lblResidentInfo);

        return pnl;
    }

    private void LoadData()
    {
        try
        {
            List<dynamic> residents;

            if (cmbFilterStatus.SelectedIndex == 0)
                residents = ResidentDAL.GetAllResidents().Cast<dynamic>().ToList();
            else
                residents = ResidentDAL.GetResidentsByStatus(ComboBoxHelper.GetSelectedValueString(cmbFilterStatus)).Cast<dynamic>().ToList();

            dgvResidents.DataSource = residents.Cast<dynamic>().ToList();
            lblStatusBar.Text = $"Đã tải {residents.Count} cư dân";
            lblResidentInfo.Text = $"Tổng cư dân: {residents.Count}";

            LoadApartments();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải cư dân: {ex.Message}", "Lỗi");
        }
    }

    private void LoadApartments()
    {
        try
        {
            var apartments = ApartmentDAL.GetAllApartments();
            cmbApartment.Items.Clear();

            foreach (var apt in apartments)
            {
                cmbApartment.Items.Add(new UiComboItem(apt.ApartmentCode, apt.ApartmentID));
            }

            cmbApartment.DisplayMember = "Text";
            cmbApartment.ValueMember = "Value";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải căn hộ: {ex.Message}", "Lỗi");
        }
    }

    private void DgvResidents_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            _selectedResidentID = (int)dgvResidents[0, e.RowIndex].Value;
            var resident = ResidentDAL.GetResidentByID(_selectedResidentID);

            if (resident != null)
            {
                txtFullName.Text = resident.FullName;
                txtPhone.Text = resident.Phone;
                txtEmail.Text = resident.Email;
                txtCCCD.Text = resident.CCCD;
                dtpDOB.Value = resident.DOB ?? DateTime.Now;
                dtpStartDate.Value = resident.StartDate ?? DateTime.Now;

                // Set apartment
                var aptItem = cmbApartment.Items.Cast<dynamic>().FirstOrDefault(a => a.Value == resident.ApartmentID);
                if (aptItem != null)
                    cmbApartment.SelectedItem = aptItem;

                cmbRelationship.SelectValue(resident.RelationshipWithOwner);
                txtNote.Text = resident.Note ?? "";

                lblStatusBar.Text = $"Đã chọn: {resident.FullName}";
            }
        }
    }

    private void BtnCreate_Click(object sender, EventArgs e)
    {
        if (cmbApartment.SelectedItem == null)
        {
            MessageBox.Show("Vui lòng chọn căn hộ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            int apartmentID = (int)((dynamic)cmbApartment.SelectedItem).Value;

            var result = ResidentBLL.CreateResident(
                null,
                txtFullName.Text,
                txtPhone.Text,
                txtEmail.Text,
                txtCCCD.Text,
                dtpDOB.Value,
                apartmentID,
                ComboBoxHelper.GetSelectedValueString(cmbRelationship),
                dtpStartDate.Value,
                txtNote.Text
            );

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Created resident {txtFullName.Text}", "FrmResidentManagement");
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
            MessageBox.Show($"Lỗi khi tạo cư dân: {ex.Message}", "Lỗi");
        }
    }

    private void BtnEdit_Click(object sender, EventArgs e)
    {
        if (_selectedResidentID == 0)
        {
            MessageBox.Show("Vui lòng chọn cư dân.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var result = ResidentBLL.UpdateResident(
                _selectedResidentID,
                txtFullName.Text,
                txtPhone.Text,
                txtEmail.Text,
                ComboBoxHelper.GetSelectedValueString(cmbRelationship),
                txtNote.Text
            );

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Updated resident {txtFullName.Text}", "FrmResidentManagement");
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
            MessageBox.Show($"Lỗi khi cập nhật cư dân: {ex.Message}", "Lỗi");
        }
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_selectedResidentID == 0)
        {
            MessageBox.Show("Vui lòng chọn cư dân.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (MessageBox.Show("Bạn có chắc muốn xóa cư dân này không?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            var result = ResidentBLL.DeleteResident(_selectedResidentID);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Deleted resident {txtFullName.Text}", "FrmResidentManagement");
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
            MessageBox.Show($"Lỗi khi xóa cư dân: {ex.Message}", "Lỗi");
        }
    }

    private void BtnMoveOut_Click(object sender, EventArgs e)
    {
        if (_selectedResidentID == 0)
        {
            MessageBox.Show("Vui lòng chọn cư dân.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var moveOutDate = DateTime.Now;
        try
        {
            var result = ResidentBLL.MoveResidentOut(_selectedResidentID, moveOutDate);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Moved out resident {txtFullName.Text}", "FrmResidentManagement");
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
            MessageBox.Show($"Lỗi khi đánh dấu rời đi: {ex.Message}", "Lỗi");
        }
    }

    private void BtnSearch_Click(object sender, EventArgs e)
    {
        try
        {
            var allResidents = ResidentDAL.GetAllResidents();
            var filtered = allResidents
                .Where(r => r.FullName.Contains(txtSearchName.Text, StringComparison.OrdinalIgnoreCase))
                .ToList();

            dgvResidents.DataSource = filtered.Cast<dynamic>().ToList();
            lblStatusBar.Text = $"Tìm thấy {filtered.Count} cư dân phù hợp với '{txtSearchName.Text}'";
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
            var stats = ResidentBLL.GetResidentStatistics();
            if (stats != null)
            {
                string message = $"Tổng cư dân: {stats.TotalResidents}\n" +
                               $"Đang hoạt động: {stats.ActiveResidents}\n" +
                               $"Ngừng hoạt động: {stats.InactiveResidents}\n" +
                               $"Trung bình mỗi căn hộ: {stats.AveragePerApartment}";

                MessageBox.Show(message, "Thống kê cư dân", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lấy thống kê: {ex.Message}", "Lỗi");
        }
    }

    private void ClearForm()
    {
        txtFullName.Clear();
        txtPhone.Clear();
        txtEmail.Clear();
        txtCCCD.Clear();
        dtpDOB.Value = DateTime.Now.AddYears(-20);
        dtpStartDate.Value = DateTime.Now;
        cmbApartment.SelectedIndex = -1;
        cmbRelationship.SelectedIndex = -1;
        txtNote.Clear();
        _selectedResidentID = 0;
    }
}

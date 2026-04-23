using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public class FrmApartmentManagement : Form
{
    private const int PRIMARY_COLOR = 0x215689;
    private const int SPACING = 10;
    private const int LABEL_WIDTH = 120;
    private const int CONTROL_WIDTH = 200;

    private readonly ComboBox cmbBuilding = new ComboBox();
    private readonly ComboBox cmbBlock = new ComboBox();
    private readonly ComboBox cmbFloor = new ComboBox();
    private readonly Label lblBuilding = new Label();
    private readonly Label lblBlock = new Label();
    private readonly Label lblFloor = new Label();

    private readonly TextBox txtApartmentCode = new TextBox();
    private readonly TextBox txtArea = new TextBox();
    private readonly ComboBox cmbApartmentType = new ComboBox();
    private readonly ComboBox cmbStatus = new ComboBox();
    private readonly NumericUpDown nudMaxResidents = new NumericUpDown();
    private readonly TextBox txtNote = new TextBox();

    private readonly Label lblApartmentCode = new Label();
    private readonly Label lblArea = new Label();
    private readonly Label lblApartmentType = new Label();
    private readonly Label lblStatus = new Label();
    private readonly Label lblMaxResidents = new Label();
    private readonly Label lblNote = new Label();

    private readonly DataGridView dgvApartments = new DataGridView();
    private readonly Button btnCreate = new Button();
    private readonly Button btnEdit = new Button();
    private readonly Button btnDelete = new Button();
    private readonly Button btnRefresh = new Button();
    private readonly Button btnStatistics = new Button();
    private readonly Button btnClose = new Button();

    private readonly Label lblStatusBar = new Label();
    private readonly Label lblOccupancyInfo = new Label();

    private int _selectedApartmentID;

    public FrmApartmentManagement()
    {
        if (SessionManager.GetSession() == null || !SessionManager.HasPermission("ManageApartments"))
        {
            MessageBox.Show("Bạn không có quyền truy cập màn hình này.", "Từ chối truy cập",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
            return;
        }

        InitializeComponent();
        LoadBuildings();
        AuditLogDAL.LogAction(SessionManager.GetSession().UserID, "Opened apartment management form", "FrmApartmentManagement");
    }

    private void InitializeComponent()
    {
        Text = "Quản lý căn hộ";
        Size = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        Controls.Add(CreateHierarchyPanel());
        Controls.Add(CreateDetailsPanel());
        Controls.Add(CreateGridPanel());
        Controls.Add(CreateButtonPanel());
        Controls.Add(CreateStatusPanel());
    }

    private Panel CreateHierarchyPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, SPACING),
            Size = new Size(ClientSize.Width - 2 * SPACING, 80),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        lblBuilding.Text = "Tòa nhà:";
        lblBuilding.Location = new Point(SPACING, SPACING);
        lblBuilding.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblBuilding);

        cmbBuilding.Location = new Point(SPACING + LABEL_WIDTH + SPACING, SPACING);
        cmbBuilding.Size = new Size(CONTROL_WIDTH, 25);
        cmbBuilding.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbBuilding.SelectedIndexChanged += CmbBuilding_SelectedIndexChanged;
        pnl.Controls.Add(cmbBuilding);

        lblBlock.Text = "Khu:";
        lblBlock.Location = new Point(SPACING + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, SPACING);
        lblBlock.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblBlock);

        cmbBlock.Location = new Point(SPACING + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, SPACING);
        cmbBlock.Size = new Size(CONTROL_WIDTH, 25);
        cmbBlock.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbBlock.SelectedIndexChanged += CmbBlock_SelectedIndexChanged;
        pnl.Controls.Add(cmbBlock);

        lblFloor.Text = "Tầng:";
        lblFloor.Location = new Point(SPACING, SPACING + 35);
        lblFloor.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblFloor);

        cmbFloor.Location = new Point(SPACING + LABEL_WIDTH + SPACING, SPACING + 35);
        cmbFloor.Size = new Size(CONTROL_WIDTH, 25);
        cmbFloor.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbFloor.SelectedIndexChanged += CmbFloor_SelectedIndexChanged;
        pnl.Controls.Add(cmbFloor);

        return pnl;
    }

    private Panel CreateDetailsPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 100),
            Size = new Size(ClientSize.Width - 2 * SPACING, 140),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        int y = SPACING;
        int x = SPACING;

        lblApartmentCode.Text = "Mã căn hộ:";
        lblApartmentCode.Location = new Point(x, y);
        lblApartmentCode.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblApartmentCode);

        txtApartmentCode.Location = new Point(x + LABEL_WIDTH + SPACING, y);
        txtApartmentCode.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtApartmentCode);

        lblArea.Text = "Diện tích (m²):";
        lblArea.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y);
        lblArea.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblArea);

        txtArea.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y);
        txtArea.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtArea);

        lblApartmentType.Text = "Loại:";
        lblApartmentType.Location = new Point(x, y + 35);
        lblApartmentType.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblApartmentType);

        cmbApartmentType.Location = new Point(x + LABEL_WIDTH + SPACING, y + 35);
        cmbApartmentType.Size = new Size(CONTROL_WIDTH, 25);
        cmbApartmentType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbApartmentType.Items.AddRange(new object[]
        {
            new UiComboItem("Studio", "Studio"),
            new UiComboItem("1 phòng ngủ", "1BR"),
            new UiComboItem("2 phòng ngủ", "2BR"),
            new UiComboItem("3 phòng ngủ", "3BR"),
            new UiComboItem("Penthouse", "Penthouse"),
            new UiComboItem("Khác", "Other")
        });
        pnl.Controls.Add(cmbApartmentType);

        lblStatus.Text = "Trạng thái:";
        lblStatus.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 35);
        lblStatus.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblStatus);

        cmbStatus.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 35);
        cmbStatus.Size = new Size(CONTROL_WIDTH, 25);
        cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbStatus.Items.AddRange(new object[]
        {
            new UiComboItem("Trống", "Empty"),
            new UiComboItem("Đang ở", "Occupied"),
            new UiComboItem("Đang thuê", "Renting"),
            new UiComboItem("Đang sửa", "Maintenance"),
            new UiComboItem("Đã khóa", "Locked")
        });
        pnl.Controls.Add(cmbStatus);

        lblMaxResidents.Text = "Số cư dân tối đa:";
        lblMaxResidents.Location = new Point(x, y + 70);
        lblMaxResidents.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblMaxResidents);

        nudMaxResidents.Location = new Point(x + LABEL_WIDTH + SPACING, y + 70);
        nudMaxResidents.Size = new Size(CONTROL_WIDTH, 25);
        nudMaxResidents.Minimum = 1;
        nudMaxResidents.Maximum = 20;
        nudMaxResidents.Value = 4;
        pnl.Controls.Add(nudMaxResidents);

        lblNote.Text = "Ghi chú:";
        lblNote.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 70);
        lblNote.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblNote);

        txtNote.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 70);
        txtNote.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtNote);

        return pnl;
    }

    private Panel CreateGridPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 250),
            Size = new Size(ClientSize.Width - 2 * SPACING, 350),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        dgvApartments.Location = new Point(0, 0);
        dgvApartments.Size = new Size(pnl.ClientSize.Width, pnl.ClientSize.Height);
        dgvApartments.Dock = DockStyle.Fill;
        dgvApartments.AllowUserToAddRows = false;
        dgvApartments.AllowUserToDeleteRows = false;
        dgvApartments.ReadOnly = true;
        dgvApartments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvApartments.MultiSelect = false;
        dgvApartments.RowHeadersVisible = false;
        dgvApartments.CellClick += DgvApartments_CellClick;

        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "ApartmentID", Width = 50, Visible = false });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã căn hộ", DataPropertyName = "ApartmentCode", Width = 100 });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Diện tích (m²)", DataPropertyName = "Area", Width = 80 });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Loại", DataPropertyName = "ApartmentType", Width = 100 });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", DataPropertyName = "Status", Width = 100 });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số cư dân tối đa", DataPropertyName = "MaxResidents", Width = 100 });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ghi chú", DataPropertyName = "Note", Width = 200 });

        pnl.Controls.Add(dgvApartments);
        return pnl;
    }

    private Panel CreateButtonPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 610),
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

        btnDelete.Text = "Xóa";
        btnDelete.Location = new Point(x + 220, y);
        btnDelete.Size = new Size(100, 30);
        btnDelete.BackColor = Color.FromArgb(0xC00000);
        btnDelete.ForeColor = Color.White;
        btnDelete.Click += BtnDelete_Click;
        pnl.Controls.Add(btnDelete);

        btnRefresh.Text = "Làm mới";
        btnRefresh.Location = new Point(x + 330, y);
        btnRefresh.Size = new Size(100, 30);
        btnRefresh.BackColor = Color.FromArgb(70, 130, 180);
        btnRefresh.ForeColor = Color.White;
        btnRefresh.Click += BtnRefresh_Click;
        pnl.Controls.Add(btnRefresh);

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
        btnClose.Click += (s, e) => Close();
        pnl.Controls.Add(btnClose);

        return pnl;
    }

    private Panel CreateStatusPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 670),
            Size = new Size(ClientSize.Width - 2 * SPACING, 60),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        lblStatusBar.Text = "Sẵn sàng";
        lblStatusBar.Location = new Point(SPACING, SPACING);
        lblStatusBar.Size = new Size(400, 20);
        lblStatusBar.ForeColor = Color.Green;
        pnl.Controls.Add(lblStatusBar);

        lblOccupancyInfo.Text = "Tỷ lệ sử dụng: 0%";
        lblOccupancyInfo.Location = new Point(SPACING, SPACING + 25);
        lblOccupancyInfo.Size = new Size(400, 20);
        pnl.Controls.Add(lblOccupancyInfo);

        return pnl;
    }

    private void LoadBuildings()
    {
        try
        {
            cmbBuilding.Items.Clear();
            var buildings = BuildingDAL.GetAllBuildings();

            if (buildings.Count == 0)
            {
                MessageBox.Show("Chưa có tòa nhà nào. Vui lòng tạo tòa nhà trước.", "Thông tin",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var building in buildings)
            {
                cmbBuilding.Items.Add(new { Text = building.BuildingName, Value = building.BuildingID });
            }

            cmbBuilding.DisplayMember = "Text";
            cmbBuilding.ValueMember = "Value";
            cmbBuilding.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải danh sách tòa nhà: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CmbBuilding_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbBuilding.SelectedItem == null)
        {
            return;
        }

        try
        {
            int buildingID = ComboBoxHelper.GetSelectedValueInt(cmbBuilding);
            cmbBlock.Items.Clear();
            var blocks = BlockDAL.GetBlocksByBuilding(buildingID);

            if (blocks.Count == 0)
            {
                lblStatusBar.Text = "Tòa nhà này chưa có khu";
                return;
            }

            foreach (var block in blocks)
            {
                cmbBlock.Items.Add(new { Text = block.BlockName, Value = block.BlockID });
            }

            cmbBlock.DisplayMember = "Text";
            cmbBlock.ValueMember = "Value";
            cmbBlock.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải danh sách khu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CmbBlock_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbBlock.SelectedItem == null)
        {
            return;
        }

        try
        {
            int blockID = ComboBoxHelper.GetSelectedValueInt(cmbBlock);
            cmbFloor.Items.Clear();
            var floors = FloorDAL.GetFloorsByBlock(blockID);

            if (floors.Count == 0)
            {
                lblStatusBar.Text = "Khu này chưa có tầng";
                return;
            }

            foreach (var floor in floors)
            {
                cmbFloor.Items.Add(new { Text = floor.FloorNumber, Value = floor.FloorID });
            }

            cmbFloor.DisplayMember = "Text";
            cmbFloor.ValueMember = "Value";
            cmbFloor.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải danh sách tầng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CmbFloor_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbFloor.SelectedItem == null)
        {
            return;
        }

        try
        {
            int floorID = ComboBoxHelper.GetSelectedValueInt(cmbFloor);
            LoadApartments(floorID);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải căn hộ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadApartments(int floorID)
    {
        try
        {
            var apartments = ApartmentDAL.GetApartmentsByFloor(floorID);
            dgvApartments.DataSource = apartments.Cast<dynamic>().ToList();
            lblStatusBar.Text = $"Đã tải {apartments.Count} căn hộ";

            int occupied = apartments.Count(a => a.Status == "Occupied");
            int occupancyRate = apartments.Count > 0 ? (occupied * 100) / apartments.Count : 0;
            lblOccupancyInfo.Text = $"Tỷ lệ sử dụng: {occupancyRate}% ({occupied}/{apartments.Count})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải căn hộ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DgvApartments_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
        {
            return;
        }

        _selectedApartmentID = (int)dgvApartments[0, e.RowIndex].Value;
        var apartment = ApartmentDAL.GetApartmentByID(_selectedApartmentID);

        if (apartment == null)
        {
            return;
        }

        txtApartmentCode.Text = apartment.ApartmentCode;
        txtArea.Text = apartment.Area.ToString();
        cmbApartmentType.SelectValue(apartment.ApartmentType);
        cmbStatus.SelectValue(apartment.Status);
        nudMaxResidents.Value = apartment.MaxResidents;
        txtNote.Text = apartment.Note ?? string.Empty;
        lblStatusBar.Text = $"Đã chọn: {apartment.ApartmentCode}";
    }

    private void BtnCreate_Click(object sender, EventArgs e)
    {
        if (cmbFloor.SelectedItem == null)
        {
            MessageBox.Show("Vui lòng chọn tầng trước.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!decimal.TryParse(txtArea.Text, out var area) || area <= 0)
        {
            MessageBox.Show("Vui lòng nhập diện tích hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            int floorID = ComboBoxHelper.GetSelectedValueInt(cmbFloor);

            var result = ApartmentBLL.CreateApartment(
                txtApartmentCode.Text,
                floorID,
                area,
                ComboBoxHelper.GetSelectedValueString(cmbApartmentType),
                (int)nudMaxResidents.Value,
                txtNote.Text);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID,
                    $"Created apartment {txtApartmentCode.Text}", "FrmApartmentManagement");
                ClearForm();
                LoadApartments(floorID);
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tạo căn hộ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnEdit_Click(object sender, EventArgs e)
    {
        if (_selectedApartmentID == 0)
        {
            MessageBox.Show("Vui lòng chọn căn hộ trước.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!decimal.TryParse(txtArea.Text, out var area) || area <= 0)
        {
            MessageBox.Show("Vui lòng nhập diện tích hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var result = ApartmentBLL.UpdateApartment(
                _selectedApartmentID,
                txtApartmentCode.Text,
                area,
                ComboBoxHelper.GetSelectedValueString(cmbApartmentType),
                (int)nudMaxResidents.Value,
                txtNote.Text);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID,
                    $"Updated apartment {txtApartmentCode.Text}", "FrmApartmentManagement");
                int floorID = ComboBoxHelper.GetSelectedValueInt(cmbFloor);
                LoadApartments(floorID);
                ClearForm();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi cập nhật căn hộ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_selectedApartmentID == 0)
        {
            MessageBox.Show("Vui lòng chọn căn hộ trước.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (MessageBox.Show("Bạn có chắc muốn xóa căn hộ này không?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            var result = ApartmentBLL.DeleteApartment(_selectedApartmentID);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID,
                    $"Deleted apartment {txtApartmentCode.Text}", "FrmApartmentManagement");
                int floorID = ComboBoxHelper.GetSelectedValueInt(cmbFloor);
                LoadApartments(floorID);
                ClearForm();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xóa căn hộ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnRefresh_Click(object sender, EventArgs e)
    {
        try
        {
            if (cmbFloor.SelectedItem != null)
            {
                int floorID = ComboBoxHelper.GetSelectedValueInt(cmbFloor);
                LoadApartments(floorID);
                lblStatusBar.Text = "Đã làm mới";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi làm mới: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnStatistics_Click(object sender, EventArgs e)
    {
        try
        {
            var stats = ApartmentBLL.GetOccupancyStatistics();
            if (stats != null)
            {
                string message = $"Tổng căn hộ: {stats.TotalApartments}\n" +
                                 $"Trống: {stats.EmptyApartments}\n" +
                                 $"Đang ở: {stats.OccupiedApartments}\n" +
                                 $"Đang thuê: {stats.RentingApartments}\n" +
                                 $"Đang sửa: {stats.MaintenanceApartments}\n" +
                                 $"Đã khóa: {stats.LockedApartments}\n" +
                                 $"Tỷ lệ sử dụng: {stats.OccupancyRate}";

                MessageBox.Show(message, "Thống kê sử dụng", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lấy thống kê: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearForm()
    {
        txtApartmentCode.Clear();
        txtArea.Clear();
        cmbApartmentType.SelectedIndex = -1;
        cmbStatus.SelectedIndex = -1;
        nudMaxResidents.Value = 4;
        txtNote.Clear();
        _selectedApartmentID = 0;
    }
}

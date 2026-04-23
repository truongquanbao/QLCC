using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

namespace ApartmentManager.GUI.Forms
{
    public partial class FrmVehicleManagement : Form
    {
        private const int PRIMARY_COLOR = 0x215689;
        private const int SPACING = 10;
        private const int LABEL_WIDTH = 120;
        private const int CONTROL_WIDTH = 200;

        private readonly ComboBox cmbFilterResident = new ComboBox();
        private readonly TextBox txtSearchLicensePlate = new TextBox();
        private readonly Button btnSearch = new Button();
        private readonly Button btnShowAll = new Button();

        private readonly ComboBox cmbResident = new ComboBox();
        private readonly TextBox txtLicensePlate = new TextBox();
        private readonly ComboBox cmbVehicleType = new ComboBox();
        private readonly TextBox txtBrand = new TextBox();
        private readonly TextBox txtModel = new TextBox();
        private readonly NumericUpDown nudYearMade = new NumericUpDown();
        private readonly TextBox txtColor = new TextBox();
        private readonly TextBox txtNote = new TextBox();

        private readonly Label lblResident = new Label();
        private readonly Label lblLicensePlate = new Label();
        private readonly Label lblVehicleType = new Label();
        private readonly Label lblBrand = new Label();
        private readonly Label lblModel = new Label();
        private readonly Label lblYearMade = new Label();
        private readonly Label lblColor = new Label();
        private readonly Label lblNote = new Label();

        private readonly DataGridView dgvVehicles = new DataGridView();
        private readonly Button btnCreate = new Button();
        private readonly Button btnEdit = new Button();
        private readonly Button btnDelete = new Button();
        private readonly Button btnStatistics = new Button();
        private readonly Button btnClose = new Button();

        private readonly Label lblStatusBar = new Label();
        private readonly Label lblVehicleInfo = new Label();

        private int _selectedVehicleID = 0;

        public FrmVehicleManagement()
        {
            if (SessionManager.GetSession() == null || !SessionManager.HasPermission("ManageVehicles"))
            {
                MessageBox.Show("Bạn không có quyền truy cập màn hình này.", "Từ chối truy cập",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
                return;
            }

            InitializeComponent();
            LoadData();
            AuditLogDAL.LogAction(SessionManager.GetSession()!.UserID, "Opened vehicle management form", "FrmVehicleManagement");
        }

        private void InitializeComponent()
        {
            Text = "Quản lý phương tiện";
            Size = new Size(1200, 850);
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

            pnl.Controls.Add(new Label { Text = "Cư dân:", Location = new Point(SPACING, SPACING), Size = new Size(70, 20) });

            cmbFilterResident.Location = new Point(SPACING + 80, SPACING);
            cmbFilterResident.Size = new Size(180, 25);
            cmbFilterResident.DropDownStyle = ComboBoxStyle.DropDownList;
            pnl.Controls.Add(cmbFilterResident);

            pnl.Controls.Add(new Label { Text = "Biển số:", Location = new Point(SPACING + 270, SPACING), Size = new Size(90, 20) });

            txtSearchLicensePlate.Location = new Point(SPACING + 365, SPACING);
            txtSearchLicensePlate.Size = new Size(150, 25);
            pnl.Controls.Add(txtSearchLicensePlate);

            btnSearch.Text = "Tìm kiếm";
            btnSearch.Location = new Point(SPACING + 525, SPACING);
            btnSearch.Size = new Size(90, 30);
            btnSearch.BackColor = Color.FromArgb(PRIMARY_COLOR);
            btnSearch.ForeColor = Color.White;
            btnSearch.Click += BtnSearch_Click;
            pnl.Controls.Add(btnSearch);

            btnShowAll.Text = "Hiển thị tất cả";
            btnShowAll.Location = new Point(SPACING + 625, SPACING);
            btnShowAll.Size = new Size(120, 30);
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
                Size = new Size(ClientSize.Width - 2 * SPACING, 160),
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

            lblLicensePlate.Text = "Biển số:";
            lblLicensePlate.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y);
            lblLicensePlate.Size = new Size(LABEL_WIDTH, 20);
            pnl.Controls.Add(lblLicensePlate);

            txtLicensePlate.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y);
            txtLicensePlate.Size = new Size(CONTROL_WIDTH, 25);
            pnl.Controls.Add(txtLicensePlate);

            lblVehicleType.Text = "Loại phương tiện:";
            lblVehicleType.Location = new Point(x, y + 35);
            lblVehicleType.Size = new Size(LABEL_WIDTH, 20);
            pnl.Controls.Add(lblVehicleType);

            cmbVehicleType.Location = new Point(x + LABEL_WIDTH + SPACING, y + 35);
            cmbVehicleType.Size = new Size(CONTROL_WIDTH, 25);
            cmbVehicleType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVehicleType.Items.AddRange(new object[]
            {
                new UiComboItem("Ô tô", "Car"),
                new UiComboItem("Xe máy", "Motorcycle"),
                new UiComboItem("Xe tải", "Truck"),
                new UiComboItem("Khác", "Other")
            });
            pnl.Controls.Add(cmbVehicleType);

            lblBrand.Text = "Hãng:";
            lblBrand.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 35);
            lblBrand.Size = new Size(LABEL_WIDTH, 20);
            pnl.Controls.Add(lblBrand);

            txtBrand.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 35);
            txtBrand.Size = new Size(CONTROL_WIDTH, 25);
            pnl.Controls.Add(txtBrand);

            lblModel.Text = "Dòng xe:";
            lblModel.Location = new Point(x, y + 70);
            lblModel.Size = new Size(LABEL_WIDTH, 20);
            pnl.Controls.Add(lblModel);

            txtModel.Location = new Point(x + LABEL_WIDTH + SPACING, y + 70);
            txtModel.Size = new Size(CONTROL_WIDTH, 25);
            pnl.Controls.Add(txtModel);

            lblYearMade.Text = "Năm sản xuất:";
            lblYearMade.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 70);
            lblYearMade.Size = new Size(LABEL_WIDTH, 20);
            pnl.Controls.Add(lblYearMade);

            nudYearMade.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 70);
            nudYearMade.Size = new Size(CONTROL_WIDTH, 25);
            nudYearMade.Minimum = 1980;
            nudYearMade.Maximum = DateTime.Now.Year;
            nudYearMade.Value = DateTime.Now.Year;
            pnl.Controls.Add(nudYearMade);

            lblColor.Text = "Màu sắc:";
            lblColor.Location = new Point(x, y + 105);
            lblColor.Size = new Size(LABEL_WIDTH, 20);
            pnl.Controls.Add(lblColor);

            txtColor.Location = new Point(x + LABEL_WIDTH + SPACING, y + 105);
            txtColor.Size = new Size(CONTROL_WIDTH, 25);
            pnl.Controls.Add(txtColor);

            lblNote.Text = "Ghi chú:";
            lblNote.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 105);
            lblNote.Size = new Size(LABEL_WIDTH, 20);
            pnl.Controls.Add(lblNote);

            txtNote.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 105);
            txtNote.Size = new Size(CONTROL_WIDTH, 25);
            pnl.Controls.Add(txtNote);

            return pnl;
        }

        private Panel CreateGridPanel()
        {
            var pnl = new Panel
            {
                Location = new Point(SPACING, 240),
                Size = new Size(ClientSize.Width - 2 * SPACING, 330),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            dgvVehicles.Dock = DockStyle.Fill;
            dgvVehicles.AllowUserToAddRows = false;
            dgvVehicles.AllowUserToDeleteRows = false;
            dgvVehicles.ReadOnly = true;
            dgvVehicles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvVehicles.MultiSelect = false;
            dgvVehicles.RowHeadersVisible = false;
            dgvVehicles.CellClick += DgvVehicles_CellClick;

            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "VehicleID", Width = 50, Visible = false });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Biển số", DataPropertyName = "LicensePlate", Width = 120 });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Loại", DataPropertyName = "VehicleType", Width = 100 });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hãng", DataPropertyName = "Brand", Width = 100 });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dòng xe", DataPropertyName = "Model", Width = 120 });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Năm", DataPropertyName = "YearMade", Width = 70 });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Màu", DataPropertyName = "Color", Width = 80 });
            dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chủ sở hữu", DataPropertyName = "ResidentName", Width = 150 });

            pnl.Controls.Add(dgvVehicles);
            return pnl;
        }

        private Panel CreateButtonPanel()
        {
            var pnl = new Panel
            {
                Location = new Point(SPACING, 580),
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

            btnStatistics.Text = "Thống kê";
            btnStatistics.Location = new Point(x + 330, y);
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
                Location = new Point(SPACING, 640),
                Size = new Size(ClientSize.Width - 2 * SPACING, 60),
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblStatusBar.Text = "Sẵn sàng";
            lblStatusBar.Location = new Point(SPACING, SPACING);
            lblStatusBar.Size = new Size(400, 20);
            lblStatusBar.ForeColor = Color.Green;
            pnl.Controls.Add(lblStatusBar);

            lblVehicleInfo.Text = "Tổng phương tiện: 0";
            lblVehicleInfo.Location = new Point(SPACING, SPACING + 25);
            lblVehicleInfo.Size = new Size(400, 20);
            pnl.Controls.Add(lblVehicleInfo);

            return pnl;
        }

        private void LoadData()
        {
            try
            {
                var vehicles = VehicleDAL.GetAllVehicles();
                dgvVehicles.DataSource = vehicles.Cast<dynamic>().ToList();
                lblStatusBar.Text = $"Đã tải {vehicles.Count} phương tiện";
                lblVehicleInfo.Text = $"Tổng phương tiện: {vehicles.Count}";
                LoadResidents();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading vehicles");
                MessageBox.Show($"Lỗi khi tải phương tiện: {ex.Message}", "Lỗi");
            }
        }

        private void LoadResidents()
        {
            try
            {
                var residents = ResidentDAL.GetAllResidents();
                cmbResident.Items.Clear();
                cmbFilterResident.Items.Clear();
                cmbFilterResident.AddOption("Tất cả", 0);

                foreach (var resident in residents)
                {
                    cmbResident.AddOption(resident.FullName ?? $"Cư dân {resident.ResidentID}", resident.ResidentID);
                    cmbFilterResident.AddOption(resident.FullName ?? $"Cư dân {resident.ResidentID}", resident.ResidentID);
                }

                if (cmbResident.Items.Count > 0)
                {
                    cmbResident.SelectedIndex = 0;
                }

                cmbFilterResident.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading residents");
                MessageBox.Show($"Lỗi khi tải cư dân: {ex.Message}", "Lỗi");
            }
        }

        private void DgvVehicles_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            try
            {
                _selectedVehicleID = Convert.ToInt32(dgvVehicles[0, e.RowIndex].Value);
                var vehicle = VehicleDAL.GetVehicleByID(_selectedVehicleID);

                if (vehicle != null)
                {
                    txtLicensePlate.Text = vehicle.LicensePlate ?? string.Empty;
                    ComboBoxHelper.SelectValue(cmbVehicleType, vehicle.VehicleType);
                    txtBrand.Text = vehicle.Brand ?? string.Empty;
                    txtModel.Text = vehicle.Model ?? string.Empty;
                    nudYearMade.Value = vehicle.YearMade;
                    txtColor.Text = vehicle.Color ?? string.Empty;
                    txtNote.Text = vehicle.Note ?? string.Empty;
                    ComboBoxHelper.SelectValue(cmbResident, vehicle.ResidentID);

                    lblStatusBar.Text = $"Đã chọn: {vehicle.LicensePlate} - {vehicle.Brand} {vehicle.Model}";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading vehicle details");
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (cmbResident.GetSelectedValueInt() == 0)
            {
                MessageBox.Show("Vui lòng chọn cư dân.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var result = VehicleBLL.CreateVehicle(
                    cmbResident.GetSelectedValueInt(),
                    txtLicensePlate.Text.Trim(),
                    string.IsNullOrWhiteSpace(cmbVehicleType.GetSelectedValueString()) ? "Other" : cmbVehicleType.GetSelectedValueString(),
                    txtBrand.Text.Trim(),
                    txtModel.Text.Trim(),
                    (int)nudYearMade.Value,
                    txtColor.Text.Trim(),
                    txtNote.Text.Trim());

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AuditLogDAL.LogAction(SessionManager.GetSession()!.UserID, $"Created vehicle {txtLicensePlate.Text}", "FrmVehicleManagement");
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
                Log.Error(ex, "Error creating vehicle");
                MessageBox.Show($"Lỗi khi tạo phương tiện: {ex.Message}", "Lỗi");
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (_selectedVehicleID == 0)
            {
                MessageBox.Show("Vui lòng chọn phương tiện.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var result = VehicleBLL.UpdateVehicle(
                    _selectedVehicleID,
                    txtLicensePlate.Text.Trim(),
                    string.IsNullOrWhiteSpace(cmbVehicleType.GetSelectedValueString()) ? "Other" : cmbVehicleType.GetSelectedValueString(),
                    txtBrand.Text.Trim(),
                    txtModel.Text.Trim(),
                    (int)nudYearMade.Value,
                    txtColor.Text.Trim(),
                    txtNote.Text.Trim());

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AuditLogDAL.LogAction(SessionManager.GetSession()!.UserID, $"Updated vehicle {txtLicensePlate.Text}", "FrmVehicleManagement");
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
                Log.Error(ex, "Error updating vehicle");
                MessageBox.Show($"Lỗi khi cập nhật phương tiện: {ex.Message}", "Lỗi");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedVehicleID == 0)
            {
                MessageBox.Show("Vui lòng chọn phương tiện.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show("Bạn có chắc muốn xóa phương tiện này không?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var result = VehicleBLL.DeleteVehicle(_selectedVehicleID);

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AuditLogDAL.LogAction(SessionManager.GetSession()!.UserID, $"Deleted vehicle {txtLicensePlate.Text}", "FrmVehicleManagement");
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
                Log.Error(ex, "Error deleting vehicle");
                MessageBox.Show($"Lỗi khi xóa phương tiện: {ex.Message}", "Lỗi");
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                var filtered = VehicleDAL.GetAllVehicles().AsEnumerable();

                int residentID = cmbFilterResident.GetSelectedValueInt();
                if (residentID != 0)
                {
                    filtered = filtered.Where(v => v.ResidentID == residentID);
                }

                if (!string.IsNullOrWhiteSpace(txtSearchLicensePlate.Text))
                {
                    filtered = filtered.Where(v => (v.LicensePlate ?? string.Empty)
                        .Contains(txtSearchLicensePlate.Text, StringComparison.OrdinalIgnoreCase));
                }

                dgvVehicles.DataSource = filtered.ToList();
                lblStatusBar.Text = $"Tìm thấy {filtered.Count()} phương tiện";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching vehicles");
                MessageBox.Show($"Lỗi tìm kiếm: {ex.Message}", "Lỗi");
            }
        }

        private void BtnStatistics_Click(object sender, EventArgs e)
        {
            try
            {
                var stats = VehicleBLL.GetVehicleStatistics();
                if (stats != null)
                {
                    string message =
                        $"Tổng phương tiện: {stats.TotalVehicles}\n" +
                        $"Theo loại:\n" +
                        $"  Ô tô: {stats.CarCount}\n" +
                        $"  Xe máy: {stats.MotorcycleCount}\n" +
                        $"  Xe tải: {stats.TruckCount}\n" +
                        $"  Khác: {stats.OtherCount}";

                    MessageBox.Show(message, "Thống kê phương tiện", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting vehicle statistics");
                MessageBox.Show($"Lỗi khi lấy thống kê: {ex.Message}", "Lỗi");
            }
        }

        private void ClearForm()
        {
            txtLicensePlate.Clear();
            cmbVehicleType.SelectedIndex = -1;
            txtBrand.Clear();
            txtModel.Clear();
            nudYearMade.Value = DateTime.Now.Year;
            txtColor.Clear();
            txtNote.Clear();

            if (cmbResident.Items.Count > 0)
            {
                cmbResident.SelectedIndex = 0;
            }

            _selectedVehicleID = 0;
        }
    }
}

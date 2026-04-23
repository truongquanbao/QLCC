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
    public partial class FrmFeeTypeManagement : Form
    {
        private readonly UserSession? _session = SessionManager.GetSession();
        private const int PADDING = 10;

        private Panel _filterPanel = null!;
        private Panel _detailsPanel = null!;
        private Panel _gridPanel = null!;
        private Panel _buttonPanel = null!;
        private Panel _statusPanel = null!;

        private readonly DataGridView _dgvFeeTypes = new DataGridView();
        private readonly TextBox _txtFeeTypeName = new TextBox();
        private readonly TextBox _txtDescription = new TextBox();
        private readonly TextBox _txtUnitOfMeasurement = new TextBox();
        private readonly Label _lblActiveCount = new Label();
        private readonly Label _lblInactiveCount = new Label();
        private readonly Label _lblTotalCount = new Label();

        public FrmFeeTypeManagement()
        {
            if (_session == null || !_session.HasPermission("ManageFeeTypes"))
            {
                MessageBox.Show("Bạn không có quyền truy cập màn hình này.", "Từ chối truy cập");
                Close();
                return;
            }

            InitializeForm();
        }

        private void InitializeComponent()
        {
            // Auto-generated method stub
        }

        private void InitializeForm()
        {
            Text = "Quản lý loại phí";
            Size = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Controls.Add(CreateFilterPanel());
            Controls.Add(CreateDetailsPanel());
            Controls.Add(CreateGridPanel());
            Controls.Add(CreateButtonPanel());
            Controls.Add(CreateStatusPanel());

            Load += (s, e) => LoadData();
        }

        private Panel CreateFilterPanel()
        {
            _filterPanel = new Panel { Dock = DockStyle.Top, Height = 80, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightGray };
            _filterPanel.Controls.Add(new Label { Text = "Tên loại phí:", Left = PADDING, Top = PADDING, Width = 100 });
            _txtFeeTypeName.Location = new Point(120, PADDING);
            _txtFeeTypeName.Width = 250;
            _filterPanel.Controls.Add(_txtFeeTypeName);

            var btnSearch = new Button { Text = "Tìm kiếm", Left = 390, Top = PADDING, Width = 80, Height = 25 };
            btnSearch.Click += (s, e) => LoadFeeTypes();
            _filterPanel.Controls.Add(btnSearch);

            _filterPanel.Controls.Add(new Label { Text = "Cấu hình loại phí cho hóa đơn căn hộ", Left = PADDING, Top = 40, Width = 400 });
            return _filterPanel;
        }

        private Panel CreateDetailsPanel()
        {
            _detailsPanel = new Panel { Dock = DockStyle.Top, Height = 100, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightGray };

            _detailsPanel.Controls.Add(new Label { Text = "Mô tả:", Left = PADDING, Top = PADDING, Width = 100 });
            _txtDescription.Location = new Point(120, PADDING);
            _txtDescription.Size = new Size(850, 30);
            _txtDescription.Multiline = true;
            _detailsPanel.Controls.Add(_txtDescription);

            _detailsPanel.Controls.Add(new Label { Text = "Đơn vị tính:", Left = PADDING, Top = 50, Width = 120 });
            _txtUnitOfMeasurement.Location = new Point(140, 50);
            _txtUnitOfMeasurement.Width = 200;
            _detailsPanel.Controls.Add(_txtUnitOfMeasurement);

            return _detailsPanel;
        }

        private Panel CreateGridPanel()
        {
            _gridPanel = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };

            _dgvFeeTypes.Dock = DockStyle.Fill;
            _dgvFeeTypes.ReadOnly = true;
            _dgvFeeTypes.AllowUserToAddRows = false;
            _dgvFeeTypes.AllowUserToDeleteRows = false;
            _dgvFeeTypes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _dgvFeeTypes.AutoGenerateColumns = false;
            _dgvFeeTypes.BackgroundColor = Color.White;
            _dgvFeeTypes.CellClick += DgvFeeTypes_CellClick;

            _dgvFeeTypes.Columns.Add(new DataGridViewTextBoxColumn { Name = "FeeTypeID", HeaderText = "ID", DataPropertyName = "FeeTypeID", Width = 50 });
            _dgvFeeTypes.Columns.Add(new DataGridViewTextBoxColumn { Name = "FeeTypeName", HeaderText = "Loại phí", DataPropertyName = "FeeTypeName", Width = 200 });
            _dgvFeeTypes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Mô tả", DataPropertyName = "Description", Width = 350 });
            _dgvFeeTypes.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitOfMeasurement", HeaderText = "Đơn vị", DataPropertyName = "UnitOfMeasurement", Width = 100 });
            _dgvFeeTypes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái", DataPropertyName = "Status", Width = 100 });

            _gridPanel.Controls.Add(_dgvFeeTypes);
            return _gridPanel;
        }

        private Panel CreateButtonPanel()
        {
            _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightGray };

            var btnCreate = new Button { Text = "Thêm", Left = PADDING, Top = 10, Width = 80, Height = 30 };
            btnCreate.Click += BtnCreate_Click;
            _buttonPanel.Controls.Add(btnCreate);

            var btnEdit = new Button { Text = "Sửa", Left = 100, Top = 10, Width = 80, Height = 30 };
            btnEdit.Click += BtnEdit_Click;
            _buttonPanel.Controls.Add(btnEdit);

            var btnDelete = new Button { Text = "Xóa", Left = 180, Top = 10, Width = 80, Height = 30 };
            btnDelete.Click += BtnDelete_Click;
            _buttonPanel.Controls.Add(btnDelete);

            var btnActivate = new Button { Text = "Kích hoạt", Left = 260, Top = 10, Width = 90, Height = 30 };
            btnActivate.Click += BtnActivate_Click;
            _buttonPanel.Controls.Add(btnActivate);

            var btnDeactivate = new Button { Text = "Vô hiệu hóa", Left = 360, Top = 10, Width = 100, Height = 30 };
            btnDeactivate.Click += BtnDeactivate_Click;
            _buttonPanel.Controls.Add(btnDeactivate);

            var btnRefresh = new Button { Text = "Làm mới", Left = 470, Top = 10, Width = 80, Height = 30 };
            btnRefresh.Click += (s, e) => LoadFeeTypes();
            _buttonPanel.Controls.Add(btnRefresh);

            return _buttonPanel;
        }

        private Panel CreateStatusPanel()
        {
            _statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Gainsboro };

            _lblTotalCount.Text = "Tổng: 0";
            _lblTotalCount.Left = PADDING;
            _lblTotalCount.Top = 10;
            _lblTotalCount.Width = 100;
            _statusPanel.Controls.Add(_lblTotalCount);

            _lblActiveCount.Text = "Đang hoạt động: 0";
            _lblActiveCount.Left = 120;
            _lblActiveCount.Top = 10;
            _lblActiveCount.Width = 140;
            _statusPanel.Controls.Add(_lblActiveCount);

            _lblInactiveCount.Text = "Ngừng hoạt động: 0";
            _lblInactiveCount.Left = 280;
            _lblInactiveCount.Top = 10;
            _lblInactiveCount.Width = 150;
            _statusPanel.Controls.Add(_lblInactiveCount);

            return _statusPanel;
        }

        private void LoadData()
        {
            try
            {
                LoadFeeTypes();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading fee type data");
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi");
            }
        }

        private void LoadFeeTypes()
        {
            try
            {
                var feeTypes = FeeTypeDAL.GetAllFeeTypes();

                if (!string.IsNullOrWhiteSpace(_txtFeeTypeName.Text))
                {
                    feeTypes = feeTypes.Where(f => f.FeeTypeName.Contains(_txtFeeTypeName.Text)).ToList();
                }

                _dgvFeeTypes.DataSource = feeTypes.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading fee types");
                MessageBox.Show($"Lỗi khi tải loại phí: {ex.Message}", "Lỗi");
            }
        }

        private void DgvFeeTypes_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            try
            {
                var row = _dgvFeeTypes.Rows[e.RowIndex];
                int feeTypeID = Convert.ToInt32(row.Cells["FeeTypeID"].Value);
                var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);

                if (feeType != null)
                {
                    _txtFeeTypeName.Text = feeType.FeeTypeName ?? string.Empty;
                    _txtDescription.Text = feeType.Description ?? string.Empty;
                    _txtUnitOfMeasurement.Text = feeType.UnitOfMeasurement ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading fee type details");
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                var dialog = new FrmFeeTypeDialog(null);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var (feeTypeName, description, unitOfMeasurement) = dialog.GetFeeTypeData();
                    var result = FeeTypeBLL.CreateFeeType(feeTypeName, description, unitOfMeasurement);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Thành công");
                        AuditLogDAL.LogAction(_session!.UserID, "CreateFeeType", $"Fee Type '{feeTypeName}' created");
                        LoadFeeTypes();
                        UpdateStatistics();
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Lỗi");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating fee type");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvFeeTypes.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn loại phí cần sửa.", "Thông báo");
                    return;
                }

                int feeTypeID = Convert.ToInt32(_dgvFeeTypes.SelectedRows[0].Cells["FeeTypeID"].Value);
                var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);

                var dialog = new FrmFeeTypeDialog(feeType);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var (feeTypeName, description, unitOfMeasurement) = dialog.GetFeeTypeData();
                    var result = FeeTypeBLL.UpdateFeeType(feeTypeID, feeTypeName, description, unitOfMeasurement);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Thành công");
                        AuditLogDAL.LogAction(_session!.UserID, "UpdateFeeType", $"Fee Type {feeTypeID} updated");
                        LoadFeeTypes();
                        UpdateStatistics();
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Lỗi");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error editing fee type");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvFeeTypes.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn loại phí cần xóa.", "Thông báo");
                    return;
                }

                int feeTypeID = Convert.ToInt32(_dgvFeeTypes.SelectedRows[0].Cells["FeeTypeID"].Value);

                if (MessageBox.Show("Bạn có chắc muốn xóa loại phí này không?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var result = FeeTypeBLL.DeleteFeeType(feeTypeID);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Thành công");
                        AuditLogDAL.LogAction(_session!.UserID, "DeleteFeeType", $"Fee Type {feeTypeID} deleted");
                        LoadFeeTypes();
                        UpdateStatistics();
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Lỗi");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting fee type");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnActivate_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvFeeTypes.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn loại phí cần kích hoạt.", "Thông báo");
                    return;
                }

                int feeTypeID = Convert.ToInt32(_dgvFeeTypes.SelectedRows[0].Cells["FeeTypeID"].Value);
                var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);

                if (feeType != null && feeType.Status == "Active")
                {
                    MessageBox.Show("Loại phí đã đang hoạt động.", "Thông báo");
                    return;
                }

                bool updated = FeeTypeDAL.UpdateFeeTypeStatus(feeTypeID, "Active");
                if (updated)
                {
                    MessageBox.Show("Kích hoạt loại phí thành công.", "Thành công");
                    AuditLogDAL.LogAction(_session!.UserID, "ActivateFeeType", $"Fee Type {feeTypeID} activated");
                    LoadFeeTypes();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show("Không thể kích hoạt loại phí.", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error activating fee type");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnDeactivate_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvFeeTypes.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn loại phí cần vô hiệu hóa.", "Thông báo");
                    return;
                }

                int feeTypeID = Convert.ToInt32(_dgvFeeTypes.SelectedRows[0].Cells["FeeTypeID"].Value);
                var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);

                if (feeType != null && feeType.Status == "Inactive")
                {
                    MessageBox.Show("Loại phí đã ở trạng thái ngừng hoạt động.", "Thông báo");
                    return;
                }

                bool updated = FeeTypeDAL.UpdateFeeTypeStatus(feeTypeID, "Inactive");
                if (updated)
                {
                    MessageBox.Show("Vô hiệu hóa loại phí thành công.", "Thành công");
                    AuditLogDAL.LogAction(_session!.UserID, "DeactivateFeeType", $"Fee Type {feeTypeID} deactivated");
                    LoadFeeTypes();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show("Không thể vô hiệu hóa loại phí.", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deactivating fee type");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                var stats = FeeTypeBLL.GetFeeTypeStatistics();
                _lblTotalCount.Text = $"Tổng: {stats.TotalFeeTypes}";
                _lblActiveCount.Text = $"Đang hoạt động: {stats.ActiveCount}";
                _lblInactiveCount.Text = $"Ngừng hoạt động: {stats.InactiveCount}";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating statistics");
            }
        }
    }

    public partial class FrmFeeTypeDialog : Form
    {
        private readonly dynamic _feeType;
        private readonly TextBox _txtFeeTypeName = new TextBox();
        private readonly TextBox _txtDescription = new TextBox();
        private readonly TextBox _txtUnitOfMeasurement = new TextBox();

        public FrmFeeTypeDialog(dynamic feeType)
        {
            _feeType = feeType;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            Text = _feeType == null ? "Tạo loại phí" : "Sửa loại phí";
            Size = new Size(450, 280);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            int y = 10;

            Controls.Add(new Label { Text = "Tên loại phí:", Left = 10, Top = y, Width = 120 });
            _txtFeeTypeName.Location = new Point(140, y);
            _txtFeeTypeName.Width = 280;
            Controls.Add(_txtFeeTypeName);

            y += 35;
            Controls.Add(new Label { Text = "Mô tả:", Left = 10, Top = y, Width = 120 });
            _txtDescription.Location = new Point(140, y);
            _txtDescription.Size = new Size(280, 60);
            _txtDescription.Multiline = true;
            Controls.Add(_txtDescription);

            y += 70;
            Controls.Add(new Label { Text = "Đơn vị tính:", Left = 10, Top = y, Width = 120 });
            _txtUnitOfMeasurement.Location = new Point(140, y);
            _txtUnitOfMeasurement.Width = 280;
            Controls.Add(_txtUnitOfMeasurement);

            y += 35;
            var btnOK = new Button { Text = "Đồng ý", Left = 230, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Hủy", Left = 340, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
            Controls.Add(btnOK);
            Controls.Add(btnCancel);

            if (_feeType != null)
            {
                _txtFeeTypeName.Text = _feeType.FeeTypeName ?? string.Empty;
                _txtDescription.Text = _feeType.Description ?? string.Empty;
                _txtUnitOfMeasurement.Text = _feeType.UnitOfMeasurement ?? string.Empty;
            }
        }

        public (string FeeTypeName, string Description, string UnitOfMeasurement) GetFeeTypeData()
        {
            return (_txtFeeTypeName.Text, _txtDescription.Text, _txtUnitOfMeasurement.Text);
        }
    }
}

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
    public partial class FrmVisitorManagement : Form
    {
        private readonly UserSession? _session = SessionManager.GetSession();
        private const int PADDING = 10;

        private Panel _filterPanel = null!;
        private Panel _detailsPanel = null!;
        private Panel _gridPanel = null!;
        private Panel _buttonPanel = null!;
        private Panel _statusPanel = null!;

        private readonly DataGridView _dgvVisitors = new DataGridView();
        private readonly ComboBox _cboApartment = new ComboBox();
        private readonly ComboBox _cboResident = new ComboBox();
        private readonly ComboBox _cboVisitorType = new ComboBox();
        private readonly ComboBox _cboStatus = new ComboBox();
        private readonly DateTimePicker _dtpCheckInDate = new DateTimePicker();
        private readonly TextBox _txtVisitorName = new TextBox();
        private readonly TextBox _txtPhone = new TextBox();
        private readonly TextBox _txtEmail = new TextBox();
        private readonly TextBox _txtPurpose = new TextBox();
        private readonly TextBox _txtCheckInTime = new TextBox();
        private readonly TextBox _txtCheckOutTime = new TextBox();
        private readonly Label _lblTodayVisitors = new Label();
        private readonly Label _lblCheckInCount = new Label();
        private readonly Label _lblCheckOutCount = new Label();

        public FrmVisitorManagement()
        {
            if (_session == null || !_session.HasPermission("ManageVisitors"))
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
            Text = "Quản lý khách ra vào";
            Size = new Size(1200, 700);
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
            _filterPanel = new Panel { Dock = DockStyle.Top, Height = 110, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightGray };
            int y = PADDING;

            _filterPanel.Controls.Add(new Label { Text = "Căn hộ:", Left = PADDING, Top = y, Width = 100 });
            _cboApartment.Location = new Point(120, y);
            _cboApartment.Size = new Size(150, 25);
            _cboApartment.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboApartment.SelectedIndexChanged += (s, e) => LoadResidents();
            _filterPanel.Controls.Add(_cboApartment);

            _filterPanel.Controls.Add(new Label { Text = "Cư dân:", Left = 290, Top = y, Width = 100 });
            _cboResident.Location = new Point(410, y);
            _cboResident.Size = new Size(150, 25);
            _cboResident.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboResident.SelectedIndexChanged += (s, e) => LoadVisitors();
            _filterPanel.Controls.Add(_cboResident);

            _filterPanel.Controls.Add(new Label { Text = "Loại khách:", Left = 580, Top = y, Width = 100 });
            _cboVisitorType.Location = new Point(700, y);
            _cboVisitorType.Size = new Size(150, 25);
            _cboVisitorType.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboVisitorType.Items.AddRange(new object[]
            {
                new UiComboItem("Tất cả", "All"),
                new UiComboItem("Khách", "Guest"),
                new UiComboItem("Giao hàng", "Delivery"),
                new UiComboItem("Dịch vụ", "Service"),
                new UiComboItem("Người thân", "Family"),
                new UiComboItem("Khác", "Other")
            });
            _cboVisitorType.SelectedIndex = 0;
            _cboVisitorType.SelectedIndexChanged += (s, e) => LoadVisitors();
            _filterPanel.Controls.Add(_cboVisitorType);

            _filterPanel.Controls.Add(new Label { Text = "Trạng thái:", Left = 870, Top = y, Width = 100 });
            _cboStatus.Location = new Point(970, y);
            _cboStatus.Size = new Size(150, 25);
            _cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboStatus.Items.AddRange(new object[]
            {
                new UiComboItem("Tất cả", "All"),
                new UiComboItem("Đang vào", "CheckedIn"),
                new UiComboItem("Đã rời", "CheckedOut"),
                new UiComboItem("Chờ xử lý", "Pending")
            });
            _cboStatus.SelectedIndex = 0;
            _cboStatus.SelectedIndexChanged += (s, e) => LoadVisitors();
            _filterPanel.Controls.Add(_cboStatus);

            y += 40;

            _filterPanel.Controls.Add(new Label { Text = "Ngày check-in:", Left = PADDING, Top = y, Width = 100 });
            _dtpCheckInDate.Location = new Point(120, y);
            _dtpCheckInDate.Width = 150;
            _dtpCheckInDate.ValueChanged += (s, e) => LoadVisitors();
            _filterPanel.Controls.Add(_dtpCheckInDate);

            _filterPanel.Controls.Add(new Label { Text = "Tên khách:", Left = 290, Top = y, Width = 100 });
            _txtVisitorName.Location = new Point(410, y);
            _txtVisitorName.Width = 150;
            _filterPanel.Controls.Add(_txtVisitorName);

            _filterPanel.Controls.Add(new Label { Text = "Số điện thoại:", Left = 580, Top = y, Width = 100 });
            _txtPhone.Location = new Point(700, y);
            _txtPhone.Width = 150;
            _filterPanel.Controls.Add(_txtPhone);

            var btnSearch = new Button { Text = "Tìm kiếm", Left = 870, Top = y, Width = 80, Height = 25 };
            btnSearch.Click += (s, e) => LoadVisitors();
            _filterPanel.Controls.Add(btnSearch);

            y += 35;

            _filterPanel.Controls.Add(new Label { Text = "Email:", Left = PADDING, Top = y, Width = 100 });
            _txtEmail.Location = new Point(120, y);
            _txtEmail.Width = 150;
            _filterPanel.Controls.Add(_txtEmail);

            _filterPanel.Controls.Add(new Label { Text = "Mục đích:", Left = 290, Top = y, Width = 100 });
            _txtPurpose.Location = new Point(410, y);
            _txtPurpose.Size = new Size(150, 35);
            _txtPurpose.Multiline = true;
            _filterPanel.Controls.Add(_txtPurpose);

            return _filterPanel;
        }

        private Panel CreateDetailsPanel()
        {
            _detailsPanel = new Panel { Dock = DockStyle.Top, Height = 80, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightGray };
            int y = PADDING;

            _detailsPanel.Controls.Add(new Label { Text = "Giờ check-in:", Left = PADDING, Top = y, Width = 100 });
            _txtCheckInTime.Location = new Point(120, y);
            _txtCheckInTime.Width = 150;
            _txtCheckInTime.ReadOnly = true;
            _detailsPanel.Controls.Add(_txtCheckInTime);

            _detailsPanel.Controls.Add(new Label { Text = "Giờ check-out:", Left = 290, Top = y, Width = 100 });
            _txtCheckOutTime.Location = new Point(410, y);
            _txtCheckOutTime.Width = 150;
            _txtCheckOutTime.ReadOnly = true;
            _detailsPanel.Controls.Add(_txtCheckOutTime);

            y += 35;
            _detailsPanel.Controls.Add(new Label { Text = "Trạng thái sẽ cập nhật sau khi check-in/check-out", Left = PADDING, Top = y, Width = 400 });

            return _detailsPanel;
        }

        private Panel CreateGridPanel()
        {
            _gridPanel = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };

            _dgvVisitors.Dock = DockStyle.Fill;
            _dgvVisitors.ReadOnly = true;
            _dgvVisitors.AllowUserToAddRows = false;
            _dgvVisitors.AllowUserToDeleteRows = false;
            _dgvVisitors.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _dgvVisitors.AutoGenerateColumns = false;
            _dgvVisitors.BackgroundColor = Color.White;

            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "VisitorID", HeaderText = "ID", DataPropertyName = "VisitorID", Width = 50 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Apartment", HeaderText = "Căn hộ", DataPropertyName = "ApartmentCode", Width = 100 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Resident", HeaderText = "Cư dân", DataPropertyName = "ResidentName", Width = 120 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "VisitorName", HeaderText = "Tên khách", DataPropertyName = "VisitorName", Width = 120 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "VisitorType", HeaderText = "Loại", DataPropertyName = "VisitorType", Width = 80 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "CheckInTime", HeaderText = "Check-in", DataPropertyName = "CheckInTime", Width = 120 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "CheckOutTime", HeaderText = "Check-out", DataPropertyName = "CheckOutTime", Width = 120 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái", DataPropertyName = "Status", Width = 100 });

            _dgvVisitors.CellClick += DgvVisitors_CellClick;
            _gridPanel.Controls.Add(_dgvVisitors);
            return _gridPanel;
        }

        private Panel CreateButtonPanel()
        {
            _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightGray };

            var btnCheckIn = new Button { Text = "Check-in", Left = PADDING, Top = 10, Width = 80, Height = 30 };
            btnCheckIn.Click += BtnCheckIn_Click;
            _buttonPanel.Controls.Add(btnCheckIn);

            var btnCheckOut = new Button { Text = "Check-out", Left = 100, Top = 10, Width = 80, Height = 30 };
            btnCheckOut.Click += BtnCheckOut_Click;
            _buttonPanel.Controls.Add(btnCheckOut);

            var btnRegisterNew = new Button { Text = "Đăng ký mới", Left = 180, Top = 10, Width = 100, Height = 30 };
            btnRegisterNew.Click += BtnRegisterNew_Click;
            _buttonPanel.Controls.Add(btnRegisterNew);

            var btnDelete = new Button { Text = "Xóa", Left = 290, Top = 10, Width = 80, Height = 30 };
            btnDelete.Click += BtnDelete_Click;
            _buttonPanel.Controls.Add(btnDelete);

            var btnStatistics = new Button { Text = "Thống kê", Left = 380, Top = 10, Width = 80, Height = 30 };
            btnStatistics.Click += BtnStatistics_Click;
            _buttonPanel.Controls.Add(btnStatistics);

            var btnRefresh = new Button { Text = "Làm mới", Left = 470, Top = 10, Width = 80, Height = 30 };
            btnRefresh.Click += (s, e) => LoadVisitors();
            _buttonPanel.Controls.Add(btnRefresh);

            return _buttonPanel;
        }

        private Panel CreateStatusPanel()
        {
            _statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Gainsboro };

            _lblTodayVisitors.Text = "Khách hôm nay: 0";
            _lblTodayVisitors.Left = PADDING;
            _lblTodayVisitors.Top = 10;
            _lblTodayVisitors.Width = 150;
            _statusPanel.Controls.Add(_lblTodayVisitors);

            _lblCheckInCount.Text = "Đang vào: 0";
            _lblCheckInCount.Left = 170;
            _lblCheckInCount.Top = 10;
            _lblCheckInCount.Width = 120;
            _statusPanel.Controls.Add(_lblCheckInCount);

            _lblCheckOutCount.Text = "Đã rời: 0";
            _lblCheckOutCount.Left = 310;
            _lblCheckOutCount.Top = 10;
            _lblCheckOutCount.Width = 120;
            _statusPanel.Controls.Add(_lblCheckOutCount);

            return _statusPanel;
        }

        private void LoadData()
        {
            try
            {
                LoadApartments();
                LoadVisitors();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading visitor data");
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi");
            }
        }

        private void LoadApartments()
        {
            try
            {
                _cboApartment.Items.Clear();
                _cboApartment.AddOption("Tất cả căn hộ", 0);

                foreach (var apartment in ApartmentDAL.GetAllApartments())
                {
                    _cboApartment.AddOption(apartment.ApartmentCode ?? $"Căn hộ {apartment.ApartmentID}", apartment.ApartmentID);
                }

                _cboApartment.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading apartments");
            }
        }

        private void LoadResidents()
        {
            try
            {
                _cboResident.Items.Clear();
                _cboResident.AddOption("Tất cả cư dân", 0);

                int apartmentID = _cboApartment.GetSelectedValueInt();
                var residents = apartmentID != 0
                    ? ResidentDAL.GetResidentsByApartment(apartmentID)
                    : ResidentDAL.GetAllResidents();

                foreach (var resident in residents)
                {
                    _cboResident.AddOption(resident.FullName ?? $"Cư dân {resident.ResidentID}", resident.ResidentID);
                }

                _cboResident.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading residents");
            }
        }

        private void LoadVisitors()
        {
            try
            {
                var visitors = VisitorDAL.GetAllVisitors();

                int apartmentID = _cboApartment.GetSelectedValueInt();
                if (apartmentID != 0)
                {
                    visitors = visitors.Where(v => v.ApartmentID == apartmentID).ToList();
                }

                int residentID = _cboResident.GetSelectedValueInt();
                if (residentID != 0)
                {
                    visitors = visitors.Where(v => v.ResidentID == residentID).ToList();
                }

                string visitorType = _cboVisitorType.GetSelectedValueString();
                if (!string.Equals(visitorType, "All", StringComparison.OrdinalIgnoreCase))
                {
                    visitors = visitors.Where(v => v.VisitorType == visitorType).ToList();
                }

                string status = _cboStatus.GetSelectedValueString();
                if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
                {
                    visitors = visitors.Where(v =>
                        status == "CheckedIn" ? v.CheckOutTime == null :
                        status == "CheckedOut" ? v.CheckOutTime != null :
                        v.Status == "Pending").ToList();
                }

                visitors = visitors.Where(v => v.CheckInTime.Date == _dtpCheckInDate.Value.Date).ToList();

                if (!string.IsNullOrWhiteSpace(_txtVisitorName.Text))
                {
                    visitors = visitors.Where(v => v.VisitorName.Contains(_txtVisitorName.Text)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(_txtPhone.Text))
                {
                    visitors = visitors.Where(v => v.Phone != null && v.Phone.Contains(_txtPhone.Text)).ToList();
                }

                _dgvVisitors.DataSource = visitors.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading visitors");
                MessageBox.Show($"Lỗi khi tải khách ra vào: {ex.Message}", "Lỗi");
            }
        }

        private void DgvVisitors_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            try
            {
                var row = _dgvVisitors.Rows[e.RowIndex];
                int visitorID = Convert.ToInt32(row.Cells["VisitorID"].Value);
                var visitor = VisitorDAL.GetVisitorByID(visitorID);

                if (visitor != null)
                {
                    _txtCheckInTime.Text = visitor.CheckInTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
                    _txtCheckOutTime.Text = visitor.CheckOutTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
                    _txtVisitorName.Text = visitor.VisitorName ?? string.Empty;
                    _txtPhone.Text = visitor.Phone ?? string.Empty;
                    _txtEmail.Text = visitor.Email ?? string.Empty;
                    _txtPurpose.Text = visitor.Purpose ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading visitor details");
            }
        }

        private void BtnCheckIn_Click(object sender, EventArgs e)
        {
            try
            {
                if (_cboResident.GetSelectedValueInt() == 0)
                {
                    MessageBox.Show("Vui lòng chọn cư dân.", "Thông báo");
                    return;
                }

                var dialog = new FrmVisitorDialog(null, _cboResident.GetSelectedValueInt(), _cboApartment.GetSelectedValueInt());
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var (visitorName, phone, email, visitorType, purpose) = dialog.GetVisitorData();
                    var result = VisitorBLL.CheckInVisitor(_cboApartment.GetSelectedValueInt(), _cboResident.GetSelectedValueInt(), visitorName, phone, email, visitorType, purpose);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Thành công");
                        AuditLogDAL.LogAction(_session!.UserID, "CheckInVisitor", $"Visitor {visitorName} checked in");
                        LoadVisitors();
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
                Log.Error(ex, "Error checking in visitor");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnCheckOut_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvVisitors.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn khách cần check-out.", "Thông báo");
                    return;
                }

                int visitorID = Convert.ToInt32(_dgvVisitors.SelectedRows[0].Cells["VisitorID"].Value);
                var visitor = VisitorDAL.GetVisitorByID(visitorID);

                if (visitor == null || visitor.CheckOutTime != null)
                {
                    MessageBox.Show("Khách đã check-out rồi.", "Thông báo");
                    return;
                }

                var result = VisitorBLL.CheckOutVisitor(visitorID, DateTime.Now);

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Thành công");
                    AuditLogDAL.LogAction(_session!.UserID, "CheckOutVisitor", $"Visitor {visitorID} checked out");
                    LoadVisitors();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show(result.Message, "Lỗi");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking out visitor");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnRegisterNew_Click(object sender, EventArgs e)
        {
            try
            {
                if (_cboResident.GetSelectedValueInt() == 0)
                {
                    MessageBox.Show("Vui lòng chọn cư dân.", "Thông báo");
                    return;
                }

                var dialog = new FrmVisitorDialog(null, _cboResident.GetSelectedValueInt(), _cboApartment.GetSelectedValueInt());
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var (visitorName, phone, email, visitorType, purpose) = dialog.GetVisitorData();
                    var result = VisitorBLL.RegisterVisitor(_cboResident.GetSelectedValueInt(), visitorName, phone, email, visitorType);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Thành công");
                        AuditLogDAL.LogAction(_session!.UserID, "RegisterVisitor", $"Visitor {visitorName} registered");
                        LoadVisitors();
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
                Log.Error(ex, "Error registering visitor");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvVisitors.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn khách cần xóa.", "Thông báo");
                    return;
                }

                int visitorID = Convert.ToInt32(_dgvVisitors.SelectedRows[0].Cells["VisitorID"].Value);

                if (MessageBox.Show("Bạn có chắc muốn xóa bản ghi khách này không?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var result = VisitorBLL.DeleteVisitor(visitorID);
                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Thành công");
                        AuditLogDAL.LogAction(_session!.UserID, "DeleteVisitor", $"Visitor {visitorID} deleted");
                        LoadVisitors();
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
                Log.Error(ex, "Error deleting visitor");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnStatistics_Click(object sender, EventArgs e)
        {
            try
            {
                var stats = VisitorBLL.GetVisitorStatistics();
                MessageBox.Show(
                    $"Tổng khách: {stats.TotalVisitors}\n" +
                    $"Khách hôm nay: {stats.TodayVisitors}\n" +
                    $"Đang vào: {stats.CheckedInCount}\n" +
                    $"Đã rời hôm nay: {stats.CheckedOutCount}\n" +
                    $"Khách: {stats.GuestCount}\n" +
                    $"Giao hàng: {stats.DeliveryCount}\n" +
                    $"Dịch vụ: {stats.ServiceCount}\n" +
                    $"Người thân: {stats.FamilyCount}\n" +
                    $"Khác: {stats.OtherCount}",
                    "Thống kê khách ra vào");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving statistics");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                var stats = VisitorBLL.GetVisitorStatistics();
                _lblTodayVisitors.Text = $"Khách hôm nay: {stats.TodayVisitors}";
                _lblCheckInCount.Text = $"Đang vào: {stats.CheckedInCount}";
                _lblCheckOutCount.Text = $"Đã rời: {stats.CheckedOutCount}";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating statistics");
            }
        }
    }

    public partial class FrmVisitorDialog : Form
    {
        private readonly dynamic _visitor;
        private readonly int _residentID;
        private readonly int _apartmentID;
        private readonly ComboBox _cboVisitorType = new ComboBox();
        private readonly TextBox _txtVisitorName = new TextBox();
        private readonly TextBox _txtPhone = new TextBox();
        private readonly TextBox _txtEmail = new TextBox();
        private readonly TextBox _txtPurpose = new TextBox();

        public FrmVisitorDialog(dynamic visitor, int residentID, int apartmentID)
        {
            _visitor = visitor;
            _residentID = residentID;
            _apartmentID = apartmentID;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            Text = _visitor == null ? "Check-in khách" : "Cập nhật khách";
            Size = new Size(450, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            int y = 10;

            Controls.Add(new Label { Text = "Tên khách:", Left = 10, Top = y, Width = 100 });
            _txtVisitorName.Location = new Point(120, y);
            _txtVisitorName.Width = 300;
            Controls.Add(_txtVisitorName);

            y += 35;
            Controls.Add(new Label { Text = "Số điện thoại:", Left = 10, Top = y, Width = 100 });
            _txtPhone.Location = new Point(120, y);
            _txtPhone.Width = 300;
            Controls.Add(_txtPhone);

            y += 35;
            Controls.Add(new Label { Text = "Email:", Left = 10, Top = y, Width = 100 });
            _txtEmail.Location = new Point(120, y);
            _txtEmail.Width = 300;
            Controls.Add(_txtEmail);

            y += 35;
            Controls.Add(new Label { Text = "Loại khách:", Left = 10, Top = y, Width = 100 });
            _cboVisitorType.Location = new Point(120, y);
            _cboVisitorType.Width = 300;
            _cboVisitorType.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboVisitorType.Items.AddRange(new object[]
            {
                new UiComboItem("Khách", "Guest"),
                new UiComboItem("Giao hàng", "Delivery"),
                new UiComboItem("Dịch vụ", "Service"),
                new UiComboItem("Người thân", "Family"),
                new UiComboItem("Khác", "Other")
            });
            _cboVisitorType.SelectedIndex = 0;
            Controls.Add(_cboVisitorType);

            y += 35;
            Controls.Add(new Label { Text = "Mục đích:", Left = 10, Top = y, Width = 100 });
            _txtPurpose.Location = new Point(120, y);
            _txtPurpose.Size = new Size(300, 50);
            _txtPurpose.Multiline = true;
            Controls.Add(_txtPurpose);

            y += 60;
            var btnOK = new Button { Text = "Đồng ý", Left = 230, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Hủy", Left = 340, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
            Controls.Add(btnOK);
            Controls.Add(btnCancel);

            if (_visitor != null)
            {
                _txtVisitorName.Text = _visitor.VisitorName ?? string.Empty;
                _txtPhone.Text = _visitor.Phone ?? string.Empty;
                _txtEmail.Text = _visitor.Email ?? string.Empty;
                ComboBoxHelper.SelectValue(_cboVisitorType, _visitor.VisitorType ?? "Guest");
                _txtPurpose.Text = _visitor.Purpose ?? string.Empty;
            }
        }

        public (string VisitorName, string Phone, string Email, string VisitorType, string Purpose) GetVisitorData()
        {
            return (
                _txtVisitorName.Text,
                _txtPhone.Text,
                _txtEmail.Text,
                _cboVisitorType.GetSelectedValueString(),
                _txtPurpose.Text
            );
        }
    }
}

using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms
{
    public partial class FrmVisitorManagement : Form
    {
        private SessionManager _session = SessionManager.Instance;
        private const int PADDING = 10;
        private const int CONTROL_HEIGHT = 25;

        // UI Controls
        private Panel _filterPanel, _detailsPanel, _gridPanel, _buttonPanel, _statusPanel;
        private DataGridView _dgvVisitors;
        private ComboBox _cboApartment, _cboResident, _cboVisitorType, _cboStatus;
        private DateTimePicker _dtpCheckInDate;
        private TextBox _txtVisitorName, _txtPhone, _txtEmail, _txtPurpose, _txtCheckInTime, _txtCheckOutTime;
        private Label _lblTodayVisitors, _lblCheckInCount, _lblCheckOutCount;

        public FrmVisitorManagement()
        {
            InitializeForm();
        }

        private void InitializeComponent()
        {
            // Auto-generated method stub
        }

        private void InitializeForm()
        {
            // Permission Check
            if (!_session.HasPermission("ManageVisitors"))
            {
                MessageBox.Show("You do not have permission to access visitor management.", "Access Denied");
                this.Close();
                return;
            }

            this.Text = "Visitor Management";
            this.Size = new System.Drawing.Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.White;

            CreateFilterPanel();
            CreateDetailsPanel();
            CreateGridPanel();
            CreateButtonPanel();
            CreateStatusPanel();

            this.Load += (s, e) => LoadData();
        }

        private void CreateFilterPanel()
        {
            _filterPanel = new Panel { Dock = DockStyle.Top, Height = 110, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_filterPanel);

            int y = PADDING;

            // Row 1: Apartment and Resident
            Label lblApartment = new Label { Text = "Apartment:", Left = PADDING, Top = y, Width = 100 };
            _cboApartment = new ComboBox { Left = 120, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboApartment.SelectedIndexChanged += (s, e) => LoadResidents();
            _filterPanel.Controls.Add(lblApartment);
            _filterPanel.Controls.Add(_cboApartment);

            Label lblResident = new Label { Text = "Resident:", Left = 290, Top = y, Width = 100 };
            _cboResident = new ComboBox { Left = 410, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboResident.SelectedIndexChanged += (s, e) => LoadVisitors();
            _filterPanel.Controls.Add(lblResident);
            _filterPanel.Controls.Add(_cboResident);

            Label lblVisitorType = new Label { Text = "Visitor Type:", Left = 580, Top = y, Width = 100 };
            _cboVisitorType = new ComboBox { Left = 700, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboVisitorType.Items.AddRange(new object[] { "All", "Guest", "Delivery", "Service", "Family", "Other" });
            _cboVisitorType.SelectedIndex = 0;
            _cboVisitorType.SelectedIndexChanged += (s, e) => LoadVisitors();
            _filterPanel.Controls.Add(lblVisitorType);
            _filterPanel.Controls.Add(_cboVisitorType);

            Label lblStatus = new Label { Text = "Status:", Left = 870, Top = y, Width = 100 };
            _cboStatus = new ComboBox { Left = 970, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboStatus.Items.AddRange(new object[] { "All", "Checked In", "Checked Out", "Pending" });
            _cboStatus.SelectedIndex = 0;
            _cboStatus.SelectedIndexChanged += (s, e) => LoadVisitors();
            _filterPanel.Controls.Add(lblStatus);
            _filterPanel.Controls.Add(_cboStatus);

            y += 40;

            // Row 2: Date and visitor details
            Label lblCheckInDate = new Label { Text = "Check-In Date:", Left = PADDING, Top = y, Width = 100 };
            _dtpCheckInDate = new DateTimePicker { Left = 120, Top = y, Width = 150 };
            _dtpCheckInDate.ValueChanged += (s, e) => LoadVisitors();
            _filterPanel.Controls.Add(lblCheckInDate);
            _filterPanel.Controls.Add(_dtpCheckInDate);

            Label lblVisitorName = new Label { Text = "Visitor Name:", Left = 290, Top = y, Width = 100 };
            _txtVisitorName = new TextBox { Left = 410, Top = y, Width = 150 };
            _filterPanel.Controls.Add(lblVisitorName);
            _filterPanel.Controls.Add(_txtVisitorName);

            Label lblPhone = new Label { Text = "Phone:", Left = 580, Top = y, Width = 100 };
            _txtPhone = new TextBox { Left = 700, Top = y, Width = 150 };
            _filterPanel.Controls.Add(lblPhone);
            _filterPanel.Controls.Add(_txtPhone);

            Button btnSearch = new Button { Text = "Search", Left = 870, Top = y, Width = 80, Height = 25 };
            btnSearch.Click += (s, e) => LoadVisitors();
            _filterPanel.Controls.Add(btnSearch);

            y += 35;

            Label lblEmail = new Label { Text = "Email:", Left = PADDING, Top = y, Width = 100 };
            _txtEmail = new TextBox { Left = 120, Top = y, Width = 150 };
            _filterPanel.Controls.Add(lblEmail);
            _filterPanel.Controls.Add(_txtEmail);

            Label lblPurpose = new Label { Text = "Purpose:", Left = 290, Top = y, Width = 100 };
            _txtPurpose = new TextBox { Left = 410, Top = y, Width = 150, Multiline = true, Height = 35 };
            _filterPanel.Controls.Add(lblPurpose);
            _filterPanel.Controls.Add(_txtPurpose);
        }

        private void CreateDetailsPanel()
        {
            _detailsPanel = new Panel { Dock = DockStyle.Top, Height = 80, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_detailsPanel);

            int y = PADDING;

            Label lblCheckInTime = new Label { Text = "Check-In Time:", Left = PADDING, Top = y, Width = 100 };
            _txtCheckInTime = new TextBox { Left = 120, Top = y, Width = 150, ReadOnly = true };
            _detailsPanel.Controls.Add(lblCheckInTime);
            _detailsPanel.Controls.Add(_txtCheckInTime);

            Label lblCheckOutTime = new Label { Text = "Check-Out Time:", Left = 290, Top = y, Width = 100 };
            _txtCheckOutTime = new TextBox { Left = 410, Top = y, Width = 150, ReadOnly = true };
            _detailsPanel.Controls.Add(lblCheckOutTime);
            _detailsPanel.Controls.Add(_txtCheckOutTime);

            y += 35;

            Label lblNote = new Label { Text = "Status will update after check-in/check-out", Left = PADDING, Top = y, Width = 400 };
            _detailsPanel.Controls.Add(lblNote);
        }

        private void CreateGridPanel()
        {
            _gridPanel = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(_gridPanel);

            _dgvVisitors = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                BackgroundColor = System.Drawing.Color.White
            };

            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "VisitorID", HeaderText = "ID", DataPropertyName = "VisitorID", Width = 50 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Apartment", HeaderText = "Apartment", DataPropertyName = "ApartmentCode", Width = 100 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Resident", HeaderText = "Resident", DataPropertyName = "ResidentName", Width = 120 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "VisitorName", HeaderText = "Visitor Name", DataPropertyName = "VisitorName", Width = 120 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "VisitorType", HeaderText = "Type", DataPropertyName = "VisitorType", Width = 80 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "CheckInTime", HeaderText = "Check-In", DataPropertyName = "CheckInTime", Width = 120 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "CheckOutTime", HeaderText = "Check-Out", DataPropertyName = "CheckOutTime", Width = 120 });
            _dgvVisitors.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 100 });

            _dgvVisitors.CellClick += DgvVisitors_CellClick;
            _gridPanel.Controls.Add(_dgvVisitors);
        }

        private void CreateButtonPanel()
        {
            _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_buttonPanel);

            Button btnCheckIn = new Button { Text = "Check-In", Left = PADDING, Top = 10, Width = 80, Height = 30 };
            btnCheckIn.Click += BtnCheckIn_Click;
            _buttonPanel.Controls.Add(btnCheckIn);

            Button btnCheckOut = new Button { Text = "Check-Out", Left = 100, Top = 10, Width = 80, Height = 30 };
            btnCheckOut.Click += BtnCheckOut_Click;
            _buttonPanel.Controls.Add(btnCheckOut);

            Button btnRegisterNew = new Button { Text = "Register New", Left = 180, Top = 10, Width = 80, Height = 30 };
            btnRegisterNew.Click += BtnRegisterNew_Click;
            _buttonPanel.Controls.Add(btnRegisterNew);

            Button btnDelete = new Button { Text = "Delete", Left = 260, Top = 10, Width = 80, Height = 30 };
            btnDelete.Click += BtnDelete_Click;
            _buttonPanel.Controls.Add(btnDelete);

            Button btnStatistics = new Button { Text = "Statistics", Left = 340, Top = 10, Width = 80, Height = 30 };
            btnStatistics.Click += BtnStatistics_Click;
            _buttonPanel.Controls.Add(btnStatistics);

            Button btnRefresh = new Button { Text = "Refresh", Left = 420, Top = 10, Width = 80, Height = 30 };
            btnRefresh.Click += (s, e) => LoadVisitors();
            _buttonPanel.Controls.Add(btnRefresh);
        }

        private void CreateStatusPanel()
        {
            _statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.Gainsboro };
            this.Controls.Add(_statusPanel);

            _lblTodayVisitors = new Label { Text = "Today's Visitors: 0", Left = PADDING, Top = 10, Width = 150 };
            _statusPanel.Controls.Add(_lblTodayVisitors);

            _lblCheckInCount = new Label { Text = "Checked In: 0", Left = 170, Top = 10, Width = 120 };
            _statusPanel.Controls.Add(_lblCheckInCount);

            _lblCheckOutCount = new Label { Text = "Checked Out: 0", Left = 310, Top = 10, Width = 120 };
            _statusPanel.Controls.Add(_lblCheckOutCount);
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
                MessageBox.Show($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void LoadApartments()
        {
            try
            {
                _cboApartment.Items.Clear();
                _cboApartment.Items.Add(new { ApartmentID = 0, DisplayText = "All Apartments" });

                var apartments = ApartmentDAL.GetAllApartments();
                foreach (var apt in apartments)
                {
                    _cboApartment.Items.Add(apt);
                }

                _cboApartment.ValueMember = "ApartmentID";
                _cboApartment.DisplayMember = "DisplayText";
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
                _cboResident.Items.Add(new { ResidentID = 0, DisplayText = "All Residents" });

                if (_cboApartment.SelectedItem != null && ((dynamic)_cboApartment.SelectedItem).ApartmentID != 0)
                {
                    int apartmentID = ((dynamic)_cboApartment.SelectedItem).ApartmentID;
                    var residents = ResidentDAL.GetResidentsByApartment(apartmentID);
                    foreach (var resident in residents)
                    {
                        _cboResident.Items.Add(resident);
                    }
                }
                else
                {
                    var residents = ResidentDAL.GetAllResidents();
                    foreach (var resident in residents)
                    {
                        _cboResident.Items.Add(resident);
                    }
                }

                _cboResident.ValueMember = "ResidentID";
                _cboResident.DisplayMember = "DisplayText";
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

                // Apply apartment filter
                if (_cboApartment.SelectedItem != null && ((dynamic)_cboApartment.SelectedItem).ApartmentID != 0)
                {
                    int apartmentID = ((dynamic)_cboApartment.SelectedItem).ApartmentID;
                    visitors = visitors.Where(v => v.ApartmentID == apartmentID).ToList();
                }

                // Apply resident filter
                if (_cboResident.SelectedItem != null && ((dynamic)_cboResident.SelectedItem).ResidentID != 0)
                {
                    int residentID = ((dynamic)_cboResident.SelectedItem).ResidentID;
                    visitors = visitors.Where(v => v.ResidentID == residentID).ToList();
                }

                // Apply visitor type filter
                if (!_cboVisitorType.SelectedItem.ToString().Equals("All"))
                {
                    string visitorType = _cboVisitorType.SelectedItem.ToString();
                    visitors = visitors.Where(v => v.VisitorType == visitorType).ToList();
                }

                // Apply status filter
                if (!_cboStatus.SelectedItem.ToString().Equals("All"))
                {
                    string status = _cboStatus.SelectedItem.ToString();
                    visitors = visitors.Where(v => 
                    {
                        if (status == "Checked In" && v.CheckOutTime == null)
                            return true;
                        if (status == "Checked Out" && v.CheckOutTime != null)
                            return true;
                        return false;
                    }).ToList();
                }

                // Apply date filter
                visitors = visitors.Where(v => v.CheckInTime.Date == _dtpCheckInDate.Value.Date).ToList();

                // Apply search filters
                if (!string.IsNullOrWhiteSpace(_txtVisitorName.Text))
                    visitors = visitors.Where(v => v.VisitorName.Contains(_txtVisitorName.Text)).ToList();

                if (!string.IsNullOrWhiteSpace(_txtPhone.Text))
                    visitors = visitors.Where(v => v.Phone != null && v.Phone.Contains(_txtPhone.Text)).ToList();

                _dgvVisitors.DataSource = visitors.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading visitors");
                MessageBox.Show($"Error loading visitors: {ex.Message}", "Error");
            }
        }

        private void DgvVisitors_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                var row = _dgvVisitors.Rows[e.RowIndex];
                int visitorID = Convert.ToInt32(row.Cells["VisitorID"].Value);
                var visitor = VisitorDAL.GetVisitorByID(visitorID);

                if (visitor != null)
                {
                    _txtCheckInTime.Text = visitor.CheckInTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    _txtCheckOutTime.Text = visitor.CheckOutTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    _txtVisitorName.Text = visitor.VisitorName ?? "";
                    _txtPhone.Text = visitor.Phone ?? "";
                    _txtEmail.Text = visitor.Email ?? "";
                    _txtPurpose.Text = visitor.Purpose ?? "";
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
                if (_cboResident.SelectedItem == null || ((dynamic)_cboResident.SelectedItem).ResidentID == 0)
                {
                    MessageBox.Show("Please select a resident.", "Information");
                    return;
                }

                FrmVisitorDialog dialog = new FrmVisitorDialog(null, ((dynamic)_cboResident.SelectedItem).ResidentID, ((dynamic)_cboApartment.SelectedItem).ApartmentID);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var (visitorName, phone, email, visitorType, purpose) = dialog.GetVisitorData();

                    var result = VisitorBLL.CheckInVisitor(
                        ((dynamic)_cboApartment.SelectedItem).ApartmentID,
                        ((dynamic)_cboResident.SelectedItem).ResidentID,
                        visitorName,
                        phone,
                        email,
                        visitorType,
                        purpose);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "CheckInVisitor", $"Visitor {visitorName} checked in");
                        LoadVisitors();
                        UpdateStatistics();
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking in visitor");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnCheckOut_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvVisitors.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a visitor to check out.", "Information");
                    return;
                }

                int visitorID = Convert.ToInt32(_dgvVisitors.SelectedRows[0].Cells["VisitorID"].Value);
                var visitor = VisitorDAL.GetVisitorByID(visitorID);

                if (visitor == null || visitor.CheckOutTime != null)
                {
                    MessageBox.Show("Visitor has already checked out.", "Information");
                    return;
                }

                var result = VisitorBLL.CheckOutVisitor(visitorID, DateTime.Now);

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Success");
                    AuditLogDAL.LogAction(_session.CurrentUser.UserID, "CheckOutVisitor", $"Visitor {visitorID} checked out");
                    LoadVisitors();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show(result.Message, "Error");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking out visitor");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnRegisterNew_Click(object sender, EventArgs e)
        {
            try
            {
                if (_cboResident.SelectedItem == null || ((dynamic)_cboResident.SelectedItem).ResidentID == 0)
                {
                    MessageBox.Show("Please select a resident.", "Information");
                    return;
                }

                FrmVisitorDialog dialog = new FrmVisitorDialog(null, ((dynamic)_cboResident.SelectedItem).ResidentID, ((dynamic)_cboApartment.SelectedItem).ApartmentID);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var (visitorName, phone, email, visitorType, purpose) = dialog.GetVisitorData();

                    var result = VisitorBLL.RegisterVisitor(
                        ((dynamic)_cboResident.SelectedItem).ResidentID,
                        visitorName,
                        phone,
                        email,
                        visitorType);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "RegisterVisitor", $"Visitor {visitorName} registered");
                        LoadVisitors();
                        UpdateStatistics();
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error registering visitor");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvVisitors.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a visitor to delete.", "Information");
                    return;
                }

                int visitorID = Convert.ToInt32(_dgvVisitors.SelectedRows[0].Cells["VisitorID"].Value);

                if (MessageBox.Show("Are you sure you want to delete this visitor record?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var result = VisitorBLL.DeleteVisitor(visitorID);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "DeleteVisitor", $"Visitor {visitorID} deleted");
                        LoadVisitors();
                        UpdateStatistics();
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting visitor");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnStatistics_Click(object sender, EventArgs e)
        {
            try
            {
                var stats = VisitorBLL.GetVisitorStatistics();
                MessageBox.Show(
                    $"Total Visitors: {stats.TotalVisitors}\n" +
                    $"Today's Visitors: {stats.TodayVisitors}\n" +
                    $"Currently Checked In: {stats.CheckedInCount}\n" +
                    $"Checked Out Today: {stats.CheckedOutCount}\n" +
                    $"Guest: {stats.GuestCount}\n" +
                    $"Delivery: {stats.DeliveryCount}\n" +
                    $"Service: {stats.ServiceCount}\n" +
                    $"Family: {stats.FamilyCount}\n" +
                    $"Other: {stats.OtherCount}",
                    "Visitor Statistics");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving statistics");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                var stats = VisitorBLL.GetVisitorStatistics();
                _lblTodayVisitors.Text = $"Today's Visitors: {stats.TodayVisitors}";
                _lblCheckInCount.Text = $"Checked In: {stats.CheckedInCount}";
                _lblCheckOutCount.Text = $"Checked Out: {stats.CheckedOutCount}";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating statistics");
            }
        }
    }

    public partial class FrmVisitorDialog : Form
    {
        private dynamic _visitor;
        private int _residentID;
        private int _apartmentID;
        private ComboBox _cboVisitorType;
        private TextBox _txtVisitorName, _txtPhone, _txtEmail, _txtPurpose;

        public FrmVisitorDialog(dynamic visitor, int residentID, int apartmentID)
        {
            _visitor = visitor;
            _residentID = residentID;
            _apartmentID = apartmentID;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            this.Text = _visitor == null ? "Check-In Visitor" : "Update Visitor";
            this.Size = new System.Drawing.Size(450, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 10;

            Label lblVisitorName = new Label { Text = "Visitor Name:", Left = 10, Top = y, Width = 100 };
            _txtVisitorName = new TextBox { Left = 120, Top = y, Width = 300 };
            this.Controls.Add(lblVisitorName);
            this.Controls.Add(_txtVisitorName);

            y += 35;

            Label lblPhone = new Label { Text = "Phone:", Left = 10, Top = y, Width = 100 };
            _txtPhone = new TextBox { Left = 120, Top = y, Width = 300 };
            this.Controls.Add(lblPhone);
            this.Controls.Add(_txtPhone);

            y += 35;

            Label lblEmail = new Label { Text = "Email:", Left = 10, Top = y, Width = 100 };
            _txtEmail = new TextBox { Left = 120, Top = y, Width = 300 };
            this.Controls.Add(lblEmail);
            this.Controls.Add(_txtEmail);

            y += 35;

            Label lblVisitorType = new Label { Text = "Visitor Type:", Left = 10, Top = y, Width = 100 };
            _cboVisitorType = new ComboBox { Left = 120, Top = y, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboVisitorType.Items.AddRange(new object[] { "Guest", "Delivery", "Service", "Family", "Other" });
            _cboVisitorType.SelectedIndex = 0;
            this.Controls.Add(lblVisitorType);
            this.Controls.Add(_cboVisitorType);

            y += 35;

            Label lblPurpose = new Label { Text = "Purpose:", Left = 10, Top = y, Width = 100 };
            _txtPurpose = new TextBox { Left = 120, Top = y, Width = 300, Height = 50, Multiline = true };
            this.Controls.Add(lblPurpose);
            this.Controls.Add(_txtPurpose);

            y += 60;

            Button btnOK = new Button { Text = "OK", Left = 230, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancel", Left = 340, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            if (_visitor != null)
            {
                _txtVisitorName.Text = _visitor.VisitorName ?? "";
                _txtPhone.Text = _visitor.Phone ?? "";
                _txtEmail.Text = _visitor.Email ?? "";
                _cboVisitorType.SelectedItem = _visitor.VisitorType ?? "Guest";
                _txtPurpose.Text = _visitor.Purpose ?? "";
            }
        }

        public (string VisitorName, string Phone, string Email, string VisitorType, string Purpose) GetVisitorData()
        {
            return (
                _txtVisitorName.Text,
                _txtPhone.Text,
                _txtEmail.Text,
                _cboVisitorType.SelectedItem.ToString(),
                _txtPurpose.Text
            );
        }
    }
}

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
    public partial class FrmContractManagement : Form
    {
        private SessionManager _session = SessionManager.Instance;
        private const int PADDING = 10;
        private const int CONTROL_HEIGHT = 25;

        // UI Controls
        private Panel _filterPanel, _detailsPanel, _gridPanel, _buttonPanel, _statusPanel;
        private DataGridView _dgvContracts;
        private ComboBox _cboApartment, _cboResident, _cboContractType, _cboStatus;
        private TextBox _txtStartDate, _txtEndDate, _txtTermDuration, _txtTermsAndConditions, _txtRenewalNotes;
        private CheckBox _chkAutoRenewal;
        private Label _lblActiveCount, _lblExpiringCount, _lblTerminatedCount;

        public FrmContractManagement()
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
            if (!_session.HasPermission("ManageContracts"))
            {
                MessageBox.Show("You do not have permission to access contract management.", "Access Denied");
                this.Close();
                return;
            }

            this.Text = "Contract Management";
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
            _filterPanel = new Panel { Dock = DockStyle.Top, Height = 120, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_filterPanel);

            int y = PADDING;

            // Row 1: Apartment Filter
            Label lblApartment = new Label { Text = "Apartment:", Left = PADDING, Top = y, Width = 100 };
            _cboApartment = new ComboBox { Left = 120, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboApartment.SelectedIndexChanged += (s, e) => LoadContracts();
            _filterPanel.Controls.Add(lblApartment);
            _filterPanel.Controls.Add(_cboApartment);

            Label lblResident = new Label { Text = "Resident:", Left = 290, Top = y, Width = 100 };
            _cboResident = new ComboBox { Left = 410, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboResident.SelectedIndexChanged += (s, e) => LoadContracts();
            _filterPanel.Controls.Add(lblResident);
            _filterPanel.Controls.Add(_cboResident);

            Label lblContractType = new Label { Text = "Contract Type:", Left = 580, Top = y, Width = 100 };
            _cboContractType = new ComboBox { Left = 700, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboContractType.Items.AddRange(new object[] { "All", "Lease", "Service" });
            _cboContractType.SelectedIndex = 0;
            _cboContractType.SelectedIndexChanged += (s, e) => LoadContracts();
            _filterPanel.Controls.Add(lblContractType);
            _filterPanel.Controls.Add(_cboContractType);

            Label lblStatus = new Label { Text = "Status:", Left = 870, Top = y, Width = 100 };
            _cboStatus = new ComboBox { Left = 970, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboStatus.Items.AddRange(new object[] { "All", "Active", "Expired", "Terminated", "Pending" });
            _cboStatus.SelectedIndex = 0;
            _cboStatus.SelectedIndexChanged += (s, e) => LoadContracts();
            _filterPanel.Controls.Add(lblStatus);
            _filterPanel.Controls.Add(_cboStatus);

            y += 40;

            // Row 2: Details
            Label lblStartDate = new Label { Text = "Start Date:", Left = PADDING, Top = y, Width = 100 };
            _txtStartDate = new TextBox { Left = 120, Top = y, Width = 150, ReadOnly = true };
            _filterPanel.Controls.Add(lblStartDate);
            _filterPanel.Controls.Add(_txtStartDate);

            Label lblEndDate = new Label { Text = "End Date:", Left = 290, Top = y, Width = 100 };
            _txtEndDate = new TextBox { Left = 410, Top = y, Width = 150, ReadOnly = true };
            _filterPanel.Controls.Add(lblEndDate);
            _filterPanel.Controls.Add(_txtEndDate);

            Label lblTermDuration = new Label { Text = "Term (Months):", Left = 580, Top = y, Width = 100 };
            _txtTermDuration = new TextBox { Left = 700, Top = y, Width = 150 };
            _filterPanel.Controls.Add(lblTermDuration);
            _filterPanel.Controls.Add(_txtTermDuration);

            _chkAutoRenewal = new CheckBox { Text = "Auto-Renewal", Left = 870, Top = y, Width = 150 };
            _filterPanel.Controls.Add(_chkAutoRenewal);
        }

        private void CreateDetailsPanel()
        {
            _detailsPanel = new Panel { Dock = DockStyle.Top, Height = 100, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_detailsPanel);

            int y = PADDING;

            Label lblTermsAndConditions = new Label { Text = "Terms & Conditions:", Left = PADDING, Top = y, Width = 150 };
            _txtTermsAndConditions = new TextBox { Left = 170, Top = y, Width = 1000, Height = 30, Multiline = true };
            _detailsPanel.Controls.Add(lblTermsAndConditions);
            _detailsPanel.Controls.Add(_txtTermsAndConditions);

            y += 40;

            Label lblRenewalNotes = new Label { Text = "Renewal Notes:", Left = PADDING, Top = y, Width = 150 };
            _txtRenewalNotes = new TextBox { Left = 170, Top = y, Width = 1000, Height = 25, Multiline = true };
            _detailsPanel.Controls.Add(lblRenewalNotes);
            _detailsPanel.Controls.Add(_txtRenewalNotes);
        }

        private void CreateGridPanel()
        {
            _gridPanel = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(_gridPanel);

            _dgvContracts = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                BackgroundColor = System.Drawing.Color.White
            };

            _dgvContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ContractID", HeaderText = "ID", DataPropertyName = "ContractID", Width = 50 });
            _dgvContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Apartment", HeaderText = "Apartment", DataPropertyName = "ApartmentCode", Width = 100 });
            _dgvContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Resident", HeaderText = "Resident", DataPropertyName = "ResidentName", Width = 150 });
            _dgvContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ContractType", HeaderText = "Type", DataPropertyName = "ContractType", Width = 80 });
            _dgvContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "StartDate", HeaderText = "Start Date", DataPropertyName = "StartDate", Width = 100 });
            _dgvContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "EndDate", HeaderText = "End Date", DataPropertyName = "EndDate", Width = 100 });
            _dgvContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 100 });
            _dgvContracts.Columns.Add(new DataGridViewTextBoxColumn { Name = "AutoRenewal", HeaderText = "Auto-Renewal", DataPropertyName = "AutoRenewal", Width = 80 });

            _dgvContracts.CellClick += DgvContracts_CellClick;
            _gridPanel.Controls.Add(_dgvContracts);
        }

        private void CreateButtonPanel()
        {
            _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_buttonPanel);

            Button btnCreate = new Button { Text = "Create", Left = PADDING, Top = 10, Width = 80, Height = 30 };
            btnCreate.Click += BtnCreate_Click;
            _buttonPanel.Controls.Add(btnCreate);

            Button btnEdit = new Button { Text = "Edit", Left = 100, Top = 10, Width = 80, Height = 30 };
            btnEdit.Click += BtnEdit_Click;
            _buttonPanel.Controls.Add(btnEdit);

            Button btnRenew = new Button { Text = "Renew", Left = 180, Top = 10, Width = 80, Height = 30 };
            btnRenew.Click += BtnRenew_Click;
            _buttonPanel.Controls.Add(btnRenew);

            Button btnTerminate = new Button { Text = "Terminate", Left = 260, Top = 10, Width = 80, Height = 30 };
            btnTerminate.Click += BtnTerminate_Click;
            _buttonPanel.Controls.Add(btnTerminate);

            Button btnDelete = new Button { Text = "Delete", Left = 340, Top = 10, Width = 80, Height = 30 };
            btnDelete.Click += BtnDelete_Click;
            _buttonPanel.Controls.Add(btnDelete);

            Button btnStatistics = new Button { Text = "Statistics", Left = 420, Top = 10, Width = 80, Height = 30 };
            btnStatistics.Click += BtnStatistics_Click;
            _buttonPanel.Controls.Add(btnStatistics);

            Button btnExpiringContracts = new Button { Text = "Expiring", Left = 500, Top = 10, Width = 80, Height = 30 };
            btnExpiringContracts.Click += BtnExpiringContracts_Click;
            _buttonPanel.Controls.Add(btnExpiringContracts);
        }

        private void CreateStatusPanel()
        {
            _statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.Gainsboro };
            this.Controls.Add(_statusPanel);

            _lblActiveCount = new Label { Text = "Active: 0", Left = PADDING, Top = 10, Width = 100 };
            _statusPanel.Controls.Add(_lblActiveCount);

            _lblExpiringCount = new Label { Text = "Expiring in 30 days: 0", Left = 120, Top = 10, Width = 180 };
            _statusPanel.Controls.Add(_lblExpiringCount);

            _lblTerminatedCount = new Label { Text = "Terminated: 0", Left = 320, Top = 10, Width = 150 };
            _statusPanel.Controls.Add(_lblTerminatedCount);
        }

        private void LoadData()
        {
            try
            {
                LoadApartments();
                LoadContracts();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading contract data");
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

        private void LoadContracts()
        {
            try
            {
                var contracts = ContractDAL.GetAllContracts();

                // Apply filters
                if (_cboApartment.SelectedItem != null && ((dynamic)_cboApartment.SelectedItem).ApartmentID != 0)
                {
                    int apartmentID = ((dynamic)_cboApartment.SelectedItem).ApartmentID;
                    contracts = contracts.Where(c => c.ApartmentID == apartmentID).ToList();
                }

                if (!_cboContractType.SelectedItem.ToString().Equals("All"))
                {
                    string contractType = _cboContractType.SelectedItem.ToString();
                    contracts = contracts.Where(c => c.ContractType == contractType).ToList();
                }

                if (!_cboStatus.SelectedItem.ToString().Equals("All"))
                {
                    string status = _cboStatus.SelectedItem.ToString();
                    contracts = contracts.Where(c => c.Status == status).ToList();
                }

                _dgvContracts.DataSource = contracts.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading contracts");
                MessageBox.Show($"Error loading contracts: {ex.Message}", "Error");
            }
        }

        private void DgvContracts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                var row = _dgvContracts.Rows[e.RowIndex];
                int contractID = Convert.ToInt32(row.Cells["ContractID"].Value);
                var contract = ContractDAL.GetContractByID(contractID);

                if (contract != null)
                {
                    _txtStartDate.Text = contract.StartDate.ToString("yyyy-MM-dd");
                    _txtEndDate.Text = contract.EndDate.ToString("yyyy-MM-dd");
                    
                    TimeSpan duration = contract.EndDate - contract.StartDate;
                    _txtTermDuration.Text = (duration.Days / 30).ToString();
                    
                    _txtTermsAndConditions.Text = contract.TermsAndConditions ?? "";
                    _txtRenewalNotes.Text = contract.RenewalNotes ?? "";
                    _chkAutoRenewal.Checked = contract.AutoRenewal;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading contract details");
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                FrmContractDialog dialog = new FrmContractDialog(null);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var (apartmentID, residentID, startDate, endDate, contractType, termsAndConditions, autoRenewal) = dialog.GetContractData();
                    
                    var result = ContractBLL.CreateContract(apartmentID, residentID, startDate, endDate, contractType, termsAndConditions, autoRenewal, "");
                    
                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "CreateContract", $"Contract {result.ContractID} created");
                        LoadContracts();
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
                Log.Error(ex, "Error creating contract");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvContracts.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a contract to edit.", "Information");
                    return;
                }

                int contractID = Convert.ToInt32(_dgvContracts.SelectedRows[0].Cells["ContractID"].Value);
                var contract = ContractDAL.GetContractByID(contractID);

                if (contract.Status == "Expired" || contract.Status == "Terminated")
                {
                    MessageBox.Show("Cannot edit expired or terminated contracts.", "Information");
                    return;
                }

                FrmContractDialog dialog = new FrmContractDialog(contract);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var (apartmentID, residentID, startDate, endDate, contractType, termsAndConditions, autoRenewal) = dialog.GetContractData();
                    
                    var result = ContractBLL.UpdateContract(contractID, startDate, endDate, contractType, termsAndConditions, autoRenewal, "");
                    
                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "UpdateContract", $"Contract {contractID} updated");
                        LoadContracts();
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
                Log.Error(ex, "Error editing contract");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnRenew_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvContracts.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a contract to renew.", "Information");
                    return;
                }

                int contractID = Convert.ToInt32(_dgvContracts.SelectedRows[0].Cells["ContractID"].Value);
                var contract = ContractDAL.GetContractByID(contractID);

                if (contract.Status != "Active" && contract.Status != "Expired")
                {
                    MessageBox.Show("Only active or expired contracts can be renewed.", "Information");
                    return;
                }

                int termMonths = 12; // Default renewal term
                DateTime newEndDate = contract.EndDate.AddMonths(termMonths);

                var result = ContractBLL.RenewContract(contractID, contract.EndDate, newEndDate);
                
                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Success");
                    AuditLogDAL.LogAction(_session.CurrentUser.UserID, "RenewContract", $"Contract {contractID} renewed");
                    LoadContracts();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show(result.Message, "Error");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error renewing contract");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnTerminate_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvContracts.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a contract to terminate.", "Information");
                    return;
                }

                int contractID = Convert.ToInt32(_dgvContracts.SelectedRows[0].Cells["ContractID"].Value);
                var contract = ContractDAL.GetContractByID(contractID);

                if (contract.Status == "Terminated")
                {
                    MessageBox.Show("Contract is already terminated.", "Information");
                    return;
                }

                if (MessageBox.Show("Are you sure you want to terminate this contract?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var result = ContractBLL.TerminateContract(contractID, DateTime.Now);
                    
                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "TerminateContract", $"Contract {contractID} terminated");
                        LoadContracts();
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
                Log.Error(ex, "Error terminating contract");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvContracts.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a contract to delete.", "Information");
                    return;
                }

                int contractID = Convert.ToInt32(_dgvContracts.SelectedRows[0].Cells["ContractID"].Value);
                var contract = ContractDAL.GetContractByID(contractID);

                if (contract.Status == "Active")
                {
                    MessageBox.Show("Cannot delete active contracts.", "Information");
                    return;
                }

                if (MessageBox.Show("Are you sure you want to delete this contract?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var result = ContractBLL.DeleteContract(contractID);
                    
                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "DeleteContract", $"Contract {contractID} deleted");
                        LoadContracts();
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
                Log.Error(ex, "Error deleting contract");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnStatistics_Click(object sender, EventArgs e)
        {
            try
            {
                var stats = ContractBLL.GetContractStatistics();
                MessageBox.Show(
                    $"Total Contracts: {stats.TotalContracts}\n" +
                    $"Active: {stats.ActiveCount}\n" +
                    $"Expired: {stats.ExpiredCount}\n" +
                    $"Terminated: {stats.TerminatedCount}\n" +
                    $"Expiring in 30 days: {stats.ExpiringCount}\n" +
                    $"Auto-Renewal Enabled: {stats.AutoRenewalCount}",
                    "Contract Statistics");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving statistics");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnExpiringContracts_Click(object sender, EventArgs e)
        {
            try
            {
                var expiringContracts = ContractDAL.GetExpiringContracts(30);
                if (expiringContracts.Count == 0)
                {
                    MessageBox.Show("No contracts expiring in the next 30 days.", "Information");
                    return;
                }

                string message = "Contracts expiring in the next 30 days:\n\n";
                foreach (var contract in expiringContracts)
                {
                    message += $"- {contract.ApartmentCode} ({contract.ResidentName}): {contract.EndDate:yyyy-MM-dd}\n";
                }

                MessageBox.Show(message, "Expiring Contracts");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving expiring contracts");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                var stats = ContractBLL.GetContractStatistics();
                _lblActiveCount.Text = $"Active: {stats.ActiveCount}";
                _lblExpiringCount.Text = $"Expiring in 30 days: {stats.ExpiringCount}";
                _lblTerminatedCount.Text = $"Terminated: {stats.TerminatedCount}";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating statistics");
            }
        }
    }

    public partial class FrmContractDialog : Form
    {
        private dynamic _contract;
        private ComboBox _cboApartment, _cboResident, _cboContractType;
        private DateTimePicker _dtpStartDate, _dtpEndDate;
        private TextBox _txtTermsAndConditions, _txtRenewalNotes;
        private CheckBox _chkAutoRenewal;

        public FrmContractDialog(dynamic contract)
        {
            _contract = contract;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            this.Text = _contract == null ? "Create Contract" : "Edit Contract";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 10;

            Label lblApartment = new Label { Text = "Apartment:", Left = 10, Top = y, Width = 100 };
            _cboApartment = new ComboBox { Left = 120, Top = y, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            LoadApartments();
            this.Controls.Add(lblApartment);
            this.Controls.Add(_cboApartment);

            y += 35;

            Label lblResident = new Label { Text = "Resident:", Left = 10, Top = y, Width = 100 };
            _cboResident = new ComboBox { Left = 120, Top = y, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            LoadResidents();
            this.Controls.Add(lblResident);
            this.Controls.Add(_cboResident);

            y += 35;

            Label lblContractType = new Label { Text = "Contract Type:", Left = 10, Top = y, Width = 100 };
            _cboContractType = new ComboBox { Left = 120, Top = y, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboContractType.Items.AddRange(new object[] { "Lease", "Service" });
            this.Controls.Add(lblContractType);
            this.Controls.Add(_cboContractType);

            y += 35;

            Label lblStartDate = new Label { Text = "Start Date:", Left = 10, Top = y, Width = 100 };
            _dtpStartDate = new DateTimePicker { Left = 120, Top = y, Width = 350 };
            this.Controls.Add(lblStartDate);
            this.Controls.Add(_dtpStartDate);

            y += 35;

            Label lblEndDate = new Label { Text = "End Date:", Left = 10, Top = y, Width = 100 };
            _dtpEndDate = new DateTimePicker { Left = 120, Top = y, Width = 350 };
            this.Controls.Add(lblEndDate);
            this.Controls.Add(_dtpEndDate);

            y += 35;

            Label lblTermsAndConditions = new Label { Text = "Terms:", Left = 10, Top = y, Width = 100 };
            _txtTermsAndConditions = new TextBox { Left = 120, Top = y, Width = 350, Height = 50, Multiline = true };
            this.Controls.Add(lblTermsAndConditions);
            this.Controls.Add(_txtTermsAndConditions);

            y += 60;

            _chkAutoRenewal = new CheckBox { Text = "Auto-Renewal", Left = 120, Top = y, Width = 200 };
            this.Controls.Add(_chkAutoRenewal);

            y += 35;

            Button btnOK = new Button { Text = "OK", Left = 250, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancel", Left = 370, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            if (_contract != null)
            {
                _dtpStartDate.Value = _contract.StartDate;
                _dtpEndDate.Value = _contract.EndDate;
                _cboContractType.SelectedItem = _contract.ContractType;
                _txtTermsAndConditions.Text = _contract.TermsAndConditions ?? "";
                _chkAutoRenewal.Checked = _contract.AutoRenewal;
            }
        }

        private void LoadApartments()
        {
            var apartments = ApartmentDAL.GetAllApartments();
            foreach (var apt in apartments)
            {
                _cboApartment.Items.Add(apt);
            }
            _cboApartment.ValueMember = "ApartmentID";
            _cboApartment.DisplayMember = "ApartmentCode";

            if (_contract != null)
                _cboApartment.SelectedValue = _contract.ApartmentID;
            else if (_cboApartment.Items.Count > 0)
                _cboApartment.SelectedIndex = 0;
        }

        private void LoadResidents()
        {
            var residents = ResidentDAL.GetAllResidents();
            foreach (var resident in residents)
            {
                _cboResident.Items.Add(resident);
            }
            _cboResident.ValueMember = "ResidentID";
            _cboResident.DisplayMember = "FullName";

            if (_contract != null)
                _cboResident.SelectedValue = _contract.ResidentID;
            else if (_cboResident.Items.Count > 0)
                _cboResident.SelectedIndex = 0;
        }

        public (int ApartmentID, int ResidentID, DateTime StartDate, DateTime EndDate, string ContractType, string TermsAndConditions, bool AutoRenewal) GetContractData()
        {
            return (
                ((dynamic)_cboApartment.SelectedItem).ApartmentID,
                ((dynamic)_cboResident.SelectedItem).ResidentID,
                _dtpStartDate.Value,
                _dtpEndDate.Value,
                _cboContractType.SelectedItem.ToString(),
                _txtTermsAndConditions.Text,
                _chkAutoRenewal.Checked
            );
        }
    }
}

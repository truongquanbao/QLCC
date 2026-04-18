using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using System.Drawing;
namespace ApartmentManager.GUI.Forms
{
    public partial class FrmFeeTypeManagement : Form
    {
        private UserSession? _session = null;
        private const int PADDING = 10;
        private const int CONTROL_HEIGHT = 25;

        // UI Controls
        private Panel _filterPanel, _detailsPanel, _gridPanel, _buttonPanel, _statusPanel;
        private DataGridView _dgvFeeTypes;
        private TextBox _txtFeeTypeName, _txtDescription, _txtUnitOfMeasurement;
        private Label _lblActiveCount, _lblInactiveCount, _lblTotalCount;

        public FrmFeeTypeManagement()
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
            if (!_session.HasPermission("ManageFeeTypes"))
            {
                MessageBox.Show("You do not have permission to access fee type management.", "Access Denied");
                this.Close();
                return;
            }

            this.Text = "Fee Type Management";
            this.Size = new System.Drawing.Size(1000, 600);
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
            _filterPanel = new Panel { Dock = DockStyle.Top, Height = 80, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_filterPanel);

            int y = PADDING;

            Label lblFeeTypeName = new Label { Text = "Fee Type Name:", Left = PADDING, Top = y, Width = 100 };
            _txtFeeTypeName = new TextBox { Left = 120, Top = y, Width = 250 };
            _filterPanel.Controls.Add(lblFeeTypeName);
            _filterPanel.Controls.Add(_txtFeeTypeName);

            Button btnSearch = new Button { Text = "Search", Left = 390, Top = y, Width = 80, Height = 25 };
            btnSearch.Click += (s, e) => LoadFeeTypes();
            _filterPanel.Controls.Add(btnSearch);

            y += 35;

            Label lblNote = new Label { Text = "Configuration fee types for apartment billing", Left = PADDING, Top = y, Width = 400 };
            _filterPanel.Controls.Add(lblNote);
        }

        private void CreateDetailsPanel()
        {
            _detailsPanel = new Panel { Dock = DockStyle.Top, Height = 100, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_detailsPanel);

            int y = PADDING;

            Label lblDescription = new Label { Text = "Description:", Left = PADDING, Top = y, Width = 100 };
            _txtDescription = new TextBox { Left = 120, Top = y, Width = 850, Height = 30, Multiline = true };
            _detailsPanel.Controls.Add(lblDescription);
            _detailsPanel.Controls.Add(_txtDescription);

            y += 40;

            Label lblUnitOfMeasurement = new Label { Text = "Unit of Measurement:", Left = PADDING, Top = y, Width = 120 };
            _txtUnitOfMeasurement = new TextBox { Left = 240, Top = y, Width = 200 };
            _detailsPanel.Controls.Add(lblUnitOfMeasurement);
            _detailsPanel.Controls.Add(_txtUnitOfMeasurement);
        }

        private void CreateGridPanel()
        {
            _gridPanel = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(_gridPanel);

            _dgvFeeTypes = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                BackgroundColor = System.Drawing.Color.White
            };

            _dgvFeeTypes.Columns.Add(new DataGridViewTextBoxColumn { Name = "FeeTypeID", HeaderText = "ID", DataPropertyName = "FeeTypeID", Width = 50 });
            _dgvFeeTypes.Columns.Add(new DataGridViewTextBoxColumn { Name = "FeeTypeName", HeaderText = "Fee Type", DataPropertyName = "FeeTypeName", Width = 200 });
            _dgvFeeTypes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", DataPropertyName = "Description", Width = 350 });
            _dgvFeeTypes.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitOfMeasurement", HeaderText = "Unit", DataPropertyName = "UnitOfMeasurement", Width = 100 });
            _dgvFeeTypes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 100 });

            _dgvFeeTypes.CellClick += DgvFeeTypes_CellClick;
            _gridPanel.Controls.Add(_dgvFeeTypes);
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

            Button btnDelete = new Button { Text = "Delete", Left = 180, Top = 10, Width = 80, Height = 30 };
            btnDelete.Click += BtnDelete_Click;
            _buttonPanel.Controls.Add(btnDelete);

            Button btnActivate = new Button { Text = "Activate", Left = 260, Top = 10, Width = 80, Height = 30 };
            btnActivate.Click += BtnActivate_Click;
            _buttonPanel.Controls.Add(btnActivate);

            Button btnDeactivate = new Button { Text = "Deactivate", Left = 340, Top = 10, Width = 80, Height = 30 };
            btnDeactivate.Click += BtnDeactivate_Click;
            _buttonPanel.Controls.Add(btnDeactivate);

            Button btnRefresh = new Button { Text = "Refresh", Left = 420, Top = 10, Width = 80, Height = 30 };
            btnRefresh.Click += (s, e) => LoadFeeTypes();
            _buttonPanel.Controls.Add(btnRefresh);
        }

        private void CreateStatusPanel()
        {
            _statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.Gainsboro };
            this.Controls.Add(_statusPanel);

            _lblTotalCount = new Label { Text = "Total: 0", Left = PADDING, Top = 10, Width = 100 };
            _statusPanel.Controls.Add(_lblTotalCount);

            _lblActiveCount = new Label { Text = "Active: 0", Left = 120, Top = 10, Width = 100 };
            _statusPanel.Controls.Add(_lblActiveCount);

            _lblInactiveCount = new Label { Text = "Inactive: 0", Left = 240, Top = 10, Width = 100 };
            _statusPanel.Controls.Add(_lblInactiveCount);
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
                MessageBox.Show($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void LoadFeeTypes()
        {
            try
            {
                var feeTypes = FeeTypeDAL.GetAllFeeTypes();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(_txtFeeTypeName.Text))
                    feeTypes = feeTypes.Where(f => f.FeeTypeName.Contains(_txtFeeTypeName.Text)).ToList();

                _dgvFeeTypes.DataSource = feeTypes.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading fee types");
                MessageBox.Show($"Error loading fee types: {ex.Message}", "Error");
            }
        }

        private void DgvFeeTypes_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                var row = _dgvFeeTypes.Rows[e.RowIndex];
                int feeTypeID = Convert.ToInt32(row.Cells["FeeTypeID"].Value);
                var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);

                if (feeType != null)
                {
                    _txtFeeTypeName.Text = feeType.FeeTypeName ?? "";
                    _txtDescription.Text = feeType.Description ?? "";
                    _txtUnitOfMeasurement.Text = feeType.UnitOfMeasurement ?? "";
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
                FrmFeeTypeDialog dialog = new FrmFeeTypeDialog(null);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var (feeTypeName, description, unitOfMeasurement) = dialog.GetFeeTypeData();

                    var result = FeeTypeBLL.CreateFeeType(feeTypeName, description, unitOfMeasurement);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "CreateFeeType", $"Fee Type '{feeTypeName}' created");
                        LoadFeeTypes();
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
                Log.Error(ex, "Error creating fee type");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvFeeTypes.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a fee type to edit.", "Information");
                    return;
                }

                int feeTypeID = Convert.ToInt32(_dgvFeeTypes.SelectedRows[0].Cells["FeeTypeID"].Value);
                var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);

                FrmFeeTypeDialog dialog = new FrmFeeTypeDialog(feeType);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var (feeTypeName, description, unitOfMeasurement) = dialog.GetFeeTypeData();

                    var result = FeeTypeBLL.UpdateFeeType(feeTypeID, feeTypeName, description, unitOfMeasurement);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "UpdateFeeType", $"Fee Type {feeTypeID} updated");
                        LoadFeeTypes();
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
                Log.Error(ex, "Error editing fee type");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvFeeTypes.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a fee type to delete.", "Information");
                    return;
                }

                int feeTypeID = Convert.ToInt32(_dgvFeeTypes.SelectedRows[0].Cells["FeeTypeID"].Value);

                if (MessageBox.Show("Are you sure you want to delete this fee type?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var result = FeeTypeBLL.DeleteFeeType(feeTypeID);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "DeleteFeeType", $"Fee Type {feeTypeID} deleted");
                        LoadFeeTypes();
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
                Log.Error(ex, "Error deleting fee type");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnActivate_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvFeeTypes.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a fee type to activate.", "Information");
                    return;
                }

                int feeTypeID = Convert.ToInt32(_dgvFeeTypes.SelectedRows[0].Cells["FeeTypeID"].Value);
                var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);

                if (feeType != null && feeType.Status == "Active")
                {
                    MessageBox.Show("Fee type is already active.", "Information");
                    return;
                }

                bool updated = FeeTypeDAL.UpdateFeeTypeStatus(feeTypeID, "Active");

                if (updated)
                {
                    MessageBox.Show("Fee type activated successfully.", "Success");
                    AuditLogDAL.LogAction(_session.CurrentUser.UserID, "ActivateFeeType", $"Fee Type {feeTypeID} activated");
                    LoadFeeTypes();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show("Failed to activate fee type.", "Error");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error activating fee type");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnDeactivate_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvFeeTypes.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a fee type to deactivate.", "Information");
                    return;
                }

                int feeTypeID = Convert.ToInt32(_dgvFeeTypes.SelectedRows[0].Cells["FeeTypeID"].Value);
                var feeType = FeeTypeDAL.GetFeeTypeByID(feeTypeID);

                if (feeType != null && feeType.Status == "Inactive")
                {
                    MessageBox.Show("Fee type is already inactive.", "Information");
                    return;
                }

                bool updated = FeeTypeDAL.UpdateFeeTypeStatus(feeTypeID, "Inactive");

                if (updated)
                {
                    MessageBox.Show("Fee type deactivated successfully.", "Success");
                    AuditLogDAL.LogAction(_session.CurrentUser.UserID, "DeactivateFeeType", $"Fee Type {feeTypeID} deactivated");
                    LoadFeeTypes();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show("Failed to deactivate fee type.", "Error");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deactivating fee type");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                var stats = FeeTypeBLL.GetFeeTypeStatistics();
                _lblTotalCount.Text = $"Total: {stats.TotalFeeTypes}";
                _lblActiveCount.Text = $"Active: {stats.ActiveCount}";
                _lblInactiveCount.Text = $"Inactive: {stats.InactiveCount}";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating statistics");
            }
        }
    }

    public partial class FrmFeeTypeDialog : Form
    {
        private dynamic _feeType;
        private TextBox _txtFeeTypeName, _txtDescription, _txtUnitOfMeasurement;

        public FrmFeeTypeDialog(dynamic feeType)
        {
            _feeType = feeType;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            this.Text = _feeType == null ? "Create Fee Type" : "Edit Fee Type";
            this.Size = new System.Drawing.Size(450, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 10;

            Label lblFeeTypeName = new Label { Text = "Fee Type Name:", Left = 10, Top = y, Width = 120 };
            _txtFeeTypeName = new TextBox { Left = 140, Top = y, Width = 280 };
            this.Controls.Add(lblFeeTypeName);
            this.Controls.Add(_txtFeeTypeName);

            y += 35;

            Label lblDescription = new Label { Text = "Description:", Left = 10, Top = y, Width = 120 };
            _txtDescription = new TextBox { Left = 140, Top = y, Width = 280, Height = 60, Multiline = true };
            this.Controls.Add(lblDescription);
            this.Controls.Add(_txtDescription);

            y += 70;

            Label lblUnitOfMeasurement = new Label { Text = "Unit of Measurement:", Left = 10, Top = y, Width = 120 };
            _txtUnitOfMeasurement = new TextBox { Left = 140, Top = y, Width = 280 };
            this.Controls.Add(lblUnitOfMeasurement);
            this.Controls.Add(_txtUnitOfMeasurement);

            y += 35;

            Button btnOK = new Button { Text = "OK", Left = 230, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancel", Left = 340, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            if (_feeType != null)
            {
                _txtFeeTypeName.Text = _feeType.FeeTypeName ?? "";
                _txtDescription.Text = _feeType.Description ?? "";
                _txtUnitOfMeasurement.Text = _feeType.UnitOfMeasurement ?? "";
            }
        }

        public (string FeeTypeName, string Description, string UnitOfMeasurement) GetFeeTypeData()
        {
            return (
                _txtFeeTypeName.Text,
                _txtDescription.Text,
                _txtUnitOfMeasurement.Text
            );
        }
    }
}


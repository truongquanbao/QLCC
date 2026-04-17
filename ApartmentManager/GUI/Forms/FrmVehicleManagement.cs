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
/// Form for managing resident vehicles with registration and tracking
/// </summary>
public class FrmVehicleManagement : Form
{
    private const int PRIMARY_COLOR = 0x215689;
    private const int SPACING = 10;
    private const int LABEL_WIDTH = 120;
    private const int CONTROL_WIDTH = 200;

    // Filter controls
    private ComboBox cmbFilterResident = new ComboBox();
    private TextBox txtSearchLicensePlate = new TextBox();
    private Button btnSearch = new Button();
    private Button btnShowAll = new Button();

    // Vehicle details controls
    private ComboBox cmbResident = new ComboBox();
    private TextBox txtLicensePlate = new TextBox();
    private ComboBox cmbVehicleType = new ComboBox();
    private TextBox txtBrand = new TextBox();
    private TextBox txtModel = new TextBox();
    private NumericUpDown nudYearMade = new NumericUpDown();
    private TextBox txtColor = new TextBox();
    private TextBox txtNote = new TextBox();

    // Labels
    private Label lblResident = new Label();
    private Label lblLicensePlate = new Label();
    private Label lblVehicleType = new Label();
    private Label lblBrand = new Label();
    private Label lblModel = new Label();
    private Label lblYearMade = new Label();
    private Label lblColor = new Label();
    private Label lblNote = new Label();

    // Grid and buttons
    private DataGridView dgvVehicles = new DataGridView();
    private Button btnCreate = new Button();
    private Button btnEdit = new Button();
    private Button btnDelete = new Button();
    private Button btnStatistics = new Button();
    private Button btnClose = new Button();

    // Status
    private Label lblStatusBar = new Label();
    private Label lblVehicleInfo = new Label();

    private int _selectedVehicleID = 0;

    public FrmVehicleManagement()
    {
        if (SessionManager.GetSession() == null || !SessionManager.HasPermission("ManageVehicles"))
        {
            MessageBox.Show("You do not have permission to access this form.", "Access Denied",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.Close();
            return;
        }

        InitializeComponent();
        LoadData();
        AuditLogDAL.LogAction(SessionManager.GetSession().UserID, "Opened vehicle management form", "FrmVehicleManagement");
    }

    private void InitializeComponent()
    {
        this.Text = "Vehicle Management";
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

        var lblResident = new Label { Text = "Resident:", Location = new Point(SPACING, SPACING), Size = new Size(70, 20) };
        pnl.Controls.Add(lblResident);

        cmbFilterResident.Location = new Point(SPACING + 80, SPACING);
        cmbFilterResident.Size = new Size(180, 25);
        cmbFilterResident.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbFilterResident);

        var lblPlate = new Label { Text = "License Plate:", Location = new Point(SPACING + 270, SPACING), Size = new Size(90, 20) };
        pnl.Controls.Add(lblPlate);

        txtSearchLicensePlate.Location = new Point(SPACING + 365, SPACING);
        txtSearchLicensePlate.Size = new Size(150, 25);
        pnl.Controls.Add(txtSearchLicensePlate);

        btnSearch.Text = "Search";
        btnSearch.Location = new Point(SPACING + 525, SPACING);
        btnSearch.Size = new Size(80, 30);
        btnSearch.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnSearch.ForeColor = Color.White;
        btnSearch.Click += BtnSearch_Click;
        pnl.Controls.Add(btnSearch);

        btnShowAll.Text = "Show All";
        btnShowAll.Location = new Point(SPACING + 615, SPACING);
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
        lblResident.Text = "Resident:";
        lblResident.Location = new Point(x, y);
        lblResident.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblResident);

        cmbResident.Location = new Point(x + LABEL_WIDTH + SPACING, y);
        cmbResident.Size = new Size(CONTROL_WIDTH, 25);
        cmbResident.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbResident);

        lblLicensePlate.Text = "License Plate:";
        lblLicensePlate.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y);
        lblLicensePlate.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblLicensePlate);

        txtLicensePlate.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y);
        txtLicensePlate.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtLicensePlate);

        // Row 2
        lblVehicleType.Text = "Vehicle Type:";
        lblVehicleType.Location = new Point(x, y + 35);
        lblVehicleType.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblVehicleType);

        cmbVehicleType.Location = new Point(x + LABEL_WIDTH + SPACING, y + 35);
        cmbVehicleType.Size = new Size(CONTROL_WIDTH, 25);
        cmbVehicleType.Items.AddRange(new string[] { "Car", "Motorcycle", "Truck", "Bus", "Bicycle", "Scooter", "Other" });
        cmbVehicleType.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbVehicleType);

        lblBrand.Text = "Brand:";
        lblBrand.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 35);
        lblBrand.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblBrand);

        txtBrand.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 35);
        txtBrand.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtBrand);

        // Row 3
        lblModel.Text = "Model:";
        lblModel.Location = new Point(x, y + 70);
        lblModel.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblModel);

        txtModel.Location = new Point(x + LABEL_WIDTH + SPACING, y + 70);
        txtModel.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtModel);

        lblYearMade.Text = "Year Made:";
        lblYearMade.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 70);
        lblYearMade.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblYearMade);

        nudYearMade.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 70);
        nudYearMade.Size = new Size(CONTROL_WIDTH, 25);
        nudYearMade.Minimum = 1980;
        nudYearMade.Maximum = DateTime.Now.Year;
        nudYearMade.Value = DateTime.Now.Year;
        pnl.Controls.Add(nudYearMade);

        // Row 4
        lblColor.Text = "Color:";
        lblColor.Location = new Point(x, y + 105);
        lblColor.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblColor);

        txtColor.Location = new Point(x + LABEL_WIDTH + SPACING, y + 105);
        txtColor.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtColor);

        lblNote.Text = "Note:";
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
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 330),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        dgvVehicles.Location = new Point(0, 0);
        dgvVehicles.Size = new Size(pnl.ClientSize.Width, pnl.ClientSize.Height);
        dgvVehicles.Dock = DockStyle.Fill;
        dgvVehicles.AllowUserToAddRows = false;
        dgvVehicles.AllowUserToDeleteRows = false;
        dgvVehicles.ReadOnly = true;
        dgvVehicles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvVehicles.MultiSelect = false;
        dgvVehicles.RowHeadersVisible = false;
        dgvVehicles.CellClick += DgvVehicles_CellClick;

        dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "VehicleID", Width = 50, Visible = false });
        dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "License Plate", DataPropertyName = "LicensePlate", Width = 120 });
        dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", DataPropertyName = "VehicleType", Width = 100 });
        dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Brand", DataPropertyName = "Brand", Width = 100 });
        dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Model", DataPropertyName = "Model", Width = 100 });
        dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Year", DataPropertyName = "YearMade", Width = 70 });
        dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Color", DataPropertyName = "Color", Width = 80 });
        dgvVehicles.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Owner", DataPropertyName = "ResidentName", Width = 120 });

        pnl.Controls.Add(dgvVehicles);
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

        btnCreate.Text = "Create";
        btnCreate.Location = new Point(x, y);
        btnCreate.Size = new Size(100, 30);
        btnCreate.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnCreate.ForeColor = Color.White;
        btnCreate.Click += BtnCreate_Click;
        pnl.Controls.Add(btnCreate);

        btnEdit.Text = "Edit";
        btnEdit.Location = new Point(x + 110, y);
        btnEdit.Size = new Size(100, 30);
        btnEdit.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnEdit.ForeColor = Color.White;
        btnEdit.Click += BtnEdit_Click;
        pnl.Controls.Add(btnEdit);

        btnDelete.Text = "Delete";
        btnDelete.Location = new Point(x + 220, y);
        btnDelete.Size = new Size(100, 30);
        btnDelete.BackColor = Color.FromArgb(0xC00000);
        btnDelete.ForeColor = Color.White;
        btnDelete.Click += BtnDelete_Click;
        pnl.Controls.Add(btnDelete);

        btnStatistics.Text = "Statistics";
        btnStatistics.Location = new Point(x + 330, y);
        btnStatistics.Size = new Size(100, 30);
        btnStatistics.BackColor = Color.FromArgb(0, 100, 0);
        btnStatistics.ForeColor = Color.White;
        btnStatistics.Click += BtnStatistics_Click;
        pnl.Controls.Add(btnStatistics);

        btnClose.Text = "Close";
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

        lblStatusBar.Text = "Ready";
        lblStatusBar.Location = new Point(SPACING, SPACING);
        lblStatusBar.Size = new Size(400, 20);
        lblStatusBar.ForeColor = Color.Green;
        pnl.Controls.Add(lblStatusBar);

        lblVehicleInfo.Text = "Total Vehicles: 0";
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
            lblStatusBar.Text = $"Loaded {vehicles.Count} vehicles";
            lblVehicleInfo.Text = $"Total Vehicles: {vehicles.Count}";

            LoadResidents();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading vehicles: {ex.Message}", "Error");
        }
    }

    private void LoadResidents()
    {
        try
        {
            var residents = ResidentDAL.GetAllResidents();
            cmbResident.Items.Clear();
            cmbFilterResident.Items.Clear();
            cmbFilterResident.Items.Add("All");

            foreach (var res in residents)
            {
                cmbResident.Items.Add(new { Text = res.FullName, Value = res.ResidentID });
                cmbFilterResident.Items.Add(new { Text = res.FullName, Value = res.ResidentID });
            }

            cmbResident.DisplayMember = "Text";
            cmbResident.ValueMember = "Value";
            cmbFilterResident.DisplayMember = "Text";
            cmbFilterResident.ValueMember = "Value";
            cmbFilterResident.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading residents: {ex.Message}", "Error");
        }
    }

    private void DgvVehicles_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            _selectedVehicleID = (int)dgvVehicles[0, e.RowIndex].Value;
            var vehicle = VehicleDAL.GetVehicleByID(_selectedVehicleID);

            if (vehicle != null)
            {
                txtLicensePlate.Text = vehicle.LicensePlate;
                cmbVehicleType.SelectedItem = vehicle.VehicleType;
                txtBrand.Text = vehicle.Brand;
                txtModel.Text = vehicle.Model;
                nudYearMade.Value = vehicle.YearMade;
                txtColor.Text = vehicle.Color ?? "";
                txtNote.Text = vehicle.Note ?? "";

                var resItem = cmbResident.Items.Cast<dynamic>().FirstOrDefault(r => r.Value == vehicle.ResidentID);
                if (resItem != null)
                    cmbResident.SelectedItem = resItem;

                lblStatusBar.Text = $"Selected: {vehicle.LicensePlate} - {vehicle.Brand} {vehicle.Model}";
            }
        }
    }

    private void BtnCreate_Click(object sender, EventArgs e)
    {
        if (cmbResident.SelectedItem == null)
        {
            MessageBox.Show("Please select a resident.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            int residentID = (int)((dynamic)cmbResident.SelectedItem).Value;

            var result = VehicleBLL.CreateVehicle(
                residentID,
                txtLicensePlate.Text,
                cmbVehicleType.SelectedItem?.ToString() ?? "Other",
                txtBrand.Text,
                txtModel.Text,
                (int)nudYearMade.Value,
                txtColor.Text,
                txtNote.Text
            );

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Created vehicle {txtLicensePlate.Text}", "FrmVehicleManagement");
                ClearForm();
                LoadData();
            }
            else
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating vehicle: {ex.Message}", "Error");
        }
    }

    private void BtnEdit_Click(object sender, EventArgs e)
    {
        if (_selectedVehicleID == 0)
        {
            MessageBox.Show("Please select a vehicle.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var result = VehicleBLL.UpdateVehicle(
                _selectedVehicleID,
                txtLicensePlate.Text,
                cmbVehicleType.SelectedItem?.ToString() ?? "Other",
                txtBrand.Text,
                txtModel.Text,
                (int)nudYearMade.Value,
                txtColor.Text,
                txtNote.Text
            );

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Updated vehicle {txtLicensePlate.Text}", "FrmVehicleManagement");
                LoadData();
                ClearForm();
            }
            else
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating vehicle: {ex.Message}", "Error");
        }
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_selectedVehicleID == 0)
        {
            MessageBox.Show("Please select a vehicle.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (MessageBox.Show("Are you sure you want to delete this vehicle?", "Confirm",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            var result = VehicleBLL.DeleteVehicle(_selectedVehicleID);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Deleted vehicle {txtLicensePlate.Text}", "FrmVehicleManagement");
                LoadData();
                ClearForm();
            }
            else
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting vehicle: {ex.Message}", "Error");
        }
    }

    private void BtnSearch_Click(object sender, EventArgs e)
    {
        try
        {
            var allVehicles = VehicleDAL.GetAllVehicles();
            var filtered = allVehicles.AsEnumerable();

            if (cmbFilterResident.SelectedIndex > 0)
            {
                int resID = (int)((dynamic)cmbFilterResident.SelectedItem).Value;
                filtered = filtered.Where(v => v.ResidentID == resID);
            }

            if (!string.IsNullOrWhiteSpace(txtSearchLicensePlate.Text))
            {
                filtered = filtered.Where(v => v.LicensePlate.Contains(txtSearchLicensePlate.Text, StringComparison.OrdinalIgnoreCase));
            }

            dgvVehicles.DataSource = filtered.ToList();
            lblStatusBar.Text = $"Found {filtered.Count()} vehicles";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error searching: {ex.Message}", "Error");
        }
    }

    private void BtnStatistics_Click(object sender, EventArgs e)
    {
        try
        {
            var stats = VehicleBLL.GetVehicleStatistics();
            if (stats != null)
            {
                string message = $"Total Vehicles: {stats.TotalVehicles}\n" +
                               $"By Type:\n" +
                               $"  Cars: {stats.CarCount}\n" +
                               $"  Motorcycles: {stats.MotorcycleCount}\n" +
                               $"  Trucks: {stats.TruckCount}\n" +
                               $"  Other: {stats.OtherCount}";

                MessageBox.Show(message, "Vehicle Statistics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error getting statistics: {ex.Message}", "Error");
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
        cmbResident.SelectedIndex = -1;
        _selectedVehicleID = 0;
    }
}

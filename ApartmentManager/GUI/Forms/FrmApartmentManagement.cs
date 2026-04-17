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
/// Form for managing apartment building hierarchy and CRUD operations
/// Manages: Buildings → Blocks → Floors → Apartments
/// </summary>
public class FrmApartmentManagement : Form
{
    private const int PRIMARY_COLOR = 0x215689; // RGB(33, 86, 155)
    private const int SPACING = 10;
    private const int LABEL_WIDTH = 120;
    private const int CONTROL_WIDTH = 200;

    // Building hierarchy controls
    private ComboBox cmbBuilding = new ComboBox();
    private ComboBox cmbBlock = new ComboBox();
    private ComboBox cmbFloor = new ComboBox();
    private Label lblBuilding = new Label();
    private Label lblBlock = new Label();
    private Label lblFloor = new Label();

    // Apartment details controls
    private TextBox txtApartmentCode = new TextBox();
    private TextBox txtArea = new TextBox();
    private ComboBox cmbApartmentType = new ComboBox();
    private ComboBox cmbStatus = new ComboBox();
    private NumericUpDown nudMaxResidents = new NumericUpDown();
    private TextBox txtNote = new TextBox();

    // Labels for apartment controls
    private Label lblApartmentCode = new Label();
    private Label lblArea = new Label();
    private Label lblApartmentType = new Label();
    private Label lblStatus = new Label();
    private Label lblMaxResidents = new Label();
    private Label lblNote = new Label();

    // Grid and buttons
    private DataGridView dgvApartments = new DataGridView();
    private Button btnCreate = new Button();
    private Button btnEdit = new Button();
    private Button btnDelete = new Button();
    private Button btnRefresh = new Button();
    private Button btnStatistics = new Button();
    private Button btnClose = new Button();

    // Status and info
    private Label lblStatusBar = new Label();
    private Label lblOccupancyInfo = new Label();

    private int _selectedApartmentID = 0;

    public FrmApartmentManagement()
    {
        // Check session
        if (SessionManager.GetSession() == null || !SessionManager.HasPermission("ManageApartments"))
        {
            MessageBox.Show("You do not have permission to access this form.", "Access Denied",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.Close();
            return;
        }

        InitializeComponent();
        LoadBuildings();
        AuditLogDAL.LogAction(SessionManager.GetSession().UserID, "Opened apartment management form", "FrmApartmentManagement");
    }

    private void InitializeComponent()
    {
        // Form settings
        this.Text = "Apartment Management";
        this.Size = new Size(1200, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.White;

        // Create panels
        var pnlHierarchy = CreateHierarchyPanel();
        var pnlDetails = CreateDetailsPanel();
        var pnlGrid = CreateGridPanel();
        var pnlButtons = CreateButtonPanel();
        var pnlStatus = CreateStatusPanel();

        // Add panels to form
        this.Controls.Add(pnlHierarchy);
        this.Controls.Add(pnlDetails);
        this.Controls.Add(pnlGrid);
        this.Controls.Add(pnlButtons);
        this.Controls.Add(pnlStatus);
    }

    private Panel CreateHierarchyPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, SPACING),
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 80),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        // Building
        lblBuilding.Text = "Building:";
        lblBuilding.Location = new Point(SPACING, SPACING);
        lblBuilding.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblBuilding);

        cmbBuilding.Location = new Point(SPACING + LABEL_WIDTH + SPACING, SPACING);
        cmbBuilding.Size = new Size(CONTROL_WIDTH, 25);
        cmbBuilding.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbBuilding.SelectedIndexChanged += CmbBuilding_SelectedIndexChanged;
        pnl.Controls.Add(cmbBuilding);

        // Block
        lblBlock.Text = "Block:";
        lblBlock.Location = new Point(SPACING + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, SPACING);
        lblBlock.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblBlock);

        cmbBlock.Location = new Point(SPACING + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, SPACING);
        cmbBlock.Size = new Size(CONTROL_WIDTH, 25);
        cmbBlock.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbBlock.SelectedIndexChanged += CmbBlock_SelectedIndexChanged;
        pnl.Controls.Add(cmbBlock);

        // Floor
        lblFloor.Text = "Floor:";
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
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 140),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        int y = SPACING;
        int x = SPACING;

        // Apartment Code
        lblApartmentCode.Text = "Code:";
        lblApartmentCode.Location = new Point(x, y);
        lblApartmentCode.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblApartmentCode);

        txtApartmentCode.Location = new Point(x + LABEL_WIDTH + SPACING, y);
        txtApartmentCode.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtApartmentCode);

        // Area
        lblArea.Text = "Area (m²):";
        lblArea.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y);
        lblArea.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblArea);

        txtArea.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y);
        txtArea.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtArea);

        // Apartment Type
        lblApartmentType.Text = "Type:";
        lblApartmentType.Location = new Point(x, y + 35);
        lblApartmentType.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblApartmentType);

        cmbApartmentType.Location = new Point(x + LABEL_WIDTH + SPACING, y + 35);
        cmbApartmentType.Size = new Size(CONTROL_WIDTH, 25);
        cmbApartmentType.Items.AddRange(new string[] { "Studio", "1BR", "2BR", "3BR", "Penthouse", "Other" });
        pnl.Controls.Add(cmbApartmentType);

        // Status
        lblStatus.Text = "Status:";
        lblStatus.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 35);
        lblStatus.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblStatus);

        cmbStatus.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 35);
        cmbStatus.Size = new Size(CONTROL_WIDTH, 25);
        cmbStatus.Items.AddRange(new string[] { "Empty", "Occupied", "Renting", "Maintenance", "Locked" });
        pnl.Controls.Add(cmbStatus);

        // Max Residents
        lblMaxResidents.Text = "Max Residents:";
        lblMaxResidents.Location = new Point(x, y + 70);
        lblMaxResidents.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblMaxResidents);

        nudMaxResidents.Location = new Point(x + LABEL_WIDTH + SPACING, y + 70);
        nudMaxResidents.Size = new Size(CONTROL_WIDTH, 25);
        nudMaxResidents.Minimum = 1;
        nudMaxResidents.Maximum = 20;
        nudMaxResidents.Value = 4;
        pnl.Controls.Add(nudMaxResidents);

        // Note
        lblNote.Text = "Note:";
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
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 350),
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

        // Configure columns
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "ApartmentID", Width = 50, Visible = false });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Code", DataPropertyName = "ApartmentCode", Width = 100 });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Area (m²)", DataPropertyName = "Area", Width = 80 });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", DataPropertyName = "ApartmentType", Width = 100 });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", Width = 100 });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Max Residents", DataPropertyName = "MaxResidents", Width = 100 });
        dgvApartments.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Note", DataPropertyName = "Note", Width = 200 });

        pnl.Controls.Add(dgvApartments);
        return pnl;
    }

    private Panel CreateButtonPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 610),
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 50),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        int x = SPACING;
        int y = SPACING;

        // Create Button
        btnCreate.Text = "Create";
        btnCreate.Location = new Point(x, y);
        btnCreate.Size = new Size(100, 30);
        btnCreate.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnCreate.ForeColor = Color.White;
        btnCreate.Click += BtnCreate_Click;
        pnl.Controls.Add(btnCreate);

        // Edit Button
        btnEdit.Text = "Edit";
        btnEdit.Location = new Point(x + 110, y);
        btnEdit.Size = new Size(100, 30);
        btnEdit.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnEdit.ForeColor = Color.White;
        btnEdit.Click += BtnEdit_Click;
        pnl.Controls.Add(btnEdit);

        // Delete Button
        btnDelete.Text = "Delete";
        btnDelete.Location = new Point(x + 220, y);
        btnDelete.Size = new Size(100, 30);
        btnDelete.BackColor = Color.FromArgb(0xC00000);
        btnDelete.ForeColor = Color.White;
        btnDelete.Click += BtnDelete_Click;
        pnl.Controls.Add(btnDelete);

        // Refresh Button
        btnRefresh.Text = "Refresh";
        btnRefresh.Location = new Point(x + 330, y);
        btnRefresh.Size = new Size(100, 30);
        btnRefresh.BackColor = Color.FromArgb(70, 130, 180);
        btnRefresh.ForeColor = Color.White;
        btnRefresh.Click += BtnRefresh_Click;
        pnl.Controls.Add(btnRefresh);

        // Statistics Button
        btnStatistics.Text = "Statistics";
        btnStatistics.Location = new Point(x + 440, y);
        btnStatistics.Size = new Size(100, 30);
        btnStatistics.BackColor = Color.FromArgb(0, 100, 0);
        btnStatistics.ForeColor = Color.White;
        btnStatistics.Click += BtnStatistics_Click;
        pnl.Controls.Add(btnStatistics);

        // Close Button
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
            Location = new Point(SPACING, 670),
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 60),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        lblStatusBar.Text = "Ready";
        lblStatusBar.Location = new Point(SPACING, SPACING);
        lblStatusBar.Size = new Size(400, 20);
        lblStatusBar.ForeColor = Color.Green;
        pnl.Controls.Add(lblStatusBar);

        lblOccupancyInfo.Text = "Occupancy: 0%";
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
                MessageBox.Show("No buildings found. Please create a building first.", "Info",
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
            MessageBox.Show($"Error loading buildings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CmbBuilding_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbBuilding.SelectedItem == null) return;

        try
        {
            int buildingID = (int)((dynamic)cmbBuilding.SelectedItem).Value;
            cmbBlock.Items.Clear();
            var blocks = BlockDAL.GetBlocksByBuilding(buildingID);

            if (blocks.Count == 0)
            {
                lblStatusBar.Text = "No blocks in this building";
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
            MessageBox.Show($"Error loading blocks: {ex.Message}", "Error");
        }
    }

    private void CmbBlock_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbBlock.SelectedItem == null) return;

        try
        {
            int blockID = (int)((dynamic)cmbBlock.SelectedItem).Value;
            cmbFloor.Items.Clear();
            var floors = FloorDAL.GetFloorsByBlock(blockID);

            if (floors.Count == 0)
            {
                lblStatusBar.Text = "No floors in this block";
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
            MessageBox.Show($"Error loading floors: {ex.Message}", "Error");
        }
    }

    private void CmbFloor_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbFloor.SelectedItem == null) return;

        try
        {
            int floorID = (int)((dynamic)cmbFloor.SelectedItem).Value;
            LoadApartments(floorID);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading apartments: {ex.Message}", "Error");
        }
    }

    private void LoadApartments(int floorID)
    {
        try
        {
            var apartments = ApartmentDAL.GetApartmentsByFloor(floorID);
            dgvApartments.DataSource = apartments.Cast<dynamic>().ToList();
            lblStatusBar.Text = $"Loaded {apartments.Count} apartments";

            // Update occupancy info
            int occupied = apartments.Count(a => a.Status == "Occupied");
            int occupancyRate = apartments.Count > 0 ? (occupied * 100) / apartments.Count : 0;
            lblOccupancyInfo.Text = $"Occupancy: {occupancyRate}% ({occupied}/{apartments.Count})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading apartments: {ex.Message}", "Error");
        }
    }

    private void DgvApartments_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            _selectedApartmentID = (int)dgvApartments[0, e.RowIndex].Value;
            var apartment = ApartmentDAL.GetApartmentByID(_selectedApartmentID);

            if (apartment != null)
            {
                txtApartmentCode.Text = apartment.ApartmentCode;
                txtArea.Text = apartment.Area.ToString();
                cmbApartmentType.SelectedItem = apartment.ApartmentType;
                cmbStatus.SelectedItem = apartment.Status;
                nudMaxResidents.Value = apartment.MaxResidents;
                txtNote.Text = apartment.Note ?? "";
                lblStatusBar.Text = $"Selected: {apartment.ApartmentCode}";
            }
        }
    }

    private void BtnCreate_Click(object sender, EventArgs e)
    {
        if (cmbFloor.SelectedItem == null)
        {
            MessageBox.Show("Please select a floor first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            int floorID = (int)((dynamic)cmbFloor.SelectedItem).Value;

            var result = ApartmentBLL.CreateApartment(
                txtApartmentCode.Text,
                floorID,
                decimal.Parse(txtArea.Text),
                cmbApartmentType.SelectedItem?.ToString() ?? "Other",
                (int)nudMaxResidents.Value,
                txtNote.Text
            );

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Created apartment {txtApartmentCode.Text}", "FrmApartmentManagement");
                ClearForm();
                LoadApartments(floorID);
            }
            else
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating apartment: {ex.Message}", "Error");
        }
    }

    private void BtnEdit_Click(object sender, EventArgs e)
    {
        if (_selectedApartmentID == 0)
        {
            MessageBox.Show("Please select an apartment first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var result = ApartmentBLL.UpdateApartment(
                _selectedApartmentID,
                txtApartmentCode.Text,
                decimal.Parse(txtArea.Text),
                cmbApartmentType.SelectedItem?.ToString() ?? "Other",
                (int)nudMaxResidents.Value,
                txtNote.Text
            );

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Updated apartment {txtApartmentCode.Text}", "FrmApartmentManagement");
                int floorID = (int)((dynamic)cmbFloor.SelectedItem).Value;
                LoadApartments(floorID);
                ClearForm();
            }
            else
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating apartment: {ex.Message}", "Error");
        }
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_selectedApartmentID == 0)
        {
            MessageBox.Show("Please select an apartment first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (MessageBox.Show("Are you sure you want to delete this apartment?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            var result = ApartmentBLL.DeleteApartment(_selectedApartmentID);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Deleted apartment {txtApartmentCode.Text}", "FrmApartmentManagement");
                int floorID = (int)((dynamic)cmbFloor.SelectedItem).Value;
                LoadApartments(floorID);
                ClearForm();
            }
            else
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting apartment: {ex.Message}", "Error");
        }
    }

    private void BtnRefresh_Click(object sender, EventArgs e)
    {
        try
        {
            if (cmbFloor.SelectedItem != null)
            {
                int floorID = (int)((dynamic)cmbFloor.SelectedItem).Value;
                LoadApartments(floorID);
                lblStatusBar.Text = "Refreshed successfully";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error refreshing: {ex.Message}", "Error");
        }
    }

    private void BtnStatistics_Click(object sender, EventArgs e)
    {
        try
        {
            var stats = ApartmentBLL.GetOccupancyStatistics();
            if (stats != null)
            {
                string message = $"Total Apartments: {stats.TotalApartments}\n" +
                               $"Empty: {stats.EmptyApartments}\n" +
                               $"Occupied: {stats.OccupiedApartments}\n" +
                               $"Renting: {stats.RentingApartments}\n" +
                               $"Maintenance: {stats.MaintenanceApartments}\n" +
                               $"Locked: {stats.LockedApartments}\n" +
                               $"Occupancy Rate: {stats.OccupancyRate}";

                MessageBox.Show(message, "Occupancy Statistics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error getting statistics: {ex.Message}", "Error");
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

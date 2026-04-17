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
/// Form for managing resident complaints with assignment and resolution tracking
/// </summary>
public class FrmComplaintManagement : Form
{
    private const int PRIMARY_COLOR = 0x215689;
    private const int SPACING = 10;
    private const int LABEL_WIDTH = 120;
    private const int CONTROL_WIDTH = 200;

    // Filter controls
    private ComboBox cmbFilterStatus = new ComboBox();
    private ComboBox cmbFilterPriority = new ComboBox();
    private Button btnSearch = new Button();
    private Button btnShowAll = new Button();

    // Complaint details controls
    private ComboBox cmbResident = new ComboBox();
    private TextBox txtTitle = new TextBox();
    private TextBox txtDescription = new TextBox();
    private ComboBox cmbPriority = new ComboBox();
    private ComboBox cmbStatus = new ComboBox();
    private ComboBox cmbAssignedTo = new ComboBox();
    private TextBox txtResolutionNote = new TextBox();
    private DateTimePicker dtpReportDate = new DateTimePicker();
    private DateTimePicker dtpCompletionDate = new DateTimePicker();

    // Labels
    private Label lblResident = new Label();
    private Label lblTitle = new Label();
    private Label lblDescription = new Label();
    private Label lblPriority = new Label();
    private Label lblStatus = new Label();
    private Label lblAssignedTo = new Label();
    private Label lblResolutionNote = new Label();
    private Label lblReportDate = new Label();
    private Label lblCompletionDate = new Label();

    // Grid and buttons
    private DataGridView dgvComplaints = new DataGridView();
    private Button btnCreate = new Button();
    private Button btnEdit = new Button();
    private Button btnAssign = new Button();
    private Button btnResolve = new Button();
    private Button btnDelete = new Button();
    private Button btnStatistics = new Button();
    private Button btnClose = new Button();

    // Status
    private Label lblStatusBar = new Label();
    private Label lblComplaintInfo = new Label();

    private int _selectedComplaintID = 0;
    private List<dynamic> _staffList = new List<dynamic>();

    public FrmComplaintManagement()
    {
        if (SessionManager.GetSession() == null || !SessionManager.HasPermission("ManageComplaints"))
        {
            MessageBox.Show("You do not have permission to access this form.", "Access Denied",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.Close();
            return;
        }

        InitializeComponent();
        LoadData();
        AuditLogDAL.LogAction(SessionManager.GetSession().UserID, "Opened complaint management form", "FrmComplaintManagement");
    }

    private void InitializeComponent()
    {
        this.Text = "Complaint Management";
        this.Size = new Size(1300, 950);
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

        var lblStatus = new Label { Text = "Status:", Location = new Point(SPACING, SPACING), Size = new Size(60, 20) };
        pnl.Controls.Add(lblStatus);

        cmbFilterStatus.Location = new Point(SPACING + 70, SPACING);
        cmbFilterStatus.Size = new Size(120, 25);
        cmbFilterStatus.Items.AddRange(new string[] { "All", "New", "Assigned", "In-Progress", "Resolved", "Closed" });
        cmbFilterStatus.SelectedIndex = 0;
        pnl.Controls.Add(cmbFilterStatus);

        var lblPriority = new Label { Text = "Priority:", Location = new Point(SPACING + 200, SPACING), Size = new Size(70, 20) };
        pnl.Controls.Add(lblPriority);

        cmbFilterPriority.Location = new Point(SPACING + 280, SPACING);
        cmbFilterPriority.Size = new Size(120, 25);
        cmbFilterPriority.Items.AddRange(new string[] { "All", "Low", "Medium", "High", "Critical" });
        cmbFilterPriority.SelectedIndex = 0;
        pnl.Controls.Add(cmbFilterPriority);

        btnSearch.Text = "Search";
        btnSearch.Location = new Point(SPACING + 410, SPACING);
        btnSearch.Size = new Size(80, 30);
        btnSearch.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnSearch.ForeColor = Color.White;
        btnSearch.Click += BtnSearch_Click;
        pnl.Controls.Add(btnSearch);

        btnShowAll.Text = "Show All";
        btnShowAll.Location = new Point(SPACING + 500, SPACING);
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
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 200),
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

        lblPriority.Text = "Priority:";
        lblPriority.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y);
        lblPriority.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblPriority);

        cmbPriority.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y);
        cmbPriority.Size = new Size(CONTROL_WIDTH, 25);
        cmbPriority.Items.AddRange(new string[] { "Low", "Medium", "High", "Critical" });
        cmbPriority.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbPriority);

        // Row 2
        lblTitle.Text = "Title:";
        lblTitle.Location = new Point(x, y + 35);
        lblTitle.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblTitle);

        txtTitle.Location = new Point(x + LABEL_WIDTH + SPACING, y + 35);
        txtTitle.Size = new Size(CONTROL_WIDTH * 2 + SPACING + 80, 25);
        pnl.Controls.Add(txtTitle);

        // Row 3
        lblDescription.Text = "Description:";
        lblDescription.Location = new Point(x, y + 70);
        lblDescription.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblDescription);

        txtDescription.Location = new Point(x + LABEL_WIDTH + SPACING, y + 70);
        txtDescription.Size = new Size(CONTROL_WIDTH * 2 + SPACING + 80, 60);
        txtDescription.Multiline = true;
        pnl.Controls.Add(txtDescription);

        // Row 4 - Right side
        lblStatus.Text = "Status:";
        lblStatus.Location = new Point(x + LABEL_WIDTH * 3 + CONTROL_WIDTH * 2 + SPACING * 5, y + 35);
        lblStatus.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblStatus);

        cmbStatus.Location = new Point(x + LABEL_WIDTH * 4 + CONTROL_WIDTH * 2 + SPACING * 6, y + 35);
        cmbStatus.Size = new Size(CONTROL_WIDTH, 25);
        cmbStatus.Items.AddRange(new string[] { "New", "Assigned", "In-Progress", "Resolved", "Closed" });
        cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbStatus);

        lblAssignedTo.Text = "Assigned To:";
        lblAssignedTo.Location = new Point(x + LABEL_WIDTH * 3 + CONTROL_WIDTH * 2 + SPACING * 5, y + 70);
        lblAssignedTo.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblAssignedTo);

        cmbAssignedTo.Location = new Point(x + LABEL_WIDTH * 4 + CONTROL_WIDTH * 2 + SPACING * 6, y + 70);
        cmbAssignedTo.Size = new Size(CONTROL_WIDTH, 25);
        cmbAssignedTo.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbAssignedTo);

        return pnl;
    }

    private Panel CreateGridPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 280),
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 340),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        dgvComplaints.Location = new Point(0, 0);
        dgvComplaints.Size = new Size(pnl.ClientSize.Width, pnl.ClientSize.Height);
        dgvComplaints.Dock = DockStyle.Fill;
        dgvComplaints.AllowUserToAddRows = false;
        dgvComplaints.AllowUserToDeleteRows = false;
        dgvComplaints.ReadOnly = true;
        dgvComplaints.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvComplaints.MultiSelect = false;
        dgvComplaints.RowHeadersVisible = false;
        dgvComplaints.CellClick += DgvComplaints_CellClick;

        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "ComplaintID", Width = 50, Visible = false });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Title", DataPropertyName = "Title", Width = 180 });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Resident", DataPropertyName = "ResidentName", Width = 120 });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Priority", DataPropertyName = "Priority", Width = 80 });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", Width = 100 });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Assigned To", DataPropertyName = "AssignedToName", Width = 120 });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Report Date", DataPropertyName = "ReportDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvComplaints.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Completion", DataPropertyName = "CompletionDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });

        pnl.Controls.Add(dgvComplaints);
        return pnl;
    }

    private Panel CreateButtonPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 630),
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

        btnAssign.Text = "Assign";
        btnAssign.Location = new Point(x + 220, y);
        btnAssign.Size = new Size(100, 30);
        btnAssign.BackColor = Color.FromArgb(0, 100, 100);
        btnAssign.ForeColor = Color.White;
        btnAssign.Click += BtnAssign_Click;
        pnl.Controls.Add(btnAssign);

        btnResolve.Text = "Resolve";
        btnResolve.Location = new Point(x + 330, y);
        btnResolve.Size = new Size(100, 30);
        btnResolve.BackColor = Color.FromArgb(0, 100, 0);
        btnResolve.ForeColor = Color.White;
        btnResolve.Click += BtnResolve_Click;
        pnl.Controls.Add(btnResolve);

        btnDelete.Text = "Delete";
        btnDelete.Location = new Point(x + 440, y);
        btnDelete.Size = new Size(100, 30);
        btnDelete.BackColor = Color.FromArgb(0xC00000);
        btnDelete.ForeColor = Color.White;
        btnDelete.Click += BtnDelete_Click;
        pnl.Controls.Add(btnDelete);

        btnStatistics.Text = "Statistics";
        btnStatistics.Location = new Point(x + 550, y);
        btnStatistics.Size = new Size(100, 30);
        btnStatistics.BackColor = Color.FromArgb(255, 140, 0);
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
            Location = new Point(SPACING, 690),
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 70),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        lblStatusBar.Text = "Ready";
        lblStatusBar.Location = new Point(SPACING, SPACING);
        lblStatusBar.Size = new Size(400, 20);
        lblStatusBar.ForeColor = Color.Green;
        pnl.Controls.Add(lblStatusBar);

        lblComplaintInfo.Text = "Total Complaints: 0 | Pending: 0 | Critical: 0";
        lblComplaintInfo.Location = new Point(SPACING, SPACING + 25);
        lblComplaintInfo.Size = new Size(600, 20);
        pnl.Controls.Add(lblComplaintInfo);

        return pnl;
    }

    private void LoadData()
    {
        try
        {
            var complaints = ComplaintDAL.GetAllComplaints();
            dgvComplaints.DataSource = complaints.Cast<dynamic>().ToList();
            lblStatusBar.Text = $"Loaded {complaints.Count} complaints";

            LoadResidents();
            LoadStaff();
            CalculateComplaintInfo();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading complaints: {ex.Message}", "Error");
        }
    }

    private void LoadResidents()
    {
        try
        {
            var residents = ResidentDAL.GetAllResidents();
            cmbResident.Items.Clear();

            foreach (var res in residents)
            {
                cmbResident.Items.Add(new { Text = res.FullName, Value = res.ResidentID });
            }

            cmbResident.DisplayMember = "Text";
            cmbResident.ValueMember = "Value";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading residents: {ex.Message}", "Error");
        }
    }

    private void LoadStaff()
    {
        try
        {
            var users = UserDAL.GetAllUsers();
            _staffList = users.Where(u => u.IsActive).ToList();
            cmbAssignedTo.Items.Clear();
            cmbAssignedTo.Items.Add(new { Text = "Unassigned", Value = 0 });

            foreach (var user in _staffList)
            {
                cmbAssignedTo.Items.Add(new { Text = user.FullName, Value = user.UserID });
            }

            cmbAssignedTo.DisplayMember = "Text";
            cmbAssignedTo.ValueMember = "Value";
            cmbAssignedTo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading staff: {ex.Message}", "Error");
        }
    }

    private void CalculateComplaintInfo()
    {
        try
        {
            var complaints = ComplaintDAL.GetAllComplaints();
            int pending = complaints.Count(c => c.Status != "Resolved" && c.Status != "Closed");
            int critical = complaints.Count(c => c.Priority == "Critical");

            lblComplaintInfo.Text = $"Total Complaints: {complaints.Count} | Pending: {pending} | Critical: {critical}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error calculating complaint info: {ex.Message}", "Error");
        }
    }

    private void DgvComplaints_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            _selectedComplaintID = (int)dgvComplaints[0, e.RowIndex].Value;
            var complaint = ComplaintDAL.GetComplaintByID(_selectedComplaintID);

            if (complaint != null)
            {
                txtTitle.Text = complaint.Title;
                txtDescription.Text = complaint.Description;
                cmbPriority.SelectedItem = complaint.Priority;
                cmbStatus.SelectedItem = complaint.Status;
                txtResolutionNote.Text = complaint.ResolutionNote ?? "";
                dtpReportDate.Value = complaint.ReportDate;

                if (complaint.CompletionDate.HasValue)
                    dtpCompletionDate.Value = complaint.CompletionDate.Value;

                var resItem = cmbResident.Items.Cast<dynamic>().FirstOrDefault(r => r.Value == complaint.ResidentID);
                if (resItem != null)
                    cmbResident.SelectedItem = resItem;

                var assignedItem = cmbAssignedTo.Items.Cast<dynamic>().FirstOrDefault(a => a.Value == (complaint.AssignedToUserID ?? 0));
                if (assignedItem != null)
                    cmbAssignedTo.SelectedItem = assignedItem;

                lblStatusBar.Text = $"Selected: {complaint.Title} - {complaint.Status}";
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

            var result = ComplaintBLL.CreateComplaint(
                residentID,
                txtTitle.Text,
                txtDescription.Text,
                cmbPriority.SelectedItem?.ToString() ?? "Medium"
            );

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Created complaint: {txtTitle.Text}", "FrmComplaintManagement");
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
            MessageBox.Show($"Error creating complaint: {ex.Message}", "Error");
        }
    }

    private void BtnEdit_Click(object sender, EventArgs e)
    {
        if (_selectedComplaintID == 0)
        {
            MessageBox.Show("Please select a complaint.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var result = ComplaintBLL.UpdateComplaint(
                _selectedComplaintID,
                txtTitle.Text,
                txtDescription.Text,
                cmbPriority.SelectedItem?.ToString() ?? "Medium"
            );

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Updated complaint {_selectedComplaintID}", "FrmComplaintManagement");
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
            MessageBox.Show($"Error updating complaint: {ex.Message}", "Error");
        }
    }

    private void BtnAssign_Click(object sender, EventArgs e)
    {
        if (_selectedComplaintID == 0)
        {
            MessageBox.Show("Please select a complaint.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            int staffID = (int)((dynamic)cmbAssignedTo.SelectedItem).Value;
            
            var result = ComplaintBLL.AssignComplaint(_selectedComplaintID, staffID > 0 ? staffID : null);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Assigned complaint {_selectedComplaintID}", "FrmComplaintManagement");
                LoadData();
            }
            else
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error assigning complaint: {ex.Message}", "Error");
        }
    }

    private void BtnResolve_Click(object sender, EventArgs e)
    {
        if (_selectedComplaintID == 0)
        {
            MessageBox.Show("Please select a complaint.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var result = ComplaintBLL.ResolveComplaint(_selectedComplaintID, txtResolutionNote.Text);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Resolved complaint {_selectedComplaintID}", "FrmComplaintManagement");
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
            MessageBox.Show($"Error resolving complaint: {ex.Message}", "Error");
        }
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_selectedComplaintID == 0)
        {
            MessageBox.Show("Please select a complaint.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (MessageBox.Show("Are you sure you want to delete this complaint?", "Confirm",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            var result = ComplaintBLL.DeleteComplaint(_selectedComplaintID);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Deleted complaint {_selectedComplaintID}", "FrmComplaintManagement");
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
            MessageBox.Show($"Error deleting complaint: {ex.Message}", "Error");
        }
    }

    private void BtnSearch_Click(object sender, EventArgs e)
    {
        try
        {
            var allComplaints = ComplaintDAL.GetAllComplaints();
            var filtered = allComplaints.AsEnumerable();

            if (cmbFilterStatus.SelectedIndex > 0)
                filtered = filtered.Where(c => c.Status == cmbFilterStatus.SelectedItem.ToString());

            if (cmbFilterPriority.SelectedIndex > 0)
                filtered = filtered.Where(c => c.Priority == cmbFilterPriority.SelectedItem.ToString());

            dgvComplaints.DataSource = filtered.ToList();
            lblStatusBar.Text = $"Found {filtered.Count()} complaints";
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
            var stats = ComplaintBLL.GetComplaintStatistics();
            if (stats != null)
            {
                string message = $"Total Complaints: {stats.TotalComplaints}\n\n" +
                               $"By Priority:\n" +
                               $"  Low: {stats.LowPriority}\n" +
                               $"  Medium: {stats.MediumPriority}\n" +
                               $"  High: {stats.HighPriority}\n" +
                               $"  Critical: {stats.CriticalPriority}\n\n" +
                               $"By Status:\n" +
                               $"  Resolved: {stats.ResolvedCount}\n" +
                               $"  Pending: {stats.PendingCount}";

                MessageBox.Show(message, "Complaint Statistics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error getting statistics: {ex.Message}", "Error");
        }
    }

    private void ClearForm()
    {
        txtTitle.Clear();
        txtDescription.Clear();
        cmbPriority.SelectedIndex = -1;
        cmbStatus.SelectedIndex = -1;
        cmbResident.SelectedIndex = -1;
        cmbAssignedTo.SelectedIndex = 0;
        txtResolutionNote.Clear();
        dtpReportDate.Value = DateTime.Now;
        dtpCompletionDate.Value = DateTime.Now;
        _selectedComplaintID = 0;
    }
}

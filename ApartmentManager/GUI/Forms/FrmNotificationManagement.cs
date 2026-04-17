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
    public partial class FrmNotificationManagement : Form
    {
        private SessionManager _session = SessionManager.Instance;
        private const int PADDING = 10;
        private const int CONTROL_HEIGHT = 25;

        // UI Controls
        private Panel _filterPanel, _detailsPanel, _gridPanel, _buttonPanel, _statusPanel;
        private DataGridView _dgvNotifications;
        private ComboBox _cboApartment, _cboResident, _cboNotificationType, _cboStatus;
        private TextBox _txtSubject, _txtBody, _txtSentDate;
        private Label _lblDraftCount, _lblSentCount, _lblFailedCount;

        public FrmNotificationManagement()
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
            if (!_session.HasPermission("ManageNotifications"))
            {
                MessageBox.Show("You do not have permission to access notification management.", "Access Denied");
                this.Close();
                return;
            }

            this.Text = "Notification Management";
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

            // Row 1: Filter controls
            Label lblApartment = new Label { Text = "Apartment:", Left = PADDING, Top = y, Width = 100 };
            _cboApartment = new ComboBox { Left = 120, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboApartment.SelectedIndexChanged += (s, e) => LoadResidents();
            _filterPanel.Controls.Add(lblApartment);
            _filterPanel.Controls.Add(_cboApartment);

            Label lblResident = new Label { Text = "Recipient:", Left = 290, Top = y, Width = 100 };
            _cboResident = new ComboBox { Left = 410, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboResident.SelectedIndexChanged += (s, e) => LoadNotifications();
            _filterPanel.Controls.Add(lblResident);
            _filterPanel.Controls.Add(_cboResident);

            Label lblNotificationType = new Label { Text = "Type:", Left = 580, Top = y, Width = 100 };
            _cboNotificationType = new ComboBox { Left = 700, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboNotificationType.Items.AddRange(new object[] { "All", "Announcement", "Maintenance", "Payment", "Warning", "Other" });
            _cboNotificationType.SelectedIndex = 0;
            _cboNotificationType.SelectedIndexChanged += (s, e) => LoadNotifications();
            _filterPanel.Controls.Add(lblNotificationType);
            _filterPanel.Controls.Add(_cboNotificationType);

            Label lblStatus = new Label { Text = "Status:", Left = 870, Top = y, Width = 100 };
            _cboStatus = new ComboBox { Left = 970, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboStatus.Items.AddRange(new object[] { "All", "Draft", "Sent", "Failed" });
            _cboStatus.SelectedIndex = 0;
            _cboStatus.SelectedIndexChanged += (s, e) => LoadNotifications();
            _filterPanel.Controls.Add(lblStatus);
            _filterPanel.Controls.Add(_cboStatus);

            y += 40;

            // Row 2: Subject and date info
            Label lblSubject = new Label { Text = "Subject:", Left = PADDING, Top = y, Width = 100 };
            _txtSubject = new TextBox { Left = 120, Top = y, Width = 300 };
            _filterPanel.Controls.Add(lblSubject);
            _filterPanel.Controls.Add(_txtSubject);

            Label lblSentDate = new Label { Text = "Sent Date:", Left = 440, Top = y, Width = 100 };
            _txtSentDate = new TextBox { Left = 560, Top = y, Width = 150, ReadOnly = true };
            _filterPanel.Controls.Add(lblSentDate);
            _filterPanel.Controls.Add(_txtSentDate);

            Button btnSearch = new Button { Text = "Search", Left = 730, Top = y, Width = 80, Height = 25 };
            btnSearch.Click += (s, e) => LoadNotifications();
            _filterPanel.Controls.Add(btnSearch);

            y += 35;

            Label lblNote = new Label { Text = "Select notification to view details", Left = PADDING, Top = y, Width = 400 };
            _filterPanel.Controls.Add(lblNote);
        }

        private void CreateDetailsPanel()
        {
            _detailsPanel = new Panel { Dock = DockStyle.Top, Height = 120, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_detailsPanel);

            int y = PADDING;

            Label lblBody = new Label { Text = "Message Body:", Left = PADDING, Top = y, Width = 100 };
            _txtBody = new TextBox { Left = 120, Top = y, Width = 1050, Height = 100, Multiline = true, ReadOnly = true };
            _detailsPanel.Controls.Add(lblBody);
            _detailsPanel.Controls.Add(_txtBody);
        }

        private void CreateGridPanel()
        {
            _gridPanel = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(_gridPanel);

            _dgvNotifications = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                BackgroundColor = System.Drawing.Color.White
            };

            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "NotificationID", HeaderText = "ID", DataPropertyName = "NotificationID", Width = 50 });
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "NotificationType", HeaderText = "Type", DataPropertyName = "NotificationType", Width = 100 });
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "Recipient", HeaderText = "Recipient", DataPropertyName = "RecipientName", Width = 120 });
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subject", HeaderText = "Subject", DataPropertyName = "Subject", Width = 250 });
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedDate", HeaderText = "Created", DataPropertyName = "CreatedDate", Width = 120 });
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "SentDate", HeaderText = "Sent", DataPropertyName = "SentDate", Width = 120 });
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 80 });

            _dgvNotifications.CellClick += DgvNotifications_CellClick;
            _gridPanel.Controls.Add(_dgvNotifications);
        }

        private void CreateButtonPanel()
        {
            _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_buttonPanel);

            Button btnCreate = new Button { Text = "Create", Left = PADDING, Top = 10, Width = 80, Height = 30 };
            btnCreate.Click += BtnCreate_Click;
            _buttonPanel.Controls.Add(btnCreate);

            Button btnSend = new Button { Text = "Send", Left = 100, Top = 10, Width = 80, Height = 30 };
            btnSend.Click += BtnSend_Click;
            _buttonPanel.Controls.Add(btnSend);

            Button btnResend = new Button { Text = "Resend", Left = 180, Top = 10, Width = 80, Height = 30 };
            btnResend.Click += BtnResend_Click;
            _buttonPanel.Controls.Add(btnResend);

            Button btnDelete = new Button { Text = "Delete", Left = 260, Top = 10, Width = 80, Height = 30 };
            btnDelete.Click += BtnDelete_Click;
            _buttonPanel.Controls.Add(btnDelete);

            Button btnStatistics = new Button { Text = "Statistics", Left = 340, Top = 10, Width = 80, Height = 30 };
            btnStatistics.Click += BtnStatistics_Click;
            _buttonPanel.Controls.Add(btnStatistics);

            Button btnRefresh = new Button { Text = "Refresh", Left = 420, Top = 10, Width = 80, Height = 30 };
            btnRefresh.Click += (s, e) => LoadNotifications();
            _buttonPanel.Controls.Add(btnRefresh);
        }

        private void CreateStatusPanel()
        {
            _statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.Gainsboro };
            this.Controls.Add(_statusPanel);

            _lblDraftCount = new Label { Text = "Draft: 0", Left = PADDING, Top = 10, Width = 100 };
            _statusPanel.Controls.Add(_lblDraftCount);

            _lblSentCount = new Label { Text = "Sent: 0", Left = 120, Top = 10, Width = 100 };
            _statusPanel.Controls.Add(_lblSentCount);

            _lblFailedCount = new Label { Text = "Failed: 0", Left = 240, Top = 10, Width = 100 };
            _statusPanel.Controls.Add(_lblFailedCount);
        }

        private void LoadData()
        {
            try
            {
                LoadApartments();
                LoadNotifications();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading notification data");
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

        private void LoadNotifications()
        {
            try
            {
                var notifications = NotificationDAL.GetAllNotifications();

                // Apply apartment filter
                if (_cboApartment.SelectedItem != null && ((dynamic)_cboApartment.SelectedItem).ApartmentID != 0)
                {
                    int apartmentID = ((dynamic)_cboApartment.SelectedItem).ApartmentID;
                    var residents = ResidentDAL.GetResidentsByApartment(apartmentID);
                    var residentIDs = residents.Select(r => r.ResidentID).ToList();
                    notifications = notifications.Where(n => residentIDs.Contains(n.ResidentID)).ToList();
                }

                // Apply resident filter
                if (_cboResident.SelectedItem != null && ((dynamic)_cboResident.SelectedItem).ResidentID != 0)
                {
                    int residentID = ((dynamic)_cboResident.SelectedItem).ResidentID;
                    notifications = notifications.Where(n => n.ResidentID == residentID).ToList();
                }

                // Apply notification type filter
                if (!_cboNotificationType.SelectedItem.ToString().Equals("All"))
                {
                    string notificationType = _cboNotificationType.SelectedItem.ToString();
                    notifications = notifications.Where(n => n.NotificationType == notificationType).ToList();
                }

                // Apply status filter
                if (!_cboStatus.SelectedItem.ToString().Equals("All"))
                {
                    string status = _cboStatus.SelectedItem.ToString();
                    notifications = notifications.Where(n => n.Status == status).ToList();
                }

                // Apply subject filter
                if (!string.IsNullOrWhiteSpace(_txtSubject.Text))
                    notifications = notifications.Where(n => n.Subject.Contains(_txtSubject.Text)).ToList();

                _dgvNotifications.DataSource = notifications.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading notifications");
                MessageBox.Show($"Error loading notifications: {ex.Message}", "Error");
            }
        }

        private void DgvNotifications_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                var row = _dgvNotifications.Rows[e.RowIndex];
                int notificationID = Convert.ToInt32(row.Cells["NotificationID"].Value);
                var notification = NotificationDAL.GetNotificationByID(notificationID);

                if (notification != null)
                {
                    _txtSubject.Text = notification.Subject ?? "";
                    _txtBody.Text = notification.Body ?? "";
                    _txtSentDate.Text = notification.SentDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not sent";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading notification details");
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                FrmNotificationDialog dialog = new FrmNotificationDialog(null);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var (residentID, subject, body, notificationType) = dialog.GetNotificationData();

                    var result = NotificationBLL.CreateNotification(residentID, subject, body, notificationType);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "CreateNotification", $"Notification {result.NotificationID} created");
                        LoadNotifications();
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
                Log.Error(ex, "Error creating notification");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvNotifications.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a notification to send.", "Information");
                    return;
                }

                int notificationID = Convert.ToInt32(_dgvNotifications.SelectedRows[0].Cells["NotificationID"].Value);
                var notification = NotificationDAL.GetNotificationByID(notificationID);

                if (notification == null)
                {
                    MessageBox.Show("Notification not found.", "Error");
                    return;
                }

                if (notification.Status != "Draft")
                {
                    MessageBox.Show("Only draft notifications can be sent.", "Information");
                    return;
                }

                var result = NotificationBLL.SendNotification(notificationID);

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Success");
                    AuditLogDAL.LogAction(_session.CurrentUser.UserID, "SendNotification", $"Notification {notificationID} sent");
                    LoadNotifications();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show(result.Message, "Error");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending notification");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnResend_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvNotifications.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a notification to resend.", "Information");
                    return;
                }

                int notificationID = Convert.ToInt32(_dgvNotifications.SelectedRows[0].Cells["NotificationID"].Value);

                var result = NotificationBLL.SendNotification(notificationID);

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Success");
                    AuditLogDAL.LogAction(_session.CurrentUser.UserID, "ResendNotification", $"Notification {notificationID} resent");
                    LoadNotifications();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show(result.Message, "Error");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error resending notification");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvNotifications.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a notification to delete.", "Information");
                    return;
                }

                int notificationID = Convert.ToInt32(_dgvNotifications.SelectedRows[0].Cells["NotificationID"].Value);
                var notification = NotificationDAL.GetNotificationByID(notificationID);

                if (notification == null || notification.Status == "Sent")
                {
                    MessageBox.Show("Cannot delete sent notifications.", "Information");
                    return;
                }

                if (MessageBox.Show("Are you sure you want to delete this notification?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var result = NotificationBLL.DeleteNotification(notificationID);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Success");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "DeleteNotification", $"Notification {notificationID} deleted");
                        LoadNotifications();
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
                Log.Error(ex, "Error deleting notification");
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void BtnStatistics_Click(object sender, EventArgs e)
        {
            try
            {
                var stats = NotificationBLL.GetNotificationStatistics();
                MessageBox.Show(
                    $"Total Notifications: {stats.TotalNotifications}\n" +
                    $"Draft: {stats.DraftCount}\n" +
                    $"Sent: {stats.SentCount}\n" +
                    $"Failed: {stats.FailedCount}\n" +
                    $"Announcement: {stats.AnnouncementCount}\n" +
                    $"Maintenance: {stats.MaintenanceCount}\n" +
                    $"Payment: {stats.PaymentCount}\n" +
                    $"Warning: {stats.WarningCount}",
                    "Notification Statistics");
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
                var stats = NotificationBLL.GetNotificationStatistics();
                _lblDraftCount.Text = $"Draft: {stats.DraftCount}";
                _lblSentCount.Text = $"Sent: {stats.SentCount}";
                _lblFailedCount.Text = $"Failed: {stats.FailedCount}";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating statistics");
            }
        }
    }

    public partial class FrmNotificationDialog : Form
    {
        private dynamic _notification;
        private ComboBox _cboResident, _cboNotificationType;
        private TextBox _txtSubject, _txtBody;

        public FrmNotificationDialog(dynamic notification)
        {
            _notification = notification;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            this.Text = _notification == null ? "Create Notification" : "Edit Notification";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 10;

            Label lblResident = new Label { Text = "Recipient:", Left = 10, Top = y, Width = 100 };
            _cboResident = new ComboBox { Left = 120, Top = y, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            LoadResidents();
            this.Controls.Add(lblResident);
            this.Controls.Add(_cboResident);

            y += 35;

            Label lblNotificationType = new Label { Text = "Type:", Left = 10, Top = y, Width = 100 };
            _cboNotificationType = new ComboBox { Left = 120, Top = y, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboNotificationType.Items.AddRange(new object[] { "Announcement", "Maintenance", "Payment", "Warning", "Other" });
            this.Controls.Add(lblNotificationType);
            this.Controls.Add(_cboNotificationType);

            y += 35;

            Label lblSubject = new Label { Text = "Subject:", Left = 10, Top = y, Width = 100 };
            _txtSubject = new TextBox { Left = 120, Top = y, Width = 350 };
            this.Controls.Add(lblSubject);
            this.Controls.Add(_txtSubject);

            y += 35;

            Label lblBody = new Label { Text = "Message:", Left = 10, Top = y, Width = 100 };
            _txtBody = new TextBox { Left = 120, Top = y, Width = 350, Height = 100, Multiline = true };
            this.Controls.Add(lblBody);
            this.Controls.Add(_txtBody);

            y += 110;

            Button btnOK = new Button { Text = "OK", Left = 260, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancel", Left = 370, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            if (_notification != null)
            {
                _cboResident.SelectedValue = _notification.ResidentID;
                _cboNotificationType.SelectedItem = _notification.NotificationType;
                _txtSubject.Text = _notification.Subject ?? "";
                _txtBody.Text = _notification.Body ?? "";
            }
            else if (_cboNotificationType.Items.Count > 0)
            {
                _cboNotificationType.SelectedIndex = 0;
            }
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

            if (_notification != null)
                _cboResident.SelectedValue = _notification.ResidentID;
            else if (_cboResident.Items.Count > 0)
                _cboResident.SelectedIndex = 0;
        }

        public (int ResidentID, string Subject, string Body, string NotificationType) GetNotificationData()
        {
            return (
                ((dynamic)_cboResident.SelectedItem).ResidentID,
                _txtSubject.Text,
                _txtBody.Text,
                _cboNotificationType.SelectedItem.ToString()
            );
        }
    }
}

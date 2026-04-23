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
    public partial class FrmNotificationManagement : Form
    {
        private UserSession? _session = null;
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
            _session = SessionManager.GetSession();

            if (_session == null || !_session.HasPermission("ManageNotifications"))
            {
                MessageBox.Show("Bạn không có quyền truy cập màn hình này.", "Từ chối truy cập");
                this.Close();
                return;
            }

            this.Text = "Quản lý thông báo";
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
            Label lblApartment = new Label { Text = "Căn hộ:", Left = PADDING, Top = y, Width = 100 };
            _cboApartment = new ComboBox { Left = 120, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboApartment.SelectedIndexChanged += (s, e) => LoadResidents();
            _filterPanel.Controls.Add(lblApartment);
            _filterPanel.Controls.Add(_cboApartment);

            Label lblResident = new Label { Text = "Người nhận:", Left = 290, Top = y, Width = 100 };
            _cboResident = new ComboBox { Left = 410, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboResident.SelectedIndexChanged += (s, e) => LoadNotifications();
            _filterPanel.Controls.Add(lblResident);
            _filterPanel.Controls.Add(_cboResident);

            Label lblNotificationType = new Label { Text = "Loại:", Left = 580, Top = y, Width = 100 };
            _cboNotificationType = new ComboBox { Left = 700, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboNotificationType.Items.AddRange(new object[]
            {
                new UiComboItem("Tất cả", "All"),
                new UiComboItem("Thông báo chung", "Announcement"),
                new UiComboItem("Bảo trì", "Maintenance"),
                new UiComboItem("Thanh toán", "Payment"),
                new UiComboItem("Cảnh báo", "Warning"),
                new UiComboItem("Khác", "Other")
            });
            _cboNotificationType.SelectedIndex = 0;
            _cboNotificationType.SelectedIndexChanged += (s, e) => LoadNotifications();
            _filterPanel.Controls.Add(lblNotificationType);
            _filterPanel.Controls.Add(_cboNotificationType);

            Label lblStatus = new Label { Text = "Trạng thái:", Left = 870, Top = y, Width = 100 };
            _cboStatus = new ComboBox { Left = 970, Top = y, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboStatus.Items.AddRange(new object[]
            {
                new UiComboItem("Tất cả", "All"),
                new UiComboItem("Nháp", "Draft"),
                new UiComboItem("Đã gửi", "Sent"),
                new UiComboItem("Thất bại", "Failed")
            });
            _cboStatus.SelectedIndex = 0;
            _cboStatus.SelectedIndexChanged += (s, e) => LoadNotifications();
            _filterPanel.Controls.Add(lblStatus);
            _filterPanel.Controls.Add(_cboStatus);

            y += 40;

            // Row 2: Subject and date info
            Label lblSubject = new Label { Text = "Tiêu đề:", Left = PADDING, Top = y, Width = 100 };
            _txtSubject = new TextBox { Left = 120, Top = y, Width = 300 };
            _filterPanel.Controls.Add(lblSubject);
            _filterPanel.Controls.Add(_txtSubject);

            Label lblSentDate = new Label { Text = "Ngày gửi:", Left = 440, Top = y, Width = 100 };
            _txtSentDate = new TextBox { Left = 560, Top = y, Width = 150, ReadOnly = true };
            _filterPanel.Controls.Add(lblSentDate);
            _filterPanel.Controls.Add(_txtSentDate);

            Button btnSearch = new Button { Text = "Tìm kiếm", Left = 730, Top = y, Width = 80, Height = 25 };
            btnSearch.Click += (s, e) => LoadNotifications();
            _filterPanel.Controls.Add(btnSearch);

            y += 35;

            Label lblNote = new Label { Text = "Chọn thông báo để xem chi tiết", Left = PADDING, Top = y, Width = 400 };
            _filterPanel.Controls.Add(lblNote);
        }

        private void CreateDetailsPanel()
        {
            _detailsPanel = new Panel { Dock = DockStyle.Top, Height = 120, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_detailsPanel);

            int y = PADDING;

            Label lblBody = new Label { Text = "Nội dung:", Left = PADDING, Top = y, Width = 100 };
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
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "NotificationType", HeaderText = "Loại", DataPropertyName = "NotificationType", Width = 100 });
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "Recipient", HeaderText = "Người nhận", DataPropertyName = "RecipientName", Width = 120 });
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subject", HeaderText = "Tiêu đề", DataPropertyName = "Subject", Width = 250 });
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedDate", HeaderText = "Ngày tạo", DataPropertyName = "CreatedDate", Width = 120 });
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "SentDate", HeaderText = "Ngày gửi", DataPropertyName = "SentDate", Width = 120 });
            _dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Trạng thái", DataPropertyName = "Status", Width = 80 });

            _dgvNotifications.CellClick += DgvNotifications_CellClick;
            _gridPanel.Controls.Add(_dgvNotifications);
        }

        private void CreateButtonPanel()
        {
            _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.LightGray };
            this.Controls.Add(_buttonPanel);

            Button btnCreate = new Button { Text = "Thêm", Left = PADDING, Top = 10, Width = 80, Height = 30 };
            btnCreate.Click += BtnCreate_Click;
            _buttonPanel.Controls.Add(btnCreate);

            Button btnSend = new Button { Text = "Gửi", Left = 100, Top = 10, Width = 80, Height = 30 };
            btnSend.Click += BtnSend_Click;
            _buttonPanel.Controls.Add(btnSend);

            Button btnResend = new Button { Text = "Gửi lại", Left = 180, Top = 10, Width = 80, Height = 30 };
            btnResend.Click += BtnResend_Click;
            _buttonPanel.Controls.Add(btnResend);

            Button btnDelete = new Button { Text = "Xóa", Left = 260, Top = 10, Width = 80, Height = 30 };
            btnDelete.Click += BtnDelete_Click;
            _buttonPanel.Controls.Add(btnDelete);

            Button btnStatistics = new Button { Text = "Thống kê", Left = 340, Top = 10, Width = 80, Height = 30 };
            btnStatistics.Click += BtnStatistics_Click;
            _buttonPanel.Controls.Add(btnStatistics);

            Button btnRefresh = new Button { Text = "Làm mới", Left = 420, Top = 10, Width = 80, Height = 30 };
            btnRefresh.Click += (s, e) => LoadNotifications();
            _buttonPanel.Controls.Add(btnRefresh);
        }

        private void CreateStatusPanel()
        {
            _statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BorderStyle = BorderStyle.FixedSingle, BackColor = System.Drawing.Color.Gainsboro };
            this.Controls.Add(_statusPanel);

            _lblDraftCount = new Label { Text = "Nháp: 0", Left = PADDING, Top = 10, Width = 100 };
            _statusPanel.Controls.Add(_lblDraftCount);

            _lblSentCount = new Label { Text = "Đã gửi: 0", Left = 120, Top = 10, Width = 100 };
            _statusPanel.Controls.Add(_lblSentCount);

            _lblFailedCount = new Label { Text = "Thất bại: 0", Left = 240, Top = 10, Width = 100 };
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
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi");
            }
        }

        private void LoadApartments()
        {
            try
            {
                _cboApartment.Items.Clear();
                _cboApartment.AddOption("Tất cả căn hộ", 0);

                var apartments = ApartmentDAL.GetAllApartments();
                foreach (var apt in apartments)
                {
                    _cboApartment.AddOption(apt.ApartmentCode ?? $"Căn hộ {apt.ApartmentID}", apt.ApartmentID);
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
                if (apartmentID != 0)
                {
                    var residents = ResidentDAL.GetResidentsByApartment(apartmentID);
                    foreach (var resident in residents)
                    {
                        _cboResident.AddOption(resident.FullName ?? $"Cư dân {resident.ResidentID}", resident.ResidentID);
                    }
                }
                else
                {
                    var residents = ResidentDAL.GetAllResidents();
                    foreach (var resident in residents)
                    {
                        _cboResident.AddOption(resident.FullName ?? $"Cư dân {resident.ResidentID}", resident.ResidentID);
                    }
                }

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
                int apartmentID = _cboApartment.GetSelectedValueInt();
                if (apartmentID != 0)
                {
                    var residents = ResidentDAL.GetResidentsByApartment(apartmentID);
                    var residentIDs = residents.Select(r => r.ResidentID).ToList();
                    notifications = notifications.Where(n => residentIDs.Contains(n.ResidentID)).ToList();
                }

                // Apply resident filter
                int residentID = _cboResident.GetSelectedValueInt();
                if (residentID != 0)
                {
                    notifications = notifications.Where(n => n.ResidentID == residentID).ToList();
                }

                // Apply notification type filter
                string notificationType = _cboNotificationType.GetSelectedValueString();
                if (!string.Equals(notificationType, "All", StringComparison.OrdinalIgnoreCase))
                {
                    notifications = notifications.Where(n => n.NotificationType == notificationType).ToList();
                }

                // Apply status filter
                string status = _cboStatus.GetSelectedValueString();
                if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
                {
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
                MessageBox.Show($"Lỗi khi tải thông báo: {ex.Message}", "Lỗi");
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
                    _txtSentDate.Text = notification.SentDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Chưa gửi";
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
                        MessageBox.Show(result.Message, "Thành công");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "CreateNotification", $"Notification {result.NotificationID} created");
                        LoadNotifications();
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
                Log.Error(ex, "Error creating notification");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvNotifications.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn thông báo cần gửi.", "Thông báo");
                    return;
                }

                int notificationID = Convert.ToInt32(_dgvNotifications.SelectedRows[0].Cells["NotificationID"].Value);
                var notification = NotificationDAL.GetNotificationByID(notificationID);

                if (notification == null)
                {
                    MessageBox.Show("Không tìm thấy thông báo.", "Lỗi");
                    return;
                }

                if (notification.Status != "Draft")
                {
                    MessageBox.Show("Chỉ có thể gửi thông báo ở trạng thái nháp.", "Thông báo");
                    return;
                }

                var result = NotificationBLL.SendNotification(notificationID);

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Thành công");
                    AuditLogDAL.LogAction(_session.CurrentUser.UserID, "SendNotification", $"Notification {notificationID} sent");
                    LoadNotifications();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show(result.Message, "Lỗi");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending notification");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnResend_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvNotifications.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn thông báo cần gửi lại.", "Thông báo");
                    return;
                }

                int notificationID = Convert.ToInt32(_dgvNotifications.SelectedRows[0].Cells["NotificationID"].Value);

                var result = NotificationBLL.SendNotification(notificationID);

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Thành công");
                    AuditLogDAL.LogAction(_session.CurrentUser.UserID, "ResendNotification", $"Notification {notificationID} resent");
                    LoadNotifications();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show(result.Message, "Lỗi");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error resending notification");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgvNotifications.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn thông báo cần xóa.", "Thông báo");
                    return;
                }

                int notificationID = Convert.ToInt32(_dgvNotifications.SelectedRows[0].Cells["NotificationID"].Value);
                var notification = NotificationDAL.GetNotificationByID(notificationID);

                if (notification == null || notification.Status == "Sent")
                {
                    MessageBox.Show("Không thể xóa thông báo đã gửi.", "Thông báo");
                    return;
                }

                if (MessageBox.Show("Bạn có chắc muốn xóa thông báo này không?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var result = NotificationBLL.DeleteNotification(notificationID);

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Thành công");
                        AuditLogDAL.LogAction(_session.CurrentUser.UserID, "DeleteNotification", $"Notification {notificationID} deleted");
                        LoadNotifications();
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
                Log.Error(ex, "Error deleting notification");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        private void BtnStatistics_Click(object sender, EventArgs e)
        {
            try
            {
                var stats = NotificationBLL.GetNotificationStatistics();
                MessageBox.Show(
                    $"Tổng thông báo: {stats.TotalNotifications}\n" +
                    $"Nháp: {stats.DraftCount}\n" +
                    $"Đã gửi: {stats.SentCount}\n" +
                    $"Thất bại: {stats.FailedCount}\n" +
                    $"Thông báo chung: {stats.AnnouncementCount}\n" +
                    $"Bảo trì: {stats.MaintenanceCount}\n" +
                    $"Thanh toán: {stats.PaymentCount}\n" +
                    $"Cảnh báo: {stats.WarningCount}",
                    "Thống kê thông báo");
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
                var stats = NotificationBLL.GetNotificationStatistics();
                _lblDraftCount.Text = $"Nháp: {stats.DraftCount}";
                _lblSentCount.Text = $"Đã gửi: {stats.SentCount}";
                _lblFailedCount.Text = $"Thất bại: {stats.FailedCount}";
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
            this.Text = _notification == null ? "Tạo thông báo" : "Sửa thông báo";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 10;

            Label lblResident = new Label { Text = "Người nhận:", Left = 10, Top = y, Width = 100 };
            _cboResident = new ComboBox { Left = 120, Top = y, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            LoadResidents();
            this.Controls.Add(lblResident);
            this.Controls.Add(_cboResident);

            y += 35;

            Label lblNotificationType = new Label { Text = "Loại:", Left = 10, Top = y, Width = 100 };
            _cboNotificationType = new ComboBox { Left = 120, Top = y, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboNotificationType.Items.AddRange(new object[]
            {
                new UiComboItem("Thông báo chung", "Announcement"),
                new UiComboItem("Bảo trì", "Maintenance"),
                new UiComboItem("Thanh toán", "Payment"),
                new UiComboItem("Cảnh báo", "Warning"),
                new UiComboItem("Khác", "Other")
            });
            this.Controls.Add(lblNotificationType);
            this.Controls.Add(_cboNotificationType);

            y += 35;

            Label lblSubject = new Label { Text = "Tiêu đề:", Left = 10, Top = y, Width = 100 };
            _txtSubject = new TextBox { Left = 120, Top = y, Width = 350 };
            this.Controls.Add(lblSubject);
            this.Controls.Add(_txtSubject);

            y += 35;

            Label lblBody = new Label { Text = "Nội dung:", Left = 10, Top = y, Width = 100 };
            _txtBody = new TextBox { Left = 120, Top = y, Width = 350, Height = 100, Multiline = true };
            this.Controls.Add(lblBody);
            this.Controls.Add(_txtBody);

            y += 110;

            Button btnOK = new Button { Text = "Đồng ý", Left = 260, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Hủy", Left = 370, Top = y, Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            if (_notification != null)
            {
                ComboBoxHelper.SelectValue(_cboResident, _notification.ResidentID);
                ComboBoxHelper.SelectValue(_cboNotificationType, _notification.NotificationType);
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
                _cboResident.AddOption(resident.FullName ?? $"Cư dân {resident.ResidentID}", resident.ResidentID);
            }

            if (_notification != null)
                ComboBoxHelper.SelectValue(_cboResident, _notification.ResidentID);
            else if (_cboResident.Items.Count > 0)
                _cboResident.SelectedIndex = 0;
        }

        public (int ResidentID, string Subject, string Body, string NotificationType) GetNotificationData()
        {
            return (
                _cboResident.GetSelectedValueInt(),
                _txtSubject.Text,
                _txtBody.Text,
                _cboNotificationType.GetSelectedValueString()
            );
        }
    }
}


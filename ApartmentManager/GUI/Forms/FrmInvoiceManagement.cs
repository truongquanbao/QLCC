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
/// Form for managing invoices and payment tracking
/// </summary>
public class FrmInvoiceManagement : Form
{
    private const int PRIMARY_COLOR = 0x215689;
    private const int SPACING = 10;
    private const int LABEL_WIDTH = 120;
    private const int CONTROL_WIDTH = 200;

    // Filter controls
    private ComboBox cmbFilterStatus = new ComboBox();
    private ComboBox cmbFilterApartment = new ComboBox();
    private TextBox txtSearchMonth = new TextBox();
    private Button btnSearch = new Button();
    private Button btnShowAll = new Button();

    // Invoice details controls
    private ComboBox cmbApartment = new ComboBox();
    private NumericUpDown nudMonth = new NumericUpDown();
    private NumericUpDown nudYear = new NumericUpDown();
    private DateTimePicker dtpDueDate = new DateTimePicker();
    private TextBox txtTotalAmount = new TextBox();
    private TextBox txtPaidAmount = new TextBox();
    private TextBox txtNote = new TextBox();

    // Labels
    private Label lblApartment = new Label();
    private Label lblMonth = new Label();
    private Label lblYear = new Label();
    private Label lblDueDate = new Label();
    private Label lblTotalAmount = new Label();
    private Label lblPaidAmount = new Label();
    private Label lblNote = new Label();

    // Grid and buttons
    private DataGridView dgvInvoices = new DataGridView();
    private Button btnCreate = new Button();
    private Button btnCreateMonthly = new Button();
    private Button btnEdit = new Button();
    private Button btnDelete = new Button();
    private Button btnRecordPayment = new Button();
    private Button btnDebtSummary = new Button();
    private Button btnClose = new Button();

    // Status
    private Label lblStatusBar = new Label();
    private Label lblDebtInfo = new Label();

    private int _selectedInvoiceID = 0;

    public FrmInvoiceManagement()
    {
        if (SessionManager.GetSession() == null || !SessionManager.HasPermission("ManageInvoices"))
        {
            MessageBox.Show("Bạn không có quyền truy cập màn hình này.", "Từ chối truy cập",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.Close();
            return;
        }

        InitializeComponent();
        LoadData();
        AuditLogDAL.LogAction(SessionManager.GetSession().UserID, "Opened invoice management form", "FrmInvoiceManagement");
    }

    private void InitializeComponent()
    {
        this.Text = "Quản lý hóa đơn";
        this.Size = new Size(1300, 900);
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

        var lblStatus = new Label { Text = "Trạng thái:", Location = new Point(SPACING, SPACING), Size = new Size(70, 20) };
        pnl.Controls.Add(lblStatus);

        cmbFilterStatus.Location = new Point(SPACING + 70, SPACING);
        cmbFilterStatus.Size = new Size(100, 25);
        cmbFilterStatus.Items.AddRange(new object[]
        {
            new UiComboItem("Tất cả", "All"),
            new UiComboItem("Đã thanh toán", "Paid"),
            new UiComboItem("Chưa thanh toán", "Unpaid"),
            new UiComboItem("Thanh toán một phần", "Partial"),
            new UiComboItem("Quá hạn", "Overdue")
        });
        cmbFilterStatus.SelectedIndex = 0;
        pnl.Controls.Add(cmbFilterStatus);

        var lblApt = new Label { Text = "Căn hộ:", Location = new Point(SPACING + 180, SPACING), Size = new Size(70, 20) };
        pnl.Controls.Add(lblApt);

        cmbFilterApartment.Location = new Point(SPACING + 260, SPACING);
        cmbFilterApartment.Size = new Size(120, 25);
        cmbFilterApartment.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbFilterApartment);

        btnSearch.Text = "Tìm kiếm";
        btnSearch.Location = new Point(SPACING + 390, SPACING);
        btnSearch.Size = new Size(80, 30);
        btnSearch.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnSearch.ForeColor = Color.White;
        btnSearch.Click += BtnSearch_Click;
        pnl.Controls.Add(btnSearch);

        btnShowAll.Text = "Hiển thị tất cả";
        btnShowAll.Location = new Point(SPACING + 480, SPACING);
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
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 130),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        int y = SPACING;
        int x = SPACING;

        // Row 1
        lblApartment.Text = "Căn hộ:";
        lblApartment.Location = new Point(x, y);
        lblApartment.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblApartment);

        cmbApartment.Location = new Point(x + LABEL_WIDTH + SPACING, y);
        cmbApartment.Size = new Size(CONTROL_WIDTH, 25);
        cmbApartment.DropDownStyle = ComboBoxStyle.DropDownList;
        pnl.Controls.Add(cmbApartment);

        lblMonth.Text = "Tháng:";
        lblMonth.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y);
        lblMonth.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblMonth);

        nudMonth.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y);
        nudMonth.Size = new Size(60, 25);
        nudMonth.Minimum = 1;
        nudMonth.Maximum = 12;
        nudMonth.Value = DateTime.Now.Month;
        pnl.Controls.Add(nudMonth);

        lblYear.Text = "Năm:";
        lblYear.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + 80 + SPACING * 5, y);
        lblYear.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblYear);

        nudYear.Location = new Point(x + LABEL_WIDTH * 3 + CONTROL_WIDTH + 80 + SPACING * 6, y);
        nudYear.Size = new Size(80, 25);
        nudYear.Minimum = 2020;
        nudYear.Maximum = 2100;
        nudYear.Value = DateTime.Now.Year;
        pnl.Controls.Add(nudYear);

        // Row 2
        lblDueDate.Text = "Hạn thanh toán:";
        lblDueDate.Location = new Point(x, y + 35);
        lblDueDate.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblDueDate);

        dtpDueDate.Location = new Point(x + LABEL_WIDTH + SPACING, y + 35);
        dtpDueDate.Size = new Size(CONTROL_WIDTH, 25);
        dtpDueDate.Format = DateTimePickerFormat.Short;
        pnl.Controls.Add(dtpDueDate);

        lblTotalAmount.Text = "Tổng tiền:";
        lblTotalAmount.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 35);
        lblTotalAmount.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblTotalAmount);

        txtTotalAmount.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 35);
        txtTotalAmount.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtTotalAmount);

        // Row 3
        lblPaidAmount.Text = "Đã thanh toán:";
        lblPaidAmount.Location = new Point(x, y + 70);
        lblPaidAmount.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblPaidAmount);

        txtPaidAmount.Location = new Point(x + LABEL_WIDTH + SPACING, y + 70);
        txtPaidAmount.Size = new Size(CONTROL_WIDTH, 25);
        txtPaidAmount.ReadOnly = true;
        pnl.Controls.Add(txtPaidAmount);

        lblNote.Text = "Ghi chú:";
        lblNote.Location = new Point(x + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 3, y + 70);
        lblNote.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblNote);

        txtNote.Location = new Point(x + LABEL_WIDTH * 2 + CONTROL_WIDTH + SPACING * 4, y + 70);
        txtNote.Size = new Size(300, 25);
        pnl.Controls.Add(txtNote);

        return pnl;
    }

    private Panel CreateGridPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 210),
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 360),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        dgvInvoices.Location = new Point(0, 0);
        dgvInvoices.Size = new Size(pnl.ClientSize.Width, pnl.ClientSize.Height);
        dgvInvoices.Dock = DockStyle.Fill;
        dgvInvoices.AllowUserToAddRows = false;
        dgvInvoices.AllowUserToDeleteRows = false;
        dgvInvoices.ReadOnly = true;
        dgvInvoices.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvInvoices.MultiSelect = false;
        dgvInvoices.RowHeadersVisible = false;
        dgvInvoices.CellClick += DgvInvoices_CellClick;

        dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "InvoiceID", Width = 50, Visible = false });
        dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Căn hộ", DataPropertyName = "ApartmentCode", Width = 100 });
        dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tháng", DataPropertyName = "Month", Width = 60 });
        dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Năm", DataPropertyName = "Year", Width = 60 });
        dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hạn thanh toán", DataPropertyName = "DueDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tổng tiền", DataPropertyName = "TotalAmount", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } });
        dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đã thanh toán", DataPropertyName = "PaidAmount", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } });
        dgvInvoices.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", DataPropertyName = "PaymentStatus", Width = 100 });

        pnl.Controls.Add(dgvInvoices);
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

        btnCreate.Text = "Thêm";
        btnCreate.Location = new Point(x, y);
        btnCreate.Size = new Size(100, 30);
        btnCreate.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnCreate.ForeColor = Color.White;
        btnCreate.Click += BtnCreate_Click;
        pnl.Controls.Add(btnCreate);

        btnCreateMonthly.Text = "Tạo hàng loạt theo tháng";
        btnCreateMonthly.Location = new Point(x + 110, y);
        btnCreateMonthly.Size = new Size(130, 30);
        btnCreateMonthly.BackColor = Color.FromArgb(0, 100, 100);
        btnCreateMonthly.ForeColor = Color.White;
        btnCreateMonthly.Click += BtnCreateMonthly_Click;
        pnl.Controls.Add(btnCreateMonthly);

        btnEdit.Text = "Sửa";
        btnEdit.Location = new Point(x + 250, y);
        btnEdit.Size = new Size(100, 30);
        btnEdit.BackColor = Color.FromArgb(PRIMARY_COLOR);
        btnEdit.ForeColor = Color.White;
        btnEdit.Click += BtnEdit_Click;
        pnl.Controls.Add(btnEdit);

        btnRecordPayment.Text = "Ghi nhận thanh toán";
        btnRecordPayment.Location = new Point(x + 360, y);
        btnRecordPayment.Size = new Size(120, 30);
        btnRecordPayment.BackColor = Color.FromArgb(0, 100, 0);
        btnRecordPayment.ForeColor = Color.White;
        btnRecordPayment.Click += BtnRecordPayment_Click;
        pnl.Controls.Add(btnRecordPayment);

        btnDebtSummary.Text = "Tổng hợp công nợ";
        btnDebtSummary.Location = new Point(x + 490, y);
        btnDebtSummary.Size = new Size(110, 30);
        btnDebtSummary.BackColor = Color.FromArgb(255, 140, 0);
        btnDebtSummary.ForeColor = Color.White;
        btnDebtSummary.Click += BtnDebtSummary_Click;
        pnl.Controls.Add(btnDebtSummary);

        btnDelete.Text = "Xóa";
        btnDelete.Location = new Point(x + 610, y);
        btnDelete.Size = new Size(100, 30);
        btnDelete.BackColor = Color.FromArgb(0xC00000);
        btnDelete.ForeColor = Color.White;
        btnDelete.Click += BtnDelete_Click;
        pnl.Controls.Add(btnDelete);

        btnClose.Text = "Đóng";
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
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 70),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        lblStatusBar.Text = "Sẵn sàng";
        lblStatusBar.Location = new Point(SPACING, SPACING);
        lblStatusBar.Size = new Size(400, 20);
        lblStatusBar.ForeColor = Color.Green;
        pnl.Controls.Add(lblStatusBar);

        lblDebtInfo.Text = "Tổng công nợ: 0 VND | Quá hạn: 0";
        lblDebtInfo.Location = new Point(SPACING, SPACING + 25);
        lblDebtInfo.Size = new Size(600, 20);
        pnl.Controls.Add(lblDebtInfo);

        return pnl;
    }

    private void LoadData()
    {
        try
        {
            var invoices = InvoiceDAL.GetAllInvoices();
            dgvInvoices.DataSource = invoices.Cast<dynamic>().ToList();
            lblStatusBar.Text = $"Đã tải {invoices.Count} hóa đơn";

            LoadApartments();
            CalculateDebtInfo();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải hóa đơn: {ex.Message}", "Lỗi");
        }
    }

    private void LoadApartments()
    {
        try
        {
            var apartments = ApartmentDAL.GetAllApartments();
            cmbApartment.Items.Clear();
            cmbFilterApartment.Items.Clear();
            cmbFilterApartment.Items.Add(new UiComboItem("Tất cả", "All"));

            foreach (var apt in apartments)
            {
                cmbApartment.Items.Add(new UiComboItem(apt.ApartmentCode, apt.ApartmentID));
                cmbFilterApartment.Items.Add(new UiComboItem(apt.ApartmentCode, apt.ApartmentID));
            }

            cmbApartment.DisplayMember = "Text";
            cmbApartment.ValueMember = "Value";
            cmbFilterApartment.DisplayMember = "Text";
            cmbFilterApartment.ValueMember = "Value";
            cmbFilterApartment.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải căn hộ: {ex.Message}", "Lỗi");
        }
    }

    private void CalculateDebtInfo()
    {
        try
        {
            var unpaidInvoices = InvoiceDAL.GetUnpaidInvoices();
            decimal totalDebt = unpaidInvoices.Sum(i => i.TotalAmount - i.PaidAmount);
            int overdueCount = unpaidInvoices.Count(i => i.DueDate < DateTime.Now);

            lblDebtInfo.Text = $"Tổng công nợ: {totalDebt:N0} VND | Hóa đơn quá hạn: {overdueCount}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tính công nợ: {ex.Message}", "Lỗi");
        }
    }

    private void DgvInvoices_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            _selectedInvoiceID = (int)dgvInvoices[0, e.RowIndex].Value;
            var invoice = InvoiceDAL.GetInvoiceByID(_selectedInvoiceID);

            if (invoice != null)
            {
                nudMonth.Value = invoice.Month;
                nudYear.Value = invoice.Year;
                dtpDueDate.Value = invoice.DueDate ?? DateTime.Now;
                txtTotalAmount.Text = invoice.TotalAmount.ToString("N0");
                txtPaidAmount.Text = invoice.PaidAmount.ToString("N0");
                txtNote.Text = invoice.Note ?? "";

                cmbApartment.SelectValue(invoice.ApartmentID);

                lblStatusBar.Text = $"Đã chọn: Hóa đơn {invoice.InvoiceID} - {invoice.ApartmentCode}";
            }
        }
    }

    private void BtnCreate_Click(object sender, EventArgs e)
    {
        if (cmbApartment.SelectedItem == null)
        {
            MessageBox.Show("Vui lòng chọn căn hộ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (decimal.TryParse(txtTotalAmount.Text, out decimal totalAmount) && totalAmount <= 0)
        {
            MessageBox.Show("Tổng tiền phải lớn hơn 0.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            int apartmentID = (int)((dynamic)cmbApartment.SelectedItem).Value;

            var result = InvoiceBLL.CreateInvoice(
                apartmentID,
                (int)nudMonth.Value,
                (int)nudYear.Value,
                dtpDueDate.Value,
                totalAmount,
                txtNote.Text
            );

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID,
                                     $"Created invoice for apartment {ComboBoxHelper.GetSelectedText(cmbApartment)}", "FrmInvoiceManagement");
                ClearForm();
                LoadData();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tạo hóa đơn: {ex.Message}", "Lỗi");
        }
    }

    private void BtnCreateMonthly_Click(object sender, EventArgs e)
    {
        if (MessageBox.Show("Tạo hóa đơn hàng tháng cho tất cả căn hộ?\nHệ thống sẽ tạo hóa đơn cho tháng đã chọn nếu chưa tồn tại.",
            "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            int month = (int)nudMonth.Value;
            int year = (int)nudYear.Value;
            decimal defaultAmount = 500000; // Default monthly fee

            var apartments = ApartmentDAL.GetAllApartments();
            int created = 0;

            foreach (var apt in apartments)
            {
                if (!InvoiceDAL.InvoiceExists(apt.ApartmentID, month, year))
                {
                    InvoiceDAL.CreateInvoice(apt.ApartmentID, month, year, dtpDueDate.Value, defaultAmount, "Monthly invoice");
                    created++;
                }
            }

            MessageBox.Show($"Đã tạo thành công {created} hóa đơn.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                 $"Created {created} monthly invoices for {month}/{year}", "FrmInvoiceManagement");
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tạo hóa đơn hàng tháng: {ex.Message}", "Lỗi");
        }
    }

    private void BtnEdit_Click(object sender, EventArgs e)
    {
        if (_selectedInvoiceID == 0)
        {
            MessageBox.Show("Vui lòng chọn hóa đơn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!decimal.TryParse(txtTotalAmount.Text, out decimal totalAmount) || totalAmount <= 0)
        {
            MessageBox.Show("Tổng tiền phải là số dương.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var result = InvoiceBLL.UpdateInvoice(_selectedInvoiceID, dtpDueDate.Value, totalAmount, txtNote.Text);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Updated invoice {_selectedInvoiceID}", "FrmInvoiceManagement");
                LoadData();
                ClearForm();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi cập nhật hóa đơn: {ex.Message}", "Lỗi");
        }
    }

    private void BtnRecordPayment_Click(object sender, EventArgs e)
    {
        if (_selectedInvoiceID == 0)
        {
            MessageBox.Show("Vui lòng chọn hóa đơn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var frm = new FrmRecordPayment(_selectedInvoiceID);
        frm.ShowDialog();
        LoadData();
    }

    private void BtnDelete_Click(object sender, EventArgs e)
    {
        if (_selectedInvoiceID == 0)
        {
            MessageBox.Show("Vui lòng chọn hóa đơn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (MessageBox.Show("Bạn có chắc muốn xóa hóa đơn này không?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            var result = InvoiceBLL.DeleteInvoice(_selectedInvoiceID);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Deleted invoice {_selectedInvoiceID}", "FrmInvoiceManagement");
                LoadData();
                ClearForm();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xóa hóa đơn: {ex.Message}", "Lỗi");
        }
    }

    private void BtnDebtSummary_Click(object sender, EventArgs e)
    {
        if (cmbApartment.SelectedItem == null)
        {
            MessageBox.Show("Vui lòng chọn căn hộ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            int apartmentID = (int)((dynamic)cmbApartment.SelectedItem).Value;
            var summary = InvoiceDAL.GetApartmentDebtSummary(apartmentID);

            if (summary != null)
            {
                string message = $"Căn hộ: {ComboBoxHelper.GetSelectedText(cmbApartment)}\n\n" +
                               $"Tổng hóa đơn: {summary.TotalInvoices}\n" +
                               $"Tổng tiền: {summary.TotalAmount:N0} VND\n" +
                               $"Đã thanh toán: {summary.TotalPaid:N0} VND\n" +
                               $"Công nợ: {summary.OutstandingDebt:N0} VND\n" +
                               $"Hóa đơn đã thanh toán: {summary.PaidCount}\n" +
                               $"Hóa đơn chưa thanh toán: {summary.UnpaidCount}";

                MessageBox.Show(message, "Tổng hợp công nợ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lấy tổng hợp công nợ: {ex.Message}", "Lỗi");
        }
    }

    private void BtnSearch_Click(object sender, EventArgs e)
    {
        try
        {
            var allInvoices = InvoiceDAL.GetAllInvoices();
            var filtered = allInvoices.AsEnumerable();

            if (cmbFilterStatus.SelectedIndex > 0)
                filtered = filtered.Where(i => i.PaymentStatus == ComboBoxHelper.GetSelectedValueString(cmbFilterStatus));

            if (cmbFilterApartment.SelectedIndex > 0)
            {
                int aptID = (int)((dynamic)cmbFilterApartment.SelectedItem).Value;
                filtered = filtered.Where(i => i.ApartmentID == aptID);
            }

            dgvInvoices.DataSource = filtered.ToList();
            lblStatusBar.Text = $"Tìm thấy {filtered.Count()} hóa đơn";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tìm kiếm: {ex.Message}", "Lỗi");
        }
    }

    private void ClearForm()
    {
        cmbApartment.SelectedIndex = -1;
        nudMonth.Value = DateTime.Now.Month;
        nudYear.Value = DateTime.Now.Year;
        dtpDueDate.Value = DateTime.Now.AddDays(30);
        txtTotalAmount.Clear();
        txtPaidAmount.Clear();
        txtNote.Clear();
        _selectedInvoiceID = 0;
    }
}

using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public class FrmRecordPayment : Form
{
    private const int PRIMARY_COLOR = 0x215689;
    private const int SPACING = 10;
    private const int LABEL_WIDTH = 120;
    private const int CONTROL_WIDTH = 200;

    private readonly Label lblInvoiceInfo = new Label();
    private readonly Label lblTotalAmount = new Label();
    private readonly Label lblPaidSoFar = new Label();
    private readonly Label lblRemaining = new Label();

    private readonly Label lblAmount = new Label();
    private readonly TextBox txtAmount = new TextBox();
    private readonly Label lblNote = new Label();
    private readonly TextBox txtNote = new TextBox();

    private readonly Button btnRecord = new Button();
    private readonly Button btnCancel = new Button();

    private readonly int _invoiceID;
    private decimal _totalAmount;
    private decimal _paidAmount;

    public FrmRecordPayment(int invoiceID)
    {
        _invoiceID = invoiceID;
        InitializeComponent();
        LoadInvoiceInfo();
    }

    private void InitializeComponent()
    {
        Text = "Ghi nhận thanh toán";
        Size = new Size(500, 350);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var pnlInfo = CreateInfoPanel();
        var pnlInput = CreateInputPanel();
        var pnlButtons = CreateButtonPanel();

        Controls.Add(pnlInfo);
        Controls.Add(pnlInput);
        Controls.Add(pnlButtons);
    }

    private Panel CreateInfoPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, SPACING),
            Size = new Size(ClientSize.Width - 2 * SPACING, 80),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        lblInvoiceInfo.Text = $"Hóa đơn #{_invoiceID}";
        lblInvoiceInfo.Location = new Point(SPACING, SPACING);
        lblInvoiceInfo.Size = new Size(300, 20);
        lblInvoiceInfo.Font = new Font(lblInvoiceInfo.Font, FontStyle.Bold);
        pnl.Controls.Add(lblInvoiceInfo);

        lblTotalAmount.Text = "Tổng tiền: ";
        lblTotalAmount.Location = new Point(SPACING, SPACING + 25);
        lblTotalAmount.Size = new Size(400, 20);
        pnl.Controls.Add(lblTotalAmount);

        lblPaidSoFar.Text = "Đã thanh toán: ";
        lblPaidSoFar.Location = new Point(SPACING, SPACING + 50);
        lblPaidSoFar.Size = new Size(400, 20);
        pnl.Controls.Add(lblPaidSoFar);

        lblRemaining.Text = "Còn lại: ";
        lblRemaining.Location = new Point(200, SPACING + 50);
        lblRemaining.Size = new Size(200, 20);
        lblRemaining.ForeColor = Color.Red;
        lblRemaining.Font = new Font(lblRemaining.Font, FontStyle.Bold);
        pnl.Controls.Add(lblRemaining);

        return pnl;
    }

    private Panel CreateInputPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 100),
            Size = new Size(ClientSize.Width - 2 * SPACING, 130),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        lblAmount.Text = "Số tiền thanh toán:";
        lblAmount.Location = new Point(SPACING, SPACING);
        lblAmount.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblAmount);

        txtAmount.Location = new Point(SPACING + LABEL_WIDTH + SPACING, SPACING);
        txtAmount.Size = new Size(CONTROL_WIDTH, 25);
        pnl.Controls.Add(txtAmount);

        var lblCurrency = new Label
        {
            Text = "VND",
            Location = new Point(SPACING + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 2, SPACING),
            Size = new Size(40, 20)
        };
        pnl.Controls.Add(lblCurrency);

        lblNote.Text = "Ghi chú:";
        lblNote.Location = new Point(SPACING, SPACING + 40);
        lblNote.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblNote);

        txtNote.Location = new Point(SPACING + LABEL_WIDTH + SPACING, SPACING + 40);
        txtNote.Size = new Size(CONTROL_WIDTH, 50);
        txtNote.Multiline = true;
        pnl.Controls.Add(txtNote);

        return pnl;
    }

    private Panel CreateButtonPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, 240),
            Size = new Size(ClientSize.Width - 2 * SPACING, 60),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnRecord.Text = "Ghi nhận";
        btnRecord.Location = new Point(SPACING, SPACING);
        btnRecord.Size = new Size(150, 35);
        btnRecord.BackColor = Color.FromArgb(0, 100, 0);
        btnRecord.ForeColor = Color.White;
        btnRecord.Click += BtnRecord_Click;
        pnl.Controls.Add(btnRecord);

        btnCancel.Text = "Hủy";
        btnCancel.Location = new Point(pnl.ClientSize.Width - 110, SPACING);
        btnCancel.Size = new Size(100, 35);
        btnCancel.BackColor = Color.Gray;
        btnCancel.ForeColor = Color.White;
        btnCancel.Click += (s, e) => Close();
        pnl.Controls.Add(btnCancel);

        return pnl;
    }

    private void LoadInvoiceInfo()
    {
        try
        {
            var invoice = InvoiceDAL.GetInvoiceByID(_invoiceID);
            if (invoice != null)
            {
                _totalAmount = invoice.TotalAmount;
                _paidAmount = invoice.PaidAmount;

                lblTotalAmount.Text = $"Tổng tiền: {_totalAmount:N0} VND";
                lblPaidSoFar.Text = $"Đã thanh toán: {_paidAmount:N0} VND";
                lblRemaining.Text = $"Còn lại: {_totalAmount - _paidAmount:N0} VND";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải hóa đơn: {ex.Message}", "Lỗi");
        }
    }

    private void BtnRecord_Click(object sender, EventArgs e)
    {
        if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
        {
            MessageBox.Show("Vui lòng nhập số tiền thanh toán hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (amount > _totalAmount - _paidAmount)
        {
            MessageBox.Show(
                $"Số tiền thanh toán không được vượt quá số dư còn lại ({_totalAmount - _paidAmount:N0} VND).",
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        try
        {
            var result = InvoiceBLL.RecordPayment(_invoiceID, amount);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID,
                                     $"Recorded payment of {amount:N0} VND for invoice {_invoiceID}", "FrmRecordPayment");
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi ghi nhận thanh toán: {ex.Message}", "Lỗi");
        }
    }
}

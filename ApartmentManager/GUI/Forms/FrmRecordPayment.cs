using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using System;
using System.Windows.Forms;
using System.Drawing;

namespace ApartmentManager.GUI.Forms;

/// <summary>
/// Dialog form for recording invoice payments
/// </summary>
public class FrmRecordPayment : Form
{
    private const int PRIMARY_COLOR = 0x215689;
    private const int SPACING = 10;
    private const int LABEL_WIDTH = 120;
    private const int CONTROL_WIDTH = 200;

    private Label lblInvoiceInfo = new Label();
    private Label lblTotalAmount = new Label();
    private Label lblPaidSoFar = new Label();
    private Label lblRemaining = new Label();

    private Label lblAmount = new Label();
    private TextBox txtAmount = new TextBox();
    private Label lblNote = new Label();
    private TextBox txtNote = new TextBox();

    private Button btnRecord = new Button();
    private Button btnCancel = new Button();

    private int _invoiceID;
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
        this.Text = "Record Payment";
        this.Size = new Size(500, 350);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.White;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        var pnlInfo = CreateInfoPanel();
        var pnlInput = CreateInputPanel();
        var pnlButtons = CreateButtonPanel();

        this.Controls.Add(pnlInfo);
        this.Controls.Add(pnlInput);
        this.Controls.Add(pnlButtons);
    }

    private Panel CreateInfoPanel()
    {
        var pnl = new Panel
        {
            Location = new Point(SPACING, SPACING),
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 80),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        lblInvoiceInfo.Text = $"Invoice #{_invoiceID}";
        lblInvoiceInfo.Location = new Point(SPACING, SPACING);
        lblInvoiceInfo.Size = new Size(300, 20);
        lblInvoiceInfo.Font = new Font(lblInvoiceInfo.Font, FontStyle.Bold);
        pnl.Controls.Add(lblInvoiceInfo);

        lblTotalAmount.Text = "Total Amount: ";
        lblTotalAmount.Location = new Point(SPACING, SPACING + 25);
        lblTotalAmount.Size = new Size(400, 20);
        pnl.Controls.Add(lblTotalAmount);

        lblPaidSoFar.Text = "Paid So Far: ";
        lblPaidSoFar.Location = new Point(SPACING, SPACING + 50);
        lblPaidSoFar.Size = new Size(400, 20);
        pnl.Controls.Add(lblPaidSoFar);

        lblRemaining.Text = "Remaining: ";
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
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 130),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        lblAmount.Text = "Payment Amount:";
        lblAmount.Location = new Point(SPACING, SPACING);
        lblAmount.Size = new Size(LABEL_WIDTH, 20);
        pnl.Controls.Add(lblAmount);

        txtAmount.Location = new Point(SPACING + LABEL_WIDTH + SPACING, SPACING);
        txtAmount.Size = new Size(CONTROL_WIDTH, 25);
        // txtAmount.Placeholder = "0";  // Placeholder not supported in Windows Forms TextBox
        pnl.Controls.Add(txtAmount);

        var lblCurrency = new Label { Text = "VND", Location = new Point(SPACING + LABEL_WIDTH + CONTROL_WIDTH + SPACING * 2, SPACING), Size = new Size(40, 20) };
        pnl.Controls.Add(lblCurrency);

        lblNote.Text = "Note:";
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
            Size = new Size(this.ClientSize.Width - 2 * SPACING, 60),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnRecord.Text = "Record Payment";
        btnRecord.Location = new Point(SPACING, SPACING);
        btnRecord.Size = new Size(150, 35);
        btnRecord.BackColor = Color.FromArgb(0, 100, 0);
        btnRecord.ForeColor = Color.White;
        btnRecord.Click += BtnRecord_Click;
        pnl.Controls.Add(btnRecord);

        btnCancel.Text = "Cancel";
        btnCancel.Location = new Point(pnl.ClientSize.Width - 110, SPACING);
        btnCancel.Size = new Size(100, 35);
        btnCancel.BackColor = Color.Gray;
        btnCancel.ForeColor = Color.White;
        btnCancel.Click += (s, e) => this.Close();
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

                lblTotalAmount.Text = $"Total Amount: {_totalAmount:N0} VND";
                lblPaidSoFar.Text = $"Paid So Far: {_paidAmount:N0} VND";
                lblRemaining.Text = $"Remaining: {_totalAmount - _paidAmount:N0} VND";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading invoice: {ex.Message}", "Error");
        }
    }

    private void BtnRecord_Click(object sender, EventArgs e)
    {
        if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
        {
            MessageBox.Show("Please enter a valid payment amount.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (amount > _totalAmount - _paidAmount)
        {
            MessageBox.Show($"Payment amount cannot exceed remaining balance ({_totalAmount - _paidAmount:N0} VND).", 
                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var result = InvoiceBLL.RecordPayment(_invoiceID, amount);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AuditLogDAL.LogAction(SessionManager.GetSession().UserID, 
                                     $"Recorded payment of {amount:N0} VND for invoice {_invoiceID}", "FrmRecordPayment");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error recording payment: {ex.Message}", "Error");
        }
    }
}

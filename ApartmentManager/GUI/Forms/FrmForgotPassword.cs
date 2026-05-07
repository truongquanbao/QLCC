using ApartmentManager.BLL;
using Serilog;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public sealed class FrmForgotPassword : Form
{
    private readonly TextBox _txtIdentity = new();
    private readonly TextBox _txtRecovery = new();
    private readonly TextBox _txtNewPassword = new();
    private readonly TextBox _txtConfirmPassword = new();
    private readonly Label _lblStatus = new();
    private readonly Button _btnReset;

    public FrmForgotPassword(string initialIdentity)
    {
        Text = "Quên mật khẩu";
        ClientSize = new Size(520, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ModernUi.ApplyFormDefaults(this, new Size(520, 620));

        _btnReset = ModernUi.Button("Đặt lại mật khẩu", ModernUi.Blue, 160, 44);

        ConfigureUi();
        _txtIdentity.Text = initialIdentity;
    }

    private void ConfigureUi()
    {
        var root = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26),
            BackColor = ModernUi.Surface
        };
        Controls.Add(root);

        var card = ModernUi.CardPanel(8);
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(24);
        root.Controls.Add(card);

        var title = ModernUi.Label("KHÔI PHỤC MẬT KHẨU", 16f, FontStyle.Bold, ModernUi.Navy);
        title.Location = new Point(0, 0);
        title.Size = new Size(420, 32);
        card.Controls.Add(title);

        var intro = ModernUi.Label(
            "Nhập tài khoản và thông tin xác minh đã đăng ký để đặt lại mật khẩu mới.",
            9.5f,
            FontStyle.Regular,
            ModernUi.Muted);
        intro.Location = new Point(0, 34);
        intro.Size = new Size(430, 40);
        card.Controls.Add(intro);

        int y = 84;
        AddLabeledTextBox(card, "Tên đăng nhập hoặc Email", _txtIdentity, ref y);
        AddLabeledTextBox(card, "Email hoặc số điện thoại đã đăng ký", _txtRecovery, ref y);
        AddLabeledTextBox(card, "Mật khẩu mới", _txtNewPassword, ref y, isPassword: true);
        AddLabeledTextBox(card, "Xác nhận mật khẩu mới", _txtConfirmPassword, ref y, isPassword: true);

        var note = ModernUi.Label(
            "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt (!@#$%^&*).",
            9f,
            FontStyle.Regular,
            ModernUi.Muted);
        note.Location = new Point(0, y + 4);
        note.Size = new Size(440, 42);
        card.Controls.Add(note);
        y += 50;

        _lblStatus.Location = new Point(0, y);
        _lblStatus.Size = new Size(440, 42);
        _lblStatus.Font = ModernUi.Font(9f, FontStyle.Bold);
        _lblStatus.ForeColor = ModernUi.Red;
        card.Controls.Add(_lblStatus);
        y += 54;

        _btnReset.Location = new Point(0, y);
        _btnReset.Click += BtnReset_Click;
        card.Controls.Add(_btnReset);

        var btnClose = ModernUi.OutlineButton("Đóng", 100, 44);
        btnClose.Location = new Point(340, y);
        btnClose.Click += (_, _) => Close();
        card.Controls.Add(btnClose);

        AcceptButton = _btnReset;
        CancelButton = btnClose;
    }

    private static void AddLabeledTextBox(Control parent, string labelText, TextBox textBox, ref int y, bool isPassword = false)
    {
        var label = ModernUi.Label(labelText, 10f, FontStyle.Bold, ModernUi.Navy);
        label.Location = new Point(0, y);
        label.Size = new Size(440, 24);
        parent.Controls.Add(label);
        y += 28;

        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = ModernUi.Font(10f);
        textBox.Location = new Point(0, y);
        textBox.Size = new Size(440, 34);
        textBox.UseSystemPasswordChar = isPassword;
        parent.Controls.Add(textBox);
        y += 48;
    }

    private async void BtnReset_Click(object? sender, EventArgs e)
    {
        await ResetPasswordAsync();
    }

    private async Task ResetPasswordAsync()
    {
        SetBusy(true, "Đang xác minh tài khoản...");

        try
        {
            var identity = _txtIdentity.Text.Trim();
            var recovery = _txtRecovery.Text.Trim();
            var newPassword = _txtNewPassword.Text;
            var confirmPassword = _txtConfirmPassword.Text;

            var (success, message) = await Task.Run(() =>
                AuthenticationBLL.ResetPasswordByIdentity(identity, recovery, newPassword, confirmPassword));

            _lblStatus.Text = message;
            _lblStatus.ForeColor = success ? ModernUi.Green : ModernUi.Red;

            if (success)
            {
                MessageBox.Show(
                    "Mật khẩu đã được đặt lại thành công. Bạn có thể đăng nhập bằng mật khẩu mới.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in forgot password dialog");
            _lblStatus.Text = "Đã xảy ra lỗi khi đặt lại mật khẩu.";
            _lblStatus.ForeColor = ModernUi.Red;
        }
        finally
        {
            if (!IsDisposed)
            {
                SetBusy(false);
            }
        }
    }

    private void SetBusy(bool isBusy, string? status = null)
    {
        _txtIdentity.Enabled = !isBusy;
        _txtRecovery.Enabled = !isBusy;
        _txtNewPassword.Enabled = !isBusy;
        _txtConfirmPassword.Enabled = !isBusy;
        _btnReset.Enabled = !isBusy;
        UseWaitCursor = isBusy;

        if (!string.IsNullOrWhiteSpace(status))
        {
            _lblStatus.Text = status;
            _lblStatus.ForeColor = ModernUi.Muted;
        }
    }
}

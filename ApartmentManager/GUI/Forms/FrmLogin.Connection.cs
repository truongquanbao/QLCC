using ApartmentManager.BLL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmLogin
{
    private Label? _connectionStatusLabel;
    private Control? _loginButton;
    private bool _loginUiInitialized;
    private bool _isLoggingIn;

    private async void FrmLogin_Shown(object? sender, EventArgs e)
    {
        InitializeLoginRuntimeUi();
        await RefreshConnectionStatusAsync();
    }

    private void InitializeLoginRuntimeUi()
    {
        if (_loginUiInitialized)
        {
            return;
        }

        _loginButton = Controls.Find("btnLogin", true).FirstOrDefault();

        var footer = Controls
            .OfType<Panel>()
            .FirstOrDefault(panel => panel.Dock == DockStyle.Bottom && panel.Height == 54);

        if (footer != null)
        {
            _connectionStatusLabel = footer.Controls
                .OfType<Label>()
                .FirstOrDefault(label => label.Size.Width >= 200 && label.Size.Width <= 240);

            if (_connectionStatusLabel != null)
            {
                _connectionStatusLabel.Cursor = Cursors.Hand;
                _connectionStatusLabel.Click += OpenDatabaseSetup_Click;
            }

            var settingsControl = footer.Controls
                .OfType<Label>()
                .FirstOrDefault(label => label.TextAlign == ContentAlignment.MiddleCenter && label.Size.Width <= 40);

            if (settingsControl != null)
            {
                settingsControl.Cursor = Cursors.Hand;
                settingsControl.Click += OpenDatabaseSetup_Click;
            }
        }

        _loginUiInitialized = true;
    }

    private async Task ExecuteLoginAsync()
    {
        if (_isLoggingIn)
        {
            return;
        }

        string username = _txtUsername.Text.Trim();
        string password = _txtPassword.Text;

        _lblUsernameError.Visible = string.IsNullOrWhiteSpace(username);
        _lblPasswordError.Visible = string.IsNullOrWhiteSpace(password);

        if (_lblUsernameError.Visible || _lblPasswordError.Visible)
        {
            _lblStatus.Text = "Vui lÃ²ng nháº­p tÃªn Ä‘Äƒng nháº­p vÃ  máº­t kháº©u";
            _lblStatus.ForeColor = ModernUi.Red;
            return;
        }

        SetLoginBusy(true, "Äang kiá»ƒm tra káº¿t ná»‘i...");

        try
        {
            var connectionResult = await Task.Run(DatabaseHelper.EnsureActiveConnection);
            UpdateConnectionStatus(connectionResult.success);

            if (!connectionResult.success)
            {
                _lblStatus.Text = connectionResult.message;
                _lblStatus.ForeColor = ModernUi.Red;
                _txtPassword.Focus();
                _txtPassword.SelectAll();
                return;
            }

            SetLoginBusy(true, "Äang Ä‘Äƒng nháº­p...");

            var (success, message, session) = await Task.Run(() => AuthenticationBLL.Login(username, password));
            if (success && session != null)
            {
                if (_chkRemember.Checked)
                {
                    RememberUsername(username);
                }

                _lblStatus.Text = message;
                _lblStatus.ForeColor = ModernUi.Green;
                Log.Information("User logged in: {Username}", username);
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            _lblStatus.Text = message;
            _lblStatus.ForeColor = ModernUi.Red;
            _txtPassword.Focus();
            _txtPassword.SelectAll();
        }
        finally
        {
            if (!IsDisposed)
            {
                SetLoginBusy(false);
            }
        }
    }

    private async Task RefreshConnectionStatusAsync()
    {
        InitializeLoginRuntimeUi();
        UpdateConnectionStatus(null);

        var result = await Task.Run(DatabaseHelper.EnsureActiveConnection);
        UpdateConnectionStatus(result.success);
    }

    private async void OpenDatabaseSetup_Click(object? sender, EventArgs e)
    {
        using var databaseSetup = new FrmDatabaseSetup();
        if (databaseSetup.ShowDialog(this) == DialogResult.OK)
        {
            await RefreshConnectionStatusAsync();
        }
    }

    private void UpdateConnectionStatus(bool? isConnected)
    {
        if (_connectionStatusLabel == null || _connectionStatusLabel.IsDisposed)
        {
            return;
        }

        if (!isConnected.HasValue)
        {
            _connectionStatusLabel.Text = "â—  Káº¿t ná»‘i: Äang kiá»ƒm tra...";
            _connectionStatusLabel.ForeColor = Color.Gold;
            return;
        }

        _connectionStatusLabel.Text = isConnected.Value
            ? "â—  Káº¿t ná»‘i: ÄÃ£ káº¿t ná»‘i"
            : "â—  Káº¿t ná»‘i: ChÆ°a káº¿t ná»‘i";
        _connectionStatusLabel.ForeColor = isConnected.Value
            ? Color.FromArgb(95, 220, 88)
            : ModernUi.Red;
    }

    private void SetLoginBusy(bool isBusy, string? statusText = null)
    {
        _isLoggingIn = isBusy;

        if (_loginButton != null)
        {
            _loginButton.Enabled = !isBusy;
        }

        _txtUsername.Enabled = !isBusy;
        _txtPassword.Enabled = !isBusy;
        _chkRemember.Enabled = !isBusy;
        UseWaitCursor = isBusy;

        if (!string.IsNullOrWhiteSpace(statusText))
        {
            _lblStatus.Text = statusText;
            _lblStatus.ForeColor = ModernUi.Muted;
        }
    }
}

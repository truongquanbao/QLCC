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
    private Label? _settingsLabel;
    private Control? _loginButton;
    private ToolTip? _loginToolTip;
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
            _connectionStatusLabel = footer.Controls["lblConnectionStatus"] as Label;
            _settingsLabel = footer.Controls["btnSettings"] as Label;

            if (_connectionStatusLabel != null)
            {
                _connectionStatusLabel.Cursor = Cursors.Hand;
                _connectionStatusLabel.Click += RefreshConnectionStatus_Click;
            }

            if (_settingsLabel != null)
            {
                _settingsLabel.Cursor = Cursors.Hand;
                _settingsLabel.Click += OpenDatabaseSetup_Click;
            }
        }

        _loginToolTip ??= new ToolTip();
        if (_connectionStatusLabel != null)
        {
            _loginToolTip.SetToolTip(_connectionStatusLabel, "Kiểm tra lại trạng thái kết nối");
        }

        if (_settingsLabel != null)
        {
            _loginToolTip.SetToolTip(_settingsLabel, "Mở cấu hình kết nối cơ sở dữ liệu");
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
            _lblStatus.Text = "Vui lòng nhập tên đăng nhập và mật khẩu";
            _lblStatus.ForeColor = ModernUi.Red;
            return;
        }

        SetLoginBusy(true, "Đang kiểm tra kết nối...");

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

            SetLoginBusy(true, "Đang đăng nhập...");

            var (success, message, session) = await Task.Run(() => AuthenticationBLL.Login(username, password));
            if (success && session != null)
            {
                if (_chkRemember.Checked)
                {
                    RememberLogin(username, password);
                }
                else
                {
                    ClearRememberedLogin();
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

    private async void RefreshConnectionStatus_Click(object? sender, EventArgs e)
    {
        await RefreshConnectionStatusAsync();
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
            _connectionStatusLabel.Text = "● Kết nối: Đang kiểm tra...";
            _connectionStatusLabel.ForeColor = Color.Gold;
            return;
        }

        _connectionStatusLabel.Text = isConnected.Value
            ? "● Kết nối: Đã kết nối"
            : "● Kết nối: Chưa kết nối";
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

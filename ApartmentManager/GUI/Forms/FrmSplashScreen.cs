using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms
{
    public partial class FrmSplashScreen : Form
    {
        private ProgressBar _progressBar = null!;
        private Label _lblStatus = null!;
        private Label _lblVersion = null!;

        public FrmSplashScreen()
        {
            InitializeComponent();
            InitializeSplashScreen();
        }

        private void InitializeComponent()
        {
        }

        private void InitializeSplashScreen()
        {
            Text = "Quản lý khu chung cư";
            Size = new Size(600, 300);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(33, 86, 155);
            FormBorderStyle = FormBorderStyle.None;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblTitle = new Label
            {
                Text = "Quản lý khu chung cư",
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Left = 50,
                Top = 40,
                Width = 500,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Hệ thống quản lý cư dân và vận hành",
                Font = new Font("Arial", 12, FontStyle.Italic),
                ForeColor = Color.LightGray,
                Left = 50,
                Top = 90,
                Width = 500,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lblSubtitle);

            _progressBar = new ProgressBar
            {
                Left = 50,
                Top = 150,
                Width = 500,
                Height = 20,
                Style = ProgressBarStyle.Continuous
            };
            Controls.Add(_progressBar);

            _lblStatus = new Label
            {
                Text = "Đang khởi tạo...",
                Font = new Font("Arial", 10),
                ForeColor = Color.White,
                Left = 50,
                Top = 180,
                Width = 500,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(_lblStatus);

            _lblVersion = new Label
            {
                Text = "Phiên bản 1.0.0",
                Font = new Font("Arial", 8),
                ForeColor = Color.LightGray,
                Left = 50,
                Top = 250,
                Width = 500,
                Height = 20,
                TextAlign = ContentAlignment.MiddleRight
            };
            Controls.Add(_lblVersion);

            Load += FrmSplashScreen_Load;
        }

        private async void FrmSplashScreen_Load(object sender, EventArgs e)
        {
            try
            {
                await InitializeApplication();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing application");
                MessageBox.Show($"Khởi tạo thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private async Task InitializeApplication()
        {
            UpdateProgress("Đang khởi tạo hệ thống ghi log...", 10);
            await Task.Delay(500);

            UpdateProgress("Đang tải cấu hình...", 25);
            await Task.Delay(500);

            UpdateProgress("Đang kết nối cơ sở dữ liệu...", 40);
            try
            {
                await Task.Delay(500);
                UpdateProgress("Đã kết nối cơ sở dữ liệu", 55);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database connection failed");
                throw;
            }

            UpdateProgress("Đang khởi tạo quản lý phiên...", 70);
            await Task.Delay(300);

            UpdateProgress("Đang tải tài nguyên giao diện...", 85);
            await Task.Delay(300);

            UpdateProgress("Hệ thống sẵn sàng", 100);
            await Task.Delay(500);
        }

        private void UpdateProgress(string status, int percentage)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    _lblStatus.Text = status;
                    _progressBar.Value = Math.Min(percentage, 100);
                    Refresh();
                }));
                return;
            }

            _lblStatus.Text = status;
            _progressBar.Value = Math.Min(percentage, 100);
            Refresh();
        }
    }
}

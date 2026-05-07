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
            Text = "Quáº£n lÃ½ khu chung cÆ°";
            Size = new Size(600, 300);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(33, 86, 155);
            FormBorderStyle = FormBorderStyle.None;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblTitle = new Label
            {
                Text = "Quáº£n lÃ½ khu chung cÆ°",
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
                Text = "Há»‡ thá»‘ng quáº£n lÃ½ cÆ° dÃ¢n vÃ  váº­n hÃ nh",
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
                Text = "Äang khá»Ÿi táº¡o...",
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
                Text = "PhiÃªn báº£n 1.0.0",
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
                MessageBox.Show($"Khá»Ÿi táº¡o tháº¥t báº¡i: {ex.Message}", "Lá»—i", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private async Task InitializeApplication()
        {
            UpdateProgress("Äang khá»Ÿi táº¡o há»‡ thá»‘ng ghi log...", 10);
            await Task.Delay(500);

            UpdateProgress("Äang táº£i cáº¥u hÃ¬nh...", 25);
            await Task.Delay(500);

            UpdateProgress("Äang káº¿t ná»‘i cÆ¡ sá»Ÿ dá»¯ liá»‡u...", 40);
            var connectionResult = await Task.Run(DatabaseHelper.EnsureActiveConnection);
            if (connectionResult.success)
            {
                UpdateProgress("ÄÃ£ káº¿t ná»‘i cÆ¡ sá»Ÿ dá»¯ liá»‡u", 55);
            }
            else
            {
                Log.Warning("Database connection is not ready during splash screen: {Message}", connectionResult.message);
                UpdateProgress("ChÆ°a káº¿t ná»‘i Ä‘Æ°á»£c cÆ¡ sá»Ÿ dá»¯ liá»‡u", 55);
            }

            UpdateProgress("Äang khá»Ÿi táº¡o quáº£n lÃ½ phiÃªn...", 70);
            await Task.Delay(300);

            UpdateProgress("Äang táº£i tÃ i nguyÃªn giao diá»‡n...", 85);
            await Task.Delay(300);

            UpdateProgress("Há»‡ thá»‘ng sáºµn sÃ ng", 100);
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

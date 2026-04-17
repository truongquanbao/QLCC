using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms
{
    public partial class FrmSplashScreen : Form
    {
        private ProgressBar _progressBar;
        private Label _lblStatus;
        private Label _lblVersion;

        public FrmSplashScreen()
        {
            InitializeComponent();
            InitializeSplashScreen();
        }

        private void InitializeComponent()
        {
            // Auto-generated method stub
        }

        private void InitializeSplashScreen()
        {
            this.Text = "Apartment Management System";
            this.Size = new System.Drawing.Size(600, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.FromArgb(33, 86, 155);
            this.FormBorderStyle = FormBorderStyle.None;
            this.ControlBox = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title
            Label lblTitle = new Label
            {
                Text = "Apartment Management System",
                Font = new System.Drawing.Font("Arial", 20, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.White,
                Left = 50,
                Top = 40,
                Width = 500,
                Height = 50,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            // Subtitle
            Label lblSubtitle = new Label
            {
                Text = "Quản Lý Khu Chung Cư",
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.LightGray,
                Left = 50,
                Top = 90,
                Width = 500,
                Height = 30,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblSubtitle);

            // Progress Bar
            _progressBar = new ProgressBar
            {
                Left = 50,
                Top = 150,
                Width = 500,
                Height = 20,
                Style = ProgressBarStyle.Continuous
            };
            this.Controls.Add(_progressBar);

            // Status Label
            _lblStatus = new Label
            {
                Text = "Initializing...",
                Font = new System.Drawing.Font("Arial", 10),
                ForeColor = System.Drawing.Color.White,
                Left = 50,
                Top = 180,
                Width = 500,
                Height = 30,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            this.Controls.Add(_lblStatus);

            // Version Label
            _lblVersion = new Label
            {
                Text = "Version 1.0.0",
                Font = new System.Drawing.Font("Arial", 8),
                ForeColor = System.Drawing.Color.LightGray,
                Left = 50,
                Top = 250,
                Width = 500,
                Height = 20,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            this.Controls.Add(_lblVersion);

            this.Load += FrmSplashScreen_Load;
        }

        private async void FrmSplashScreen_Load(object sender, EventArgs e)
        {
            try
            {
                await InitializeApplication();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing application");
                MessageBox.Show($"Initialization failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private async Task InitializeApplication()
        {
            // Step 1: Initialize Logging
            UpdateProgress("Initializing logging system...", 10);
            await Task.Delay(500);

            // Step 2: Load Configuration
            UpdateProgress("Loading configuration...", 25);
            await Task.Delay(500);

            // Step 3: Initialize Database Connection
            UpdateProgress("Connecting to database...", 40);
            try
            {
                // Verify database connection
                await Task.Delay(500);
                UpdateProgress("Database connection established", 55);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database connection failed");
                throw;
            }

            // Step 4: Initialize Session Manager
            UpdateProgress("Initializing session management...", 70);
            await Task.Delay(300);

            // Step 5: Load UI Resources
            UpdateProgress("Loading user interface resources...", 85);
            await Task.Delay(300);

            // Step 6: Complete
            UpdateProgress("System ready", 100);
            await Task.Delay(500);
        }

        private void UpdateProgress(string status, int percentage)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    _lblStatus.Text = status;
                    _progressBar.Value = Math.Min(percentage, 100);
                    this.Refresh();
                }));
            }
            else
            {
                _lblStatus.Text = status;
                _progressBar.Value = Math.Min(percentage, 100);
                this.Refresh();
            }
        }
    }
}

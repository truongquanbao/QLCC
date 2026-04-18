using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ApartmentManager.BLL;
using Serilog;

namespace ApartmentManager.GUI
{
    /// <summary>
    /// Reports and Export Module Form
    /// Allows users to generate and export various reports in Excel, PDF, or CSV formats
    /// </summary>
    public partial class FrmReportsModule : Form
    {
        private List<string> exportedFiles = new List<string>();

        public FrmReportsModule()
        {
            // Designer initialization - not used for this form
            // InitializeComponent();
        }

        private void FrmReportsModule_Load(object sender, EventArgs e)
        {
            InitializeUI();
            LoadReportTypes();
            Log.Information("Reports Module form loaded");
        }

        private void InitializeUI()
        {
            this.Text = "Reports & Export Module";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Main container panel
            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            this.Controls.Add(pnlMain);

            // Title label
            Label lblTitle = new Label
            {
                Text = "Reports & Export Manager",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                Location = new Point(20, 20),
                AutoSize = true
            };
            pnlMain.Controls.Add(lblTitle);

            // Report type selection panel
            Panel pnlReportSelection = CreateReportSelectionPanel();
            pnlReportSelection.Location = new Point(20, 60);
            pnlReportSelection.Size = new Size(860, 150);
            pnlMain.Controls.Add(pnlReportSelection);

            // Options panel
            Panel pnlOptions = CreateOptionsPanel();
            pnlOptions.Location = new Point(20, 220);
            pnlOptions.Size = new Size(860, 100);
            pnlMain.Controls.Add(pnlOptions);

            // Export format panel
            Panel pnlFormat = CreateFormatPanel();
            pnlFormat.Location = new Point(20, 330);
            pnlFormat.Size = new Size(860, 80);
            pnlMain.Controls.Add(pnlFormat);

            // Buttons panel
            Panel pnlButtons = CreateButtonsPanel();
            pnlButtons.Location = new Point(20, 420);
            pnlButtons.Size = new Size(860, 50);
            pnlMain.Controls.Add(pnlButtons);

            // Results panel
            Panel pnlResults = CreateResultsPanel();
            pnlResults.Location = new Point(20, 480);
            pnlResults.Size = new Size(860, 180);
            pnlMain.Controls.Add(pnlResults);
        }

        private Panel CreateReportSelectionPanel()
        {
            Panel pnl = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.AliceBlue
            };

            Label lblReport = new Label
            {
                Text = "Select Report Type:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            pnl.Controls.Add(lblReport);

            ComboBox cmbReportType = new ComboBox
            {
                Name = "cmbReportType",
                Location = new Point(10, 35),
                Size = new Size(400, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbReportType.Items.AddRange(new string[]
            {
                "Occupancy Report",
                "Payment Collection Report",
                "Complaint Resolution Report",
                "Contract Expiration Report",
                "All Apartments",
                "Resident List",
                "Invoice History"
            });
            cmbReportType.SelectedIndex = 0;
            pnl.Controls.Add(cmbReportType);

            Label lblDescription = new Label
            {
                Text = "Select the type of report you want to generate",
                Font = new Font("Arial", 9, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(10, 65),
                AutoSize = true
            };
            pnl.Controls.Add(lblDescription);

            return pnl;
        }

        private Panel CreateOptionsPanel()
        {
            Panel pnl = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightGreen
            };

            Label lblOptions = new Label
            {
                Text = "Report Options:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            pnl.Controls.Add(lblOptions);

            CheckBox chkByResident = new CheckBox
            {
                Name = "chkByResident",
                Text = "Filter by Resident (Payment Report only)",
                Location = new Point(10, 35),
                AutoSize = true
            };
            pnl.Controls.Add(chkByResident);

            TextBox txtDaysLookout = new TextBox
            {
                Name = "txtDaysLookout",
                Text = "30",
                Location = new Point(10, 60),
                Size = new Size(100, 20)
            };
            pnl.Controls.Add(txtDaysLookout);

            Label lblDaysLookout = new Label
            {
                Text = "Days Lookout (Contracts):",
                Location = new Point(120, 62),
                AutoSize = true
            };
            pnl.Controls.Add(lblDaysLookout);

            return pnl;
        }

        private Panel CreateFormatPanel()
        {
            Panel pnl = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightYellow
            };

            Label lblFormat = new Label
            {
                Text = "Export Format:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            pnl.Controls.Add(lblFormat);

            RadioButton rbExcel = new RadioButton
            {
                Name = "rbExcel",
                Text = "Excel (XLSX)",
                Location = new Point(10, 35),
                Checked = true,
                AutoSize = true
            };
            pnl.Controls.Add(rbExcel);

            RadioButton rbPDF = new RadioButton
            {
                Name = "rbPDF",
                Text = "PDF",
                Location = new Point(150, 35),
                AutoSize = true
            };
            pnl.Controls.Add(rbPDF);

            RadioButton rbCSV = new RadioButton
            {
                Name = "rbCSV",
                Text = "CSV",
                Location = new Point(250, 35),
                AutoSize = true
            };
            pnl.Controls.Add(rbCSV);

            return pnl;
        }

        private Panel CreateButtonsPanel()
        {
            Panel pnl = new Panel();

            Button btnGenerate = new Button
            {
                Text = "Generate Report",
                Location = new Point(10, 10),
                Size = new Size(120, 35),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnGenerate.Click += BtnGenerate_Click;
            pnl.Controls.Add(btnGenerate);

            Button btnClear = new Button
            {
                Text = "Clear Results",
                Location = new Point(140, 10),
                Size = new Size(100, 35),
                BackColor = Color.Orange,
                ForeColor = Color.White
            };
            btnClear.Click += BtnClear_Click;
            pnl.Controls.Add(btnClear);

            Button btnOpenFolder = new Button
            {
                Text = "Open Downloads",
                Location = new Point(250, 10),
                Size = new Size(120, 35),
                BackColor = Color.Green,
                ForeColor = Color.White
            };
            btnOpenFolder.Click += BtnOpenFolder_Click;
            pnl.Controls.Add(btnOpenFolder);

            Button btnClose = new Button
            {
                Text = "Close",
                Location = new Point(750, 10),
                Size = new Size(100, 35),
                BackColor = Color.Red,
                ForeColor = Color.White
            };
            btnClose.Click += (s, e) => this.Close();
            pnl.Controls.Add(btnClose);

            return pnl;
        }

        private Panel CreateResultsPanel()
        {
            Panel pnl = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Honeydew
            };

            Label lblResults = new Label
            {
                Text = "Export Results:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            pnl.Controls.Add(lblResults);

            ListBox lbxResults = new ListBox
            {
                Name = "lbxResults",
                Location = new Point(10, 35),
                Size = new Size(840, 135),
                BackColor = Color.White
            };
            pnl.Controls.Add(lbxResults);

            return pnl;
        }

        private void LoadReportTypes()
        {
            ComboBox cmbReportType = this.Controls.Find("cmbReportType", true)[0] as ComboBox;
            if (cmbReportType != null)
            {
                cmbReportType.SelectedIndex = 0;
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                ComboBox cmbReportType = this.Controls.Find("cmbReportType", true)[0] as ComboBox;
                ListBox lbxResults = this.Controls.Find("lbxResults", true)[0] as ListBox;
                TextBox txtDaysLookout = this.Controls.Find("txtDaysLookout", true)[0] as TextBox;
                RadioButton rbExcel = this.Controls.Find("rbExcel", true)[0] as RadioButton;
                RadioButton rbPDF = this.Controls.Find("rbPDF", true)[0] as RadioButton;
                RadioButton rbCSV = this.Controls.Find("rbCSV", true)[0] as RadioButton;

                string selectedReport = cmbReportType.SelectedItem?.ToString() ?? "";
                string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ApartmentManager_Reports");

                if (!Directory.Exists(downloadFolder))
                {
                    Directory.CreateDirectory(downloadFolder);
                }

                (bool Success, string Message, byte[] FileContent, string FileName) result;

                switch (selectedReport)
                {
                    case "Occupancy Report":
                        result = ReportsBLL.GenerateOccupancyReport();
                        break;
                    case "Payment Collection Report":
                        result = ReportsBLL.GeneratePaymentCollectionReport();
                        break;
                    case "Complaint Resolution Report":
                        result = ReportsBLL.GenerateComplaintResolutionReport();
                        break;
                    case "Contract Expiration Report":
                        if (int.TryParse(txtDaysLookout.Text, out int days))
                        {
                            result = ReportsBLL.GenerateContractExpirationReport(days);
                        }
                        else
                        {
                            result = (false, "Invalid days lookout value", null, null);
                        }
                        break;
                    case "All Apartments":
                        result = ReportsBLL.ExportApartmentsToPDF();
                        break;
                    case "Resident List":
                        result = ReportsBLL.ExportDataToCSV("residents");
                        break;
                    case "Invoice History":
                        result = ReportsBLL.ExportDataToCSV("invoices");
                        break;
                    default:
                        result = (false, "Unknown report type", null, null);
                        break;
                }

                if (result.Success && result.FileContent != null)
                {
                    string filePath = Path.Combine(downloadFolder, result.FileName);
                    File.WriteAllBytes(filePath, result.FileContent);
                    exportedFiles.Add(filePath);

                    lbxResults.Items.Add($"✓ {result.FileName}");
                    lbxResults.Items.Add($"  Location: {filePath}");
                    lbxResults.Items.Add($"  Size: {(result.FileContent.Length / 1024.0):F2} KB");
                    lbxResults.Items.Add("");

                    MessageBox.Show($"Report generated successfully!\n\nFile: {result.FileName}\nLocation: {downloadFolder}", 
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Log.Information("Report generated: {ReportType} - {FileName}", selectedReport, result.FileName);
                }
                else
                {
                    lbxResults.Items.Add($"✗ Error: {result.Message}");
                    MessageBox.Show($"Error: {result.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.Error(ex, "Error generating report");
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            ListBox lbxResults = this.Controls.Find("lbxResults", true)[0] as ListBox;
            if (lbxResults != null)
            {
                lbxResults.Items.Clear();
            }
        }

        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ApartmentManager_Reports");

                if (!Directory.Exists(downloadFolder))
                {
                    Directory.CreateDirectory(downloadFolder);
                }

                System.Diagnostics.Process.Start("open", downloadFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

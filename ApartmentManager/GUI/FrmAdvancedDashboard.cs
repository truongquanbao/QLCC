using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ApartmentManager.BLL;
using Serilog;

namespace ApartmentManager.GUI
{
    /// <summary>
    /// Advanced Dashboard displaying analytics, alerts, and performance metrics
    /// </summary>
    public partial class FrmAdvancedDashboard : Form
    {

        public FrmAdvancedDashboard()
        {
            // Designer initialization - not used for this form
            // InitializeComponent();
        }

        private void FrmAdvancedDashboard_Load(object sender, EventArgs e)
        {
            InitializeUI();
            LoadDashboardData();
            Log.Information("Advanced Dashboard loaded");
        }

        private void InitializeUI()
        {
            this.Text = "Advanced Analytics Dashboard";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;

            // Create tabs for different dashboard sections
            TabControl tabDashboard = new TabControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10)
            };
            this.Controls.Add(tabDashboard);

            // Tab 1: Analytics Overview
            TabPage tabAnalytics = new TabPage("Analytics")
            {
                BackColor = Color.White
            };
            CreateAnalyticsTab(tabAnalytics);
            tabDashboard.TabPages.Add(tabAnalytics);

            // Tab 2: Smart Alerts
            TabPage tabAlerts = new TabPage("Smart Alerts")
            {
                BackColor = Color.White
            };
            CreateAlertsTab(tabAlerts);
            tabDashboard.TabPages.Add(tabAlerts);

            // Tab 3: Maintenance Schedule
            TabPage tabMaintenance = new TabPage("Maintenance")
            {
                BackColor = Color.White
            };
            CreateMaintenanceTab(tabMaintenance);
            tabDashboard.TabPages.Add(tabMaintenance);

            // Tab 4: System Health
            TabPage tabHealth = new TabPage("System Health")
            {
                BackColor = Color.White
            };
            CreateHealthTab(tabHealth);
            tabDashboard.TabPages.Add(tabHealth);
        }

        private void CreateAnalyticsTab(TabPage tab)
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

            // Revenue Analysis
            GroupBox gbRevenue = CreateGroupBox("Revenue Analysis", 10, 10, 380, 200);
            ListBox lbxRevenue = new ListBox
            {
                Name = "lbxRevenue",
                Dock = DockStyle.Fill,
                BackColor = Color.Honeydew
            };
            gbRevenue.Controls.Add(lbxRevenue);
            pnl.Controls.Add(gbRevenue);

            // Occupancy Analysis
            GroupBox gbOccupancy = CreateGroupBox("Occupancy by Building", 400, 10, 380, 200);
            ListBox lbxOccupancy = new ListBox
            {
                Name = "lbxOccupancy",
                Dock = DockStyle.Fill,
                BackColor = Color.LightBlue
            };
            gbOccupancy.Controls.Add(lbxOccupancy);
            pnl.Controls.Add(gbOccupancy);

            // Complaint Metrics
            GroupBox gbComplaints = CreateGroupBox("Complaint Metrics", 10, 220, 380, 200);
            ListBox lbxComplaints = new ListBox
            {
                Name = "lbxComplaints",
                Dock = DockStyle.Fill,
                BackColor = Color.LightCoral
            };
            gbComplaints.Controls.Add(lbxComplaints);
            pnl.Controls.Add(gbComplaints);

            // Top Debtors
            GroupBox gbDebtors = CreateGroupBox("Top Outstanding Debtors", 400, 220, 380, 200);
            ListBox lbxDebtors = new ListBox
            {
                Name = "lbxDebtors",
                Dock = DockStyle.Fill,
                BackColor = Color.LightYellow
            };
            gbDebtors.Controls.Add(lbxDebtors);
            pnl.Controls.Add(gbDebtors);

            // Refresh button
            Button btnRefresh = new Button
            {
                Text = "Refresh Analytics",
                Location = new Point(10, 430),
                Size = new Size(150, 35),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White
            };
            btnRefresh.Click += (s, e) => LoadDashboardData();
            pnl.Controls.Add(btnRefresh);

            tab.Controls.Add(pnl);
        }

        private void CreateAlertsTab(TabPage tab)
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

            // Active Alerts
            GroupBox gbAlerts = CreateGroupBox("Active System Alerts", 10, 10, 760, 250);
            ListBox lbxAlerts = new ListBox
            {
                Name = "lbxAlerts",
                Dock = DockStyle.Fill,
                BackColor = Color.MistyRose
            };
            gbAlerts.Controls.Add(lbxAlerts);
            pnl.Controls.Add(gbAlerts);

            // Communication Recommendations
            GroupBox gbRecommendations = CreateGroupBox("Communication Recommendations", 10, 270, 760, 200);
            ListBox lbxRecommendations = new ListBox
            {
                Name = "lbxRecommendations",
                Dock = DockStyle.Fill,
                BackColor = Color.LightCyan
            };
            gbRecommendations.Controls.Add(lbxRecommendations);
            pnl.Controls.Add(gbRecommendations);

            // Action buttons
            Button btnDismissAlert = new Button
            {
                Text = "Acknowledge Alert",
                Location = new Point(10, 480),
                Size = new Size(150, 35),
                BackColor = Color.OrangeRed,
                ForeColor = Color.White
            };
            pnl.Controls.Add(btnDismissAlert);

            Button btnGenerateReport = new Button
            {
                Text = "Generate Alert Report",
                Location = new Point(170, 480),
                Size = new Size(150, 35),
                BackColor = Color.Purple,
                ForeColor = Color.White
            };
            pnl.Controls.Add(btnGenerateReport);

            tab.Controls.Add(pnl);
        }

        private void CreateMaintenanceTab(TabPage tab)
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

            // Scheduled Maintenance
            GroupBox gbMaintenance = CreateGroupBox("Scheduled Maintenance Reminders", 10, 10, 760, 300);
            ListBox lbxMaintenance = new ListBox
            {
                Name = "lbxMaintenance",
                Dock = DockStyle.Fill,
                BackColor = Color.LightGoldenrodYellow
            };
            gbMaintenance.Controls.Add(lbxMaintenance);
            pnl.Controls.Add(gbMaintenance);

            // Legend
            Label lblLegend = new Label
            {
                Text = "Priority: 1=Low, 2=Medium, 3=High. Sorted by due date.",
                Location = new Point(10, 320),
                AutoSize = true,
                Font = new Font("Arial", 9, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            pnl.Controls.Add(lblLegend);

            // Maintenance buttons
            Button btnScheduleMaintenance = new Button
            {
                Text = "Schedule Maintenance",
                Location = new Point(10, 350),
                Size = new Size(150, 35),
                BackColor = Color.DarkGreen,
                ForeColor = Color.White
            };
            pnl.Controls.Add(btnScheduleMaintenance);

            Button btnExportSchedule = new Button
            {
                Text = "Export Schedule",
                Location = new Point(170, 350),
                Size = new Size(150, 35),
                BackColor = Color.DarkBlue,
                ForeColor = Color.White
            };
            pnl.Controls.Add(btnExportSchedule);

            tab.Controls.Add(pnl);
        }

        private void CreateHealthTab(TabPage tab)
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

            // System Statistics
            GroupBox gbHealth = CreateGroupBox("System Health & Statistics", 10, 10, 760, 400);
            ListBox lbxHealth = new ListBox
            {
                Name = "lbxHealth",
                Dock = DockStyle.Fill,
                BackColor = Color.LightGreen
            };
            gbHealth.Controls.Add(lbxHealth);
            pnl.Controls.Add(gbHealth);

            // Status indicator
            Panel pnlStatus = new Panel
            {
                Location = new Point(10, 420),
                Size = new Size(760, 50),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightGreen
            };

            Label lblStatus = new Label
            {
                Text = "System Status: HEALTHY",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlStatus.Controls.Add(lblStatus);
            pnl.Controls.Add(pnlStatus);

            // Last check
            Label lblLastCheck = new Label
            {
                Text = $"Last check: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Location = new Point(10, 480),
                AutoSize = true,
                Font = new Font("Arial", 9, FontStyle.Italic)
            };
            pnl.Controls.Add(lblLastCheck);

            tab.Controls.Add(pnl);
        }

        private GroupBox CreateGroupBox(string title, int x, int y, int width, int height)
        {
            return new GroupBox
            {
                Text = title,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
        }

        private void LoadDashboardData()
        {
            try
            {
                // Load Analytics Tab
                LoadRevenueAnalytics();
                LoadOccupancyAnalytics();
                LoadComplaintMetrics();
                LoadTopDebtors();

                // Load Alerts Tab
                LoadSmartAlerts();
                LoadRecommendations();

                // Load Maintenance Tab
                LoadMaintenanceSchedule();

                // Load Health Tab
                LoadSystemHealth();

                Log.Information("Dashboard data refreshed successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.Error(ex, "Error loading dashboard data");
            }
        }

        private void LoadRevenueAnalytics()
        {
            try
            {
                var analysis = AnalyticsBLL.GetRevenueAnalysis();
                ListBox lbx = this.Controls.Find("lbxRevenue", true).FirstOrDefault() as ListBox;

                if (lbx != null)
                {
                    lbx.Items.Clear();
                    foreach (var item in analysis)
                    {
                        lbx.Items.Add($"{item.Key}: {item.Value:C}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading revenue analytics");
            }
        }

        private void LoadOccupancyAnalytics()
        {
            try
            {
                var occupancy = AnalyticsBLL.GetOccupancyByBuilding();
                ListBox lbx = this.Controls.Find("lbxOccupancy", true).FirstOrDefault() as ListBox;

                if (lbx != null)
                {
                    lbx.Items.Clear();
                    foreach (var building in occupancy)
                    {
                        lbx.Items.Add($"{building.Key}: {building.Value.Occupied}/{building.Value.Total} ({building.Value.Rate:F1}%)");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading occupancy analytics");
            }
        }

        private void LoadComplaintMetrics()
        {
            try
            {
                var metrics = AnalyticsBLL.GetComplaintMetrics();
                ListBox lbx = this.Controls.Find("lbxComplaints", true).FirstOrDefault() as ListBox;

                if (lbx != null)
                {
                    lbx.Items.Clear();
                    foreach (var metric in metrics)
                    {
                        lbx.Items.Add($"{metric.Key}: {metric.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading complaint metrics");
            }
        }

        private void LoadTopDebtors()
        {
            try
            {
                var debtors = AnalyticsBLL.GetTopOutstandingDebtors(5);
                ListBox lbx = this.Controls.Find("lbxDebtors", true).FirstOrDefault() as ListBox;

                if (lbx != null)
                {
                    lbx.Items.Clear();
                    foreach (var debtor in debtors)
                    {
                        lbx.Items.Add($"{debtor.Name} - Owes: {debtor.OutstandingAmount:C}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading top debtors");
            }
        }

        private void LoadSmartAlerts()
        {
            try
            {
                var alerts = SmartAlertsBLL.GenerateSmartAlerts();
                ListBox lbx = this.Controls.Find("lbxAlerts", true).FirstOrDefault() as ListBox;

                if (lbx != null)
                {
                    lbx.Items.Clear();
                    foreach (var alert in alerts.OrderByDescending(a => a.Priority))
                    {
                        string priority = alert.Priority == 3 ? "🔴 HIGH" : alert.Priority == 2 ? "🟠 MEDIUM" : "🟡 LOW";
                        lbx.Items.Add($"{priority} | {alert.AlertType}: {alert.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading smart alerts");
            }
        }

        private void LoadRecommendations()
        {
            try
            {
                var recommendations = SmartAlertsBLL.GetCommunicationRecommendations();
                ListBox lbx = this.Controls.Find("lbxRecommendations", true).FirstOrDefault() as ListBox;

                if (lbx != null)
                {
                    lbx.Items.Clear();
                    foreach (var rec in recommendations)
                    {
                        lbx.Items.Add($"✓ {rec}");
                    }
                    if (recommendations.Count == 0)
                    {
                        lbx.Items.Add("No recommendations at this time.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading recommendations");
            }
        }

        private void LoadMaintenanceSchedule()
        {
            try
            {
                var schedule = AnalyticsBLL.GetMaintenanceReminders();
                ListBox lbx = this.Controls.Find("lbxMaintenance", true).FirstOrDefault() as ListBox;

                if (lbx != null)
                {
                    lbx.Items.Clear();
                    foreach (var item in schedule)
                    {
                        int daysUntilDue = (int)(item.DueDate - DateTime.Now).TotalDays;
                        string status = daysUntilDue <= 0 ? "⚠️ OVERDUE" : daysUntilDue <= 7 ? "🔔 DUE SOON" : "📅 SCHEDULED";
                        lbx.Items.Add($"{status} | {item.Category} ({item.Description}) - Due: {item.DueDate:yyyy-MM-dd}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading maintenance schedule");
            }
        }

        private void LoadSystemHealth()
        {
            try
            {
                var health = SystemHealthBLL.GetSystemHealth();
                ListBox lbx = this.Controls.Find("lbxHealth", true).FirstOrDefault() as ListBox;

                if (lbx != null)
                {
                    lbx.Items.Clear();
                    lbx.Items.Add("");
                    foreach (var item in health)
                    {
                        lbx.Items.Add($"{item.Key}: {item.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading system health");
            }
        }
    }
}

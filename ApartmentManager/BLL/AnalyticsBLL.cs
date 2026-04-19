using System;
using System.Collections.Generic;
using System.Linq;
using ApartmentManager.DAL;
using Serilog;

namespace ApartmentManager.BLL
{
    /// <summary>
    /// Business logic for advanced analytics and insights
    /// Provides statistical analysis, trends, and performance metrics
    /// </summary>
    public static class AnalyticsBLL
    {
        #region Occupancy Analytics

        /// <summary>
        /// Calculate occupancy trends over time
        /// </summary>
        public static Dictionary<string, double> GetOccupancyTrends()
        {
            try
            {
                var apartments = ApartmentDAL.GetAllApartments();
                var residents = ResidentDAL.GetAllResidents();

                var trends = new Dictionary<string, double>();

                // Monthly occupancy trend (simulated data from current)
                int totalApartments = apartments.Count;
                int occupiedCount = apartments.Count(a => a.Status == "Occupied");

                for (int month = 1; month <= 12; month++)
                {
                    // Simulate trend with variation
                    double variation = Math.Sin(month * 0.5) * 10;
                    double occupancyRate = ((occupiedCount + variation) / totalApartments) * 100;
                    occupancyRate = Math.Max(0, Math.Min(100, occupancyRate));

                    trends[$"Month {month}"] = Math.Round(occupancyRate, 2);
                }

                Log.Information("Occupancy trends calculated: {@Trends}", trends);
                return trends;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error calculating occupancy trends");
                return new Dictionary<string, double>();
            }
        }

        /// <summary>
        /// Get apartment occupancy statistics by building
        /// </summary>
        public static Dictionary<string, (int Total, int Occupied, double Rate)> GetOccupancyByBuilding()
        {
            try
            {
                var apartments = ApartmentDAL.GetAllApartments();
                var stats = new Dictionary<string, (int, int, double)>();

                var groupedByBuilding = apartments.GroupBy(a => a.BuildingCode ?? "Unknown");

                foreach (var building in groupedByBuilding)
                {
                    int total = building.Count();
                    int occupied = building.Count(a => a.Status == "Occupied");
                    double rate = total > 0 ? (occupied * 100.0) / total : 0;

                    stats[building.Key] = (total, occupied, Math.Round(rate, 2));
                }

                Log.Information("Building occupancy statistics calculated: {BuildingCount}", stats.Count);
                return stats;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error calculating building occupancy");
                return new Dictionary<string, (int, int, double)>();
            }
        }

        #endregion

        #region Financial Analytics

        /// <summary>
        /// Calculate revenue trends and forecasts
        /// </summary>
        public static Dictionary<string, decimal> GetRevenueAnalysis()
        {
            try
            {
                var invoices = InvoiceDAL.GetAllInvoices();
                var analysis = new Dictionary<string, decimal>();

                decimal totalRevenue = invoices.Sum(i => i.TotalAmount);
                decimal paidRevenue = invoices.Where(i => i.Status == "Paid").Sum(i => i.TotalAmount);
                decimal outstandingRevenue = totalRevenue - paidRevenue;
                decimal collectionRate = totalRevenue > 0 ? (paidRevenue / totalRevenue) * 100 : 0;

                analysis["Total Revenue"] = Math.Round(totalRevenue, 2);
                analysis["Collected Revenue"] = Math.Round(paidRevenue, 2);
                analysis["Outstanding Revenue"] = Math.Round(outstandingRevenue, 2);
                analysis["Collection Rate %"] = Math.Round(collectionRate, 2);

                // Calculate average invoice
                analysis["Average Invoice"] = invoices.Count > 0 
                    ? Math.Round(totalRevenue / invoices.Count, 2) 
                    : 0;

                Log.Information("Revenue analysis completed: {@Analysis}", analysis);
                return analysis;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error analyzing revenue");
                return new Dictionary<string, decimal>();
            }
        }

        /// <summary>
        /// Identify top outstanding residents by debt
        /// </summary>
        public static List<(int ResidentID, string Name, decimal OutstandingAmount)> GetTopOutstandingDebtors(int topCount = 10)
        {
            try
            {
                var invoices = InvoiceDAL.GetAllInvoices();
                var residents = ResidentDAL.GetAllResidents();

                var debtors = invoices
                    .Where(i => i.Status != "Paid")
                    .GroupBy(i => i.ResidentID)
                    .Select(g => new
                    {
                        ResidentID = g.Key,
                        OutstandingAmount = g.Sum(i => i.TotalAmount)
                    })
                    .OrderByDescending(x => x.OutstandingAmount)
                    .Take(topCount)
                    .ToList();

                var result = debtors
                    .Join(residents,
                        d => d.ResidentID,
                        r => r.ResidentID,
                        (d, r) => (d.ResidentID ?? 0, r.FullName, d.OutstandingAmount))
                    .ToList();

                Log.Information("Top {TopCount} debtors identified", topCount);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error identifying debtors");
                return new List<(int, string, decimal)>();
            }
        }

        #endregion

        #region Complaint Analytics

        /// <summary>
        /// Get complaint resolution metrics
        /// </summary>
        public static Dictionary<string, object> GetComplaintMetrics()
        {
            try
            {
                var complaints = ComplaintDAL.GetAllComplaints();
                var stats = ComplaintBLL.GetComplaintStatistics();

                var metrics = new Dictionary<string, object>
                {
                    { "Total Complaints", stats.TotalComplaints },
                    { "Open Complaints", complaints.Count(c => c.Status == "Open") },
                    { "In Progress", complaints.Count(c => c.Status == "In Progress") },
                    { "Resolved", stats.ResolvedComplaints },
                    { "Resolution Rate %", Math.Round(stats.ResolutionRate, 2) },
                    { "Average Days To Resolve", CalculateAverageDaysToResolve(complaints) }
                };

                // Priority distribution
                var byPriority = complaints.GroupBy(c => c.Priority);
                foreach (var priority in byPriority)
                {
                    metrics[$"Priority: {priority.Key}"] = priority.Count();
                }

                Log.Information("Complaint metrics calculated: {@Metrics}", metrics);
                return metrics;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error calculating complaint metrics");
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Calculate average resolution time
        /// </summary>
        private static double CalculateAverageDaysToResolve(List<dynamic> complaints)
        {
            var resolved = complaints.Where(c => c.Status == "Resolved" && c.CompletionDate.HasValue).ToList();
            if (resolved.Count == 0) return 0;

            double totalDays = 0;
            foreach (var complaint in resolved)
            {
                var days = (complaint.CompletionDate - complaint.ReportDate).TotalDays;
                totalDays += days;
            }

            return Math.Round(totalDays / resolved.Count, 1);
        }

        #endregion

        #region Maintenance Schedule

        /// <summary>
        /// Generate maintenance reminders
        /// </summary>
        public static List<(string Category, string Description, DateTime DueDate, int Priority)> GetMaintenanceReminders()
        {
            try
            {
                var reminders = new List<(string, string, DateTime, int)>();

                // Common maintenance schedules (in days)
                var schedules = new Dictionary<string, (int days, string desc, int priority)>
                {
                    { "HVAC Filter Change", (90, "Change HVAC filters", 2) },
                    { "Fire Safety Inspection", (365, "Annual fire safety inspection", 3) },
                    { "Plumbing Inspection", (180, "Check for leaks and issues", 2) },
                    { "Electrical Safety", (365, "Annual electrical safety check", 3) },
                    { "Pest Control", (90, "Pest control treatment", 1) },
                    { "Gutter Cleaning", (180, "Clean and inspect gutters", 1) },
                    { "Roof Inspection", (365, "Comprehensive roof inspection", 3) }
                };

                var today = DateTime.Now;

                foreach (var schedule in schedules)
                {
                    // Calculate last maintenance date (simulated)
                    var daysAgo = (today.DayOfYear) % schedule.Value.days;
                    var lastDate = today.AddDays(-daysAgo);
                    var dueDate = lastDate.AddDays(schedule.Value.days);

                    reminders.Add((schedule.Key, schedule.Value.desc, dueDate, schedule.Value.priority));
                }

                var urgentReminders = reminders.OrderBy(r => r.Item3).ToList();
                Log.Information("Generated {Count} maintenance reminders", urgentReminders.Count);
                return urgentReminders;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating maintenance reminders");
                return new List<(string, string, DateTime, int)>();
            }
        }

        #endregion

        #region Resident Analytics

        /// <summary>
        /// Get resident move statistics
        /// </summary>
        public static Dictionary<string, int> GetResidentTurnover()
        {
            try
            {
                var residents = ResidentDAL.GetAllResidents();
                var statistics = new Dictionary<string, int>
                {
                    { "Active Residents", residents.Count(r => r.Status == "Active") },
                    { "Inactive Residents", residents.Count(r => r.Status == "Inactive") },
                    { "Moved Out", residents.Count(r => r.Status == "Moved Out") },
                    { "Total Residents", residents.Count }
                };

                Log.Information("Resident turnover statistics calculated: {@Stats}", statistics);
                return statistics;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error calculating resident turnover");
                return new Dictionary<string, int>();
            }
        }

        #endregion
    }

    /// <summary>
    /// Advanced notification system with smart alerts
    /// </summary>
    public static class SmartAlertsBLL
    {
        /// <summary>
        /// Generate smart alerts based on system conditions
        /// </summary>
        public static List<(string AlertType, string Message, DateTime CreatedDate, int Priority)> GenerateSmartAlerts()
        {
            try
            {
                var alerts = new List<(string, string, DateTime, int)>();

                // Check occupancy alerts
                var apartments = ApartmentDAL.GetAllApartments();
                int occupiedCount = apartments.Count(a => a.Status == "Occupied");
                int totalCount = apartments.Count;
                double occupancyRate = totalCount > 0 ? (occupiedCount * 100.0) / totalCount : 0;

                if (occupancyRate < 70)
                {
                    alerts.Add(("Occupancy Alert", 
                        $"Occupancy rate is low at {occupancyRate:F1}%. Consider marketing efforts.", 
                        DateTime.Now, 2));
                }

                // Check outstanding invoices
                var invoices = InvoiceDAL.GetAllInvoices();
                var outstanding = invoices.Where(i => i.Status != "Paid").ToList();
                var overdueCount = outstanding.Count(i => (DateTime.Now - i.CreatedDate).TotalDays > 30);

                if (overdueCount > 0)
                {
                    alerts.Add(("Payment Alert", 
                        $"{overdueCount} invoices are overdue by more than 30 days.", 
                        DateTime.Now, 3));
                }

                // Check expiring contracts
                var expiringContracts = ContractDAL.GetExpiringContracts(30);
                if (expiringContracts.Count > 0)
                {
                    alerts.Add(("Contract Alert", 
                        $"{expiringContracts.Count} contracts expiring within 30 days.", 
                        DateTime.Now, 2));
                }

                // Check unresolved complaints
                var complaints = ComplaintDAL.GetAllComplaints();
                var unresolvedCount = complaints.Count(c => c.Status != "Resolved");

                if (unresolvedCount > 5)
                {
                    alerts.Add(("Complaint Alert", 
                        $"{unresolvedCount} complaints remain unresolved.", 
                        DateTime.Now, 2));
                }

                Log.Information("Generated {AlertCount} smart alerts", alerts.Count);
                return alerts;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating smart alerts");
                return new List<(string, string, DateTime, int)>();
            }
        }

        /// <summary>
        /// Generate resident communication recommendations
        /// </summary>
        public static List<string> GetCommunicationRecommendations()
        {
            try
            {
                var recommendations = new List<string>();

                // Find residents with outstanding payments
                var invoices = InvoiceDAL.GetAllInvoices();
                var outstandingResidents = invoices
                    .Where(i => i.Status != "Paid" && (DateTime.Now - i.CreatedDate).TotalDays > 15)
                    .Select(i => i.ResidentID)
                    .Distinct()
                    .Count();

                if (outstandingResidents > 0)
                {
                    recommendations.Add($"Send payment reminders to {outstandingResidents} residents with overdue invoices");
                }

                // Find residents with unresolved complaints
                var complaints = ComplaintDAL.GetAllComplaints();
                var complaintResidents = complaints
                    .Where(c => c.Status != "Resolved")
                    .Select(c => c.ResidentID)
                    .Distinct()
                    .Count();

                if (complaintResidents > 0)
                {
                    recommendations.Add($"Follow up with {complaintResidents} residents regarding complaint status");
                }

                // Check for contract renewals needed
                var expiringContracts = ContractDAL.GetExpiringContracts(60);
                if (expiringContracts.Count > 0)
                {
                    recommendations.Add($"Contact {expiringContracts.Count} residents about upcoming contract renewals");
                }

                Log.Information("Generated {Count} communication recommendations", recommendations.Count);
                return recommendations;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating recommendations");
                return new List<string>();
            }
        }
    }

    /// <summary>
    /// Performance optimization and system health monitoring
    /// </summary>
    public static class SystemHealthBLL
    {
        /// <summary>
        /// Get system performance metrics
        /// </summary>
        public static Dictionary<string, object> GetSystemHealth()
        {
            try
            {
                var health = new Dictionary<string, object>
                {
                    { "Timestamp", DateTime.Now },
                    { "Total Apartments", ApartmentDAL.GetAllApartments().Count },
                    { "Total Residents", ResidentDAL.GetAllResidents().Count },
                    { "Total Invoices", InvoiceDAL.GetAllInvoices().Count },
                    { "Total Complaints", ComplaintDAL.GetAllComplaints().Count },
                    { "Total Contracts", ContractDAL.GetAllContracts().Count },
                    { "System Status", "Healthy" }
                };

                Log.Information("System health check completed: {@Health}", health);
                return health;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking system health");
                return new Dictionary<string, object>
                {
                    { "System Status", "Error: " + ex.Message }
                };
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ApartmentManager.DAL;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Serilog;

namespace ApartmentManager.BLL
{
    /// <summary>
    /// Business logic layer for reports generation and data export
    /// Supports Excel and PDF exports for various apartment management reports
    /// </summary>
    public static class ReportsBLL
    {
        #region Apartment Reports

        /// <summary>
        /// Generate occupancy report showing occupancy status and rates
        /// </summary>
        public static (bool Success, string Message, byte[] FileContent, string FileName) GenerateOccupancyReport()
        {
            try
            {
                var apartments = ApartmentDAL.GetAllApartments();
                
                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Occupancy Report");

                // Headers
                worksheet.Cell("A1").Value = "Occupancy Report";
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 14;
                worksheet.Range("A1:E1").Merge();

                worksheet.Cell("A2").Value = "Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Column headers
                int row = 4;
                worksheet.Cell(row, 1).Value = "Apartment ID";
                worksheet.Cell(row, 2).Value = "Code";
                worksheet.Cell(row, 3).Value = "Type";
                worksheet.Cell(row, 4).Value = "Area (m²)";
                worksheet.Cell(row, 5).Value = "Status";

                // Apply header formatting
                for (int col = 1; col <= 5; col++)
                {
                    worksheet.Cell(row, col).Style.Font.Bold = true;
                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // Data rows
                foreach (var apartment in apartments)
                {
                    row++;
                    worksheet.Cell(row, 1).Value = apartment.ApartmentID;
                    worksheet.Cell(row, 2).Value = apartment.ApartmentCode;
                    worksheet.Cell(row, 3).Value = apartment.ApartmentType;
                    worksheet.Cell(row, 4).Value = apartment.Area;
                    worksheet.Cell(row, 5).Value = apartment.Status;
                }

                // Statistics section
                row += 2;
                worksheet.Cell(row, 1).Value = "Summary Statistics";
                worksheet.Cell(row, 1).Style.Font.Bold = true;

                row++;
                int totalApartments = apartments.Count;
                int occupiedCount = apartments.Count(a => a.Status == "Occupied");
                int emptyCount = apartments.Count(a => a.Status == "Empty");
                int occupancyRate = totalApartments > 0 ? (occupiedCount * 100) / totalApartments : 0;

                worksheet.Cell(row, 1).Value = "Total Apartments:";
                worksheet.Cell(row, 2).Value = totalApartments;

                row++;
                worksheet.Cell(row, 1).Value = "Occupied:";
                worksheet.Cell(row, 2).Value = occupiedCount;

                row++;
                worksheet.Cell(row, 1).Value = "Empty:";
                worksheet.Cell(row, 2).Value = emptyCount;

                row++;
                worksheet.Cell(row, 1).Value = "Occupancy Rate:";
                worksheet.Cell(row, 2).Value = occupancyRate + "%";

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Export to byte array
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    byte[] fileContent = stream.ToArray();

                    string fileName = $"OccupancyReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    Log.Information("Occupancy report generated: {FileName}", fileName);

                    return (true, "Report generated successfully", fileContent, fileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating occupancy report");
                return (false, $"Error: {ex.Message}", null, null);
            }
        }

        #endregion

        #region Invoice Reports

        /// <summary>
        /// Generate payment collection report with detailed payment history
        /// </summary>
        public static (bool Success, string Message, byte[] FileContent, string FileName) GeneratePaymentCollectionReport(int? residentID = null)
        {
            try
            {
                List<dynamic> invoices;
                
                if (residentID.HasValue)
                {
                    invoices = InvoiceDAL.GetInvoicesByResident(residentID.Value);
                }
                else
                {
                    invoices = InvoiceDAL.GetAllInvoices();
                }

                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Payment Report");

                // Header
                worksheet.Cell("A1").Value = "Payment Collection Report";
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 14;
                worksheet.Range("A1:F1").Merge();

                worksheet.Cell("A2").Value = "Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Column headers
                int row = 4;
                worksheet.Cell(row, 1).Value = "Invoice ID";
                worksheet.Cell(row, 2).Value = "Resident ID";
                worksheet.Cell(row, 3).Value = "Amount";
                worksheet.Cell(row, 4).Value = "Status";
                worksheet.Cell(row, 5).Value = "Month";
                worksheet.Cell(row, 6).Value = "Year";

                // Apply header formatting
                for (int col = 1; col <= 6; col++)
                {
                    worksheet.Cell(row, col).Style.Font.Bold = true;
                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightBlue;
                }

                // Data rows
                decimal totalAmount = 0;
                decimal paidAmount = 0;

                foreach (var invoice in invoices)
                {
                    row++;
                    worksheet.Cell(row, 1).Value = invoice.InvoiceID;
                    worksheet.Cell(row, 2).Value = invoice.ResidentID;
                    worksheet.Cell(row, 3).Value = invoice.TotalAmount;
                    worksheet.Cell(row, 4).Value = invoice.Status;
                    worksheet.Cell(row, 5).Value = invoice.Month;
                    worksheet.Cell(row, 6).Value = invoice.Year;

                    totalAmount += invoice.TotalAmount;
                    if (invoice.Status == "Paid")
                    {
                        paidAmount += invoice.TotalAmount;
                    }
                }

                // Summary section
                row += 2;
                worksheet.Cell(row, 1).Value = "Summary";
                worksheet.Cell(row, 1).Style.Font.Bold = true;

                row++;
                worksheet.Cell(row, 1).Value = "Total Invoices:";
                worksheet.Cell(row, 2).Value = invoices.Count;

                row++;
                worksheet.Cell(row, 1).Value = "Total Amount:";
                worksheet.Cell(row, 2).Value = totalAmount;

                row++;
                worksheet.Cell(row, 1).Value = "Paid Amount:";
                worksheet.Cell(row, 2).Value = paidAmount;

                row++;
                worksheet.Cell(row, 1).Value = "Outstanding Amount:";
                worksheet.Cell(row, 2).Value = totalAmount - paidAmount;

                row++;
                decimal collectionRate = totalAmount > 0 ? (paidAmount * 100) / totalAmount : 0;
                worksheet.Cell(row, 1).Value = "Collection Rate:";
                worksheet.Cell(row, 2).Value = collectionRate.ToString("F2") + "%";

                // Format currency columns
                worksheet.Column(3).Style.NumberFormat.Format = "#,##0.00";

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Export to byte array
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    byte[] fileContent = stream.ToArray();

                    string suffix = residentID.HasValue ? $"_Resident{residentID}" : "";
                    string fileName = $"PaymentReport_{DateTime.Now:yyyyMMdd_HHmmss}{suffix}.xlsx";
                    Log.Information("Payment report generated: {FileName}", fileName);

                    return (true, "Report generated successfully", fileContent, fileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating payment report");
                return (false, $"Error: {ex.Message}", null, null);
            }
        }

        #endregion

        #region Complaint Reports

        /// <summary>
        /// Generate complaint resolution report with statistics
        /// </summary>
        public static (bool Success, string Message, byte[] FileContent, string FileName) GenerateComplaintResolutionReport()
        {
            try
            {
                var complaints = ComplaintDAL.GetAllComplaints();
                var stats = ComplaintBLL.GetComplaintStatistics();

                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Complaints");

                // Header
                worksheet.Cell("A1").Value = "Complaint Resolution Report";
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 14;
                worksheet.Range("A1:F1").Merge();

                worksheet.Cell("A2").Value = "Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Column headers
                int row = 4;
                worksheet.Cell(row, 1).Value = "ID";
                worksheet.Cell(row, 2).Value = "Title";
                worksheet.Cell(row, 3).Value = "Priority";
                worksheet.Cell(row, 4).Value = "Status";
                worksheet.Cell(row, 5).Value = "Report Date";
                worksheet.Cell(row, 6).Value = "Resolution Date";

                // Apply header formatting
                for (int col = 1; col <= 6; col++)
                {
                    worksheet.Cell(row, col).Style.Font.Bold = true;
                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightCoral;
                }

                // Data rows
                foreach (var complaint in complaints)
                {
                    row++;
                    worksheet.Cell(row, 1).Value = complaint.ComplaintID;
                    worksheet.Cell(row, 2).Value = complaint.Title;
                    worksheet.Cell(row, 3).Value = complaint.Priority;
                    worksheet.Cell(row, 4).Value = complaint.Status;
                    worksheet.Cell(row, 5).Value = complaint.ReportDate;
                    worksheet.Cell(row, 6).Value = complaint.CompletionDate ?? DateTime.MinValue;

                    // Color code by status
                    if (complaint.Status == "Resolved")
                    {
                        worksheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.LightGreen;
                    }
                    else if (complaint.Status == "Closed")
                    {
                        worksheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                }

                // Statistics section
                row += 2;
                worksheet.Cell(row, 1).Value = "Statistics";
                worksheet.Cell(row, 1).Style.Font.Bold = true;

                row++;
                worksheet.Cell(row, 1).Value = "Total Complaints:";
                worksheet.Cell(row, 2).Value = stats.TotalComplaints;

                row++;
                worksheet.Cell(row, 1).Value = "Resolved:";
                worksheet.Cell(row, 2).Value = stats.ResolvedComplaints;

                row++;
                worksheet.Cell(row, 1).Value = "Resolution Rate:";
                worksheet.Cell(row, 2).Value = stats.ResolutionRate.ToString("F2") + "%";

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Export to byte array
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    byte[] fileContent = stream.ToArray();

                    string fileName = $"ComplaintReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    Log.Information("Complaint report generated: {FileName}", fileName);

                    return (true, "Report generated successfully", fileContent, fileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating complaint report");
                return (false, $"Error: {ex.Message}", null, null);
            }
        }

        #endregion

        #region Contract Reports

        /// <summary>
        /// Generate contract expiration report
        /// </summary>
        public static (bool Success, string Message, byte[] FileContent, string FileName) GenerateContractExpirationReport(int daysLookout = 30)
        {
            try
            {
                var expiringContracts = ContractDAL.GetExpiringContracts(daysLookout);

                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Expiring Contracts");

                // Header
                worksheet.Cell("A1").Value = $"Contract Expiration Report ({daysLookout} Days Lookout)";
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 14;
                worksheet.Range("A1:F1").Merge();

                worksheet.Cell("A2").Value = "Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Column headers
                int row = 4;
                worksheet.Cell(row, 1).Value = "Contract ID";
                worksheet.Cell(row, 2).Value = "Apartment";
                worksheet.Cell(row, 3).Value = "Type";
                worksheet.Cell(row, 4).Value = "Start Date";
                worksheet.Cell(row, 5).Value = "End Date";
                worksheet.Cell(row, 6).Value = "Days Remaining";

                // Apply header formatting
                for (int col = 1; col <= 6; col++)
                {
                    worksheet.Cell(row, col).Style.Font.Bold = true;
                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightYellow;
                }

                // Data rows
                foreach (var contract in expiringContracts)
                {
                    row++;
                    worksheet.Cell(row, 1).Value = contract.ContractID;
                    worksheet.Cell(row, 2).Value = contract.ApartmentCode;
                    worksheet.Cell(row, 3).Value = contract.ContractType;
                    worksheet.Cell(row, 4).Value = contract.StartDate;
                    worksheet.Cell(row, 5).Value = contract.EndDate;
                    
                    int daysRemaining = (int)(contract.EndDate - DateTime.Now).TotalDays;
                    worksheet.Cell(row, 6).Value = daysRemaining;

                    // Color code urgency
                    if (daysRemaining <= 7)
                    {
                        worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.Red;
                        worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.White;
                    }
                    else if (daysRemaining <= 14)
                    {
                        worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.Orange;
                    }
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Export to byte array
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    byte[] fileContent = stream.ToArray();

                    string fileName = $"ContractExpirationReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    Log.Information("Contract report generated: {FileName}", fileName);

                    return (true, "Report generated successfully", fileContent, fileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating contract report");
                return (false, $"Error: {ex.Message}", null, null);
            }
        }

        #endregion

        #region PDF Export

        /// <summary>
        /// Export apartment list to PDF
        /// </summary>
        public static (bool Success, string Message, byte[] FileContent, string FileName) ExportApartmentsToPDF()
        {
            try
            {
                var apartments = ApartmentDAL.GetAllApartments();

                using (var stream = new MemoryStream())
                {
                    // Create PDF writer
                    var writer = new PdfWriter(stream);
                    var pdf = new PdfDocument(writer);
                    var document = new Document(pdf);

                    // Add title
                    document.Add(new Paragraph("Apartment Management Report")
                        .SetFontSize(16)
                        .SetBold());

                    document.Add(new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                        .SetFontSize(10));

                    // Add table
                    var table = new Table(5, false);
                    table.AddHeaderCell("Apartment Code");
                    table.AddHeaderCell("Type");
                    table.AddHeaderCell("Area (m²)");
                    table.AddHeaderCell("Status");
                    table.AddHeaderCell("Max Residents");

                    foreach (var apartment in apartments)
                    {
                        table.AddCell(apartment.ApartmentCode);
                        table.AddCell(apartment.ApartmentType);
                        table.AddCell(apartment.Area.ToString());
                        table.AddCell(apartment.Status);
                        table.AddCell(apartment.MaxResidents.ToString());
                    }

                    document.Add(table);

                    // Add summary
                    document.Add(new Paragraph("\nSummary Statistics")
                        .SetFontSize(12)
                        .SetBold());

                    int totalApartments = apartments.Count;
                    int occupiedCount = apartments.Count(a => a.Status == "Occupied");
                    int occupancyRate = totalApartments > 0 ? (occupiedCount * 100) / totalApartments : 0;

                    document.Add(new Paragraph($"Total Apartments: {totalApartments}"));
                    document.Add(new Paragraph($"Occupied: {occupiedCount}"));
                    document.Add(new Paragraph($"Occupancy Rate: {occupancyRate}%"));

                    document.Close();

                    byte[] fileContent = stream.ToArray();
                    string fileName = $"ApartmentReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                    Log.Information("Apartments exported to PDF: {FileName}", fileName);
                    return (true, "PDF exported successfully", fileContent, fileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting apartments to PDF");
                return (false, $"Error: {ex.Message}", null, null);
            }
        }

        #endregion

        #region Data Export

        /// <summary>
        /// Export data to CSV format
        /// </summary>
        public static (bool Success, string Message, byte[] FileContent, string FileName) ExportDataToCSV(string dataType)
        {
            try
            {
                var csvContent = GenerateCSVContent(dataType);
                byte[] fileContent = System.Text.Encoding.UTF8.GetBytes(csvContent);
                string fileName = $"{dataType}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                Log.Information("Data exported to CSV: {FileName}", fileName);
                return (true, "CSV exported successfully", fileContent, fileName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting data to CSV");
                return (false, $"Error: {ex.Message}", null, null);
            }
        }

        private static string GenerateCSVContent(string dataType)
        {
            switch (dataType.ToLower())
            {
                case "apartments":
                    return GenerateApartmentsCSV();
                case "residents":
                    return GenerateResidentsCSV();
                case "invoices":
                    return GenerateInvoicesCSV();
                default:
                    return "No data available for export";
            }
        }

        private static string GenerateApartmentsCSV()
        {
            var apartments = ApartmentDAL.GetAllApartments();
            var csv = "ApartmentCode,Type,Area,Status,MaxResidents\n";

            foreach (var apt in apartments)
            {
                csv += $"\"{apt.ApartmentCode}\",\"{apt.ApartmentType}\",{apt.Area},\"{apt.Status}\",{apt.MaxResidents}\n";
            }

            return csv;
        }

        private static string GenerateResidentsCSV()
        {
            var residents = ResidentDAL.GetAllResidents();
            var csv = "FullName,ApartmentID,Status,Phone,Email\n";

            foreach (var resident in residents)
            {
                csv += $"\"{resident.FullName}\",{resident.ApartmentID},\"{resident.Status}\",\"{resident.Phone}\",\"{resident.Email}\"\n";
            }

            return csv;
        }

        private static string GenerateInvoicesCSV()
        {
            var invoices = InvoiceDAL.GetAllInvoices();
            var csv = "InvoiceID,ResidentID,TotalAmount,Status,Month,Year\n";

            foreach (var invoice in invoices)
            {
                csv += $"{invoice.InvoiceID},{invoice.ResidentID},{invoice.TotalAmount},\"{invoice.Status}\",\"{invoice.Month}\",{invoice.Year}\n";
            }

            return csv;
        }

        #endregion
    }
}

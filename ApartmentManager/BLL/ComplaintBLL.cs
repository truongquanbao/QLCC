using ApartmentManager.DAL;
using ApartmentManager.Utilities;
using Serilog;
using System;
using System.Collections.Generic;

namespace ApartmentManager.BLL;

/// <summary>
/// Business Logic Layer for Complaint management
/// </summary>
public class ComplaintBLL
{
    /// <summary>
    /// Create a new complaint with validation
    /// </summary>
    public static (bool Success, string Message, int ComplaintID) CreateComplaint(
        int residentID,
        string title,
        string description,
        string priority = "Medium")
    {
        try
        {
            // Validate input
            if (residentID <= 0)
                return (false, "Please select a valid resident.", 0);

            if (string.IsNullOrWhiteSpace(title))
                return (false, "Complaint title is required.", 0);

            if (title.Length < 10 || title.Length > 200)
                return (false, "Title must be between 10 and 200 characters.", 0);

            if (string.IsNullOrWhiteSpace(description))
                return (false, "Description is required.", 0);

            if (description.Length < 20 || description.Length > 2000)
                return (false, "Description must be between 20 and 2000 characters.", 0);

            var validPriorities = new[] { "Low", "Medium", "High", "Critical" };
            if (!validPriorities.Contains(priority))
                return (false, "Invalid priority level.", 0);

            // Check if resident exists
            var resident = ResidentDAL.GetResidentByID(residentID);
            if (resident == null)
                return (false, "Selected resident does not exist.", 0);

            // Create complaint
            var complaintID = ComplaintDAL.CreateComplaint(
                residentID,
                title,
                description,
                priority,
                "New",
                null,
                DateTime.Now
            );

            if (complaintID > 0)
            {
                Log.Information($"Complaint created: {title} by resident {residentID}");
                return (true, $"Complaint '{title}' created successfully.", complaintID);
            }

            return (false, "Failed to create complaint.", 0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating complaint");
            return (false, $"Error: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Update complaint details
    /// </summary>
    public static (bool Success, string Message) UpdateComplaint(
        int complaintID,
        string title,
        string description,
        string priority)
    {
        try
        {
            if (complaintID <= 0)
                return (false, "Invalid complaint ID.");

            if (string.IsNullOrWhiteSpace(title) || title.Length < 10)
                return (false, "Invalid title.");

            if (string.IsNullOrWhiteSpace(description) || description.Length < 20)
                return (false, "Invalid description.");

            var validPriorities = new[] { "Low", "Medium", "High", "Critical" };
            if (!validPriorities.Contains(priority))
                return (false, "Invalid priority level.");

            var complaint = ComplaintDAL.GetComplaintByID(complaintID);
            if (complaint == null)
                return (false, "Complaint not found.");

            // Cannot update resolved/closed complaints
            if (complaint.Status == "Resolved" || complaint.Status == "Closed")
                return (false, "Cannot update resolved or closed complaints.");

            var success = ComplaintDAL.UpdateComplaint(complaintID, title, description, priority);

            if (success)
            {
                Log.Information($"Complaint updated: {complaintID}");
                return (true, "Complaint updated successfully.");
            }

            return (false, "Failed to update complaint.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating complaint");
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Assign complaint to staff member
    /// </summary>
    public static (bool Success, string Message) AssignComplaint(int complaintID, int? assignedToUserID)
    {
        try
        {
            if (complaintID <= 0)
                return (false, "Invalid complaint ID.");

            var complaint = ComplaintDAL.GetComplaintByID(complaintID);
            if (complaint == null)
                return (false, "Complaint not found.");

            // If assigning, verify user exists
            if (assignedToUserID.HasValue && assignedToUserID > 0)
            {
                var user = UserDAL.GetUserByID(assignedToUserID.Value);
                if (user == null)
                    return (false, "Selected staff member not found.");
            }

            var success = ComplaintDAL.AssignComplaint(complaintID, assignedToUserID, "Assigned");

            if (success)
            {
                Log.Information($"Complaint assigned: {complaintID}");
                return (true, "Complaint assigned successfully.");
            }

            return (false, "Failed to assign complaint.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error assigning complaint");
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Resolve complaint
    /// </summary>
    public static (bool Success, string Message) ResolveComplaint(int complaintID, string resolutionNote)
    {
        try
        {
            if (complaintID <= 0)
                return (false, "Invalid complaint ID.");

            var complaint = ComplaintDAL.GetComplaintByID(complaintID);
            if (complaint == null)
                return (false, "Complaint not found.");

            if (complaint.Status == "Closed")
                return (false, "Cannot resolve a closed complaint.");

            var success = ComplaintDAL.ResolveComplaint(complaintID, "Resolved", resolutionNote, DateTime.Now);

            if (success)
            {
                Log.Information($"Complaint resolved: {complaintID}");
                return (true, "Complaint resolved successfully.");
            }

            return (false, "Failed to resolve complaint.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resolving complaint");
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete complaint
    /// </summary>
    public static (bool Success, string Message) DeleteComplaint(int complaintID)
    {
        try
        {
            if (complaintID <= 0)
                return (false, "Invalid complaint ID.");

            var complaint = ComplaintDAL.GetComplaintByID(complaintID);
            if (complaint == null)
                return (false, "Complaint not found.");

            // Cannot delete resolved/closed complaints (audit trail)
            if (complaint.Status == "Resolved" || complaint.Status == "Closed")
                return (false, "Cannot delete resolved or closed complaints for audit purposes.");

            var success = ComplaintDAL.DeleteComplaint(complaintID);

            if (success)
            {
                Log.Information($"Complaint deleted: {complaintID}");
                return (true, "Complaint deleted successfully.");
            }

            return (false, "Failed to delete complaint.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting complaint");
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get complaint statistics
    /// </summary>
    public static dynamic? GetComplaintStatistics()
    {
        try
        {
            var complaints = ComplaintDAL.GetAllComplaints();
            if (complaints.Count == 0)
                return null;

            int lowPriority = 0;
            int mediumPriority = 0;
            int highPriority = 0;
            int criticalPriority = 0;
            int resolved = 0;
            int pending = 0;

            foreach (var c in complaints)
            {
                switch (c.Priority)
                {
                    case "Low":
                        lowPriority++;
                        break;
                    case "Medium":
                        mediumPriority++;
                        break;
                    case "High":
                        highPriority++;
                        break;
                    case "Critical":
                        criticalPriority++;
                        break;
                }

                if (c.Status == "Resolved" || c.Status == "Closed")
                    resolved++;
                else
                    pending++;
            }

            return new
            {
                TotalComplaints = complaints.Count,
                LowPriority = lowPriority,
                MediumPriority = mediumPriority,
                HighPriority = highPriority,
                CriticalPriority = criticalPriority,
                ResolvedCount = resolved,
                PendingCount = pending,
                ResolutionRate = complaints.Count > 0 ? (resolved * 100.0 / complaints.Count).ToString("F1") + "%" : "0%"
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting complaint statistics");
            return null;
        }
    }
}

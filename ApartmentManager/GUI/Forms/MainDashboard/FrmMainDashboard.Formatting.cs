using ApartmentManager.BLL;
using ApartmentManager.DAL;
using ApartmentManager.DTO;
using ApartmentManager.Utilities;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

public partial class FrmMainDashboard
{
    private static object[][] RowsOrEmpty<T>(IEnumerable<T> items, int columnCount, Func<T, int, object[]> map, string message = "Không có dữ liệu")
    {
        var rows = items.Select(map).ToArray();
        return rows.Length > 0 ? rows : new[] { EmptyRow(columnCount, message) };
    }

    private static object[] EmptyRow(int columnCount, string message)
    {
        var row = Enumerable.Repeat<object>("", columnCount).ToArray();
        row[0] = message;
        return row;
    }

    private static string Display(string? value, string fallback = "-")
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string DateText(DateTime? value)
        => value.HasValue && value.Value > DateTime.MinValue ? value.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "-";

    private static string DateTimeText(DateTime? value)
        => value.HasValue && value.Value > DateTime.MinValue ? value.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : "-";

    private static string Money(decimal value)
        => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string MoneyShort(decimal value)
    {
        if (value >= 1_000_000_000m)
        {
            return $"{value / 1_000_000_000m:0.##}B";
        }

        if (value >= 1_000_000m)
        {
            return $"{value / 1_000_000m:0.##}M";
        }

        return Money(value);
    }

    private static int ChartValue(decimal value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value >= int.MaxValue ? int.MaxValue : (int)value;
    }

    private static string InvoiceCode(InvoiceDTO invoice)
        => $"HD{invoice.Year}{invoice.Month:00}-{invoice.InvoiceID:00000}";

    private static string ViStatus(string? status)
    {
        return (status ?? string.Empty).Trim() switch
        {
            "Active" => "Hoạt động",
            "Inactive" => "Tạm khóa",
            "Pending" => "Chờ duyệt",
            "Approved" => "Đã duyệt",
            "Rejected" => "Từ chối",
            "Paid" => "Đã thanh toán",
            "Partial" or "PartiallyPaid" => "Thanh toán một phần",
            "Unpaid" => "Chưa thanh toán",
            "Overdue" => "Quá hạn",
            "New" => "Mới",
            "Open" => "Mới",
            "InProgress" or "In Progress" => "Đang xử lý",
            "Resolved" => "Đã xử lý",
            "Closed" => "Đã đóng",
            "High" => "Cao",
            "Medium" => "Trung bình",
            "Low" => "Thấp",
            "Occupied" or "Using" or "InUse" => "\u0110ang s\u1eed d\u1ee5ng",
            "Renting" => "Đang thuê",
            "Vacant" or "Empty" or "Available" => "\u0110ang tr\u1ed1ng",
            "Maintenance" => "Bảo trì",
            "Locked" => "Đang khóa",
            "Sent" => "Đã gửi",
            "Draft" => "Nháp",
            "Failed" => "Lỗi",
            "CheckedOut" => "Đã rời",
            "CheckedIn" => "Đã vào",
            "" => "-",
            var other => other
        };
    }

    private static string ResidentLivingStatus(ResidentDTO? resident)
    {
        string residentStatus = Display(resident?.ResidentStatus, "");
        if (residentStatus != "")
        {
            return residentStatus;
        }

        return (resident?.Status ?? string.Empty).Trim() switch
        {
            "Active" => "Đang cư trú",
            "MovedOut" or "Moved Out" => "Chuyển ra",
            "" => "-",
            var other => ViStatus(other)
        };
    }

    private static bool IsActiveStatus(string? status)
        => string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(status, "Hoạt động", StringComparison.OrdinalIgnoreCase);

    private static string ViVehicleType(string? type)
    {
        return (type ?? string.Empty).Trim() switch
        {
            "Car" or "Ô tô" => "Ô tô",
            "Motorbike" or "Motorcycle" or "Xe máy" => "Xe máy",
            "ElectricBike" or "Xe đạp điện" => "Xe đạp điện",
            "Bicycle" or "Xe đạp" => "Xe đạp",
            "" => "-",
            var other => other
        };
    }

    private static string GetDynamicString(dynamic? obj, params string[] names)
    {
        if (obj == null) return "-";

        var type = obj.GetType();

        foreach (string name in names)
        {
            var prop = type.GetProperty(name);
            if (prop == null) continue;

            var value = prop.GetValue(obj);
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                return value.ToString()!;
        }

        return "-";
    }

    private static string UserRoleLabel(string? roleName)
    {
        return (roleName ?? string.Empty).Trim() switch
        {
            "Resident" => "Cư dân",
            "" => "-",
            var other => other
        };
    }

    private static string DbUserStatus(string? status)
    {
        return (status ?? string.Empty).Trim() switch
        {
            "Hoạt động" => "Active",
            "Tạm khóa" => "Inactive",
            "Chờ duyệt" => "Pending",
            "Từ chối" => "Rejected",
            "Đã duyệt" => "Approved",
            "" => "Pending",
            var other => other
        };
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        Span<char> password = stackalloc char[12];
        for (int i = 0; i < password.Length; i++)
        {
            password[i] = chars[Random.Shared.Next(chars.Length)];
        }

        bool hasUpper = false;
        bool hasLower = false;
        bool hasDigit = false;
        bool hasSpecial = false;
        for (int i = 0; i < password.Length; i++)
        {
            char ch = password[i];
            hasUpper |= char.IsUpper(ch);
            hasLower |= char.IsLower(ch);
            hasDigit |= char.IsDigit(ch);
            hasSpecial |= "!@#$%^&*".Contains(ch);
        }

        if (!hasUpper)
        {
            password[0] = 'A';
        }

        if (!hasLower)
        {
            password[1] = 'b';
        }

        if (!hasDigit)
        {
            password[2] = '7';
        }

        if (!hasSpecial)
        {
            password[3] = '!';
        }

        return new string(password);
    }

    private static string AuditLevel(string? action)
    {
        string value = action ?? string.Empty;
        if (value.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Fail", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Lỗi", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Thất bại", StringComparison.OrdinalIgnoreCase))
        {
            return "Lỗi";
        }

        if (value.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Reset", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Reject", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Xóa", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Từ chối", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Cảnh báo", StringComparison.OrdinalIgnoreCase))
        {
            return "Cảnh báo";
        }

        if (value.Contains("Success", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Create", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Insert", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Add", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Tạo", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Cập nhật", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Thêm", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Xử lý", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Hoàn thành", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Thành công", StringComparison.OrdinalIgnoreCase))
        {
            return "Thành công";
        }

        return "Thông tin";
    }

    private static List<SystemConfigRow> GetSystemConfigs()
    {
        var rows = new List<SystemConfigRow>();
        try
        {
            const string query = @"
                SELECT c.ConfigKey, c.ConfigValue, c.Description, c.UpdatedAt, ISNULL(u.Username, '') AS UpdatedBy
                FROM SystemConfig c
                LEFT JOIN Users u ON c.UpdatedBy = u.UserID
                ORDER BY c.ConfigKey";

            using var connection = DatabaseHelper.CreateConnection();
            using var command = new SqlCommand(query, connection);
            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new SystemConfigRow
                {
                    ConfigKey = reader.GetString(0),
                    ConfigValue = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    UpdatedAt = reader.GetDateTime(3),
                    UpdatedBy = reader.GetString(4)
                });
            }
        }
        catch
        {
            // Keep the dashboard usable if the optional config table has not been migrated yet.
        }

        return rows;
    }

    private static string ConfigValue(List<SystemConfigRow> configs, string key, string fallback)
        => configs.FirstOrDefault(c => string.Equals(c.ConfigKey, key, StringComparison.OrdinalIgnoreCase))?.ConfigValue ?? fallback;

    private sealed class SystemConfigRow
    {
        public string ConfigKey { get; init; } = "";
        public string ConfigValue { get; init; } = "";
        public string Description { get; init; } = "";
        public DateTime UpdatedAt { get; init; }
        public string UpdatedBy { get; init; } = "";
    }

    private static DateTime? ParseDashboardDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] formats =
        {
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "MM/dd/yyyy HH:mm:ss"
        };

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var exact))
        {
            return exact;
        }

        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var local))
        {
            return local;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var invariant)
            ? invariant
            : null;
    }

    private static bool IsBackupOverdue(DateTime? lastBackupAt, int warningDays)
    {
        int threshold = warningDays > 0 ? warningDays : 7;
        return !lastBackupAt.HasValue || lastBackupAt.Value < DateTime.Now.AddDays(-threshold);
    }

    private static string BackupDisplayText(string rawValue)
        => ParseDashboardDate(rawValue) is DateTime parsed
            ? DateTimeText(parsed)
            : (string.IsNullOrWhiteSpace(rawValue) ? "Chưa có dữ liệu" : rawValue);
}

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

public class CanHo
{
    public string MaCanHo { get; set; } = string.Empty;
    public string Tang { get; set; } = string.Empty;
    public string LoaiCanHo { get; set; } = string.Empty;
    public double DienTich { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string MaCuDan { get; set; } = string.Empty;
}

public class CuDan
{
    public string MaCuDan { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string CCCD { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string MaCanHo { get; set; } = string.Empty;
    public DateTime NgayVao { get; set; }
}

public class PhiDichVu
{
    public string MaPhieu { get; set; } = string.Empty;
    public string MaCanHo { get; set; } = string.Empty;
    public int Thang { get; set; }
    public int Nam { get; set; }
    public double PhiQuanLy { get; set; }
    public double PhiDienNuoc { get; set; }
    public double PhiGuiXe { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public double TongPhi => PhiQuanLy + PhiDienNuoc + PhiGuiXe;
}

public partial class FrmMainDashboard : Form
{
    private const int SidebarWidth = 238;
    private const int HeaderHeight = 64;
    private const int FooterHeight = 64;
    private static readonly Font GridBadgeFont = ModernUi.Font(8.4f, FontStyle.Bold);

    private readonly List<CanHo> _apartments = new();
    private readonly List<CuDan> _residents = new();
    private readonly List<PhiDichVu> _fees = new();
    private readonly Dictionary<string, Button> _navButtons = new();
    private UserSession? _session;

    private Panel _sidebar = null!;
    private Panel _content = null!;
    private Panel _footer = null!;
    private Label _clockLabel = null!;
    private string _activePage = "dashboard";
    private Timer _clockTimer = null!;
    private ContextMenuStrip? _quickActionMenu;
    private string? _quickActionMenuKey;
    private Control? _quickActionAnchor;
    private string? _pendingQuickActionPage;
    private string? _pendingQuickActionMode;

    private sealed class PaginationState
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; private set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)Math.Max(1, PageSize)));
        public int StartIndex => TotalRecords == 0 ? 0 : (CurrentPage - 1) * PageSize;

        public void SetPageSize(int pageSize)
        {
            PageSize = pageSize > 0 ? pageSize : 10;
            CurrentPage = 1;
        }

        public void ClampCurrentPage()
        {
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages));
        }

        public void MoveToFirstPage() => CurrentPage = 1;
        public void MoveToPreviousPage() => CurrentPage = Math.Max(1, CurrentPage - 1);
        public void MoveToNextPage() => CurrentPage = Math.Min(TotalPages, CurrentPage + 1);
        public void MoveToLastPage() => CurrentPage = TotalPages;
    }

    private sealed class PaginationControls
    {
        public Label SummaryLabel { get; init; } = null!;
        public Button FirstButton { get; init; } = null!;
        public Button PreviousButton { get; init; } = null!;
        public Button PageButton { get; init; } = null!;
        public Button NextButton { get; init; } = null!;
        public Button LastButton { get; init; } = null!;
        public ComboBox PageSizeCombo { get; init; } = null!;
    }

    private sealed class VehicleViewModel
    {
        public int VehicleID { get; set; }
        public int ResidentID { get; set; }
        public string VehicleCode { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int YearMade { get; set; } = DateTime.Today.Year;
        public string Color { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
        public string Apartment { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public DateTime ExpiredAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public FrmMainDashboard()
    {
        _session = SessionManager.GetSession();
        InitSampleData();
        InitializeComponent();
        BuildShell();
        Shown += (_, _) =>
        {
            Navigate(GetDefaultPage());
        };
    }

}

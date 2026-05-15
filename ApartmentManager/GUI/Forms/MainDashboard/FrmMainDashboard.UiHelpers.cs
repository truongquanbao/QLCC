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
    private static int AddRow(Control parent, int y, int gap, params Control[] controls)
    {
        return LayoutWrappedControls(parent, 18, y, gap, 12, true, controls);
    }

    private static int ReflowRow(Control parent, int y, int gap, params Control[] controls)
    {
        return LayoutWrappedControls(parent, 18, y, gap, 12, false, controls);
    }

    private static int LayoutWrappedControls(Control parent, int startX, int top, int gapX, int gapY, bool attachToParent, params Control[] controls)
    {
        int viewportWidth = parent.ClientSize.Width > 0 ? parent.ClientSize.Width : parent.Width;
        int maxRight = Math.Max(startX, viewportWidth - 18 - SystemInformation.VerticalScrollBarWidth);
        int x = startX;
        int y = top;
        int rowHeight = 0;

        foreach (var control in controls)
        {
            if (x > startX && x + control.Width > maxRight)
            {
                x = startX;
                y += rowHeight + gapY;
                rowHeight = 0;
            }

            control.Location = new Point(x, y);
            if (attachToParent && control.Parent != parent)
            {
                parent.Controls.Add(control);
            }

            x += control.Width + gapX;
            rowHeight = Math.Max(rowHeight, control.Height);
        }

        return y + rowHeight;
    }

    private static Panel AddDashboardActionBar(Control page, int y, int width, params Button[] buttons)
    {
        var actionBar = new Panel
        {
            Location = new Point(18, y),
            Size = new Size(width, 44),
            BackColor = ModernUi.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        foreach (var button in buttons)
        {
            button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            actionBar.Controls.Add(button);
        }

        void LayoutActions()
        {
            int right = actionBar.ClientSize.Width;
            for (int i = buttons.Length - 1; i >= 0; i--)
            {
                var button = buttons[i];
                button.SetBounds(Math.Max(0, right - button.Width), 4, button.Width, 36);
                right = button.Left - 12;
            }
        }

        actionBar.Resize += (_, _) => LayoutActions();
        LayoutActions();
        page.Controls.Add(actionBar);
        return actionBar;
    }

    private static (int LeftWidth, int RightWidth) DashboardListDetailWidths(int width, float leftRatio = 0.62f, int gap = 12, int minLeft = 620, int minRight = 340)
    {
        int maxLeft = Math.Max(minLeft, width - gap - minRight);
        int left = Math.Max(minLeft, (int)(width * leftRatio));
        left = Math.Min(left, maxLeft);
        return (left, width - left - gap);
    }

    private static int LayoutSearchWithRefresh(Control search, Control refreshButton, int pageWidth, int compactTop, int normalTop, int preferredLeft)
    {
        if (pageWidth < 1220)
        {
            refreshButton.Location = new Point(pageWidth - 124, compactTop);
            search.SetBounds(16, compactTop, Math.Max(260, refreshButton.Left - 28), search.Height);
            return 132;
        }

        refreshButton.Location = new Point(pageWidth - 124, normalTop);
        int searchLeft = Math.Max(preferredLeft, refreshButton.Left - 12 - Math.Max(260, pageWidth - 980));
        search.SetBounds(searchLeft, normalTop, Math.Max(260, refreshButton.Left - 12 - searchLeft), search.Height);
        return 86;
    }

    private static RoundedPanel ResidentCard(string title, string value, string detail, Color accent, string icon, string action, int width)
    {
        var card = ModernUi.CardPanel();
        card.Size = new Size(width, 126);

        var titleLabel = ModernUi.Label(title.ToUpperInvariant(), 8.6f, FontStyle.Bold, accent);
        titleLabel.Location = new Point(16, 12);
        titleLabel.Size = new Size(width - 32, 22);
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        titleLabel.AutoEllipsis = true;
        card.Controls.Add(titleLabel);

        var circle = new CircleLabel
        {
            Text = icon,
            CircleColor = accent,
            ForeColor = Color.White,
            Font = ModernUi.Font(21f, FontStyle.Bold),
            Size = new Size(58, 58),
            Location = new Point(18, 48),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(circle);

        var valueLabel = ModernUi.Label(value, value.Length > 16 ? 10.5f : 13.5f, FontStyle.Bold, ModernUi.Navy);
        valueLabel.Location = new Point(90, 40);
        valueLabel.Size = new Size(width - 106, 30);
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.AutoEllipsis = true;
        card.Controls.Add(valueLabel);

        var detailLabel = ModernUi.Label(detail, 8.5f, FontStyle.Regular, ModernUi.Text);
        detailLabel.Location = new Point(90, 68);
        detailLabel.Size = new Size(width - 106, 36);
        detailLabel.TextAlign = ContentAlignment.MiddleLeft;
        detailLabel.AutoEllipsis = true;
        card.Controls.Add(detailLabel);

        var actionLabel = ModernUi.Label(action + " →", 8.8f, FontStyle.Bold, ModernUi.Blue);
        actionLabel.Location = new Point(90, 102);
        actionLabel.Size = new Size(width - 106, 18);
        actionLabel.TextAlign = ContentAlignment.MiddleLeft;
        actionLabel.AutoEllipsis = true;
        card.Controls.Add(actionLabel);

        return card;
    }

    private static void AddFilter(Control parent, string label, string selected, int x, int y = 8)
    {
        var lbl = ModernUi.Label(label, 8.7f, FontStyle.Bold, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(170, 18);
        parent.Controls.Add(lbl);
        var combo = ModernUi.ComboBox(new[] { selected, "Tất cả", "Tòa A", "Tòa B", "Tòa C" }, 210);
        combo.Location = new Point(x, y + 22);
        parent.Controls.Add(combo);
    }

    private static void AddDetailField(Control parent, string label, string value, int y)
    {
        int labelW = Math.Min(126, Math.Max(104, parent.Width / 3));
        int inputX = 18 + labelW + 12;
        int inputW = Math.Max(120, parent.Width - inputX - 18);

        var lbl = ModernUi.Label(label, 8.8f, FontStyle.Regular, ModernUi.Text);
        lbl.Location = new Point(18, y);
        lbl.Size = new Size(labelW, 26);
        parent.Controls.Add(lbl);

        var input = ModernUi.TextBox("", inputW);
        input.Text = value;
        input.Location = new Point(inputX, y);
        input.Height = 28;
        parent.Controls.Add(input);
    }

    private static void AddSmallField(Control parent, string label, string value, int x, int y)
    {
        var lbl = ModernUi.Label(label, 8.8f, FontStyle.Regular, ModernUi.Text);
        lbl.Location = new Point(x, y);
        lbl.Size = new Size(95, 24);
        parent.Controls.Add(lbl);
        int inputX = x + 108;
        int inputW = Math.Max(120, parent.Width - inputX - 18);
        var input = ModernUi.TextBox("", inputW);
        input.Text = value;
        input.Location = new Point(inputX, y);
        input.Height = 28;
        parent.Controls.Add(input);
    }

    private static void AddFormActions(Control parent, int x, int y)
    {
        var add = ModernUi.Button("⊕  Thêm mới", ModernUi.Green, 116, 34);
        add.Location = new Point(x, y);
        parent.Controls.Add(add);
        var edit = ModernUi.Button("✎  Sửa", ModernUi.Orange, 110, 34);
        edit.Location = new Point(x + 126, y);
        parent.Controls.Add(edit);
        var delete = ModernUi.Button("×  Xóa", ModernUi.Red, 110, 34);
        delete.Location = new Point(x + 246, y);
        parent.Controls.Add(delete);
    }

    private static void AddResponsiveFormActions(Control parent, int y)
    {
        const int x = 18;
        const int gap = 8;
        int buttonW = Math.Max(82, (parent.Width - x * 2 - gap * 2) / 3);
        string addText = buttonW < 104 ? "⊕ Thêm" : "⊕  Thêm";
        string editText = buttonW < 104 ? "✎ Sửa" : "✎  Sửa";
        string deleteText = buttonW < 104 ? "× Xóa" : "×  Xóa";

        var add = ModernUi.Button(addText, ModernUi.Green, buttonW, 32);
        add.Location = new Point(x, y);
        parent.Controls.Add(add);

        var edit = ModernUi.Button(editText, ModernUi.Orange, buttonW, 32);
        edit.Location = new Point(x + buttonW + gap, y);
        parent.Controls.Add(edit);

        var delete = ModernUi.Button(deleteText, ModernUi.Red, buttonW, 32);
        delete.Location = new Point(x + (buttonW + gap) * 2, y);
        parent.Controls.Add(delete);
    }

    private static void BindTileClick(Control tile, EventHandler handler)
    {
        tile.Click += handler;
        foreach (Control child in tile.Controls)
        {
            child.Click += handler;
        }
    }

    private static bool HeaderContains(string header, params string[] keywords)
        => keywords.Any(keyword => header.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    private static bool ShouldLeftAlignColumn(string header)
        => HeaderContains(header, "Nội dung", "Hạng mục", "Mô tả", "Ghi chú", "Địa chỉ", "Họ tên", "Người gửi", "Người phụ trách", "Tiêu đề");

    private static float GridFillWeight(string header)
    {
        if (HeaderContains(header, "STT", "No."))
        {
            return 42f;
        }

        if (HeaderContains(header, "ID", "Mã"))
        {
            return 78f;
        }

        if (HeaderContains(header, "Ngày", "Thời gian", "Kỳ"))
        {
            return 96f;
        }

        if (HeaderContains(header, "Nội dung", "Mô tả", "Ghi chú", "Địa chỉ"))
        {
            return 180f;
        }

        if (HeaderContains(header, "Hạng mục", "Tiêu đề", "Loại"))
        {
            return 126f;
        }

        if (HeaderContains(header, "Cư dân", "Người", "Chủ hộ", "Tài khoản", "Email", "Điện thoại"))
        {
            return 116f;
        }

        if (HeaderContains(header, "Căn hộ", "Tòa nhà", "Block", "Phương tiện"))
        {
            return 92f;
        }

        if (HeaderContains(header, "Trạng thái", "Ưu tiên", "Mức độ"))
        {
            return 86f;
        }

        if (HeaderContains(header, "VNĐ", "Tiền", "Doanh thu", "Phí"))
        {
            return 108f;
        }

        return 100f;
    }

    private static int GridMinimumWidth(string header)
    {
        if (HeaderContains(header, "STT", "No."))
        {
            return 52;
        }

        if (HeaderContains(header, "ID", "Mã"))
        {
            return 84;
        }

        if (HeaderContains(header, "Ngày", "Thời gian", "Kỳ"))
        {
            return 118;
        }

        if (HeaderContains(header, "Nội dung", "Mô tả", "Ghi chú", "Địa chỉ"))
        {
            return 180;
        }

        if (HeaderContains(header, "Cư dân", "Người", "Email", "Điện thoại"))
        {
            return 116;
        }

        if (HeaderContains(header, "Trạng thái", "Ưu tiên", "Mức độ"))
        {
            return 96;
        }

        return 90;
    }

    private static void ConfigureGridColumns(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.FillWeight = GridFillWeight(column.HeaderText);
            column.MinimumWidth = GridMinimumWidth(column.HeaderText);
            column.DefaultCellStyle.Alignment = ShouldLeftAlignColumn(column.HeaderText)
                ? DataGridViewContentAlignment.MiddleLeft
                : DataGridViewContentAlignment.MiddleCenter;
        }

        grid.ClearSelection();
    }

    private static RoundedPanel AddActionTile(Control parent, string icon, string title, string subtitle, Color color, int x, int y, Color? textColor = null, int width = 178, int height = 72)
    {
        bool lightTile = color.GetBrightness() > 0.92f;
        var tile = ModernUi.CardPanel(12);
        tile.Location = new Point(x, y);
        tile.Size = new Size(width, height);
        tile.BackColor = color;
        tile.BorderColor = lightTile ? ModernUi.Border : color;
        tile.Cursor = Cursors.Hand;

        Color foreground = textColor ?? Color.White;

        var iconLabel = ModernUi.Label(icon, 11f, FontStyle.Bold, foreground);
        iconLabel.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        iconLabel.BackColor = Color.Transparent;
        iconLabel.Cursor = Cursors.Hand;
        iconLabel.TextAlign = ContentAlignment.MiddleCenter;

        var titleLabel = ModernUi.Label(title, 8.8f, FontStyle.Bold, foreground);
        titleLabel.BackColor = Color.Transparent;
        titleLabel.Cursor = Cursors.Hand;
        titleLabel.AutoEllipsis = false;
        titleLabel.TextAlign = ContentAlignment.MiddleCenter;

        iconLabel.SetBounds(0, 7, width, 18);
        titleLabel.SetBounds(8, 30, width - 16, 26);

        tile.Controls.Add(iconLabel);
        tile.Controls.Add(titleLabel);

        Color original = tile.BackColor;
        tile.MouseEnter += (_, _) => tile.BackColor = lightTile ? ModernUi.LightBlue : ControlPaint.Light(original, 0.08f);
        tile.MouseLeave += (_, _) => tile.BackColor = original;
        foreach (Control child in tile.Controls)
        {
            child.MouseEnter += (_, _) => tile.BackColor = lightTile ? ModernUi.LightBlue : ControlPaint.Light(original, 0.08f);
            child.MouseLeave += (_, _) => tile.BackColor = original;
        }

        parent.Controls.Add(tile);
        return tile;
    }

    private static DataGridView CreateGrid(string[] columns, object[][] rows)
    {
        var grid = ModernUi.Grid();
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ScrollBars = ScrollBars.Vertical;
        grid.AllowUserToResizeColumns = false;
        grid.AllowUserToResizeRows = false;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.RowTemplate.Height = 36;
        grid.ColumnHeadersHeight = 38;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.DefaultCellStyle.Font = ModernUi.Font(8.1f);
        grid.ColumnHeadersDefaultCellStyle.Font = ModernUi.Font(8.2f, FontStyle.Bold);
        grid.ShowCellToolTips = true;
        var table = new DataTable();
        foreach (var column in columns)
        {
            table.Columns.Add(column);
        }

        foreach (var row in rows)
        {
            table.Rows.Add(row);
        }

        grid.DataSource = table;
        grid.DataBindingComplete += (_, _) =>
        {
            ConfigureGridColumns(grid);
        };
        grid.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                e.ToolTipText = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
            }
        };
        grid.CellFormatting += (_, e) =>
        {
            ApplyGridCellStyle(e);

            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Value is not string text)
            {
                return;
            }

            string header = grid.Columns[e.ColumnIndex].HeaderText;
            if (!HeaderContains(header, "Nội dung", "Mô tả", "Ghi chú", "Địa chỉ", "Tiêu đề"))
            {
                return;
            }

            string singleLine = text.Replace("\r", " ").Replace("\n", " ").Trim();
            if (singleLine.Length <= 36)
            {
                return;
            }

            e.Value = $"{singleLine[..33]}...";
            e.FormattingApplied = true;
        };
        return grid;
    }

    private static void SetGridData(DataGridView grid, string[] columns, object[][] rows)
    {
        var table = new DataTable();
        foreach (var column in columns)
        {
            table.Columns.Add(column);
        }

        foreach (var row in rows)
        {
            table.Rows.Add(row);
        }

        grid.DataSource = table;
    }

    private static void ApplyGridCellStyle(DataGridViewCellFormattingEventArgs e)
    {
        if (e.Value == null)
        {
            return;
        }

        string value = e.Value.ToString() ?? string.Empty;
        (Color fore, Color back)? style = value switch
        {
            "Cao" or "Quá hạn" => (ModernUi.Red, Color.FromArgb(255, 235, 235)),
            "Từ chối" or "Tạm khóa" or "Hết hạn" or "Hỏng" or "Lỗi" => (ModernUi.Red, Color.FromArgb(255, 235, 235)),
            "Trung bình" or "Chưa thanh toán" or "Cần bảo trì" or "Sắp đến hạn" or "Cảnh báo" => (ModernUi.Orange, Color.FromArgb(255, 244, 224)),
            "Thấp" or "Đã thanh toán" or "Đã lên lịch" or "Đang sử dụng" or "Đang thuê" or "Hoạt động" or "Đã duyệt" or "Đã vào" or "Đã rời" or "Tốt" or "Thành công" or "Đã sao lưu" => (ModernUi.Green, Color.FromArgb(232, 248, 235)),
            "Đang xử lý" or "Đang cư trú" or "Chờ duyệt" or "Chờ xử lý" or "Thông tin" => (ModernUi.Blue, Color.FromArgb(232, 241, 255)),
            "Mới" => (ModernUi.Purple, Color.FromArgb(243, 232, 255)),
            "Đang trống" => (ModernUi.Teal, Color.FromArgb(229, 247, 250)),
            "Bảo trì" => (ModernUi.Orange, Color.FromArgb(255, 244, 224)),
            "Đang khóa" or "Chuyển ra" => (ModernUi.Muted, Color.FromArgb(241, 245, 249)),
            _ => null
        };

        if (style == null)
        {
            return;
        }

        e.CellStyle.ForeColor = style.Value.fore;
        e.CellStyle.BackColor = style.Value.back;
        e.CellStyle.SelectionForeColor = style.Value.fore;
        e.CellStyle.SelectionBackColor = ControlPaint.Light(style.Value.back, 0.12f);
        e.CellStyle.Font = GridBadgeFont;
    }

    private static void RenderResidentLinkedDetails(Control parent, ResidentDTO? resident, ApartmentDTO? apartment, IReadOnlyList<ResidentDTO> apartmentResidents)
    {
        parent.SuspendLayout();
        parent.Controls.Clear();

        if (resident == null)
        {
            var empty = ModernUi.Label("Không có cư dân phù hợp với bộ lọc hiện tại.", 10f, FontStyle.Regular, ModernUi.Muted);
            empty.Location = new Point(16, 18);
            empty.Size = new Size(parent.Width - 32, 28);
            parent.Controls.Add(empty);
            parent.ResumeLayout();
            return;
        }

        string initials = string.Concat(
            Display(resident.FullName, "Cư dân")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Reverse()
                .Select(part => char.ToUpperInvariant(part[0])));
        if (string.IsNullOrWhiteSpace(initials))
        {
            initials = "CD";
        }

        var avatar = new CircleLabel
        {
            Text = initials,
            CircleColor = ModernUi.Orange,
            ForeColor = Color.White,
            Font = ModernUi.Font(17f, FontStyle.Bold),
            Location = new Point(16, 12),
            Size = new Size(64, 64),
            TextAlign = ContentAlignment.MiddleCenter
        };
        parent.Controls.Add(avatar);

        string residentStatusText = ResidentLivingStatus(resident);
        Color badgeBack = residentStatusText.IndexOf("đang", StringComparison.CurrentCultureIgnoreCase) >= 0
            ? Color.FromArgb(230, 248, 236)
            : residentStatusText.IndexOf("tạm", StringComparison.CurrentCultureIgnoreCase) >= 0
                ? Color.FromArgb(255, 244, 221)
                : Color.FromArgb(232, 241, 255);
        Color badgeFore = residentStatusText.IndexOf("đang", StringComparison.CurrentCultureIgnoreCase) >= 0
            ? ModernUi.Green
            : residentStatusText.IndexOf("tạm", StringComparison.CurrentCultureIgnoreCase) >= 0
                ? ModernUi.Orange
                : ModernUi.Blue;
        var statusBadge = new Label
        {
            Text = residentStatusText,
            AutoSize = false,
            BackColor = badgeBack,
            ForeColor = badgeFore,
            Font = ModernUi.Font(8.2f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(Math.Max(96, parent.Width - 106), 16),
            Size = new Size(86, 24)
        };
        parent.Controls.Add(statusBadge);

        var title = ModernUi.Label(Display(resident.FullName), 11.5f, FontStyle.Bold, ModernUi.Navy);
        title.Location = new Point(96, 14);
        title.Size = new Size(Math.Max(120, parent.Width - 200), 24);
        title.AutoEllipsis = true;
        parent.Controls.Add(title);

        string apartmentText = apartment == null
            ? $"Căn hộ liên kết: {Display(resident.ApartmentCode)}"
            : $"Căn hộ liên kết: {Display(resident.ApartmentCode)} · {BuildingShort(apartment.BuildingName)} / {BlockShort(apartment.BlockName)} / Tầng {apartment.FloorNumber?.ToString("00") ?? "-"}";
        var subtitle = ModernUi.Label(apartmentText, 8.8f, FontStyle.Regular, ModernUi.Muted);
        subtitle.Location = new Point(96, 40);
        subtitle.Size = new Size(Math.Max(150, parent.Width - 116), 34);
        subtitle.AutoEllipsis = true;
        parent.Controls.Add(subtitle);

        int infoWidth = (parent.Width - 44) / 2;
        AddResidentInfoBlock(parent, "Mã cư dân", $"CD{resident.ResidentID:0000}", 16, 92, infoWidth);
        AddResidentInfoBlock(parent, "Căn hộ", Display(resident.ApartmentCode), 28 + infoWidth, 92, infoWidth);
        AddResidentInfoBlock(parent, "Số điện thoại", Display(resident.Phone), 16, 134, infoWidth);
        AddResidentInfoBlock(parent, "Tình trạng", residentStatusText, 28 + infoWidth, 134, infoWidth);
        AddResidentInfoBlock(parent, "CCCD", Display(resident.CCCD), 16, 176, infoWidth);
        AddResidentInfoBlock(parent, "Vai trò", Display(resident.RelationshipWithOwner), 28 + infoWidth, 176, infoWidth);
        AddResidentInfoBlock(parent, "Email", Display(resident.Email), 16, 218, infoWidth);
        AddResidentInfoBlock(parent, "Ngày vào ở", DateText(resident.StartDate ?? resident.MoveInDate), 28 + infoWidth, 218, infoWidth);

        var apartmentCard = ModernUi.CardPanel(6);
        apartmentCard.Location = new Point(16, 266);
        apartmentCard.Size = new Size(parent.Width - 32, 102);
        parent.Controls.Add(apartmentCard);

        var apartmentTitle = ModernUi.Label("THÔNG TIN CĂN HỘ", 8.7f, FontStyle.Bold, ModernUi.Blue);
        apartmentTitle.Location = new Point(12, 10);
        apartmentTitle.Size = new Size(190, 20);
        apartmentCard.Controls.Add(apartmentTitle);

        string apartmentLine1 = apartment == null
            ? "Chưa tìm thấy thông tin căn hộ."
            : $"{BuildingShort(apartment.BuildingName)} / {BlockShort(apartment.BlockName)} / Tầng {apartment.FloorNumber?.ToString("00") ?? "-"}";
        var apartmentInfo1 = ModernUi.Label(apartmentLine1, 9.1f, FontStyle.Bold, ModernUi.Text);
        apartmentInfo1.Location = new Point(12, 34);
        apartmentInfo1.Size = new Size(apartmentCard.Width - 24, 22);
        apartmentCard.Controls.Add(apartmentInfo1);

        string apartmentLine2 = apartment == null
            ? "-"
            : $"Loại căn: {ApartmentTypeText(apartment.ApartmentType)} · Diện tích: {apartment.Area.ToString("N2", CultureInfo.InvariantCulture)} m²";
        var apartmentInfo2 = ModernUi.Label(apartmentLine2, 8.8f, FontStyle.Regular, ModernUi.Text);
        apartmentInfo2.Location = new Point(12, 56);
        apartmentInfo2.Size = new Size(apartmentCard.Width - 24, 18);
        apartmentCard.Controls.Add(apartmentInfo2);

        string apartmentLine3 = apartment == null
            ? "-"
            : $"Trạng thái căn: {ViStatus(apartment.Status)} · Sức chứa: {apartmentResidents.Count}/{Math.Max(1, apartment.MaxResidents)} người";
        var apartmentInfo3 = ModernUi.Label(apartmentLine3, 8.8f, FontStyle.Regular, ModernUi.Text);
        apartmentInfo3.Location = new Point(12, 74);
        apartmentInfo3.Size = new Size(apartmentCard.Width - 24, 18);
        apartmentCard.Controls.Add(apartmentInfo3);

        var roommatesTitle = ModernUi.Label($"CƯ DÂN CÙNG CĂN HỘ ({apartmentResidents.Count:N0})", 9f, FontStyle.Bold, ModernUi.Blue);
        int roommatesTop = 404;
        int roommatesHeight = Math.Max(92, parent.Height - roommatesTop - 18);
        roommatesTitle.Location = new Point(16, roommatesTop - 24);
        roommatesTitle.Size = new Size(parent.Width - 32, 22);
        parent.Controls.Add(roommatesTitle);

        var roommatesGrid = CreateGrid(
            new[] { "Mã cư dân", "Họ tên", "Vai trò", "Tình trạng" },
            RowsOrEmpty(apartmentResidents, 4, (mate, _) => new object[]
            {
                $"CD{mate.ResidentID:0000}",
                Display(mate.FullName),
                Display(mate.RelationshipWithOwner),
                ResidentLivingStatus(mate)
            }, "Không có cư dân cùng căn"));
        roommatesGrid.Location = new Point(16, roommatesTop);
        roommatesGrid.Size = new Size(parent.Width - 32, roommatesHeight);
        roommatesGrid.ColumnHeadersHeight = 34;
        roommatesGrid.RowTemplate.Height = 32;
        roommatesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        roommatesGrid.ScrollBars = ScrollBars.Vertical;
        roommatesGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        roommatesGrid.ShowCellToolTips = true;
        if (roommatesGrid.Columns.Count >= 4)
        {
            roommatesGrid.Columns[0].Width = 84;
            roommatesGrid.Columns[0].MinimumWidth = 84;
            roommatesGrid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            roommatesGrid.Columns[1].MinimumWidth = 120;
            roommatesGrid.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            roommatesGrid.Columns[1].DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
            roommatesGrid.Columns[2].Width = 84;
            roommatesGrid.Columns[2].MinimumWidth = 84;
            roommatesGrid.Columns[3].Width = 92;
            roommatesGrid.Columns[3].MinimumWidth = 92;
        }
        roommatesGrid.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1)
            {
                e.ToolTipText = Convert.ToString(roommatesGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? string.Empty;
            }
        };
        int[] roommateWeights = { 84, 150, 86, 94 };
        for (int i = 0; i < roommatesGrid.Columns.Count && i < roommateWeights.Length; i++)
        {
            roommatesGrid.Columns[i].FillWeight = roommateWeights[i];
        }
        parent.Controls.Add(roommatesGrid);

        parent.ResumeLayout();
    }

    private static void AddResidentInfoBlock(Control parent, string label, string value, int x, int y, int width)
    {
        var caption = ModernUi.Label(label.ToUpperInvariant(), 8.1f, FontStyle.Bold, ModernUi.Muted);
        caption.Location = new Point(x, y);
        caption.Size = new Size(width, 16);
        caption.AutoEllipsis = true;
        parent.Controls.Add(caption);

        var text = ModernUi.Label(value, 9.4f, FontStyle.Bold, ModernUi.Text);
        text.Location = new Point(x, y + 16);
        text.Size = new Size(width, 22);
        text.AutoEllipsis = true;
        parent.Controls.Add(text);
    }

    private static void AddResidentProfile(Control parent, ResidentDTO? resident)
    {
        var avatar = new CircleLabel
        {
            Text = "●",
            CircleColor = Color.FromArgb(152, 163, 179),
            ForeColor = Color.White,
            Font = ModernUi.Font(32f),
            Size = new Size(120, 120),
            Location = new Point(26, 56)
        };
        parent.Controls.Add(avatar);
        var choose = ModernUi.Button("▣  Chọn ảnh", ModernUi.Blue, 110, 30);
        choose.Location = new Point(32, 190);
        parent.Controls.Add(choose);
        var remove = ModernUi.OutlineButton("×  Xóa ảnh", 110, 30);
        remove.Location = new Point(32, 226);
        remove.ForeColor = ModernUi.Red;
        parent.Controls.Add(remove);

        int fx = 200;
        AddSmallField(parent, "Mã cư dân:", resident == null ? "" : $"CD{resident.ResidentID:0000}", fx, 44);
        AddSmallField(parent, "Họ và tên:", Display(resident?.FullName, ""), fx, 82);
        AddSmallField(parent, "Ngày sinh:", DateText(resident?.DOB), fx, 120);
        AddSmallField(parent, "CCCD:", Display(resident?.CCCD, ""), fx, 158);
        AddSmallField(parent, "Email:", Display(resident?.Email, ""), fx, 196);
        AddSmallField(parent, "Số điện thoại:", Display(resident?.Phone, ""), fx, 234);
        AddSmallField(parent, "Địa chỉ thường trú:", Display(resident?.AddressRegistration, ""), fx, 272);
        AddSmallField(parent, "Căn hộ liên kết:", Display(resident?.ApartmentCode, ""), fx, 310);
        AddSmallField(parent, "Vai trò trong căn hộ:", Display(resident?.RelationshipWithOwner, ""), fx, 348);
        AddSmallField(parent, "Tình trạng cư trú:", resident == null ? "" : ViStatus(resident.Status), fx, 386);

        var historyTitle = ModernUi.Label("Lịch sử cư trú", 9.5f, FontStyle.Bold, ModernUi.Blue);
        historyTitle.Location = new Point(24, 430);
        historyTitle.Size = new Size(180, 24);
        parent.Controls.Add(historyTitle);
        var history = CreateGrid(
            new[] { "STT", "Căn hộ", "Vai trò", "Tình trạng", "Ngày bắt đầu", "Ngày kết thúc", "Ghi chú" },
            resident == null
                ? new[] { EmptyRow(7, "Không có dữ liệu") }
                : new object[][]
                {
                    new object[]
                    {
                        1,
                        Display(resident.ApartmentCode),
                        Display(resident.RelationshipWithOwner),
                        ViStatus(resident.Status),
                        DateText(resident.StartDate ?? resident.MoveInDate),
                        DateText(resident.EndDate ?? resident.MoveOutDate),
                        Display(resident.Note, "")
                    }
                });
        history.Location = new Point(18, 458);
        history.Size = new Size(parent.Width - 36, 66);
        parent.Controls.Add(history);
    }

    private static void AddComplaintDetail(Control parent, dynamic complaint)
    {
        parent.Controls.Clear();

        int pad = 18;
        int contentW = parent.Width - pad * 2;

        var sectionTitle = ModernUi.Label("Thông tin phản ánh", 10f, FontStyle.Bold, ModernUi.Blue);
        sectionTitle.Location = new Point(pad, 42);
        sectionTitle.Size = new Size(contentW, 24);
        parent.Controls.Add(sectionTitle);

        if (complaint == null)
        {
            var empty = ModernUi.Label(
                "Chọn phản ánh để xem thông tin chi tiết.",
                9.4f,
                FontStyle.Regular,
                ModernUi.Muted);
            empty.Location = new Point(pad, 82);
            empty.Size = new Size(contentW, 28);
            parent.Controls.Add(empty);
            return;
        }

        string code = $"PA{complaint.CreatedAt:yyMMdd}-{complaint.ComplaintID:000}";

        var title = ModernUi.Label(Display((string?)complaint.Title, "Không có tiêu đề"), 11.5f, FontStyle.Bold, ModernUi.Navy);
        title.Location = new Point(pad, 76);
        title.Size = new Size(contentW, 26);
        title.AutoEllipsis = true;
        parent.Controls.Add(title);

        var meta = ModernUi.Label(
            $"{code} · {Display((string?)complaint.ApartmentCode)} · {DateTimeText((DateTime?)complaint.CreatedAt)}",
            9f,
            FontStyle.Regular,
            ModernUi.Text);
        meta.Location = new Point(pad, 104);
        meta.Size = new Size(contentW, 22);
        meta.AutoEllipsis = true;
        parent.Controls.Add(meta);

        int colW = Math.Max(130, contentW / 2 - 12);
        int leftX = pad;
        int rightX = pad + colW + 24;
        int y = 142;

        AddLine(parent, "Cư dân", Display(GetDynamicString(complaint, "FullName", "ResidentName", "Resident", "Name")), leftX, y, colW);
        AddLine(parent, "Loại phản ánh", Display((string?)complaint.Category), rightX, y, colW);
        y += 46;

        AddLine(parent, "Ưu tiên", ViStatus((string?)complaint.Priority), leftX, y, colW);
        AddLine(parent, "Trạng thái", ViStatus((string?)complaint.Status), rightX, y, colW);
        y += 46;

        AddLine(parent, "Cập nhật", DateTimeText((DateTime?)complaint.UpdatedAt), leftX, y, colW);

        var descTitle = ModernUi.Label("Nội dung phản ánh", 10f, FontStyle.Bold, ModernUi.Blue);
        descTitle.Location = new Point(pad, y + 58);
        descTitle.Size = new Size(contentW, 22);
        parent.Controls.Add(descTitle);

        var description = new RoundedPanel
        {
            Radius = 8,
            BorderColor = Color.FromArgb(226, 232, 240),
            BackColor = Color.White,
            Location = new Point(pad, y + 86),
            Size = new Size(contentW, Math.Max(120, parent.Height - (y + 110) - 18))
        };

        var descText = ModernUi.Label(Display((string?)complaint.Description, "Không có mô tả"), 9.2f, FontStyle.Regular, ModernUi.Text);
        descText.Location = new Point(14, 12);
        descText.Size = new Size(description.Width - 28, description.Height - 24);
        descText.TextAlign = ContentAlignment.TopLeft;
        parent.Controls.Add(description);
        description.Controls.Add(descText);

        static void AddLine(Control target, string label, string value, int x, int y, int width)
        {
            var lbl = ModernUi.Label(label, 8.8f, FontStyle.Regular, ModernUi.Muted);
            lbl.Location = new Point(x, y);
            lbl.Size = new Size(width, 18);
            target.Controls.Add(lbl);

            var text = ModernUi.Label(Display(value), 9.4f, FontStyle.Bold, ModernUi.Text);
            text.Location = new Point(x, y + 18);
            text.Size = new Size(width, 22);
            text.AutoEllipsis = true;
            target.Controls.Add(text);
        }
    }

    private static void AddInvoiceDetail(Control parent, InvoiceDTO? invoice, ResidentDTO? resident)
    {
        parent.Controls.Clear();

        int pad = 18;
        int contentW = parent.Width - pad * 2;

        var title = ModernUi.Label("Thông tin căn hộ - chủ hộ", 10f, FontStyle.Bold, ModernUi.Blue);
        title.Location = new Point(pad, 42);
        title.Size = new Size(contentW, 24);
        parent.Controls.Add(title);

        if (invoice == null)
        {
            var empty = ModernUi.Label(
                "Chọn hóa đơn để xem thông tin chi tiết.",
                9.4f,
                FontStyle.Regular,
                ModernUi.Muted);

            empty.Location = new Point(pad, 82);
            empty.Size = new Size(contentW, 28);
            parent.Controls.Add(empty);

            var detailTitleEmpty = ModernUi.Label("Chi tiết các khoản phí", 10f, FontStyle.Bold, ModernUi.Blue);
            detailTitleEmpty.Location = new Point(pad, 190);
            detailTitleEmpty.Size = new Size(contentW, 24);
            parent.Controls.Add(detailTitleEmpty);

            var emptyGrid = CreateGrid(
                new[] { "STT", "Khoản phí", "Đơn giá", "Số lượng", "Thành tiền" },
                new[] { EmptyRow(5, "Không có dữ liệu") });

            emptyGrid.Location = new Point(pad, 222);
            emptyGrid.Size = new Size(contentW, 120);
            emptyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            parent.Controls.Add(emptyGrid);

            var totalEmpty = ModernUi.Label("TỔNG CỘNG                                      0 VNĐ", 11f, FontStyle.Bold, ModernUi.Red);
            totalEmpty.Location = new Point(pad, parent.Height - 48);
            totalEmpty.Size = new Size(contentW, 30);
            totalEmpty.TextAlign = ContentAlignment.MiddleLeft;
            parent.Controls.Add(totalEmpty);
            return;
        }

        var avatar = new CircleLabel
        {
            Text = Initials(Display(resident?.FullName, Display(invoice.ApartmentCode, "HD"))),
            CircleColor = ModernUi.Blue,
            ForeColor = Color.White,
            Font = ModernUi.Font(14f, FontStyle.Bold),
            Location = new Point(pad, 76),
            Size = new Size(58, 58),
            TextAlign = ContentAlignment.MiddleCenter
        };
        parent.Controls.Add(avatar);

        var name = ModernUi.Label(
            Display(resident?.FullName, "Chưa có chủ hộ"),
            11.5f,
            FontStyle.Bold,
            ModernUi.Navy);

        name.Location = new Point(avatar.Right + 14, 78);
        name.Size = new Size(contentW - avatar.Width - 20, 26);
        name.AutoEllipsis = true;
        parent.Controls.Add(name);

        var apartmentLine = ModernUi.Label(
            $"Căn hộ {Display(invoice.ApartmentCode)} · Kỳ {invoice.Month:00}/{invoice.Year}",
            9f,
            FontStyle.Regular,
            ModernUi.Text);

        apartmentLine.Location = new Point(avatar.Right + 14, 106);
        apartmentLine.Size = new Size(contentW - avatar.Width - 20, 22);
        apartmentLine.AutoEllipsis = true;
        parent.Controls.Add(apartmentLine);

        var status = ModernUi.Label(
            ViStatus(invoice.PaymentStatus),
            8.5f,
            FontStyle.Bold,
            StatusColor(invoice.PaymentStatus));

        status.Location = new Point(avatar.Right + 14, 130);
        status.Size = new Size(contentW - avatar.Width - 20, 22);
        parent.Controls.Add(status);

        int leftX = pad;
        int rightX = pad + Math.Max(170, contentW / 2);
        int fieldY = 168;
        int fieldW = Math.Max(130, contentW / 2 - 26);

        AddInfoPair(parent, "Mã hóa đơn", InvoiceCode(invoice), leftX, fieldY, fieldW);
        AddInfoPair(parent, "Căn hộ", Display(invoice.ApartmentCode), rightX, fieldY, fieldW);

        fieldY += 50;
        AddInfoPair(parent, "Chủ hộ", Display(resident?.FullName), leftX, fieldY, fieldW);
        AddInfoPair(parent, "Số điện thoại", Display(resident?.Phone), rightX, fieldY, fieldW);

        fieldY += 50;
        AddInfoPair(parent, "Ngày lập", DateTimeText(invoice.CreatedAt), leftX, fieldY, fieldW);
        AddInfoPair(parent, "Hạn thanh toán", DateText(invoice.DueDate), rightX, fieldY, fieldW);

        var line = new Panel
        {
            BackColor = ModernUi.Border,
            Location = new Point(pad, 318),
            Size = new Size(contentW, 1)
        };
        parent.Controls.Add(line);

        var detailTitle = ModernUi.Label("Chi tiết các khoản phí", 10f, FontStyle.Bold, ModernUi.Blue);
        detailTitle.Location = new Point(pad, 334);
        detailTitle.Size = new Size(contentW, 24);
        parent.Controls.Add(detailTitle);

        decimal remaining = Math.Max(0, invoice.TotalAmount - invoice.PaidAmount);

        var detailGrid = CreateGrid(
            new[] { "STT", "Khoản phí", "Đơn giá", "SL/DT", "Thành tiền" },
            new object[][]
            {
            new object[] { 1, "Phí quản lý", Money(invoice.TotalAmount * 0.45m), "1", Money(invoice.TotalAmount * 0.45m) },
            new object[] { 2, "Phí dịch vụ", Money(invoice.TotalAmount * 0.30m), "1", Money(invoice.TotalAmount * 0.30m) },
            new object[] { 3, "Phí gửi xe / khác", Money(invoice.TotalAmount * 0.25m), "1", Money(invoice.TotalAmount * 0.25m) }
            });

        detailGrid.Location = new Point(pad, 364);
        detailGrid.Size = new Size(contentW, 120);
        detailGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        detailGrid.ScrollBars = ScrollBars.None;
        parent.Controls.Add(detailGrid);

        var paidLabel = ModernUi.Label(
            $"Đã thanh toán: {Money(invoice.PaidAmount)} VNĐ",
            9.2f,
            FontStyle.Bold,
            ModernUi.Green);

        paidLabel.Location = new Point(pad, 492);
        paidLabel.Size = new Size(contentW, 24);
        parent.Controls.Add(paidLabel);

        var remainLabel = ModernUi.Label(
            $"Còn phải thu: {Money(remaining)} VNĐ",
            9.2f,
            FontStyle.Bold,
            remaining > 0 ? ModernUi.Red : ModernUi.Green);

        remainLabel.Location = new Point(pad, 518);
        remainLabel.Size = new Size(contentW, 24);
        parent.Controls.Add(remainLabel);

        var total = ModernUi.Label(
            $"TỔNG CỘNG: {Money(invoice.TotalAmount)} VNĐ",
            11.5f,
            FontStyle.Bold,
            ModernUi.Red);

        total.Location = new Point(pad, parent.Height - 44);
        total.Size = new Size(contentW, 30);
        total.TextAlign = ContentAlignment.MiddleRight;
        parent.Controls.Add(total);

        static void AddInfoPair(Control target, string label, string value, int x, int y, int width)
        {
            var caption = ModernUi.Label(label.ToUpperInvariant(), 8.1f, FontStyle.Bold, ModernUi.Muted);
            caption.Location = new Point(x, y);
            caption.Size = new Size(width, 18);
            target.Controls.Add(caption);

            var text = ModernUi.Label(Display(value), 9.4f, FontStyle.Bold, ModernUi.Text);
            text.Location = new Point(x, y + 18);
            text.Size = new Size(width, 24);
            text.AutoEllipsis = true;
            target.Controls.Add(text);
        }

        static string Initials(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "HD";
            }

            var parts = text
                .Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                return parts[0].Length >= 2
                    ? parts[0].Substring(0, 2).ToUpperInvariant()
                    : parts[0].ToUpperInvariant();
            }

            return (parts[0][0].ToString() + parts[parts.Length - 1][0]).ToUpperInvariant();
        }

        static Color StatusColor(string? status)
        {
            string vi = ViStatus(status);

            if (vi == "Đã thanh toán")
            {
                return ModernUi.Green;
            }

            if (vi == "Quá hạn")
            {
                return ModernUi.Red;
            }

            return ModernUi.Orange;
        }
    }


    private static void AddTimeline(Control parent)
    {
        int x = 20;
        int y = 54;
        int itemGap = 52;

        var line = new Panel
        {
            BackColor = Color.FromArgb(202, 213, 228),
            Location = new Point(x + 15, y + 28),
            Size = new Size(3, 148)
        };
        parent.Controls.Add(line);

        AddTimelineItem(parent, x, y, ModernUi.Orange, "!", "#PA240515-001 - Đèn hành lang không sáng", "Đang xử lý");
        AddTimelineItem(parent, x, y + itemGap, ModernUi.Green, "✓", "Tiếp nhận phản ánh", "16/05/2024 08:45");
        AddTimelineItem(parent, x, y + itemGap * 2, ModernUi.Blue, "⚙", "Đang xử lý", "16/05/2024 10:20");
        AddTimelineItem(parent, x, y + itemGap * 3, Color.FromArgb(209, 213, 219), "○", "Hoàn tất", "Chưa hoàn thành");
    }

    private static void AddTimelineItem(Control parent, int x, int y, Color color, string icon, string title, string body)
    {
        var dot = new CircleLabel
        {
            Text = icon,
            CircleColor = color,
            ForeColor = Color.White,
            Font = ModernUi.Font(11.5f, FontStyle.Bold),
            Location = new Point(x, y),
            Size = new Size(32, 32),
            TextAlign = ContentAlignment.MiddleCenter
        };
        parent.Controls.Add(dot);

        var titleLabel = ModernUi.Label(title, 8.7f, FontStyle.Bold, ModernUi.Text);
        titleLabel.Location = new Point(x + 44, y - 2);
        titleLabel.Size = new Size(parent.Width - x - 58, 20);
        titleLabel.AutoEllipsis = true;
        parent.Controls.Add(titleLabel);

        var bodyLabel = ModernUi.Label(body, 8.2f, FontStyle.Regular, ModernUi.Muted);
        bodyLabel.Location = new Point(x + 44, y + 18);
        bodyLabel.Size = new Size(parent.Width - x - 58, 20);
        bodyLabel.AutoEllipsis = true;
        parent.Controls.Add(bodyLabel);
    }

    private static void AddProfileField(Control parent, string label, string value, int x, int y, int width)
    {
        var caption = ModernUi.Label(label.ToUpperInvariant(), 8.1f, FontStyle.Bold, ModernUi.Muted);
        caption.Location = new Point(x, y);
        caption.Size = new Size(width, 16);
        parent.Controls.Add(caption);

        var text = ModernUi.Label(value, 9.4f, FontStyle.Bold, ModernUi.Text);
        text.Location = new Point(x, y + 16);
        text.Size = new Size(width, 22);
        parent.Controls.Add(text);
    }
}

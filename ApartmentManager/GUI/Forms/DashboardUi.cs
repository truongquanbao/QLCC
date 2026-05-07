using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ApartmentManager.GUI.Forms;

internal static class DashboardUi
{
    public static RoundedPanel CreateSearchBox(string placeholder, int width = 296, int height = 40)
    {
        var wrapper = ModernUi.CardPanel(12);
        wrapper.Size = new Size(width, height);
        wrapper.BackColor = Color.White;
        wrapper.BorderColor = Color.FromArgb(218, 226, 238);
        wrapper.Padding = Padding.Empty;

        var icon = ModernUi.Label("⌕", 16f, FontStyle.Bold, ModernUi.Muted);
        icon.Name = "icon";
        icon.TextAlign = ContentAlignment.MiddleCenter;
        wrapper.Controls.Add(icon);

        var input = new TextBox
        {
            Name = "input",
            PlaceholderText = placeholder,
            BorderStyle = BorderStyle.None,
            Font = ModernUi.Font(10f),
            ForeColor = ModernUi.Text,
            BackColor = Color.White
        };
        wrapper.Controls.Add(input);

        void LayoutSearch()
        {
            icon.SetBounds(12, 6, 28, Math.Max(20, wrapper.Height - 12));
            input.SetBounds(48, 11, Math.Max(120, wrapper.Width - 60), Math.Max(18, wrapper.Height - 18));
        }

        wrapper.Resize += (_, _) => LayoutSearch();
        LayoutSearch();
        return wrapper;
    }

    public static RoundedPanel CreateStatCard(string title, string value, string unit, Color accent, string iconText, string trend, int width, int height = 136)
    {
        var card = ModernUi.CardPanel(14);
        card.Size = new Size(width, height);
        card.BackColor = Color.White;
        card.BorderColor = Color.FromArgb(223, 231, 242);
        card.Padding = Padding.Empty;

        var accentBar = new Panel
        {
            Height = 4,
            BackColor = accent
        };
        card.Controls.Add(accentBar);

        var titleLabel = ModernUi.Label(title.ToUpperInvariant(), 8.5f, FontStyle.Bold, ModernUi.Muted);
        card.Controls.Add(titleLabel);

        var icon = new CircleLabel
        {
            Text = iconText,
            CircleColor = Color.FromArgb(235, 243, 255),
            ForeColor = accent,
            Font = ModernUi.Font(20f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(icon);

        var valueLabel = ModernUi.Label(value, 25f, FontStyle.Bold, ModernUi.Navy);
        card.Controls.Add(valueLabel);

        var unitLabel = ModernUi.Label(unit, 9f, FontStyle.Regular, ModernUi.Muted);
        card.Controls.Add(unitLabel);

        var trendLabel = ModernUi.Label(trend, 8.7f, FontStyle.Bold, accent);
        card.Controls.Add(trendLabel);

        void LayoutCard()
        {
            accentBar.SetBounds(0, 0, card.Width, 4);
            titleLabel.SetBounds(18, 18, Math.Max(120, card.Width - 96), 20);
            icon.SetBounds(card.Width - 66, 16, 44, 44);

            float valueSize = value.Length > 11 ? 16f : value.Length > 7 ? 20f : 26f;
            valueLabel.Font = ModernUi.Font(valueSize, FontStyle.Bold);
            valueLabel.SetBounds(18, 48, Math.Max(130, card.Width - 36), 38);

            unitLabel.SetBounds(18, 84, Math.Max(120, card.Width - 36), 18);
            trendLabel.SetBounds(18, card.Height - 34, Math.Max(140, card.Width - 36), 18);
        }

        card.Resize += (_, _) => LayoutCard();
        LayoutCard();
        return card;
    }

    public static void ApplySummaryGridStyle(DataGridView grid)
    {
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToResizeColumns = false;
        grid.ColumnHeadersHeight = 42;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.RowTemplate.Height = 40;
        grid.ScrollBars = ScrollBars.Vertical;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = ModernUi.Navy;
        grid.ColumnHeadersDefaultCellStyle.Font = ModernUi.Font(8.9f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        grid.DefaultCellStyle.Font = ModernUi.Font(8.9f);
        grid.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 236, 251);
        grid.DefaultCellStyle.SelectionForeColor = ModernUi.Text;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 255);
        grid.GridColor = Color.FromArgb(232, 237, 245);
        grid.BackgroundColor = Color.White;
    }
}

internal sealed class RoundedButton : Button
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 12;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverBackColor { get; set; } = Color.Empty;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        Font = ModernUi.Font(9.5f, FontStyle.Bold);
        ForeColor = Color.White;
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = CreateRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
        using var brush = new SolidBrush(Enabled ? BackColor : Color.FromArgb(180, BackColor));
        using var pen = new Pen(BackColor);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (HoverBackColor != Color.Empty)
        {
            BackColor = HoverBackColor;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Invalidate();
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class DashboardBarChartPanel : Control
{
    public List<(string Label, decimal Value)> Bars { get; } = new();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BarColor { get; set; } = ModernUi.Blue;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal AxisMax { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SeriesLabel { get; set; } = "Doanh thu";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string EmptyMessage { get; set; } = "Chưa có dữ liệu doanh thu";

    public DashboardBarChartPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var chartArea = new Rectangle(52, 18, Math.Max(120, Width - 78), Math.Max(90, Height - 66));
        using var textBrush = new SolidBrush(ModernUi.Muted);
        using var linePen = new Pen(Color.FromArgb(231, 236, 244));
        using var axisPen = new Pen(Color.FromArgb(208, 216, 228));
        using var barBrush = new SolidBrush(BarColor);

        decimal maxValue = AxisMax > 0 ? AxisMax : (Bars.Count == 0 ? 0 : Bars.Max(b => b.Value));
        bool hasData = Bars.Count > 0 && maxValue > 0;
        int steps = 4;

        for (int i = 0; i <= steps; i++)
        {
            int y = chartArea.Bottom - (chartArea.Height * i / steps);
            e.Graphics.DrawLine(linePen, chartArea.Left, y, chartArea.Right, y);
            string label = hasData ? CompactMoney(maxValue * i / steps) : i == 0 ? "0" : string.Empty;
            if (!string.IsNullOrWhiteSpace(label))
            {
                e.Graphics.DrawString(label, ModernUi.Font(7.7f), textBrush, 0, y - 8);
            }
        }

        e.Graphics.DrawLine(axisPen, chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom);

        if (!hasData)
        {
            using var emptyBrush = new SolidBrush(Color.FromArgb(120, ModernUi.Muted));
            e.Graphics.DrawString(EmptyMessage, ModernUi.Font(10f, FontStyle.Bold), emptyBrush, chartArea, CenterFormat());
            return;
        }

        int slotWidth = Math.Max(26, chartArea.Width / Math.Max(1, Bars.Count));
        int barWidth = Math.Max(16, Math.Min(28, slotWidth - 18));
        for (int i = 0; i < Bars.Count; i++)
        {
            decimal normalized = Bars[i].Value <= 0 ? 0 : Bars[i].Value / maxValue;
            int barHeight = Math.Max(4, (int)Math.Round(normalized * chartArea.Height));
            int x = chartArea.Left + i * slotWidth + (slotWidth - barWidth) / 2;
            int y = chartArea.Bottom - barHeight;

            using var path = CreateRoundedRect(new Rectangle(x, y, barWidth, barHeight), 8);
            e.Graphics.FillPath(barBrush, path);
            e.Graphics.DrawString(Bars[i].Label, ModernUi.Font(8f), textBrush, x - 6, chartArea.Bottom + 8);
        }

        int legendY = Height - 24;
        e.Graphics.FillRectangle(barBrush, chartArea.Left, legendY + 3, 12, 12);
        e.Graphics.DrawString(SeriesLabel, ModernUi.Font(8.5f, FontStyle.Bold), textBrush, chartArea.Left + 20, legendY);
    }

    private static string CompactMoney(decimal value)
    {
        if (value >= 1_000_000_000m)
        {
            return $"{value / 1_000_000_000m:0.#}B";
        }

        if (value >= 1_000_000m)
        {
            return $"{value / 1_000_000m:0.#}M";
        }

        if (value >= 1_000m)
        {
            return $"{value / 1_000m:0.#}K";
        }

        return $"{value:0}";
    }

    private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
    {
        int diameter = Math.Min(bounds.Width, radius * 2);
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddLine(bounds.Right, bounds.Y + diameter / 2, bounds.Right, bounds.Bottom);
        path.AddLine(bounds.Right, bounds.Bottom, bounds.X, bounds.Bottom);
        path.CloseFigure();
        return path;
    }

    private static StringFormat CenterFormat() => new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };
}

internal sealed class DashboardDonutChartPanel : Control
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Percent { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor { get; set; } = ModernUi.Green;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CenterText { get; set; } = "0%";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SubText { get; set; } = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PrimaryLabel { get; set; } = "Đang ở";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PrimaryValue { get; set; } = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SecondaryLabel { get; set; } = "Còn trống";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SecondaryValue { get; set; } = string.Empty;

    public DashboardDonutChartPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int donutSize = Math.Min(140, Math.Min(Width - 40, Height - 72));
        donutSize = Math.Max(100, donutSize);
        int donutX = (Width - donutSize) / 2;
        int donutY = 8;
        var donutRect = new Rectangle(donutX, donutY, donutSize, donutSize);

        using var bgPen = new Pen(Color.FromArgb(221, 227, 236), 20) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var accentPen = new Pen(AccentColor, 20) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawArc(bgPen, donutRect, -90, 360);
        e.Graphics.DrawArc(accentPen, donutRect, -90, Math.Max(0, Math.Min(100, Percent)) * 3.6f);

        using var centerBrush = new SolidBrush(AccentColor);
        using var textBrush = new SolidBrush(ModernUi.Navy);
        using var mutedBrush = new SolidBrush(ModernUi.Muted);

        var centerRect = new Rectangle(donutRect.X, donutRect.Y + 6, donutRect.Width, donutRect.Height - 12);
        e.Graphics.DrawString(CenterText, ModernUi.Font(19f, FontStyle.Bold), centerBrush, centerRect, CenterFormat());
        e.Graphics.DrawString(SubText, ModernUi.Font(9.3f, FontStyle.Bold), textBrush, new Rectangle(donutRect.X, donutRect.Y + 54, donutRect.Width, 24), CenterFormat());

        DrawLegendRow(e.Graphics, 18, Height - 50, AccentColor, PrimaryLabel, PrimaryValue);
        DrawLegendRow(e.Graphics, 18, Height - 26, Color.FromArgb(221, 227, 236), SecondaryLabel, SecondaryValue);
    }

    private static void DrawLegendRow(Graphics graphics, int x, int y, Color color, string label, string value)
    {
        using var brush = new SolidBrush(color);
        using var labelBrush = new SolidBrush(ModernUi.Text);
        using var valueBrush = new SolidBrush(ModernUi.Muted);
        graphics.FillEllipse(brush, x, y + 2, 10, 10);
        graphics.DrawString(label, ModernUi.Font(8.8f, FontStyle.Bold), labelBrush, x + 18, y - 3);
        graphics.DrawString(value, ModernUi.Font(8.7f), valueBrush, x + 102, y - 3);
    }

    private static StringFormat CenterFormat() => new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };
}

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
        wrapper.MinimumSize = new Size(220, height);

        var icon = ModernUi.Label("⌕", 15f, FontStyle.Bold, ModernUi.Muted);
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

    public static RoundedPanel CreateStatCard(string title, string value, string unit, Color accent, string iconText, string trend, int width, int height = 126)
    {
        var card = ModernUi.CardPanel(14);
        card.Size = new Size(width, height);
        card.BackColor = Color.White;
        card.BorderColor = Color.FromArgb(223, 231, 242);
        card.Padding = Padding.Empty;
        card.MinimumSize = new Size(150, 112);

        var icon = new CircleLabel
        {
            Text = iconText,
            CircleColor = accent,
            ForeColor = Color.White,
            Font = ModernUi.Font(20f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        card.Controls.Add(icon);

        var titleLabel = ModernUi.Label(title.ToUpperInvariant(), 8.6f, FontStyle.Bold, accent);
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        titleLabel.AutoEllipsis = true;
        card.Controls.Add(titleLabel);

        var valueLabel = ModernUi.Label(value, 22f, FontStyle.Bold, accent);
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.AutoEllipsis = true;
        card.Controls.Add(valueLabel);

        var unitLabel = ModernUi.Label(unit, 8.8f, FontStyle.Regular, ModernUi.Muted);
        unitLabel.TextAlign = ContentAlignment.MiddleLeft;
        unitLabel.AutoEllipsis = true;
        card.Controls.Add(unitLabel);

        Color trendColor = accent == ModernUi.Red
            ? ModernUi.Red
            : accent == ModernUi.Orange
                ? ModernUi.Orange
                : Color.FromArgb(22, 163, 74);
        var trendLabel = ModernUi.Label(trend, 8.6f, FontStyle.Bold, trendColor);
        trendLabel.TextAlign = ContentAlignment.MiddleLeft;
        trendLabel.AutoEllipsis = true;
        card.Controls.Add(trendLabel);

        void LayoutCard()
        {
            int iconSize = card.Width < 190 ? 44 : card.Width < 260 ? 50 : 56;
            int iconTop = Math.Max(20, (card.Height - iconSize) / 2 - 4);
            int iconLeft = 18;
            int textLeft = iconLeft + iconSize + 16;
            int textWidth = Math.Max(84, card.Width - textLeft - 16);

            icon.Font = ModernUi.Font(iconSize >= 56 ? 21f : iconSize >= 50 ? 18f : 16f, FontStyle.Bold);
            icon.SetBounds(iconLeft, iconTop, iconSize, iconSize);

            float valueSize = card.Width < 190
                ? (value.Length > 10 ? 13f : 16f)
                : value.Length > 13 ? 15f : value.Length > 10 ? 17f : value.Length > 7 ? 19f : 22f;
            valueLabel.Font = ModernUi.Font(valueSize, FontStyle.Bold);

            titleLabel.SetBounds(textLeft, 18, textWidth, 18);
            valueLabel.SetBounds(textLeft, 38, textWidth, 32);
            unitLabel.SetBounds(textLeft, 68, textWidth, 18);
            trendLabel.SetBounds(textLeft, card.Height - 28, textWidth, 18);
        }

        card.Resize += (_, _) => LayoutCard();
        LayoutCard();
        return card;
    }

    public static void ApplySummaryGridStyle(DataGridView grid)
    {
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Color.White;
        grid.GridColor = Color.FromArgb(232, 237, 245);
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToResizeColumns = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ScrollBars = ScrollBars.Vertical;

        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 40;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = ModernUi.Navy;
        grid.ColumnHeadersDefaultCellStyle.Font = ModernUi.Font(8.8f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        grid.DefaultCellStyle.Font = ModernUi.Font(8.8f);
        grid.DefaultCellStyle.ForeColor = ModernUi.Text;
        grid.DefaultCellStyle.Padding = new Padding(6, 2, 6, 2);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 236, 251);
        grid.DefaultCellStyle.SelectionForeColor = ModernUi.Text;
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 255);
        grid.RowTemplate.Height = 34;
    }
}

internal sealed class RoundedButton : Button
{
    private Color _normalBackColor = Color.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 10;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverBackColor { get; set; } = Color.Empty;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        Font = ModernUi.Font(9.3f, FontStyle.Bold);
        ForeColor = Color.White;
        DoubleBuffered = true;
    }

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        if (_normalBackColor == Color.Empty)
        {
            _normalBackColor = BackColor;
        }
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        if (!ClientRectangle.IsEmpty && !Focused)
        {
            _normalBackColor = BackColor;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = CreateRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
        using var brush = new SolidBrush(Enabled ? BackColor : Color.FromArgb(185, 190, 198));
        using var pen = new Pen(BackColor);

        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            Enabled ? ForeColor : Color.FromArgb(245, 245, 245),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (_normalBackColor == Color.Empty)
        {
            _normalBackColor = BackColor;
        }

        if (HoverBackColor != Color.Empty && Enabled)
        {
            BackColor = HoverBackColor;
        }

        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_normalBackColor != Color.Empty && Enabled)
        {
            BackColor = _normalBackColor;
        }

        Invalidate();
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        int diameter = Math.Max(2, radius * 2);
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
        MinimumSize = new Size(240, 170);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        decimal maxValue = AxisMax > 0 ? AxisMax : (Bars.Count == 0 ? 0 : Bars.Max(b => b.Value));
        bool hasData = Bars.Count > 0 && maxValue > 0;

        if (!hasData)
        {
            using var iconBrush = new SolidBrush(Color.FromArgb(166, 178, 194));
            using var textBrush = new SolidBrush(ModernUi.Muted);
            using var iconFont = new Font("Segoe UI Symbol", 28f, FontStyle.Regular);
            using var messageFont = ModernUi.Font(10.2f, FontStyle.Bold);

            var iconRect = new Rectangle(0, Math.Max(24, Height / 2 - 34), Width, 34);
            var textRect = new Rectangle(18, iconRect.Bottom + 4, Math.Max(0, Width - 36), 24);

            e.Graphics.DrawString("▤", iconFont, iconBrush, iconRect, CenterFormat());
            e.Graphics.DrawString(EmptyMessage, messageFont, textBrush, textRect, CenterFormat());
            return;
        }

        var chartArea = new Rectangle(52, 18, Math.Max(120, Width - 78), Math.Max(90, Height - 70));
        using var textBrush2 = new SolidBrush(ModernUi.Muted);
        using var linePen = new Pen(Color.FromArgb(231, 236, 244));
        using var axisPen = new Pen(Color.FromArgb(208, 216, 228));
        using var barBrush = new SolidBrush(BarColor);
        int steps = 4;

        for (int i = 0; i <= steps; i++)
        {
            int y = chartArea.Bottom - chartArea.Height * i / steps;
            e.Graphics.DrawLine(linePen, chartArea.Left, y, chartArea.Right, y);
            e.Graphics.DrawString(CompactMoney(maxValue * i / steps), ModernUi.Font(7.6f), textBrush2, 4, y - 8);
        }

        e.Graphics.DrawLine(axisPen, chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom);

        int slotWidth = Math.Max(26, chartArea.Width / Math.Max(1, Bars.Count));
        int barWidth = Math.Max(16, Math.Min(30, slotWidth - 18));

        for (int i = 0; i < Bars.Count; i++)
        {
            decimal normalized = Bars[i].Value <= 0 ? 0 : Bars[i].Value / maxValue;
            int barHeight = Math.Max(4, (int)Math.Round(normalized * chartArea.Height));
            int x = chartArea.Left + i * slotWidth + (slotWidth - barWidth) / 2;
            int y = chartArea.Bottom - barHeight;

            using var path = CreateRoundedRect(new Rectangle(x, y, barWidth, barHeight), 7);
            e.Graphics.FillPath(barBrush, path);

            var labelRect = new Rectangle(x - 10, chartArea.Bottom + 8, slotWidth + 8, 18);
            e.Graphics.DrawString(Bars[i].Label, ModernUi.Font(7.8f), textBrush2, labelRect, CenterFormat());
        }

        int legendY = Height - 24;
        e.Graphics.FillRectangle(barBrush, chartArea.Left, legendY + 4, 12, 12);
        e.Graphics.DrawString(SeriesLabel, ModernUi.Font(8.4f, FontStyle.Bold), textBrush2, chartArea.Left + 20, legendY);
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
        int diameter = Math.Max(2, Math.Min(bounds.Width, radius * 2));
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
        MinimumSize = new Size(240, 190);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var labelFont = ModernUi.Font(8.5f, FontStyle.Bold);
        using var valueFont = ModernUi.Font(8.3f);
        using var centerFont = ModernUi.Font(17f, FontStyle.Bold);
        using var subFont = ModernUi.Font(9f, FontStyle.Bold);
        using var labelBrush = new SolidBrush(ModernUi.Text);
        using var valueBrush = new SolidBrush(ModernUi.Muted);
        using var centerBrush = new SolidBrush(AccentColor);
        using var subBrush = new SolidBrush(ModernUi.Navy);

        float primaryLabelWidth = e.Graphics.MeasureString(PrimaryLabel, labelFont).Width;
        float primaryValueWidth = e.Graphics.MeasureString(PrimaryValue, valueFont).Width;
        float secondaryLabelWidth = e.Graphics.MeasureString(SecondaryLabel, labelFont).Width;
        float secondaryValueWidth = e.Graphics.MeasureString(SecondaryValue, valueFont).Width;
        int legendWidth = (int)Math.Ceiling(Math.Max(primaryLabelWidth + primaryValueWidth, secondaryLabelWidth + secondaryValueWidth)) + 54;

        bool sideLegend = Width >= Math.Max(320, legendWidth + 180);
        int stroke = sideLegend ? 16 : 14;
        int donutAvailableWidth = sideLegend ? Width - legendWidth - 46 : Width - 36;
        int donutSize = sideLegend
            ? Math.Min(136, Math.Min(donutAvailableWidth - 8, Height - 42))
            : Math.Min(120, Math.Min(donutAvailableWidth - 8, Height - 82));
        donutSize = Math.Max(96, donutSize);

        int donutX = sideLegend ? 18 : (Width - donutSize) / 2;
        int donutY = sideLegend ? Math.Max(18, (Height - donutSize) / 2 - 2) : Math.Max(14, (Height - donutSize - 36) / 2);
        var donutRect = new Rectangle(donutX, donutY, donutSize, donutSize);

        using var bgPen = new Pen(Color.FromArgb(221, 227, 236), stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var accentPen = new Pen(AccentColor, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawArc(bgPen, donutRect, -90, 360);
        e.Graphics.DrawArc(accentPen, donutRect, -90, Math.Max(0, Math.Min(100, Percent)) * 3.6f);

        SizeF centerTextSize = e.Graphics.MeasureString(CenterText, centerFont);
        SizeF subTextSize = string.IsNullOrWhiteSpace(SubText) ? SizeF.Empty : e.Graphics.MeasureString(SubText, subFont);
        float blockHeight = centerTextSize.Height + (subTextSize.Height > 0 ? subTextSize.Height - 2 : 0);
        float startY = donutRect.Y + (donutRect.Height - blockHeight) / 2f - 2f;

        var centerTextRect = new RectangleF(donutRect.X, startY, donutRect.Width, centerTextSize.Height + 2);
        e.Graphics.DrawString(CenterText, centerFont, centerBrush, centerTextRect, CenterFormat());

        if (!string.IsNullOrWhiteSpace(SubText))
        {
            var subTextRect = new RectangleF(donutRect.X, centerTextRect.Bottom - 1, donutRect.Width, subTextSize.Height + 2);
            e.Graphics.DrawString(SubText, subFont, subBrush, subTextRect, CenterFormat());
        }

        if (sideLegend)
        {
            int legendX = donutRect.Right + 20;
            int legendY = Math.Max(34, (Height - 52) / 2);
            int rowWidth = Math.Max(120, Width - legendX - 16);
            DrawLegendRow(e.Graphics, new Rectangle(legendX, legendY, rowWidth, 20), AccentColor, PrimaryLabel, PrimaryValue, labelFont, valueFont, labelBrush, valueBrush);
            DrawLegendRow(e.Graphics, new Rectangle(legendX, legendY + 40, rowWidth, 20), Color.FromArgb(221, 227, 236), SecondaryLabel, SecondaryValue, labelFont, valueFont, labelBrush, valueBrush);
            return;
        }

        int bottomLegendWidth = Math.Max(160, Width - 32);
        int bottomLegendX = Math.Max(12, (Width - bottomLegendWidth) / 2);
        DrawLegendRow(e.Graphics, new Rectangle(bottomLegendX, Height - 48, bottomLegendWidth, 18), AccentColor, PrimaryLabel, PrimaryValue, labelFont, valueFont, labelBrush, valueBrush);
        DrawLegendRow(e.Graphics, new Rectangle(bottomLegendX, Height - 24, bottomLegendWidth, 18), Color.FromArgb(221, 227, 236), SecondaryLabel, SecondaryValue, labelFont, valueFont, labelBrush, valueBrush);
    }

    private static void DrawLegendRow(Graphics graphics, Rectangle bounds, Color color, string label, string value, Font labelFont, Font valueFont, Brush labelBrush, Brush valueBrush)
    {
        using var bulletBrush = new SolidBrush(color);
        graphics.FillEllipse(bulletBrush, bounds.X, bounds.Y + 4, 10, 10);

        var labelRect = new Rectangle(bounds.X + 18, bounds.Y - 1, Math.Max(50, bounds.Width - 94), bounds.Height + 2);
        graphics.DrawString(label, labelFont, labelBrush, labelRect, LeftFormat());

        if (!string.IsNullOrWhiteSpace(value))
        {
            var valueRect = new Rectangle(bounds.Right - 90, bounds.Y - 1, 90, bounds.Height + 2);
            graphics.DrawString(value, valueFont, valueBrush, valueRect, RightFormat());
        }
    }

    private static StringFormat CenterFormat() => new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };

    private static StringFormat LeftFormat() => new()
    {
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter
    };

    private static StringFormat RightFormat() => new()
    {
        Alignment = StringAlignment.Far,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter
    };
}

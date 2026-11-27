using Microsoft.Maui.Graphics;

namespace Keto_Tracker2.ViewModels;

public class WeightChartDrawable : IDrawable
{
    public List<(DateTime Date, double Weight)> Points { get; set; } = [];

    /// <summary>
    /// Optional start of the X axis. When null, the axis starts at the first point.
    /// Setting it (e.g. "today - 29 days" for a 30-day view) keeps the axis fixed to
    /// the selected window, so days without entries at the edges are visible too.
    /// </summary>
    public DateTime? RangeStart { get; set; }

    /// <summary>Optional end of the X axis. When null, the axis ends at the last point.</summary>
    public DateTime? RangeEnd { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Points.Count == 0) return;

        const float paddingLeft = 38f;
        const float paddingRight = 8f;
        const float paddingTop = 8f;
        const float paddingBottom = 28f;

        float left   = dirtyRect.Left + paddingLeft;
        float right  = dirtyRect.Right - paddingRight;
        float top    = dirtyRect.Top + paddingTop;
        float bottom = dirtyRect.Bottom - paddingBottom;
        float width  = right - left;
        float height = bottom - top;

        var minW = (float)Points.Min(p => p.Weight);
        var maxW = (float)Points.Max(p => p.Weight);
        float range = maxW - minW;
        if (range < 0.5f) range = 0.5f;

        // X axis is a real time axis: position is proportional to the date, so days
        // without a weight entry still take up space and gaps are visible.
        DateTime axisStart = (RangeStart ?? Points[0].Date).Date;
        DateTime axisEnd = (RangeEnd ?? Points[^1].Date).Date;
        if (Points[0].Date.Date < axisStart) axisStart = Points[0].Date.Date;
        if (Points[^1].Date.Date > axisEnd) axisEnd = Points[^1].Date.Date;
        double spanDays = (axisEnd - axisStart).TotalDays;

        float XFor(DateTime d) => spanDays <= 0
            ? left + width / 2f
            : left + (float)((d.Date - axisStart).TotalDays / spanDays) * width;

        // Grid lines
        canvas.StrokeColor = Color.FromArgb("#3D3D54");
        canvas.StrokeSize = 1;
        for (int i = 0; i <= 3; i++)
        {
            float y = top + (i / 3f) * height;
            canvas.DrawLine(left, y, right, y);
        }

        // Y-axis labels
        canvas.FontColor = Color.FromArgb("#9E9EC8");
        canvas.FontSize = 9f;
        canvas.DrawString($"{maxW:F1}", dirtyRect.Left, top - 6f, paddingLeft - 4f, 14f,
            HorizontalAlignment.Right, VerticalAlignment.Center);
        canvas.DrawString($"{minW:F1}", dirtyRect.Left, bottom - 6f, paddingLeft - 4f, 14f,
            HorizontalAlignment.Right, VerticalAlignment.Center);

        // Map to screen coords
        var coords = new List<PointF>(Points.Count);
        foreach (var p in Points)
        {
            float y = bottom - ((float)(p.Weight - minW) / range) * height;
            coords.Add(new PointF(XFor(p.Date), y));
        }

        // Line: continuous from each logged point to the next, even across missing days.
        if (coords.Count > 1)
        {
            canvas.StrokeColor = Color.FromArgb("#4FC3F7");
            canvas.StrokeSize = 2f;
            for (int i = 0; i < coords.Count - 1; i++)
                canvas.DrawLine(coords[i].X, coords[i].Y, coords[i + 1].X, coords[i + 1].Y);
        }

        // Dots (only on days that actually have an entry)
        canvas.FillColor = Color.FromArgb("#4FC3F7");
        float dotRadius = Points.Count > 120 ? 1.5f : 3f;
        foreach (var pt in coords)
            canvas.FillCircle(pt.X, pt.Y, dotRadius);

        // Date labels along the time axis (evenly spaced in time, not per entry)
        canvas.FontColor = Color.FromArgb("#9E9EC8");
        canvas.FontSize = 8f;

        float labelSlotWidth = spanDays <= 31 ? 18f : spanDays <= 366 ? 40f : 50f;
        float labelBoxWidth = spanDays <= 31 ? 16f : spanDays <= 366 ? 38f : 48f;

        string FormatLabel(DateTime d) => spanDays switch
        {
            <= 31 => d.Day.ToString(),
            <= 366 => d.ToString("MMM d"),
            _ => d.ToString("MMM yyyy")
        };

        if (spanDays <= 0)
        {
            DrawLabel(axisStart);
            return;
        }

        int maxLabels = Math.Max(1, (int)(width / labelSlotWidth));
        int stepDays = NiceStep(spanDays / maxLabels);
        for (var d = axisStart; d <= axisEnd; d = d.AddDays(stepDays))
            DrawLabel(d);

        void DrawLabel(DateTime d)
        {
            float x = XFor(d);
            // Keep the label box inside the chart area.
            float maxBoxLeft = Math.Max(dirtyRect.Left, dirtyRect.Right - labelBoxWidth);
            float boxLeft = Math.Clamp(x - labelBoxWidth / 2f, dirtyRect.Left, maxBoxLeft);
            canvas.DrawString(FormatLabel(d), boxLeft, bottom + 2f, labelBoxWidth, 14f,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }

    private static int NiceStep(double minDays)
    {
        int[] steps = [1, 2, 3, 5, 7, 14, 30, 61, 91, 182, 365, 730, 1825];
        foreach (var s in steps)
            if (s >= minDays) return s;
        return (int)Math.Ceiling(minDays);
    }
}

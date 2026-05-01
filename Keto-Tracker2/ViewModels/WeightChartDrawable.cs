using Microsoft.Maui.Graphics;

namespace Keto_Tracker2.ViewModels;

public class WeightChartDrawable : IDrawable
{
    public List<(DateTime Date, double Weight)> Points { get; set; } = [];

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
        for (int i = 0; i < Points.Count; i++)
        {
            float x = Points.Count == 1
                ? left + width / 2f
                : left + (i / (float)(Points.Count - 1)) * width;
            float y = bottom - ((float)(Points[i].Weight - minW) / range) * height;
            coords.Add(new PointF(x, y));
        }

        // Line
        if (coords.Count > 1)
        {
            canvas.StrokeColor = Color.FromArgb("#4FC3F7");
            canvas.StrokeSize = 2f;
            for (int i = 0; i < coords.Count - 1; i++)
                canvas.DrawLine(coords[i].X, coords[i].Y, coords[i + 1].X, coords[i + 1].Y);
        }

        // Dots
        canvas.FillColor = Color.FromArgb("#4FC3F7");
        foreach (var pt in coords)
            canvas.FillCircle(pt.X, pt.Y, 3f);

        // Day labels
        canvas.FontColor = Color.FromArgb("#9E9EC8");
        canvas.FontSize = 8f;
        int maxLabels = (int)(width / 18f);
        int step = Points.Count <= maxLabels ? 1 : (int)Math.Ceiling((double)Points.Count / maxLabels);
        for (int i = 0; i < coords.Count; i += step)
        {
            string label = Points[i].Date.Day.ToString();
            canvas.DrawString(label, coords[i].X - 8f, bottom + 2f, 16f, 14f,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }
}

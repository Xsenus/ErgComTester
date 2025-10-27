using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using SkiaSharp;

namespace ErgData;

/// <summary>
///     Provides utilities for rendering ERG graphs and preparing the data required for drawing.
///     The implementation reuses the same rendering logic as the PDF/Quest report builder so that
///     callers can generate standalone PNG previews from arbitrary <see cref="ErgTest"/> instances.
/// </summary>
public static class GraphRenderer
{
    /// <summary>
    ///     Attempts to render the eye graph using SkiaSharp (preferred) or GDI+ (fallback).
    ///     Returns <c>null</c> when rendering is disabled or when there is no data for the graph.
    /// </summary>
    public static GraphImage? TryRenderGraphImage(ErgTest test, EyeData eye)
    {
        if (test == null) throw new ArgumentNullException(nameof(test));
        if (eye == null) throw new ArgumentNullException(nameof(eye));

        if (!RenderingSupport.GraphRenderingSupported)
            return null;

        if (!TryPrepareGraphData(test, eye, out var context))
            return null;

        return RenderingSupport.UseLegacyGraphRendering
            ? TryRenderGraphImageWithGdi(test, context)
            : TryRenderGraphImageWithSkia(test, context);
    }

    /// <summary>
    ///     Returns the normalized graph samples. When the parsed data already contains normalized
    ///     values they are returned as-is; otherwise the method derives them from the raw samples.
    /// </summary>
    public static double[][]? GetNormalizedGraphs(ErgTest test, EyeData eye)
    {
        if (test == null) throw new ArgumentNullException(nameof(test));
        if (eye == null) throw new ArgumentNullException(nameof(eye));

        if (eye.GraphsNormalized is { Length: > 0 })
            return eye.GraphsNormalized;

        if (eye.GraphSamples is not { Length: > 0 })
            return null;

        int declaredPoints = Math.Clamp(test.GraphNumPoints, 0, 128);
        double divisor = test.GraphDiscrPerMkV == 0 ? 1d : test.GraphDiscrPerMkV;

        var normalized = new double[eye.GraphSamples.Length][];
        for (int graph = 0; graph < eye.GraphSamples.Length; graph++)
        {
            var samples = eye.GraphSamples[graph] ?? Array.Empty<short>();
            if (samples.Length == 0 || declaredPoints == 0)
            {
                normalized[graph] = Array.Empty<double>();
                continue;
            }

            int count = declaredPoints > 0 ? Math.Min(declaredPoints, samples.Length) : samples.Length;
            var converted = new double[count];
            for (int i = 0; i < count; i++)
            {
                converted[i] = samples[i] / divisor;
            }

            normalized[graph] = converted;
        }

        return normalized;
    }

    private static bool TryPrepareGraphData(ErgTest test, EyeData eye, out GraphRenderContext context)
    {
        context = default!;

        var graphs = GetNormalizedGraphs(test, eye);
        if (graphs == null || graphs.Length == 0)
            return false;

        int curves = Math.Clamp(eye.GraphCount, 0, graphs.Length);
        if (curves <= 0)
            return false;

        bool hasSamples = false;
        for (int i = 0; i < curves; i++)
        {
            var samples = graphs[i];
            if (samples is { Length: > 1 })
            {
                hasSamples = true;
                break;
            }
        }

        if (!hasSamples)
            return false;

        double xMin = test.GraphXScaleMin;
        double xMax = test.GraphXScaleMax;
        if (xMax <= xMin)
            xMax = xMin + 1;

        double yMin = test.GraphYScaleMin;
        double yMax = test.GraphYScaleMax;
        if (yMax <= yMin)
            yMax = yMin + 1;

        var markers = BuildMarkers(eye, xMin, xMax);

        context = new GraphRenderContext(graphs, curves, test.GraphNumPoints, xMin, xMax, yMin, yMax, markers);
        return true;
    }

    private static GraphMarker[] BuildMarkers(EyeData eye, double xMin, double xMax)
    {
        static bool TryCreateMarker(byte? value, double xMin, double xMax, GraphMarkerKind kind, out GraphMarker marker)
        {
            marker = default!;
            if (!value.HasValue || value.Value == 0)
                return false;

            double position = value.Value;
            if (position < xMin || position > xMax)
                return false;

            marker = new GraphMarker(kind, position);
            return true;
        }

        var markers = new List<GraphMarker>(2);
        if (TryCreateMarker(eye.AWaveMarker, xMin, xMax, GraphMarkerKind.AWave, out var aMarker))
            markers.Add(aMarker);
        if (TryCreateMarker(eye.BWaveMarker, xMin, xMax, GraphMarkerKind.BWave, out var bMarker))
            markers.Add(bMarker);

        return markers.ToArray();
    }

    private static GraphImage? TryRenderGraphImageWithSkia(ErgTest test, GraphRenderContext context)
    {
        try
        {
            const int width = 900;
            const int height = 360;
            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            if (surface == null)
            {
                RenderingSupport.DisableGraphRendering("Не удалось инициализировать движок SkiaSharp для построения графиков.");
                return null;
            }

            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            const float marginLeft = 80f;
            const float marginRight = 30f;
            const float marginTop = 20f;
            const float marginBottom = 60f;
            const float tickInside = 4f;
            const float tickOutside = 6f;

            var chartRect = new SKRect(marginLeft, marginTop, width - marginRight, height - marginBottom);

            double xMin = context.XMin;
            double xMax = context.XMax;
            double yMin = context.YMin;
            double yMax = context.YMax;

            float TransformX(double value) => (float)(chartRect.Left + (value - xMin) / (xMax - xMin) * chartRect.Width);
            float TransformY(double value) => (float)(chartRect.Bottom - (value - yMin) / (yMax - yMin) * chartRect.Height);

            var xStep = DetermineAxisStep(xMin, xMax, test.GraphXValueStep, test.GraphXLineStep);
            var yStep = DetermineAxisStep(yMin, yMax, test.GraphYValueStep, test.GraphYLineStep);
            var xTicks = BuildAxisTicks(xMin, xMax, xStep);
            var yTicks = BuildAxisTicks(yMin, yMax, yStep);

            using (var borderPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.5f, IsAntialias = true, Style = SKPaintStyle.Stroke })
            {
                canvas.DrawRect(chartRect, borderPaint);
            }

            using (var axisPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.8f, IsAntialias = true })
            {
                canvas.DrawLine(chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom, axisPaint);
                canvas.DrawLine(chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom, axisPaint);
            }

            using (var tickPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.2f, IsAntialias = true })
            {
                foreach (var tick in xTicks)
                {
                    var px = TransformX(tick);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;
                    canvas.DrawLine(px, chartRect.Bottom - tickInside, px, chartRect.Bottom + tickOutside, tickPaint);
                }

                foreach (var tick in yTicks)
                {
                    var py = TransformY(tick);
                    if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                        continue;
                    canvas.DrawLine(chartRect.Left - tickOutside, py, chartRect.Left + tickInside, py, tickPaint);
                }
            }

            using (var zeroPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.2f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 4f, 4f }, 0) })
            {
                if (yMin < 0 && yMax > 0)
                {
                    var zeroY = TransformY(0);
                    canvas.DrawLine(chartRect.Left, zeroY, chartRect.Right, zeroY, zeroPaint);
                }

                if (xMin < 0 && xMax > 0)
                {
                    var zeroX = TransformX(0);
                    canvas.DrawLine(zeroX, chartRect.Top, zeroX, chartRect.Bottom, zeroPaint);
                }
            }

            if (test.GraphFlashPosition >= xMin && test.GraphFlashPosition <= xMax)
            {
                using var flashPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1.5f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 6f, 4f }, 0) };
                var flashX = TransformX(test.GraphFlashPosition);
                canvas.DrawLine(flashX, chartRect.Top, flashX, chartRect.Bottom, flashPaint);
            }

            var graphStyles = test.GraphStyles ?? Array.Empty<GraphStyle>();

            if (context.Markers.Length > 0)
            {
                foreach (var marker in context.Markers)
                {
                    var px = TransformX(marker.PositionMs);
                    if (double.IsNaN(px) || double.IsInfinity(px))
                        continue;
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;

                    var markerColor = GetMarkerColor(marker);
                    var skColor = new SKColor(markerColor.R, markerColor.G, markerColor.B);

                    using var markerPaint = new SKPaint
                    {
                        Color = skColor,
                        StrokeWidth = 1.5f,
                        IsAntialias = true,
                        PathEffect = SKPathEffect.CreateDash(new[] { 6f, 4f }, 0)
                    };
                    canvas.DrawLine(px, chartRect.Top, px, chartRect.Bottom, markerPaint);

                    using var labelPaint = new SKPaint
                    {
                        Color = skColor,
                        TextSize = 16f,
                        IsAntialias = true,
                        IsStroke = false,
                        FakeBoldText = true
                    };

                    var label = GetMarkerLabel(marker);
                    var textWidth = labelPaint.MeasureText(label);
                    float labelY = chartRect.Top - 6f;
                    if (labelY < 12f)
                        labelY = chartRect.Top + 16f;
                    canvas.DrawText(label, px - textWidth / 2f, labelY, labelPaint);
                }
            }

            canvas.Save();
            canvas.ClipRect(chartRect);

            double graphDt = test.GraphDt;
            bool hasGraphDt = graphDt > 0;

            for (int graphIndex = 0; graphIndex < context.Curves; graphIndex++)
            {
                var samples = context.Graphs[graphIndex];
                if (samples == null || samples.Length == 0)
                    continue;

                int count = context.DeclaredPointCount > 1 ? Math.Min(context.DeclaredPointCount, samples.Length) : samples.Length;
                if (count < 2)
                    continue;

                using var path = new SKPath();
                bool hasPoint = false;
                for (int point = 0; point < count; point++)
                {
                    double xValue;
                    if (hasGraphDt)
                    {
                        xValue = point * graphDt;
                        if (xValue < xMin)
                            continue;
                        if (xValue > xMax)
                            break;
                    }
                    else if (count == 1)
                    {
                        xValue = xMin;
                    }
                    else
                    {
                        xValue = xMin + (xMax - xMin) * point / (count - 1);
                    }

                    double yValue = samples[point];

                    var px = TransformX(xValue);
                    var py = TransformY(yValue);
                    if (double.IsNaN(px) || double.IsNaN(py) || double.IsInfinity(px) || double.IsInfinity(py))
                        continue;

                    if (!hasPoint)
                    {
                        path.MoveTo(px, py);
                        hasPoint = true;
                    }
                    else
                    {
                        path.LineTo(px, py);
                    }
                }

                if (!hasPoint)
                    continue;

                var style = graphIndex < graphStyles.Length ? graphStyles[graphIndex] : null;
                var color = style != null ? new SKColor(style.Red, style.Green, style.Blue) : new SKColor(56, 109, 179);

                using var linePaint = new SKPaint { Color = color, StrokeWidth = 2f, IsAntialias = true, Style = SKPaintStyle.Stroke };
                if (style?.Dotted == true)
                {
                    linePaint.PathEffect = SKPathEffect.CreateDash(new[] { 6f, 4f }, 0);
                }

                canvas.DrawPath(path, linePaint);
            }

            canvas.Restore();

            using (var labelPaint = new SKPaint { Color = SKColors.Black, TextSize = 14f, IsAntialias = true })
            {
                var metrics = labelPaint.FontMetrics;
                float textHeight = metrics.Descent - metrics.Ascent;

                foreach (var tick in xTicks)
                {
                    var px = TransformX(tick);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;
                    var text = FormatAxisValue(tick);
                    var textWidth = labelPaint.MeasureText(text);
                    canvas.DrawText(text, px - textWidth / 2f, chartRect.Bottom + textHeight, labelPaint);
                }

                foreach (var tick in yTicks)
                {
                    var py = TransformY(tick);
                    if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                        continue;
                    var text = FormatAxisValue(tick);
                    var textWidth = labelPaint.MeasureText(text);
                    canvas.DrawText(text, chartRect.Left - 10f - textWidth, py + textHeight / 3f, labelPaint);
                }
            }

            using (var titlePaint = new SKPaint { Color = SKColors.Black, TextSize = 16f, IsAntialias = true })
            {
                var xLabel = "ms";
                var xWidth = titlePaint.MeasureText(xLabel);
                var midX = (chartRect.Left + chartRect.Right) / 2f;
                canvas.DrawText(xLabel, midX - xWidth / 2f, height - 12, titlePaint);

                var yLabel = "µV";
                canvas.Save();
                canvas.Translate(20, (chartRect.Top + chartRect.Bottom) / 2f);
                canvas.RotateDegrees(-90);
                var yWidth = titlePaint.MeasureText(yLabel);
                canvas.DrawText(yLabel, -yWidth / 2f, 0, titlePaint);
                canvas.Restore();
            }

            using var snapshot = surface.Snapshot();
            using var data = snapshot.Encode(SKEncodedImageFormat.Png, 90);
            if (data == null)
                return null;

            return new GraphImage(data.ToArray(), width, height);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or TypeInitializationException or NotSupportedException)
        {
            RenderingSupport.DisableGraphRendering($"Построение графиков отключено: {ex.Message}");
            return null;
        }
    }

    private static GraphImage? TryRenderGraphImageWithGdi(ErgTest test, GraphRenderContext context)
    {
        const int width = 900;
        const int height = 360;

        try
        {
            using var bitmap = new Bitmap(width, height);
            bitmap.SetResolution(96, 96);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            graphics.Clear(Color.White);

            const float marginLeft = 80f;
            const float marginRight = 30f;
            const float marginTop = 20f;
            const float marginBottom = 60f;
            const float tickInside = 4f;
            const float tickOutside = 6f;

            var chartRect = new RectangleF(marginLeft, marginTop, width - marginLeft - marginRight, height - marginTop - marginBottom);

            double xMin = context.XMin;
            double xMax = context.XMax;
            double yMin = context.YMin;
            double yMax = context.YMax;

            float TransformX(double value) => (float)(chartRect.Left + (value - xMin) / (xMax - xMin) * chartRect.Width);
            float TransformY(double value) => (float)(chartRect.Bottom - (value - yMin) / (yMax - yMin) * chartRect.Height);

            var xStep = DetermineAxisStep(xMin, xMax, test.GraphXValueStep, test.GraphXLineStep);
            var yStep = DetermineAxisStep(yMin, yMax, test.GraphYValueStep, test.GraphYLineStep);
            var xTicks = BuildAxisTicks(xMin, xMax, xStep);
            var yTicks = BuildAxisTicks(yMin, yMax, yStep);

            using (var borderPen = new Pen(Color.Black, 1.5f))
            {
                graphics.DrawRectangle(borderPen, chartRect.X, chartRect.Y, chartRect.Width, chartRect.Height);
            }

            using (var axisPen = new Pen(Color.Black, 1.8f))
            {
                graphics.DrawLine(axisPen, chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom);
                graphics.DrawLine(axisPen, chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom);
            }

            using (var tickPen = new Pen(Color.Black, 1.2f))
            {
                foreach (var tick in xTicks)
                {
                    var px = TransformX(tick);
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;
                    graphics.DrawLine(tickPen, px, chartRect.Bottom - tickInside, px, chartRect.Bottom + tickOutside);
                }

                foreach (var tick in yTicks)
                {
                    var py = TransformY(tick);
                    if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                        continue;
                    graphics.DrawLine(tickPen, chartRect.Left - tickOutside, py, chartRect.Left + tickInside, py);
                }
            }

            using (var dashedPen = new Pen(Color.Black, 1.2f) { DashPattern = new[] { 4f, 4f } })
            {
                if (yMin < 0 && yMax > 0)
                {
                    var zeroY = TransformY(0);
                    graphics.DrawLine(dashedPen, chartRect.Left, zeroY, chartRect.Right, zeroY);
                }

                if (xMin < 0 && xMax > 0)
                {
                    var zeroX = TransformX(0);
                    graphics.DrawLine(dashedPen, zeroX, chartRect.Top, zeroX, chartRect.Bottom);
                }
            }

            if (test.GraphFlashPosition >= xMin && test.GraphFlashPosition <= xMax)
            {
                using var flashPen = new Pen(Color.Black, 1.5f) { DashPattern = new[] { 6f, 4f } };
                var flashX = TransformX(test.GraphFlashPosition);
                graphics.DrawLine(flashPen, flashX, chartRect.Top, flashX, chartRect.Bottom);
            }

            var graphStyles = test.GraphStyles ?? Array.Empty<GraphStyle>();

            if (context.Markers.Length > 0)
            {
                using var markerFont = new Font("Arial", 10f, FontStyle.Bold, GraphicsUnit.Point);

                foreach (var marker in context.Markers)
                {
                    var px = TransformX(marker.PositionMs);
                    if (float.IsNaN(px) || float.IsInfinity(px))
                        continue;
                    if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                        continue;

                    var markerColor = GetMarkerColor(marker);
                    using var markerPen = new Pen(markerColor, 1.5f) { DashPattern = new[] { 6f, 4f } };
                    graphics.DrawLine(markerPen, px, chartRect.Top, px, chartRect.Bottom);

                    var label = GetMarkerLabel(marker);
                    using var markerBrush = new SolidBrush(markerColor);
                    var size = graphics.MeasureString(label, markerFont);
                    float labelY = chartRect.Top - size.Height;
                    if (labelY < 2f)
                        labelY = chartRect.Top + 2f;
                    graphics.DrawString(label, markerFont, markerBrush, px - size.Width / 2f, labelY);
                }
            }

            var state = graphics.Save();
            graphics.SetClip(chartRect);

            double graphDt = test.GraphDt;
            bool hasGraphDt = graphDt > 0;

            for (int graphIndex = 0; graphIndex < context.Curves; graphIndex++)
            {
                var samples = context.Graphs[graphIndex];
                if (samples == null || samples.Length == 0)
                    continue;

                int count = context.DeclaredPointCount > 1 ? Math.Min(context.DeclaredPointCount, samples.Length) : samples.Length;
                if (count < 2)
                    continue;

                var points = new List<PointF>(count);
                for (int point = 0; point < count; point++)
                {
                    double xValue;
                    if (hasGraphDt)
                    {
                        xValue = point * graphDt;
                        if (xValue < xMin)
                            continue;
                        if (xValue > xMax)
                            break;
                    }
                    else if (count == 1)
                    {
                        xValue = xMin;
                    }
                    else
                    {
                        xValue = xMin + (xMax - xMin) * point / (count - 1);
                    }

                    double yValue = samples[point];

                    var px = TransformX(xValue);
                    var py = TransformY(yValue);
                    if (float.IsNaN(px) || float.IsNaN(py) || float.IsInfinity(px) || float.IsInfinity(py))
                        continue;

                    points.Add(new PointF(px, py));
                }

                if (points.Count < 2)
                    continue;

                var style = graphIndex < graphStyles.Length ? graphStyles[graphIndex] : null;
                var color = style != null ? Color.FromArgb(style.Red, style.Green, style.Blue) : Color.FromArgb(56, 109, 179);

                using var pen = new Pen(color, 2f) { LineJoin = LineJoin.Round };
                if (style?.Dotted == true)
                {
                    pen.DashPattern = new[] { 6f, 4f };
                }

                graphics.DrawLines(pen, points.ToArray());
            }

            graphics.Restore(state);

            using var tickFont = new Font("Arial", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

            foreach (var tick in xTicks)
            {
                var px = TransformX(tick);
                if (px < chartRect.Left - 1 || px > chartRect.Right + 1)
                    continue;
                var text = FormatAxisValue(tick);
                var size = graphics.MeasureString(text, tickFont);
                graphics.DrawString(text, tickFont, Brushes.Black, px - size.Width / 2f, chartRect.Bottom + size.Height);
            }

            using var tickFormatLeft = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

            foreach (var tick in yTicks)
            {
                var py = TransformY(tick);
                if (py < chartRect.Top - 1 || py > chartRect.Bottom + 1)
                    continue;
                var rect = new RectangleF(chartRect.Left - 16f, py - tickFont.GetHeight(graphics) / 2f, 40f, tickFont.GetHeight(graphics));
                graphics.DrawString(FormatAxisValue(tick), tickFont, Brushes.Black, rect, tickFormatLeft);
            }

            using var axisTitleFont = new Font("Arial", 11f, FontStyle.Regular, GraphicsUnit.Point);
            var xLabelSize = graphics.MeasureString("ms", axisTitleFont);
            graphics.DrawString("ms", axisTitleFont, Brushes.Black, chartRect.Left + (chartRect.Width - xLabelSize.Width) / 2f, height - xLabelSize.Height - 6f);

            graphics.TranslateTransform(20f, chartRect.Top + chartRect.Height / 2f);
            graphics.RotateTransform(-90f);
            var yLabelSize = graphics.MeasureString("µV", axisTitleFont);
            graphics.DrawString("µV", axisTitleFont, Brushes.Black, -yLabelSize.Width / 2f, -yLabelSize.Height / 2f);
            graphics.ResetTransform();

            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return new GraphImage(ms.ToArray(), width, height);
        }
        catch (Exception ex) when (ex is ExternalException or ArgumentException or PlatformNotSupportedException)
        {
            RenderingSupport.DisableGraphRendering($"Построение графиков отключено: {ex.Message}");
            return null;
        }
    }

    private static double DetermineAxisStep(double min, double max, int valueStep, int lineStep)
    {
        var range = max - min;
        if (range <= 0)
            return 0;

        if (valueStep > 0 && lineStep > 0)
            return valueStep * lineStep;

        if (valueStep > 0)
            return valueStep;

        var roughStep = range / 8.0;
        if (roughStep <= 0)
            return 0;

        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
        var normalized = roughStep / magnitude;
        double stepNormalized = normalized switch
        {
            < 1.5 => 1,
            < 3 => 2,
            < 7 => 5,
            _ => 10
        };

        return stepNormalized * magnitude;
    }

    private static double[] BuildAxisTicks(double min, double max, double step)
    {
        if (max <= min)
            return Array.Empty<double>();

        var values = new List<double>();

        if (step > 0)
        {
            double start = Math.Ceiling(min / step) * step;
            int guard = 0;
            for (double value = start; value <= max + 1e-6 && guard < 512; value += step, guard++)
            {
                values.Add(value);
            }
        }
        else
        {
            const int fallbackSegments = 5;
            double range = max - min;
            for (int i = 0; i <= fallbackSegments; i++)
            {
                double value = min + range * i / fallbackSegments;
                values.Add(value);
            }
        }

        values.Add(min);
        values.Add(max);

        return values
            .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
            .Select(v => Math.Round(v, 6))
            .Distinct()
            .Where(v => v >= min - 1e-6 && v <= max + 1e-6)
            .OrderBy(v => v)
            .ToArray();
    }

    private static string FormatAxisValue(double value)
    {
        if (Math.Abs(value) >= 1000)
            return value.ToString("0", CultureInfo.InvariantCulture);

        if (Math.Abs(value) >= 100)
            return value.ToString("0.#", CultureInfo.InvariantCulture);

        if (Math.Abs(value) >= 1)
            return value.ToString("0.##", CultureInfo.InvariantCulture);

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static Color GetMarkerColor(GraphMarker marker)
        => marker.Kind == GraphMarkerKind.AWave
            ? Color.FromArgb(217, 83, 79)
            : Color.FromArgb(92, 184, 92);

    private static string GetMarkerLabel(GraphMarker marker)
        => marker.Kind == GraphMarkerKind.AWave ? "a" : "b";

    private sealed record GraphRenderContext(
        double[][] Graphs,
        int Curves,
        int DeclaredPointCount,
        double XMin,
        double XMax,
        double YMin,
        double YMax,
        GraphMarker[] Markers);

    private enum GraphMarkerKind
    {
        AWave,
        BWave
    }

    private sealed record GraphMarker(GraphMarkerKind Kind, double PositionMs);
}

public sealed record GraphImage(byte[] Data, int Width, int Height);

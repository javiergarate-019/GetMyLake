using System.Globalization;
using GetMyLake.Core.Normalization;
using GetMyLake.Core.Search;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using SkiaSharp;

namespace GetMyLake.Core.Visualization;

public sealed class ComparisonRenderer
{
    private static readonly SKColor Background = SKColor.Parse("#F7F9FC");
    private static readonly SKColor Text = SKColor.Parse("#172033");
    private static readonly SKColor MutedText = SKColor.Parse("#58657A");
    private static readonly SKColor Border = SKColor.Parse("#D7DEEA");
    private static readonly SKColor Uruguay = SKColor.Parse("#0072B2");
    private static readonly SKColor Lake = SKColor.Parse("#D55E00");

    public IReadOnlyList<string> WriteAll(
        string outputDirectory,
        Geometry normalizedReference,
        IReadOnlyList<LakeMatchResult> results,
        ComparisonRenderOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(normalizedReference);
        ArgumentNullException.ThrowIfNull(results);

        Directory.CreateDirectory(outputDirectory);
        var paths = new List<string>(results.Count);

        for (var index = 0; index < results.Count; index++)
        {
            var result = results[index];
            var path = Path.Combine(outputDirectory, $"rank-{index + 1:00}-lake-{result.LakeId}.png");
            File.WriteAllBytes(path, RenderPng(normalizedReference, result, index + 1, options));
            paths.Add(Path.GetFullPath(path));
        }

        return paths;
    }

    public byte[] RenderPng(
        Geometry normalizedReference,
        LakeMatchResult result,
        int rank,
        ComparisonRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(normalizedReference);
        ArgumentNullException.ThrowIfNull(result);

        if (normalizedReference.IsEmpty || result.NormalizedGeometry.IsEmpty)
        {
            throw new ArgumentException("Comparison geometries must not be empty.");
        }

        if (rank <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rank));
        }

        options ??= new ComparisonRenderOptions();
        options.Validate();

        var normalizer = new GeometryNormalizer();
        var renderReference = normalizer.Normalize(normalizedReference);
        var renderCandidate = normalizer.Normalize(result.NormalizedGeometry);
        var radians = result.BestRotationDegrees * Math.PI / 180.0;
        var rotatedCandidate = AffineTransformation
            .RotationInstance(radians)
            .Transform(renderCandidate);

        var imageInfo = new SKImageInfo(options.Width, options.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(imageInfo)
            ?? throw new InvalidOperationException("Could not create the image surface.");
        var canvas = surface.Canvas;
        canvas.Clear(Background);

        DrawHeader(canvas, result, rank, options.Width);

        const float outerMargin = 36;
        const float gap = 24;
        const float plotTop = 174;
        const float bottomMargin = 82;
        var panelWidth = (options.Width - (2 * outerMargin) - (2 * gap)) / 3f;
        var panelHeight = options.Height - plotTop - bottomMargin;
        var commonEnvelope = new Envelope(renderReference.EnvelopeInternal);
        commonEnvelope.ExpandToInclude(rotatedCandidate.EnvelopeInternal);

        var referencePanel = new SKRect(outerMargin, plotTop, outerMargin + panelWidth, plotTop + panelHeight);
        var lakePanel = new SKRect(
            referencePanel.Right + gap,
            plotTop,
            referencePanel.Right + gap + panelWidth,
            plotTop + panelHeight);
        var overlayPanel = new SKRect(
            lakePanel.Right + gap,
            plotTop,
            lakePanel.Right + gap + panelWidth,
            plotTop + panelHeight);

        DrawPanel(canvas, referencePanel, "URUGUAY", commonEnvelope, (renderReference, Uruguay));
        DrawPanel(canvas, lakePanel, "LAKE", commonEnvelope, (rotatedCandidate, Lake));
        DrawPanel(
            canvas,
            overlayPanel,
            "OVERLAY",
            commonEnvelope,
            (renderReference, Uruguay),
            (rotatedCandidate, Lake));
        DrawLegend(canvas, options.Height);

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Could not encode the comparison image.");
        return encoded.ToArray();
    }

    private static void DrawHeader(SKCanvas canvas, LakeMatchResult result, int rank, float width)
    {
        using var titleFont = new SKFont(SKTypeface.Default, 34);
        using var bodyFont = new SKFont(SKTypeface.Default, 21);
        using var titlePaint = Paint(Text);
        using var bodyPaint = Paint(MutedText);

        canvas.DrawText($"Uruguay shape match #{rank}", 36, 50, SKTextAlign.Left, titleFont, titlePaint);
        var name = string.IsNullOrWhiteSpace(result.LakeName) ? "Unnamed lake" : result.LakeName;
        canvas.DrawText(
            $"HydroLAKES {result.LakeId}  |  {name}  |  {Display(result.Country)}  |  " +
            $"{result.AreaKm2.ToString("0.###", CultureInfo.InvariantCulture)} km2  |  {LakeTypeName(result.LakeType)}",
            36,
            91,
            SKTextAlign.Left,
            bodyFont,
            bodyPaint);
        canvas.DrawText(
            $"Similarity {result.Similarity:P2}  |  Best rotation {result.BestRotationDegrees:F2} degrees",
            36,
            126,
            SKTextAlign.Left,
            bodyFont,
            bodyPaint);

        using var rule = new SKPaint { Color = Border, StrokeWidth = 1, IsAntialias = true };
        canvas.DrawLine(36, 148, width - 36, 148, rule);
    }

    private static void DrawPanel(
        SKCanvas canvas,
        SKRect panel,
        string title,
        Envelope commonEnvelope,
        params (Geometry Geometry, SKColor Color)[] shapes)
    {
        using var panelPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill };
        using var borderPaint = new SKPaint
        {
            Color = Border,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };
        using var titleFont = new SKFont(SKTypeface.Default, 18);
        using var titlePaint = Paint(MutedText);

        canvas.DrawRoundRect(panel, 12, 12, panelPaint);
        canvas.DrawRoundRect(panel, 12, 12, borderPaint);
        canvas.DrawText(title, panel.Left + 20, panel.Top + 32, SKTextAlign.Left, titleFont, titlePaint);

        var plot = new SKRect(panel.Left + 32, panel.Top + 54, panel.Right - 32, panel.Bottom - 28);
        foreach (var shape in shapes)
        {
            using var path = CreatePath(shape.Geometry, commonEnvelope, plot);
            using var fill = new SKPaint
            {
                Color = shape.Color.WithAlpha(shapes.Length > 1 ? (byte)120 : (byte)185),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            using var stroke = new SKPaint
            {
                Color = shape.Color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3,
                IsAntialias = true,
                StrokeJoin = SKStrokeJoin.Round
            };
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);
        }
    }

    private static SKPath CreatePath(Geometry geometry, Envelope envelope, SKRect plot)
    {
        using var builder = new SKPathBuilder { FillType = SKPathFillType.Winding };
        var width = Math.Max(envelope.Width, 1e-12);
        var height = Math.Max(envelope.Height, 1e-12);
        var scale = Math.Min(plot.Width / width, plot.Height / height) * 0.9;
        var centerX = (envelope.MinX + envelope.MaxX) / 2;
        var centerY = (envelope.MinY + envelope.MaxY) / 2;

        AddGeometry(builder, geometry, coordinate => new SKPoint(
            plot.MidX + (float)((coordinate.X - centerX) * scale),
            plot.MidY - (float)((coordinate.Y - centerY) * scale)));
        return builder.Detach();
    }

    private static void AddGeometry(SKPathBuilder path, Geometry geometry, Func<Coordinate, SKPoint> map)
    {
        if (geometry is Polygon polygon)
        {
            var coordinates = polygon.ExteriorRing.Coordinates;
            if (coordinates.Length == 0)
            {
                return;
            }

            path.MoveTo(map(coordinates[0]));
            for (var index = 1; index < coordinates.Length; index++)
            {
                path.LineTo(map(coordinates[index]));
            }

            path.Close();
            return;
        }

        for (var index = 0; index < geometry.NumGeometries; index++)
        {
            AddGeometry(path, geometry.GetGeometryN(index), map);
        }
    }

    private static void DrawLegend(SKCanvas canvas, float height)
    {
        using var font = new SKFont(SKTypeface.Default, 18);
        using var textPaint = Paint(MutedText);
        using var uruguayPaint = new SKPaint { Color = Uruguay, Style = SKPaintStyle.Fill };
        using var lakePaint = new SKPaint { Color = Lake, Style = SKPaintStyle.Fill };
        var y = height - 42;

        canvas.DrawRect(36, y - 15, 18, 18, uruguayPaint);
        canvas.DrawText("Uruguay", 64, y, SKTextAlign.Left, font, textPaint);
        canvas.DrawRect(170, y - 15, 18, 18, lakePaint);
        canvas.DrawText("Lake after best rotation", 198, y, SKTextAlign.Left, font, textPaint);
        canvas.DrawText(
            "Shapes are centered and normalized to equal area",
            470,
            y,
            SKTextAlign.Left,
            font,
            textPaint);
    }

    private static SKPaint Paint(SKColor color) => new()
    {
        Color = color,
        IsAntialias = true
    };

    private static string Display(string value) => string.IsNullOrWhiteSpace(value) ? "Unknown country" : value;

    private static string LakeTypeName(int lakeType) => lakeType switch
    {
        1 => "Lake",
        2 => "Reservoir",
        3 => "Regulated natural lake",
        _ => $"Unknown type {lakeType}"
    };
}

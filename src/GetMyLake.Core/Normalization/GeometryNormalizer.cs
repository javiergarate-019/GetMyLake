using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;

namespace GetMyLake.Core.Normalization;

/// <summary>
/// Converts polygonal geometries into comparable, origin-centered unit-area outlines.
/// </summary>
public sealed class GeometryNormalizer
{
    public Geometry Normalize(Geometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        if (geometry.IsEmpty)
        {
            throw new ArgumentException("The geometry must not be empty.", nameof(geometry));
        }

        var repaired = geometry.IsValid ? geometry.Copy() : GeometryFixer.Fix(geometry);
        var polygon = FindLargestPolygon(repaired)
            ?? throw new ArgumentException("The geometry does not contain a polygon.", nameof(geometry));

        var shell = polygon.Factory.CreateLinearRing(polygon.ExteriorRing.Coordinates);
        var outline = polygon.Factory.CreatePolygon(shell);
        if (outline.Area <= 0 || !double.IsFinite(outline.Area))
        {
            throw new ArgumentException("The polygon must have a finite, positive area.", nameof(geometry));
        }

        var centroid = outline.Centroid.Coordinate;
        var centered = AffineTransformation
            .TranslationInstance(-centroid.X, -centroid.Y)
            .Transform(outline);

        var scale = 1.0 / Math.Sqrt(centered.Area);
        return AffineTransformation.ScaleInstance(scale, scale).Transform(centered);
    }

    private static Polygon? FindLargestPolygon(Geometry geometry)
    {
        Polygon? largest = null;

        for (var index = 0; index < geometry.NumGeometries; index++)
        {
            var component = geometry.GetGeometryN(index);
            if (component is Polygon polygon && (largest is null || polygon.Area > largest.Area))
            {
                largest = polygon;
            }
        }

        return largest;
    }
}

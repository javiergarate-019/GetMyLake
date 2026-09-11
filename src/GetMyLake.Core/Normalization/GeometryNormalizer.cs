using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Simplify;

namespace GetMyLake.Core.Normalization;

/// <summary>
/// Converts polygonal geometries into comparable, origin-centered unit-area outlines.
/// </summary>
public sealed class GeometryNormalizer
{
    public Geometry Normalize(
        Geometry geometry,
        double simplificationTolerance = 0,
        bool repairInvalid = true)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        if (geometry.IsEmpty)
        {
            throw new ArgumentException("The geometry must not be empty.", nameof(geometry));
        }

        var repaired = !repairInvalid || geometry.IsValid
            ? geometry.Copy()
            : GeometryFixer.Fix(geometry);
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
        var normalized = AffineTransformation.ScaleInstance(scale, scale).Transform(centered);

        if (simplificationTolerance <= 0)
        {
            return normalized;
        }

        var simplified = DouglasPeuckerSimplifier.Simplify(normalized, simplificationTolerance);
        return simplified.IsEmpty ? normalized : simplified;
    }

    private static Polygon? FindLargestPolygon(Geometry geometry)
    {
        if (geometry is Polygon polygonGeometry)
        {
            return polygonGeometry;
        }

        Polygon? largest = null;

        for (var index = 0; index < geometry.NumGeometries; index++)
        {
            var component = geometry.GetGeometryN(index);
            if (ReferenceEquals(component, geometry))
            {
                continue;
            }

            var polygon = FindLargestPolygon(component);
            if (polygon is not null && (largest is null || polygon.Area > largest.Area))
            {
                largest = polygon;
            }
        }

        return largest;
    }
}

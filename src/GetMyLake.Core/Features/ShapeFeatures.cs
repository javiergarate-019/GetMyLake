using NetTopologySuite.Geometries;

namespace GetMyLake.Core.Features;

public sealed record ShapeFeatures(double Compactness, double Elongation)
{
    public static ShapeFeatures Extract(Geometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        if (geometry.IsEmpty || geometry.Area <= 0 || geometry.Length <= 0)
        {
            throw new ArgumentException("A non-empty polygon with positive area is required.", nameof(geometry));
        }

        var compactness = 4 * Math.PI * geometry.Area / (geometry.Length * geometry.Length);
        var coordinates = geometry.Coordinates;
        var meanX = coordinates.Average(coordinate => coordinate.X);
        var meanY = coordinates.Average(coordinate => coordinate.Y);
        var xx = 0.0;
        var yy = 0.0;
        var xy = 0.0;

        foreach (var coordinate in coordinates)
        {
            var x = coordinate.X - meanX;
            var y = coordinate.Y - meanY;
            xx += x * x;
            yy += y * y;
            xy += x * y;
        }

        xx /= coordinates.Length;
        yy /= coordinates.Length;
        xy /= coordinates.Length;

        var trace = xx + yy;
        var discriminant = Math.Sqrt(Math.Max(0, ((xx - yy) * (xx - yy)) + (4 * xy * xy)));
        var largest = Math.Max((trace + discriminant) / 2, 1e-12);
        var smallest = Math.Max((trace - discriminant) / 2, 1e-12);
        return new ShapeFeatures(compactness, Math.Sqrt(largest / smallest));
    }

    public double DistanceTo(ShapeFeatures other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var compactnessDistance = Math.Abs(Math.Log(Compactness / other.Compactness));
        var elongationDistance = Math.Abs(Math.Log(Elongation / other.Elongation));
        return compactnessDistance + elongationDistance;
    }
}

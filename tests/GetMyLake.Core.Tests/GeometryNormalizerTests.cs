using GetMyLake.Core.Normalization;
using NetTopologySuite.Geometries;

namespace GetMyLake.Core.Tests;

public sealed class GeometryNormalizerTests
{
    private readonly GeometryFactory _factory = new();

    [Fact]
    public void Normalize_CentersGeometryAndScalesAreaToOne()
    {
        var polygon = _factory.CreatePolygon(
        [
            new Coordinate(10, 20),
            new Coordinate(14, 20),
            new Coordinate(14, 22),
            new Coordinate(10, 22),
            new Coordinate(10, 20)
        ]);

        var normalized = new GeometryNormalizer().Normalize(polygon);

        Assert.Equal(1, normalized.Area, 10);
        Assert.Equal(0, normalized.Centroid.X, 10);
        Assert.Equal(0, normalized.Centroid.Y, 10);
    }

    [Fact]
    public void Normalize_UsesLargestPolygonAndDropsHoles()
    {
        var small = _factory.CreatePolygon(
        [
            new Coordinate(0, 0), new Coordinate(1, 0), new Coordinate(1, 1),
            new Coordinate(0, 1), new Coordinate(0, 0)
        ]);
        var large = _factory.CreatePolygon(
        [
            new Coordinate(0, 0), new Coordinate(4, 0), new Coordinate(4, 2),
            new Coordinate(0, 2), new Coordinate(0, 0)
        ]);

        var normalized = new GeometryNormalizer().Normalize(_factory.CreateMultiPolygon([small, large]));

        Assert.Equal(1, normalized.Area, 10);
        Assert.Equal(2, normalized.EnvelopeInternal.Width / normalized.EnvelopeInternal.Height, 10);
    }
}

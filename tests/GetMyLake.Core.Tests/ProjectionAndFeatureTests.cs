using GetMyLake.Core.Features;
using GetMyLake.Core.Normalization;
using GetMyLake.Core.Projection;
using NetTopologySuite.Geometries;

namespace GetMyLake.Core.Tests;

public sealed class ProjectionAndFeatureTests
{
    private readonly GeometryFactory _factory = new(new PrecisionModel(), 4326);

    [Fact]
    public void LocalProjection_CentersLongitudeLatitudeGeometryNearOrigin()
    {
        var uruguayLikeBox = _factory.CreatePolygon(
        [
            new Coordinate(-58, -35), new Coordinate(-53, -35),
            new Coordinate(-53, -30), new Coordinate(-58, -30),
            new Coordinate(-58, -35)
        ]);

        var projected = new LambertAzimuthalEqualAreaProjector().Project(uruguayLikeBox);

        Assert.InRange(Math.Abs(projected.Centroid.X), 0, 0.01);
        Assert.InRange(Math.Abs(projected.Centroid.Y), 0, 0.01);
        Assert.True(projected.Area > 0);
    }

    [Fact]
    public void ShapeFeatures_AreInvariantToTranslationAndScaleAfterNormalization()
    {
        var first = _factory.CreatePolygon(
        [
            new Coordinate(0, 0), new Coordinate(4, 0), new Coordinate(1, 3), new Coordinate(0, 0)
        ]);
        var second = _factory.CreatePolygon(
        [
            new Coordinate(10, 20), new Coordinate(18, 20), new Coordinate(12, 26), new Coordinate(10, 20)
        ]);
        var normalizer = new GeometryNormalizer();

        var distance = ShapeFeatures.Extract(normalizer.Normalize(first))
            .DistanceTo(ShapeFeatures.Extract(normalizer.Normalize(second)));

        Assert.InRange(distance, 0, 1e-10);
    }
}

using GetMyLake.Core.Matching;
using GetMyLake.Core.Similarity;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;

namespace GetMyLake.Core.Tests;

public sealed class ShapeMatcherTests
{
    private readonly GeometryFactory _factory = new();

    [Fact]
    public void IntersectionOverUnion_OfIdenticalPolygons_IsOne()
    {
        var triangle = CreateTriangle();

        var score = IntersectionOverUnion.Calculate(triangle, triangle.Copy());

        Assert.Equal(1, score, 10);
    }

    [Fact]
    public void FindBestRotation_AlignsRotatedShape()
    {
        var reference = CreateTriangle();
        var candidate = AffineTransformation
            .RotationInstance(37 * Math.PI / 180.0)
            .Transform(reference);

        var result = new ShapeMatcher().FindBestRotation(
            reference,
            candidate,
            new RotationSearchOptions { CoarseStepDegrees = 2, FineStepDegrees = 0.1 });

        Assert.True(result.Similarity > 0.999, $"Actual similarity: {result.Similarity}");
        Assert.InRange(result.RotationDegrees, 322.9, 323.1);
    }

    private Polygon CreateTriangle() => _factory.CreatePolygon(
    [
        new Coordinate(0, 0),
        new Coordinate(4, 0),
        new Coordinate(1, 3),
        new Coordinate(0, 0)
    ]);
}

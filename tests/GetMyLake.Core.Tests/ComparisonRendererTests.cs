using GetMyLake.Core.Search;
using GetMyLake.Core.Visualization;
using NetTopologySuite.Geometries;
using SkiaSharp;

namespace GetMyLake.Core.Tests;

public sealed class ComparisonRendererTests
{
    [Fact]
    public void RenderPng_CreatesDecodableImageAtRequestedSize()
    {
        var geometry = new GeometryFactory().CreatePolygon(
        [
            new Coordinate(-1, -1),
            new Coordinate(1, -1),
            new Coordinate(0.5, 1),
            new Coordinate(-1, -1)
        ]);
        var result = new LakeMatchResult(188254, "", "Canada", 0.15, 1, 0.8874827, 347.5, geometry);

        var bytes = new ComparisonRenderer().RenderPng(
            geometry,
            result,
            1,
            new ComparisonRenderOptions { Width = 900, Height = 500 });

        Assert.True(bytes.Length > 1_000);
        using var bitmap = SKBitmap.Decode(bytes);
        Assert.NotNull(bitmap);
        Assert.Equal(900, bitmap.Width);
        Assert.Equal(500, bitmap.Height);
    }
}

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;

namespace GetMyLake.Core.Similarity;

public static class IntersectionOverUnion
{
    public static double Calculate(Geometry left, Geometry right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.IsEmpty || right.IsEmpty)
        {
            return 0;
        }

        try
        {
            return CalculateCore(left, right);
        }
        catch (TopologyException)
        {
            return CalculateCore(GeometryFixer.Fix(left), GeometryFixer.Fix(right));
        }
    }

    private static double CalculateCore(Geometry left, Geometry right)
    {
        var intersectionArea = left.Intersection(right).Area;
        var unionArea = left.Union(right).Area;
        return unionArea <= 0 ? 0 : Math.Clamp(intersectionArea / unionArea, 0, 1);
    }
}

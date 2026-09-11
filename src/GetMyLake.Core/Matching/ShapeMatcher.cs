using GetMyLake.Core.Normalization;
using GetMyLake.Core.Similarity;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;

namespace GetMyLake.Core.Matching;

public sealed class ShapeMatcher(GeometryNormalizer? normalizer = null)
{
    private readonly GeometryNormalizer _normalizer = normalizer ?? new GeometryNormalizer();

    public ShapeMatchResult FindBestRotation(
        Geometry reference,
        Geometry candidate,
        RotationSearchOptions? options = null)
    {
        options ??= new RotationSearchOptions();
        options.Validate();

        var normalizedReference = _normalizer.Normalize(reference);
        var normalizedCandidate = _normalizer.Normalize(candidate);

        var best = Search(
            normalizedReference,
            normalizedCandidate,
            startDegrees: 0,
            endDegrees: 360 - options.CoarseStepDegrees,
            options.CoarseStepDegrees);

        return Search(
            normalizedReference,
            normalizedCandidate,
            best.RotationDegrees - options.CoarseStepDegrees,
            best.RotationDegrees + options.CoarseStepDegrees,
            options.FineStepDegrees,
            best);
    }

    private static ShapeMatchResult Search(
        Geometry reference,
        Geometry candidate,
        double startDegrees,
        double endDegrees,
        double stepDegrees,
        ShapeMatchResult? initialBest = null)
    {
        var best = initialBest ?? new ShapeMatchResult(-1, 0);

        for (var degrees = startDegrees; degrees <= endDegrees + 1e-9; degrees += stepDegrees)
        {
            var normalizedDegrees = NormalizeDegrees(degrees);
            var radians = normalizedDegrees * Math.PI / 180.0;
            var rotated = AffineTransformation.RotationInstance(radians).Transform(candidate);
            var score = IntersectionOverUnion.Calculate(reference, rotated);

            if (score > best.Similarity)
            {
                best = new ShapeMatchResult(score, normalizedDegrees);
            }
        }

        return best;
    }

    private static double NormalizeDegrees(double degrees)
    {
        var normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}

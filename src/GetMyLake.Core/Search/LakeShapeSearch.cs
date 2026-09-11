using System.Collections.Concurrent;
using GetMyLake.Core.Data;
using GetMyLake.Core.Features;
using GetMyLake.Core.Matching;
using GetMyLake.Core.Normalization;
using GetMyLake.Core.Projection;
using NetTopologySuite.Geometries;

namespace GetMyLake.Core.Search;

public sealed class LakeShapeSearch(
    NaturalEarthCountryReader? countryReader = null,
    HydroLakesReader? lakesReader = null,
    LambertAzimuthalEqualAreaProjector? projector = null,
    GeometryNormalizer? normalizer = null)
{
    private readonly NaturalEarthCountryReader _countryReader = countryReader ?? new NaturalEarthCountryReader();
    private readonly HydroLakesReader _lakesReader = lakesReader ?? new HydroLakesReader();
    private readonly LambertAzimuthalEqualAreaProjector _projector = projector ?? new LambertAzimuthalEqualAreaProjector();
    private readonly GeometryNormalizer _normalizer = normalizer ?? new GeometryNormalizer();

    public IReadOnlyList<LakeMatchResult> Run(
        string naturalEarthShapefile,
        string referenceCountryCode,
        string hydroLakesShapefile,
        LakeSearchOptions? options = null,
        Action<SearchProgress>? progress = null)
    {
        options ??= new LakeSearchOptions();
        options.Validate();

        ArgumentException.ThrowIfNullOrWhiteSpace(referenceCountryCode);

        var referenceGeographic = _countryReader.ReadByCode(naturalEarthShapefile, referenceCountryCode);
        var reference = Prepare(referenceGeographic, options.SimplificationTolerance, repairInvalid: true);
        var referenceFeatures = ShapeFeatures.Extract(reference);
        var total = _lakesReader.GetRecordCount(hydroLakesShapefile);
        var retained = new PriorityQueue<PreparedLake, double>();
        var processed = 0;
        var errors = 0;

        foreach (var lake in _lakesReader.Read(hydroLakesShapefile))
        {
            processed++;
            try
            {
                var geometry = Prepare(lake.Geometry, options.SimplificationTolerance, repairInvalid: false);
                var featureDistance = ShapeFeatures.Extract(geometry).DistanceTo(referenceFeatures);
                retained.Enqueue(new PreparedLake(
                    lake.Id,
                    lake.Name,
                    lake.Country,
                    lake.AreaKm2,
                    lake.LakeType,
                    geometry), -featureDistance);
                if (retained.Count > options.PrefilterCount)
                {
                    retained.Dequeue();
                }
            }
            catch (Exception exception) when (exception is ArgumentException or TopologyException or ArithmeticException)
            {
                errors++;
            }

            if (processed % 25_000 == 0 || processed == total)
            {
                progress?.Invoke(new SearchProgress("Prefilter", processed, total, retained.Count, errors));
            }
        }

        var candidates = retained.UnorderedItems.Select(item => item.Element).ToArray();
        var matches = new ConcurrentBag<LakeMatchResult>();
        var fineProcessed = 0;
        var fineErrors = 0;
        var matcher = new ShapeMatcher();

        Parallel.ForEach(
            candidates,
            new ParallelOptions { MaxDegreeOfParallelism = options.MaxDegreeOfParallelism },
            candidate =>
            {
                try
                {
                    var match = matcher.FindBestRotation(reference, candidate.Geometry);
                    matches.Add(new LakeMatchResult(
                        candidate.Id,
                        candidate.Name,
                        candidate.Country,
                        candidate.AreaKm2,
                        candidate.LakeType,
                        match.Similarity,
                        match.RotationDegrees));
                }
                catch (Exception exception) when (exception is ArgumentException or TopologyException or ArithmeticException)
                {
                    Interlocked.Increment(ref fineErrors);
                }

                var current = Interlocked.Increment(ref fineProcessed);
                if (current % 25 == 0 || current == candidates.Length)
                {
                    progress?.Invoke(new SearchProgress(
                        "Fine match",
                        current,
                        candidates.Length,
                        candidates.Length,
                        errors + fineErrors));
                }
            });

        return matches
            .OrderByDescending(match => match.Similarity)
            .ThenBy(match => match.LakeId)
            .Take(options.TopCount)
            .ToArray();
    }

    private Geometry Prepare(Geometry geometry, double simplificationTolerance, bool repairInvalid)
    {
        var projected = _projector.Project(geometry);
        return _normalizer.Normalize(projected, simplificationTolerance, repairInvalid);
    }

    private sealed record PreparedLake(
        long Id,
        string Name,
        string Country,
        double AreaKm2,
        int LakeType,
        Geometry Geometry);
}

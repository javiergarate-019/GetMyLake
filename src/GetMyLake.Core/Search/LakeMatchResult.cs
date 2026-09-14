using NetTopologySuite.Geometries;

namespace GetMyLake.Core.Search;

public sealed record LakeMatchResult(
    long LakeId,
    string LakeName,
    string Country,
    double AreaKm2,
    int LakeType,
    double Similarity,
    double BestRotationDegrees,
    Geometry NormalizedGeometry);

using NetTopologySuite.Geometries;

namespace GetMyLake.Core.Data;

public sealed record HydroLakeShape(
    long Id,
    string Name,
    string Country,
    double AreaKm2,
    int LakeType,
    Geometry Geometry);

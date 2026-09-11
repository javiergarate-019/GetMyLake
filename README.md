# GetMyLake

GetMyLake is a general-purpose polygon shape-matching tool. Its first research
task is to rank the lakes in HydroLAKES by how closely their outline resembles
Uruguay, regardless of geographic position, real-world size, or rotation.

## Current status

The initial geometry engine can:

- repair invalid polygonal geometry;
- select the largest polygon from a multipart shape;
- compare exterior silhouettes while ignoring holes;
- center shapes at the origin and scale them to unit area;
- calculate Intersection over Union (IoU);
- find the best rotation using a coarse-to-fine search.

Dataset ingestion, candidate filtering, CSV output, and comparison images are
the next implementation stages.

## Requirements

- .NET SDK 10

## Build and test

```powershell
dotnet build GetMyLake.slnx
dotnet test GetMyLake.slnx --no-build
```

## Try the geometry engine

```powershell
dotnet run --project src/GetMyLake.Cli -- compare-wkt `
  "POLYGON ((0 0, 4 0, 1 3, 0 0))" `
  "POLYGON ((10 10, 14 10, 11 13, 10 10))"
```

## Planned data sources

- [HydroLAKES](https://www.hydrosheds.org/products/hydrolakes) for candidate lakes.
- [Natural Earth Admin 0 Countries, 1:10m](https://www.naturalearthdata.com/downloads/10m-cultural-vectors/10m-admin-0-countries/) for the initial Uruguay reference shape.

The matching engine is not tied to either dataset. Future adapters can accept
other reference shapes and candidate polygon collections.

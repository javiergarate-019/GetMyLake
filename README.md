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

The end-to-end Uruguay search also supports:

- streaming all 1.4 million HydroLAKES records without loading the dataset at once;
- a local Lambert azimuthal equal-area projection for every source geometry;
- rotation-invariant compactness and elongation prefiltering;
- parallel coarse-to-fine IoU matching of the retained candidates;
- ranked CSV output with HydroLAKES identifiers and metadata.

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

## Search HydroLAKES for Uruguay-shaped lakes

Place the extracted datasets at the default paths shown below, then run:

```powershell
dotnet run --project src/GetMyLake.Cli -- search-uruguay
```

Default input files:

```text
data/natural-earth/ne_10m_admin_0_countries.shp
data/hydrolakes/HydroLAKES_polys_v10_shp/HydroLAKES_polys_v10.shp
```

The default result is `results/uruguay-lakes.csv`. Input paths and search size
can be changed explicitly:

```powershell
dotnet run --project src/GetMyLake.Cli -- search-uruguay `
  --natural-earth data/natural-earth/ne_10m_admin_0_countries.shp `
  --hydrolakes data/hydrolakes/HydroLAKES_polys_v10_shp/HydroLAKES_polys_v10.shp `
  --prefilter 1000 `
  --top 20 `
  --output results/uruguay-lakes.csv
```

Look up the geographic coordinates and bounds of any ranked feature:

```powershell
dotnet run --project src/GetMyLake.Cli -- lake-info 188254
```

## Planned data sources

- [HydroLAKES](https://www.hydrosheds.org/products/hydrolakes) for candidate lakes.
- [Natural Earth Admin 0 Countries, 1:10m](https://www.naturalearthdata.com/downloads/10m-cultural-vectors/10m-admin-0-countries/) for the initial Uruguay reference shape.

The matching engine is not tied to either dataset. Future adapters can accept
other reference shapes and candidate polygon collections.

HydroLAKES is distributed under the Creative Commons Attribution 4.0 license.
Downloaded datasets and generated results are intentionally excluded from Git.

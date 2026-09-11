using System.Diagnostics;
using System.Globalization;
using GetMyLake.Core.Data;
using GetMyLake.Core.Matching;
using GetMyLake.Core.Search;
using NetTopologySuite.IO;

return args.Length == 0 || args[0] is "--help" or "-h"
    ? PrintHelp(0)
    : args[0].ToLowerInvariant() switch
    {
        "compare-wkt" => CompareWkt(args),
        "lake-info" => LakeInfo(args),
        "search-uruguay" => SearchUruguay(args),
        _ => UnknownCommand(args[0])
    };

static int CompareWkt(string[] arguments)
{
    if (arguments.Length != 3)
    {
        Console.Error.WriteLine("compare-wkt requires exactly two WKT geometries.");
        return 1;
    }

    try
    {
        var reader = new WKTReader();
        var result = new ShapeMatcher().FindBestRotation(reader.Read(arguments[1]), reader.Read(arguments[2]));
        Console.WriteLine($"Similarity: {result.Similarity:P2}");
        Console.WriteLine($"Best rotation: {result.RotationDegrees:F2} degrees");
        return 0;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Comparison failed: {exception.Message}");
        return 2;
    }
}

static int SearchUruguay(string[] arguments)
{
    try
    {
        var values = ParseOptions(arguments.Skip(1).ToArray());
        var naturalEarth = Get(values, "--natural-earth", "data/natural-earth/ne_10m_admin_0_countries.shp");
        var hydroLakes = Get(
            values,
            "--hydrolakes",
            "data/hydrolakes/HydroLAKES_polys_v10_shp/HydroLAKES_polys_v10.shp");
        var output = Get(values, "--output", "results/uruguay-lakes.csv");
        var options = new LakeSearchOptions
        {
            PrefilterCount = GetInt(values, "--prefilter", 500),
            TopCount = GetInt(values, "--top", 20),
            MaxDegreeOfParallelism = GetInt(values, "--threads", Environment.ProcessorCount)
        };

        RequireFile(naturalEarth, "Natural Earth");
        RequireFile(hydroLakes, "HydroLAKES");

        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("Reference: Uruguay (Natural Earth ADM0_A3=URY)");
        Console.WriteLine($"Candidates: {Path.GetFullPath(hydroLakes)}");
        Console.WriteLine($"Prefilter retention: {options.PrefilterCount:N0}");
        Console.WriteLine();

        var results = new LakeShapeSearch().Run(
            naturalEarth,
            "URY",
            hydroLakes,
            options,
            progress => Console.WriteLine(
                $"{progress.Stage}: {progress.Processed:N0}/{progress.Total:N0}; " +
                $"retained={progress.RetainedCandidates:N0}; errors={progress.Errors:N0}"));

        CsvResultWriter.Write(output, results);
        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine("Rank  Lake ID   Similarity  Rotation   Area km2  Name / country");
        for (var index = 0; index < results.Count; index++)
        {
            var result = results[index];
            Console.WriteLine(
                $"{index + 1,4}  {result.LakeId,7}   {result.Similarity,9:P2}  " +
                $"{result.BestRotationDegrees,7:F2}  {result.AreaKm2,9:F2}  " +
                $"{DisplayName(result.LakeName)} / {DisplayName(result.Country)} " +
                $"[{LakeTypeName(result.LakeType)}]");
        }

        Console.WriteLine();
        Console.WriteLine($"CSV: {Path.GetFullPath(output)}");
        Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");
        return 0;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Search failed: {exception.Message}");
        return 2;
    }
}

static int LakeInfo(string[] arguments)
{
    try
    {
        if (arguments.Length < 2 || !long.TryParse(arguments[1], out var lakeId))
        {
            throw new ArgumentException("lake-info requires a numeric HydroLAKES ID.");
        }

        var values = ParseOptions(arguments.Skip(2).ToArray());
        var hydroLakes = Get(
            values,
            "--hydrolakes",
            "data/hydrolakes/HydroLAKES_polys_v10_shp/HydroLAKES_polys_v10.shp");
        RequireFile(hydroLakes, "HydroLAKES");

        var lake = new HydroLakesReader().Read(hydroLakes).FirstOrDefault(item => item.Id == lakeId)
            ?? throw new InvalidOperationException($"HydroLAKES ID {lakeId} was not found.");
        var centroid = lake.Geometry.Centroid;
        var inside = lake.Geometry.InteriorPoint;
        var bounds = lake.Geometry.EnvelopeInternal;

        Console.WriteLine($"HydroLAKES ID: {lake.Id}");
        Console.WriteLine($"Name: {DisplayName(lake.Name)}");
        Console.WriteLine($"Country: {DisplayName(lake.Country)}");
        Console.WriteLine($"Area: {lake.AreaKm2.ToString("0.###", CultureInfo.InvariantCulture)} km2");
        Console.WriteLine($"Type: {LakeTypeName(lake.LakeType)}");
        Console.WriteLine($"Interior point: {Coordinate(inside.Y)}, {Coordinate(inside.X)} (latitude, longitude)");
        Console.WriteLine($"Centroid: {Coordinate(centroid.Y)}, {Coordinate(centroid.X)} (latitude, longitude)");
        Console.WriteLine(
            $"Bounds: latitude {Coordinate(bounds.MinY)} to {Coordinate(bounds.MaxY)}; " +
            $"longitude {Coordinate(bounds.MinX)} to {Coordinate(bounds.MaxX)}");
        return 0;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Lake lookup failed: {exception.Message}");
        return 2;
    }
}

static Dictionary<string, string> ParseOptions(string[] arguments)
{
    if (arguments.Length % 2 != 0)
    {
        throw new ArgumentException("Every search option must have a value.");
    }

    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var index = 0; index < arguments.Length; index += 2)
    {
        if (!arguments[index].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Invalid option: {arguments[index]}");
        }

        values[arguments[index]] = arguments[index + 1];
    }

    return values;
}

static string Get(IReadOnlyDictionary<string, string> values, string name, string defaultValue) =>
    values.TryGetValue(name, out var value) ? value : defaultValue;

static int GetInt(IReadOnlyDictionary<string, string> values, string name, int defaultValue) =>
    values.TryGetValue(name, out var value) && int.TryParse(value, out var parsed)
        ? parsed
        : values.ContainsKey(name)
            ? throw new ArgumentException($"{name} requires an integer value.")
            : defaultValue;

static void RequireFile(string path, string label)
{
    if (!File.Exists(path))
    {
        throw new FileNotFoundException($"{label} Shapefile was not found.", Path.GetFullPath(path));
    }
}

static string DisplayName(string value) => string.IsNullOrWhiteSpace(value) ? "Unnamed" : value;

static string Coordinate(double value) => value.ToString("0.000000", CultureInfo.InvariantCulture);

static string LakeTypeName(int lakeType) => lakeType switch
{
    1 => "Lake",
    2 => "Reservoir",
    3 => "Lake control",
    _ => $"Unknown type {lakeType}"
};

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    return PrintHelp(1);
}

static int PrintHelp(int exitCode)
{
    Console.WriteLine("GetMyLake - polygon shape matching");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  compare-wkt <reference-wkt> <candidate-wkt>");
    Console.WriteLine("  lake-info <HydroLAKES-ID> [--hydrolakes <path>]");
    Console.WriteLine("  search-uruguay [options]");
    Console.WriteLine();
    Console.WriteLine("search-uruguay options:");
    Console.WriteLine("  --natural-earth <path>  Natural Earth countries Shapefile");
    Console.WriteLine("  --hydrolakes <path>     HydroLAKES polygons Shapefile");
    Console.WriteLine("  --output <path>         Output CSV (default: results/uruguay-lakes.csv)");
    Console.WriteLine("  --prefilter <count>     Candidates retained for IoU (default: 500)");
    Console.WriteLine("  --top <count>           Ranked results written (default: 20)");
    Console.WriteLine("  --threads <count>       Fine-matching parallelism");
    return exitCode;
}

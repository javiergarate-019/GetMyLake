using GetMyLake.Core.Matching;
using NetTopologySuite.IO;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    PrintHelp();
    return 0;
}

if (!string.Equals(args[0], "compare-wkt", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Unknown command: {args[0]}");
    PrintHelp();
    return 1;
}

if (args.Length != 3)
{
    Console.Error.WriteLine("compare-wkt requires exactly two WKT geometries.");
    return 1;
}

try
{
    var reader = new WKTReader();
    var reference = reader.Read(args[1]);
    var candidate = reader.Read(args[2]);
    var result = new ShapeMatcher().FindBestRotation(reference, candidate);

    Console.WriteLine($"Similarity: {result.Similarity:P2}");
    Console.WriteLine($"Best rotation: {result.RotationDegrees:F2} degrees");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Comparison failed: {exception.Message}");
    return 2;
}

static void PrintHelp()
{
    Console.WriteLine("GetMyLake - polygon shape matching");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  getmylake compare-wkt <reference-wkt> <candidate-wkt>");
}

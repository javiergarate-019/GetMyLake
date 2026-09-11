using System.Globalization;
using System.Text;

namespace GetMyLake.Core.Search;

public static class CsvResultWriter
{
    public static void Write(string path, IReadOnlyList<LakeMatchResult> results)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(results);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        Directory.CreateDirectory(directory!);

        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("Rank,LakeId,LakeName,Country,AreaKm2,Similarity,BestRotation,LakeTypeCode,LakeType");

        for (var index = 0; index < results.Count; index++)
        {
            var result = results[index];
            writer.WriteLine(string.Join(",",
                index + 1,
                result.LakeId,
                Escape(result.LakeName),
                Escape(result.Country),
                result.AreaKm2.ToString("0.###", CultureInfo.InvariantCulture),
                result.Similarity.ToString("0.########", CultureInfo.InvariantCulture),
                result.BestRotationDegrees.ToString("0.###", CultureInfo.InvariantCulture),
                result.LakeType,
                Escape(LakeTypeName(result.LakeType))));
        }
    }

    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static string LakeTypeName(int lakeType) => lakeType switch
    {
        1 => "Lake",
        2 => "Reservoir",
        3 => "Lake control",
        _ => "Unknown"
    };
}

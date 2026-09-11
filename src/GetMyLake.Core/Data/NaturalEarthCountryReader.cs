using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace GetMyLake.Core.Data;

public sealed class NaturalEarthCountryReader(GeometryFactory? geometryFactory = null)
{
    private readonly GeometryFactory _geometryFactory = geometryFactory ?? new GeometryFactory(new PrecisionModel(), 4326);

    public Geometry ReadByCode(string shapefilePath, string countryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapefilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        using var reader = new ShapefileDataReader(shapefilePath, _geometryFactory);
        var codeOrdinal = FindOrdinal(reader, "ADM0_A3", "ISO_A3", "SOV_A3");

        while (reader.Read())
        {
            var code = Convert.ToString(reader.GetValue(codeOrdinal));
            if (string.Equals(code, countryCode, StringComparison.OrdinalIgnoreCase))
            {
                return reader.Geometry.Copy();
            }
        }

        throw new InvalidOperationException($"Country code '{countryCode}' was not found in '{shapefilePath}'.");
    }

    private static int FindOrdinal(ShapefileDataReader reader, params string[] candidates)
    {
        for (var index = 0; index < reader.FieldCount; index++)
        {
            if (candidates.Any(candidate => string.Equals(reader.GetName(index), candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return index;
            }
        }

        throw new InvalidDataException($"None of the required fields were found: {string.Join(", ", candidates)}.");
    }
}

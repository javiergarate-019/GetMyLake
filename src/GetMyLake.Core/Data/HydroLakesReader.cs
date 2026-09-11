using System.Collections;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace GetMyLake.Core.Data;

public sealed class HydroLakesReader(GeometryFactory? geometryFactory = null)
{
    private readonly GeometryFactory _geometryFactory = geometryFactory ?? new GeometryFactory(new PrecisionModel(), 4326);

    public int GetRecordCount(string shapefilePath)
    {
        using var reader = new ShapefileDataReader(shapefilePath, _geometryFactory);
        return reader.RecordCount;
    }

    public IEnumerable<HydroLakeShape> Read(string shapefilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shapefilePath);
        return new LakeEnumerable(shapefilePath, _geometryFactory);
    }

    private sealed class LakeEnumerable(string shapefilePath, GeometryFactory geometryFactory)
        : IEnumerable<HydroLakeShape>
    {
        public IEnumerator<HydroLakeShape> GetEnumerator() => new LakeEnumerator(shapefilePath, geometryFactory);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class LakeEnumerator : IEnumerator<HydroLakeShape>
    {
        private readonly ShapefileDataReader _reader;
        private readonly int _idOrdinal;
        private readonly int _nameOrdinal;
        private readonly int _countryOrdinal;
        private readonly int _areaOrdinal;
        private readonly int _typeOrdinal;

        public LakeEnumerator(string shapefilePath, GeometryFactory geometryFactory)
        {
            _reader = new ShapefileDataReader(shapefilePath, geometryFactory);
            _idOrdinal = FindOrdinal(_reader, "Hylak_id");
            _nameOrdinal = FindOrdinal(_reader, "Lake_name");
            _countryOrdinal = FindOrdinal(_reader, "Country");
            _areaOrdinal = FindOrdinal(_reader, "Lake_area");
            _typeOrdinal = FindOrdinal(_reader, "Lake_type");
        }

        public HydroLakeShape Current { get; private set; } = null!;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (!_reader.Read())
            {
                return false;
            }

            Current = new HydroLakeShape(
                Convert.ToInt64(_reader.GetValue(_idOrdinal)),
                Convert.ToString(_reader.GetValue(_nameOrdinal))?.Trim() ?? string.Empty,
                Convert.ToString(_reader.GetValue(_countryOrdinal))?.Trim() ?? string.Empty,
                Convert.ToDouble(_reader.GetValue(_areaOrdinal)),
                Convert.ToInt32(_reader.GetValue(_typeOrdinal)),
                _reader.Geometry);
            return true;
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose() => _reader.Dispose();

        private static int FindOrdinal(ShapefileDataReader reader, string name)
        {
            for (var index = 0; index < reader.FieldCount; index++)
            {
                if (string.Equals(reader.GetName(index), name, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            throw new InvalidDataException($"Required HydroLAKES field '{name}' was not found.");
        }
    }
}

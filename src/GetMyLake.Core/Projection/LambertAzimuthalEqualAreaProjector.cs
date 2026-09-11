using NetTopologySuite.Geometries;

namespace GetMyLake.Core.Projection;

/// <summary>
/// Projects a WGS84 longitude/latitude geometry to a local spherical
/// Lambert azimuthal equal-area plane centered on the geometry itself.
/// </summary>
public sealed class LambertAzimuthalEqualAreaProjector
{
    public Geometry Project(Geometry geographicGeometry)
    {
        ArgumentNullException.ThrowIfNull(geographicGeometry);

        if (geographicGeometry.IsEmpty)
        {
            throw new ArgumentException("The geometry must not be empty.", nameof(geographicGeometry));
        }

        var center = geographicGeometry.Centroid.Coordinate;
        var projected = geographicGeometry.Copy();
        projected.Apply(new ProjectionFilter(center.X, center.Y));
        projected.GeometryChanged();
        projected.SRID = 0;
        return projected;
    }

    private sealed class ProjectionFilter(double centerLongitude, double centerLatitude)
        : ICoordinateSequenceFilter
    {
        private readonly double _lambda0 = ToRadians(centerLongitude);
        private readonly double _phi0 = ToRadians(centerLatitude);

        public bool Done => false;

        public bool GeometryChanged => true;

        public void Filter(CoordinateSequence sequence, int index)
        {
            var lambda = ToRadians(sequence.GetX(index));
            var phi = ToRadians(sequence.GetY(index));
            var deltaLambda = NormalizeRadians(lambda - _lambda0);
            var sinPhi = Math.Sin(phi);
            var cosPhi = Math.Cos(phi);
            var sinPhi0 = Math.Sin(_phi0);
            var cosPhi0 = Math.Cos(_phi0);
            var denominator = 1 + (sinPhi0 * sinPhi) + (cosPhi0 * cosPhi * Math.Cos(deltaLambda));
            var k = Math.Sqrt(2 / Math.Max(denominator, 1e-12));

            sequence.SetX(index, k * cosPhi * Math.Sin(deltaLambda));
            sequence.SetY(index, k * ((cosPhi0 * sinPhi) - (sinPhi0 * cosPhi * Math.Cos(deltaLambda))));
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        private static double NormalizeRadians(double radians)
        {
            while (radians > Math.PI)
            {
                radians -= 2 * Math.PI;
            }

            while (radians < -Math.PI)
            {
                radians += 2 * Math.PI;
            }

            return radians;
        }
    }
}

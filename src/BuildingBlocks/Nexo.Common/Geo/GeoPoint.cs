namespace Nexo.Common.Geo;

/// <summary>Coordenada GeoJSON: longitud primero, latitud despues, sin datos de contacto.</summary>
public readonly record struct GeoPoint
{
    public GeoPoint(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "La latitud debe estar entre -90 y 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "La longitud debe estar entre -180 y 180.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    /// <summary>Arreglo listo para un campo GeoJSON `Point [lng, lat]`.</summary>
    public double[] ToGeoJson() => [Longitude, Latitude];
}

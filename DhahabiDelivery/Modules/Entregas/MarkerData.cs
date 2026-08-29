using GoogleMapsComponents.Maps;

namespace DhahabiDelivery.Modules.Entregas;

public class MarkerData(int id = 0, double lat = 0, double lng = 0)
{
    public int Id { get; set; } = id;
    public double Lat { get; set; } = lat;
    public double Lng { get; set; } = lng;

    public void UpdatePosition(LatLngLiteral position)
    {
        Lat = position.Lat;
        Lng = position.Lng;
    }
}
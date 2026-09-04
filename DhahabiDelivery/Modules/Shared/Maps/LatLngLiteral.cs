namespace DhahabiDelivery.Modules.Shared.Maps;

public class LatLngLiteral
{
    public LatLngLiteral()
    {
    }

    public LatLngLiteral(double lat, double lng)
    {
        Lat = lat;
        Lng = lng;
    }

    public LatLngLiteral(decimal lat, decimal lng)
    {
        Lat = (double)lat;
        Lng = (double)lng;
    }

    public double Lat { get; set; }
    public double Lng { get; set; }
}

using System.Runtime.Serialization;

namespace DhahabiDelivery.Modules.Entregas.Exceptions;

/// <summary>
/// Excepción que se lanza cuando el GPS del dispositivo no está habilitado
/// </summary>
[Serializable]
public class GpsNotEnabledException : Exception
{
    public GpsNotEnabledException()
    {
    }

    public GpsNotEnabledException(string message) : base(message)
    {
    }

    public GpsNotEnabledException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected GpsNotEnabledException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

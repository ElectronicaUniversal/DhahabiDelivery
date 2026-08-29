using DhahabiDelivery.Configuration;
using FrontentCompartido.Modules.Shared.Services;

namespace DhahabiDelivery.Modules.Shared.Services;

public class ImageService(AppSettings config) : IImageService
{
    public string GetImageUrl(string imageName)
    {
        var baseUrl = config.ImageServer;
        return baseUrl + imageName;
    }
}
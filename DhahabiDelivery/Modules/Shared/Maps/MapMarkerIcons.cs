namespace DhahabiDelivery.Modules.Shared.Maps;

public static class MapMarkerIcons
{
    public static string LocationPin(string imageUrl) =>
        $"""
         <svg width="50px" height="50px" viewBox="0 0 84.00105 103" version="1.1" xmlns:xlink="http://www.w3.org/1999/xlink" xmlns="http://www.w3.org/2000/svg">
             <defs>
                 <filter filterUnits="userSpaceOnUse" color-interpolation-filters="sRGB" id="filter_1">
                     <feFlood flood-opacity="0" result="BackgroundImageFix"/>
                     <feColorMatrix in="SourceAlpha" type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 128 0"/>
                     <feOffset dx="0" dy="2"/>
                     <feGaussianBlur stdDeviation="2"/>
                     <feColorMatrix type="matrix" values="0 0 0 0 0.39215687 0 0 0 0 0.39215687 0 0 0 0 0.39215687 0 0 0 0.49803922 0"/>
                     <feBlend mode="normal" in2="BackgroundImageFix" result="effect0_dropShadow"/>
                     <feBlend mode="normal" in="SourceGraphic" in2="effect0_dropShadow" result="shape"/>
                 </filter>
                 <image width="512" height="512" href="{imageUrl}" id="img_1"/>
                 <pattern patternUnits="objectBoundingBox" y="-0%" height="100%" width="100%" id="pattern_1">
                     <use xlink:href="#img_1" transform="matrix(0.12890625 0 0 0.12890625 0 0)"/>
                 </pattern>
             </defs>
             <path d="M42.001 4C22.1188 4 6.00104 20.1178 6.00104 40C6.00104 40.3672 6.00653 40.7332 6.01746 41.0978C6.00656 41.1012 6.00107 41.1029 6.00107 41.1029C6.00107 41.1029 5.97852 41.8235 6.14178 43.2054C6.54047 47.7239 7.77396 52.0027 9.68701 55.8866C13.8855 66.1948 22.8572 81.4128 42.0198 100C42.0198 100 64.9816 76.9454 74.2186 56.0809C75.7136 53.0916 76.8048 49.8651 77.4207 46.473C77.7995 44.6204 78.003 42.8232 78 41.1029C77.9998 40.9835 77.9976 40.8654 77.9934 40.7484C77.9985 40.4996 78.001 40.2501 78.001 40C78.001 20.1178 61.8833 4 42.001 4Z" id="Oval-Union-Copy" fill="#FFFFFF" fill-rule="evenodd" stroke="none" filter="url(#filter_1)"/>
             <path d="M42 7C60.2279 7 75 21.7721 75 40L75 40C75 58.2279 60.2279 73 42 73L42 73C23.7721 73 9 58.2279 9 40L9 40C9 21.7721 23.7721 7 42 7Z" id="Avatar-Copy" fill="url(#pattern_1)" stroke="none"/>
         </svg>
         """;

    public const string DeliveryPosition =
        """
        <div class="delivery-location-marker">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="#E53935" viewBox="0 0 16 16">
                <path d="M12.166 8.94c-.524 1.062-1.234 2.12-1.96 3.07A31.493 31.493 0 0 1 8 14.58a31.481 31.481 0 0 1-2.206-2.57c-.726-.95-1.436-2.008-1.96-3.07C3.304 7.867 3 6.862 3 6a5 5 0 0 1 10 0c0 .862-.305 1.867-.834 2.94zM8 16s6-5.686 6-10A6 6 0 0 0 2 6c0 4.314 6 10 6 10z"/>
                <path d="M8 8a2 2 0 1 1 0-4 2 2 0 0 1 0 4zm0 1a3 3 0 1 0 0-6 3 3 0 0 0 0 6z"/>
            </svg>
        </div>
        """;
}

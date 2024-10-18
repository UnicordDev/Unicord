using System;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using static Unicord.Constants;

namespace Unicord.Universal.Utilities;

internal static class WebPHelpers
{

    private static readonly Lazy<bool> hasWebPSupport = new Lazy<bool>(() => CheckWebPSupport());

    public static bool ShouldUseWebP
        => HasWebPSupport() && App.LocalSettings.Read(ENABLE_WEBP, ENABLE_WEBP_DEFAULT);

    public static bool HasWebPSupport()
        => hasWebPSupport.Value;

    private static bool CheckWebPSupport()
    {
        if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7, 0))
            return false;

        foreach (var item in BitmapDecoder.GetDecoderInformationEnumerator())
        {
            if (item.CodecId == BitmapDecoder.WebpDecoderId)
                return true;
        }

        return false;
    }
}
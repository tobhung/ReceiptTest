using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
using QRCoder;

namespace ReceiptTest.Codes;
public class ImageRender
{
    public static byte[] GetBytes(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        int commaIndex = json.IndexOf(',');
        
        string cleanBase64 = commaIndex >= 0 ? json.Substring(commaIndex + 1) : json;

        try 
        {
            return Convert.FromBase64String(cleanBase64);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
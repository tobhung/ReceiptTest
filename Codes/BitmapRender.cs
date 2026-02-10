using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ReceiptTest.Models;
using System.Text;
using QRCoder;
using System.Drawing;

namespace ReceiptTest.Codes;

public class BitmapRender
{
    public static byte[] ImageToEscPos(Image<Rgba32> img)
    {
        int widthBytes = (img.Width + 7) / 8;
        var data = new List<byte>();

        data.AddRange(new byte[]
        {
            0x1D, 0x76, 0x30, 0x00,
            (byte)(widthBytes & 0xFF),
            (byte)(widthBytes >> 8),
            (byte)(img.Height & 0xFF),
            (byte)(img.Height >> 8)
        });

        for (int y = 0; y < img.Height; y++)
        {
            for (int x = 0; x < widthBytes * 8; x += 8)
            {
                byte b = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    int px = x + bit;
                    if (px < img.Width)
                    {
                        var p = img[px, y];
                        int lum = (p.R + p.G + p.B) / 3;
                        if (lum < 128)
                        {
                            b |= (byte)(1 << (7 - bit));
                        }
                    }
                }
                data.Add(b);
            }
        }

        return data.ToArray();
    }

    // public static byte[] GetTaiwanReceiptBytes(StoreRequest model)
    // {
    //     Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    //     var big5 = Encoding.GetEncoding("BIG5");

    //     var logoPath = "images/logo.png";

    //     var data = new List<byte>();

    //     var year = model.Date.ToString("yyyy");
    //     var currentYear = int.Parse(year) - 1911;
    //     var month = int.Parse(model.Date.ToString("MM"));

    //     Console.WriteLine(year + " "+currentYear + " " +month);

    //     // --- 1. Initialization ---
    //     data.AddRange(new byte[] { 0x1B, 0x40 }); // Initialize
    //     data.AddRange(new byte[] { 0x1C, 0x26 }); // Enter Chinese Mode

    //     if (File.Exists(logoPath))
    //     {
    //         var logo = SixLabors.ImageSharp.Image.Load<Rgba32>(logoPath);
            
    //         // Resize logo to fit (e.g., max width 200px)
    //         logo.Mutate(x => x
    //             .Resize(new ResizeOptions { 
    //                 Size = new SixLabors.ImageSharp.Size(400, 0), 
    //                 Mode = ResizeMode.Max 
    //             })
    //             .Grayscale()
    //         );

    //         // We need to pad the logo to 384px to center it easily, 
    //         // or just center the 200px image in your ImageToEscPos logic.
    //         var canvas = new Image<Rgba32>(384, logo.Height);
            
    //         canvas.Mutate(ctx => ctx.Fill(SixLabors.ImageSharp.Color.White));
    //             // Draw logo in the center of the 384px canvas
    //         canvas.Mutate(ctx => ctx.DrawImage(logo, new SixLabors.ImageSharp.Point((384 - logo.Width) / 2, 0), 1f));
                
    //             // Convert processed image to printer commands
    //         data.AddRange(ImageToEscPos(canvas));
            
    //     }

    //     // 3. Add a little space after logo
    //     data.AddRange(new byte[] { 0x1B, 0x64, 0x01 });

    //     // --- 2. Title (Large Font) ---
    //     data.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // Center Align
    //     data.AddRange(new byte[] { 0x1D, 0x21, 0x11 }); // Double width, double height
    //     data.AddRange(big5.GetBytes(model.Name + "\n\n"));

    //     // --- 3. Invoice Header ---
    //     data.AddRange(new byte[] { 0x1D, 0x21, 0x11 }); //
    //     data.AddRange(big5.GetBytes("電子發票證明聯\n"));
    //     data.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); 
    //     data.AddRange(new byte[] { 0x1B, 0x64, 0x01 });
    //     data.AddRange(big5.GetBytes($"{currentYear}年 {month} - {month + 1} 月\n"));
    //     data.AddRange(new byte[] { 0x1B, 0x64, 0x01 });
    //     data.AddRange(big5.GetBytes($"{model.InvoiceNO}\n"));
    //     data.AddRange(new byte[] { 0x1B, 0x64, 0x01 });
    //     data.AddRange(new byte[] { 0x1B, 0x61, 0x00 }); // Left Align

    //     // --- 4. Transaction Info ---
    //     data.AddRange(big5.GetBytes($"{model.Date:yyyy-MM-dd HH:mm:ss}\n"));
    //     data.AddRange(big5.GetBytes($"隨機碼 {model.RandomCode}  總計 {model.Total}\n"));
    //     data.AddRange(big5.GetBytes($"賣方 {model.SellerID}  買方 {model.BuyerID}\n"));
    //     data.AddRange(big5.GetBytes("--------------------------------\n"));

    //     data.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // Center align

    //     // Format: 11502 (Year 115, Feb) + Invoice Number
    //     string barcodeData = "11502AB12345678"; 
    //     AddBarcode(data, barcodeData);

    //     data.AddRange(new byte[] { 0x1B, 0x64, 0x02 }); // Feed 2 lines after barcode
    
    //     data.AddRange(big5.GetBytes("--------------------------------\n"));

    //     byte[] qrSection = GenerateQRCodes(model.LeftQRData, model.RightQRData);
    //     data.AddRange(qrSection);

    //     // --- 7. Footer & Cut ---
    //     data.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // Center
    //     data.AddRange(big5.GetBytes("\n退貨請持證明聯正本辦理\n"));
    //     data.AddRange(new byte[] { 0x1B, 0x64, 0x05 }); // Feed 5 lines
    //     data.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 }); // Cut

    //     return data.ToArray();
    // }

//   public static byte[] GenerateQRCodes(string leftData, string rightData)
//  {
//     //create the canvas (384px is standard for 58mm thermal printers)
//     using (var canvas = new Image<Rgba32>(384, 150))
//     {
//         canvas.Mutate(ctx => ctx.Fill(SixLabors.ImageSharp.Color.White));

//         DrawQRCode(canvas, leftData, 40, 10);
//         DrawQRCode(canvas, rightData, 210, 10);
//         return ImageToEscPos(canvas);
        
//     }
//   }

    private static void DrawQRCode(Image<Rgba32> canvas, string data, int x, int y)
    {
        using (var qrGenerator = new QRCodeGenerator())
        using (var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q))
        {
            // use PngByteQRCode to get a format ImageSharp can load
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(4); // 4 pixels per module

            using (var qrImage = SixLabors.ImageSharp.Image.Load<Rgba32>(qrBytes))
            {
                //resize to 130x130 to fit 150 height
                qrImage.Mutate(img => img.Resize(130, 130));

                //draw the QR onto the main canvas
                canvas.Mutate(ctx => ctx.DrawImage(qrImage, new SixLabors.ImageSharp.Point(x, y), 1f));

                qrImage.SaveAsPng("~/download");
            }
        }
    }

    public static void AddBarcode(List<byte> data, string invoiceText)
    {
        // 1. Set barcode height (0x1D 0x68 n) - n = dots (e.g., 60 to 100)
        data.AddRange(new byte[] { 0x1D, 0x68, 80 });

        // 2. Set barcode width (0x1D 0x77 n) - n = 2 or 3 (2 is thinner)
        data.AddRange(new byte[] { 0x1D, 0x77, 2 });

        // 3. Set text position (0x1D 0x48 n) - 0: None, 2: Below
        data.AddRange(new byte[] { 0x1D, 0x48, 0 });

        // 4. Print Barcode (GS k m n d1...dn)
        // m = 73 is Code128
        data.AddRange(new byte[] { 0x1D, 0x6B, 73 });

        // n = length of data
        data.Add((byte)invoiceText.Length);

        // d = the actual text bytes
        data.AddRange(Encoding.ASCII.GetBytes(invoiceText));
    }


    public static byte[] PrintFullImage(string base64String)
    {
        byte[] bytes = Convert.FromBase64String(base64String);
        using Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(bytes);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new SixLabors.ImageSharp.Size(384, 0),
            Mode = ResizeMode.Stretch
        }).Grayscale());

        return TrueFullSize(image);
    }

    public static byte[] TrueFullSize(Image<Rgba32> image)
    {
        List<byte> result = new();
        //GS W nL nH -> 576 is 0x0240. nL = 0x40, nH = 0x02
        //80mm = 576px
        result.AddRange(new byte[] { 0x1B, 0x40 }); // Reset
        //result.AddRange(new byte[] { 0x1D, 0x57, 0xC0, 0x01 }); // Width 448
        //result.AddRange(new byte[] { 0x1D, 0x57, 0x40, 0x02 }); //576

        result.AddRange(new byte[] { 0x1D, 0x4C, 0x00, 0x00 }); // Left Margin 0
    
        // Set Print Area Width to 384 (GS W nL nH)
        // 384 = 0x0180 -> nL = 0x80, nH = 0x01
        // This tells the printer: "Do not pad the sides, use all 384 dots."
        result.AddRange(new byte[] { 0x1D, 0x57, 0x80, 0x01 });
        // GS v 0 (Bit Image Command)
        int widthBytes = 48; // 384 / 8
        result.AddRange(new byte[] { 0x1D, 0x76, 0x30, 0x00 });
        result.Add((byte)(widthBytes & 0xFF));    
        result.Add((byte)(widthBytes >> 8));      
        result.Add((byte)(image.Height & 0xFF));  
        result.Add((byte)(image.Height >> 8));    

        for (int y = 0; y < image.Height; y++)
        {
            for (int xByte = 0; xByte < widthBytes; xByte++)
            {
                byte b = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    if (image[xByte * 8 + bit, y].R < 128)
                    {
                        b |= (byte)(0x80 >> bit);
                    }
                }
                result.Add(b);
            }
        }

        result.AddRange(new byte[] { 0x1B, 0x64, 0x02 });
        return result.ToArray();
    }


    public static byte[] ParseRawHex(string hexInput)
    {
        if (string.IsNullOrWhiteSpace(hexInput)) return Array.Empty<byte>();

        // clean up brackets, spaces, and "0x" prefixes
        var cleaned = hexInput.Replace("{", "").Replace("}", "").Replace("0x", "").Replace(" ", "");
        var hexParts = cleaned.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        return hexParts.Select(s => Convert.ToByte(s.Trim(), 16)).ToArray();
    }
    
    public static byte[] GetImageBinaryData(Image<Rgba32> image)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // ESC @ - Initialize printer
        bw.Write(new byte[] { 0x1B, 0x40 });

        // Line Spacing to 0
        bw.Write(new byte[] { 0x1B, 0x33, 0x00 });

        int width = image.Width;

        var a = "";
        int height = image.Height;
        int byteWidth = (width + 7) / 8; // 因為 1 個位元組（byte）包含 8 個位元，所以寬度必須除以 8。(width + 7) / 8 是為了確保如果寬度不是 8 的倍數，也能補足空間

        // GS v 0 - Print Raster Bit Image Command
        // Format: GS v 0 m xL xH yL yH d1...dk
        bw.Write(new byte[] { 0x1D, 0x76, 0x30, 0x00 }); // 0x1D 進階功能 0x76 列印 0x30 使用GSv0 0x00 一般模式
        
        bw.Write((byte)(byteWidth % 256)); // xL
        bw.Write((byte)(byteWidth / 256)); // xH
        bw.Write((byte)(height % 256));    // yL
        bw.Write((byte)(height / 256));    // yH

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < byteWidth; x++)
            {
                byte b = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    int xPos = (x * 8) + bit;
                    if (xPos < width)
                    {
                        var pixel = image[xPos, y];
                        // If the pixel is "dark" (Black), set the corresponding bit to 1
                        if (pixel.R < 128) 
                        {
                            b |= (byte)(0x80 >> bit);
                        }
                    }
                }
                bw.Write(b);
            }
        }

        // Paper Feed (3 lines) and Cut
        //bw.Write(new byte[] { 0x1B, 0x64, 0x03 }); // Feed 3 lines
        bw.Write(new byte[] { 0x1D, 0x56, 0x42, 0x00 }); // Partial cut

        return ms.ToArray();
    }

}
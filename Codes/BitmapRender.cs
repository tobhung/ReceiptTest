using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
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

    public static byte[] GetTaiwanReceiptBytes(StoreRequest model)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var big5 = Encoding.GetEncoding("BIG5");

        var logoPath = "images/logo.png";

        var data = new List<byte>();

        var year = model.Date.ToString("yyyy");
        var currentYear = int.Parse(year) - 1911;
        var month = int.Parse(model.Date.ToString("MM"));

        Console.WriteLine(year + " "+currentYear + " " +month);

        // --- 1. Initialization ---
        data.AddRange(new byte[] { 0x1B, 0x40 }); // Initialize
        data.AddRange(new byte[] { 0x1C, 0x26 }); // Enter Chinese Mode

        if (File.Exists(logoPath))
        {
            var logo = SixLabors.ImageSharp.Image.Load<Rgba32>(logoPath);
            
            // Resize logo to fit (e.g., max width 200px)
            logo.Mutate(x => x
                .Resize(new ResizeOptions { 
                    Size = new SixLabors.ImageSharp.Size(400, 0), 
                    Mode = ResizeMode.Max 
                })
                .Grayscale()
            );

            // We need to pad the logo to 384px to center it easily, 
            // or just center the 200px image in your ImageToEscPos logic.
            var canvas = new Image<Rgba32>(384, logo.Height);
            
            canvas.Mutate(ctx => ctx.Fill(SixLabors.ImageSharp.Color.White));
                // Draw logo in the center of the 384px canvas
            canvas.Mutate(ctx => ctx.DrawImage(logo, new SixLabors.ImageSharp.Point((384 - logo.Width) / 2, 0), 1f));
                
                // Convert processed image to printer commands
            data.AddRange(ImageToEscPos(canvas));
            
        }

        // 3. Add a little space after logo
        data.AddRange(new byte[] { 0x1B, 0x64, 0x01 });

        // --- 2. Title (Large Font) ---
        data.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // Center Align
        data.AddRange(new byte[] { 0x1D, 0x21, 0x11 }); // Double width, double height
        data.AddRange(big5.GetBytes(model.Name + "\n\n"));

        // --- 3. Invoice Header ---
        data.AddRange(new byte[] { 0x1D, 0x21, 0x11 }); //
        data.AddRange(big5.GetBytes("電子發票證明聯\n"));
        data.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); 
        data.AddRange(new byte[] { 0x1B, 0x64, 0x01 });
        data.AddRange(big5.GetBytes($"{currentYear}年 {month} - {month + 1} 月\n"));
        data.AddRange(new byte[] { 0x1B, 0x64, 0x01 });
        data.AddRange(big5.GetBytes($"{model.InvoiceNO}\n"));
        data.AddRange(new byte[] { 0x1B, 0x64, 0x01 });
        data.AddRange(new byte[] { 0x1B, 0x61, 0x00 }); // Left Align

        // --- 4. Transaction Info ---
        data.AddRange(big5.GetBytes($"{model.Date:yyyy-MM-dd HH:mm:ss}\n"));
        data.AddRange(big5.GetBytes($"隨機碼 {model.RandomCode}  總計 {model.Total}\n"));
        data.AddRange(big5.GetBytes($"賣方 {model.SellerID}  買方 {model.BuyerID}\n"));
        data.AddRange(big5.GetBytes("--------------------------------\n"));

        data.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // Center align

        // Format: 11502 (Year 115, Feb) + Invoice Number
        string barcodeData = "11502AB12345678"; 
        AddBarcode(data, barcodeData);

        data.AddRange(new byte[] { 0x1B, 0x64, 0x02 }); // Feed 2 lines after barcode
        // --- 5. Items ---
        // foreach (var item in model.Items)
        // {
        //     // Simple manual padding for price alignment
        //     string line = $"{item.Name}".PadRight(14);
        //     string price = $"{item.Qty}x{item.Price}".PadLeft(18);
        //     data.AddRange(big5.GetBytes(line + price + "\n"));
        // }
        data.AddRange(big5.GetBytes("--------------------------------\n"));

        // --- 6. The QR Codes (Side-by-Side) ---
        // Since native ESC/POS usually prints QR codes one per line, 
        // the most reliable way to get two side-by-side is to render 
        // ONLY the QR code section as a small 384x150 bitmap.

        byte[] qrSection = GenerateQRCodes(model.LeftQRData, model.RightQRData);
        data.AddRange(qrSection);

        // --- 7. Footer & Cut ---
        data.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // Center
        data.AddRange(big5.GetBytes("\n退貨請持證明聯正本辦理\n"));
        data.AddRange(new byte[] { 0x1B, 0x64, 0x05 }); // Feed 5 lines
        data.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 }); // Cut

        return data.ToArray();
    }

  public static byte[] GenerateQRCodes(string leftData, string rightData)
  {
    // 1. Create the canvas (384px is standard for 58mm thermal printers)
    using (var canvas = new Image<Rgba32>(384, 150))
    {
        canvas.Mutate(ctx => ctx.Fill(SixLabors.ImageSharp.Color.White));

       
        DrawQRCode(canvas, leftData, 40, 10);
        DrawQRCode(canvas, rightData, 210, 10);

            // 3. Convert to ESC/POS bytes
            return ImageToEscPos(canvas);
        
        }
  }

    private static void DrawQRCode(Image<Rgba32> canvas, string data, int x, int y)
    {
    using (var qrGenerator = new QRCodeGenerator())
    using (var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q))
    {
        // We use PngByteQRCode to get a format ImageSharp can easily load
        var qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrBytes = qrCode.GetGraphic(4); // 4 pixels per module

        using (var qrImage = SixLabors.ImageSharp.Image.Load<Rgba32>(qrBytes))
        {
            // Resize if necessary (e.g., to 130x130 to fit your 150 height)
            qrImage.Mutate(img => img.Resize(130, 130));

                // Draw the QR onto the main canvas
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

    public static byte[] ConvertLogo(byte[] imageBytes)
    {
        // 1. 使用 ImageSharp 載入圖片
        using (Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes))
        {
            // 2. 縮放至 384 像素寬度 (57mm 紙張標準)
            int targetWidth = 384;
            int targetHeight = (int)((double)image.Height * targetWidth / image.Width);

            // 使用高品質 Bicubic 縮放
            image.Mutate(x => x.Resize(targetWidth, targetHeight, KnownResamplers.Bicubic));

            // 3. 轉換為 ESC/POS 二進位位元流 (GS v 0 指令)
            return ConvertToEscPos(image);
        }
    }

    private static byte[] ConvertToEscPos(Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;
        int widthBytes = (width + 7) / 8; // 每 8 個點組成一個 Byte
        
        List<byte> data = new List<byte>();

        // ESC/POS 指令: GS v 0 m xL xH yL yH
        // 這是熱感印表機列印點陣圖的標準指令
        data.AddRange(new byte[] { 
            0x1D, 0x76, 0x30, 0, 
            (byte)(widthBytes % 256), (byte)(widthBytes / 256), 
            (byte)(height % 256), (byte)(height / 256) 
        });

        for (int y = 0; y < height; y++)
        {
            for (int xByte = 0; xByte < widthBytes; xByte++)
            {
                byte currentByte = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    int xPixel = (xByte * 8) + bit;
                    if (xPixel < width)
                    {
                        // 取得像素
                        var pixel = image[xPixel, y];
                        
                        // 計算亮度 (Luminance) 決定黑白
                        // 這是標準的灰階公式: 0.299*R + 0.587*G + 0.114*B
                        double gray = (0.299 * pixel.R) + (0.587 * pixel.G) + (0.114 * pixel.B);
                        
                        // 如果透明度太低或顏色夠深，就印出黑色 (1)
                        if (pixel.A > 128 && gray < 128) 
                        {
                            currentByte |= (byte)(0x80 >> bit);
                        }
                    }
                }
                data.Add(currentByte);
            }
        }
        return data.ToArray();
    }
    
    

    public static byte[] ParseRawHex(string hexInput)
    {
    if (string.IsNullOrWhiteSpace(hexInput)) return Array.Empty<byte>();

   
        // Clean up brackets, spaces, and "0x" prefixes
        var cleaned = hexInput.Replace("{", "").Replace("}", "").Replace("0x", "").Replace(" ", "");
        var hexParts = cleaned.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        return hexParts.Select(s => Convert.ToByte(s.Trim(), 16)).ToArray();
    }

}
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using ReceiptTest.Models;
using System.Text;

namespace ReceiptTest.Codes;

public class BitmapRender
{
    public static Image<Rgba32> Render(StoreRequest model)
    {
        int width = 384;
        int height = 1200;

        var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx => ctx.Fill(Color.White));

        var fontCollection = new FontCollection();
        var family = fontCollection.Add("/usr/share/fonts/truetype/noto/NotoSansMono-Regular.ttf");

        var font = family.CreateFont(22, FontStyle.Regular);
        int y = 20;

        var options = new TextOptions(font)
        {
            Origin = new PointF(10, y),
            WrappingLength = width - 20
        };

        void Draw(string text)
        {
            image.Mutate(ctx =>
                ctx.DrawText(text, font, Color.Black, new PointF(10, y)));
            y += 30;
        }

        Draw(model.Name);
        Draw($"發票號碼 {model.InvoiceNO}");
        Draw(model.Date.ToString("yyyy/MM/dd HH:mm"));
        Draw("--------------------------------");

        foreach (var item in model.Items)
        {
            Draw(item.Name);
            Draw($"{item.Qty} x {item.Price} = {item.Qty * item.Price}");
        }

        Draw("--------------------------------");
        Draw($"總計 {model.Total}");

        return image.Clone(ctx => ctx.Crop(width, y + 20));
    }

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

    // this worked
    public static byte[] GetPrintBytes(StoreRequest model)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var big5 = Encoding.GetEncoding("BIG5");
        var data = new List<byte>();

        // 1. Initialize Printer
        data.AddRange(new byte[] { 0x1B, 0x40 });

        // 2. Select International Character Set (Taiwan)
        // Some printers use 0x1B 0x52 0x0F, others use 0x1C 0x26 to enter Chinese mode
        data.AddRange(new byte[] { 0x1C, 0x26 });

        void WriteLine(string text)
        {
            data.AddRange(big5.GetBytes(text + "\n"));
        }

        WriteLine(model.Name);
        WriteLine($"發票號碼: {model.InvoiceNO}");
        WriteLine(model.Date.ToString("yyyy/MM/dd HH:mm"));
        WriteLine("--------------------------------");

        foreach (var item in model.Items)
        {
            WriteLine(item.Name);
            WriteLine($"{item.Qty} x {item.Price} = {item.Qty * item.Price}");
        }

        WriteLine("--------------------------------");
        WriteLine($"總計: {model.Total}");

        // Cut Paper
        data.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 });

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
            var logo = Image.Load<Rgba32>(logoPath);
            
            // Resize logo to fit (e.g., max width 200px)
            logo.Mutate(x => x
                .Resize(new ResizeOptions { 
                    Size = new Size(200, 0), 
                    Mode = ResizeMode.Max 
                })
                .Grayscale()
            );

            // We need to pad the logo to 384px to center it easily, 
            // or just center the 200px image in your ImageToEscPos logic.
            var canvas = new Image<Rgba32>(384, logo.Height);
            
            canvas.Mutate(ctx => ctx.Fill(Color.White));
                // Draw logo in the center of the 384px canvas
            canvas.Mutate(ctx => ctx.DrawImage(logo, new Point((384 - logo.Width) / 2, 0), 1f));
                
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

        //byte[] qrSection = GenerateQRSection(model.LeftQRData, model.RightQRData);
        //data.AddRange(qrSection);

        // --- 7. Footer & Cut ---
        data.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // Center
        data.AddRange(big5.GetBytes("\n退貨請持證明聯正本辦理\n"));
        data.AddRange(new byte[] { 0x1B, 0x64, 0x05 }); // Feed 5 lines
        data.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 }); // Cut

        return data.ToArray();
    }

    public static byte[] GenerateQRSection(string leftData, string rightData)
    {
        // Use ImageSharp to create a small canvas
        using (var canvas = new Image<Rgba32>(384, 150))
        {
            canvas.Mutate(ctx => ctx.Fill(Color.White));

            // Use QRCoder or similar to generate two Bitmaps, 
            // then draw them onto 'canvas' at:
            // Left: (x: 40, y: 10)
            // Right: (x: 210, y: 10)

            // After drawing both QR codes onto the canvas:
            return ImageToEscPos(canvas);
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

}
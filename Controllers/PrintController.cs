// Controllers/PrintController.cs
using Microsoft.AspNetCore.Mvc;
using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using System.Text;
using ESCPOS_NET.Utilities;
using System.Diagnostics;
using ReceiptTest.Models;
using ReceiptTest.Codes;
using System.Drawing.Printing;
using SixLabors.ImageSharp;
using ESC_POS_USB_NET;
using System.Drawing;
using ESC_POS_USB_NET.Printer;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace ReceiptTest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintController : ControllerBase
    {
        private readonly ILogger<PrintController> _logger;
        private readonly IConfiguration _configuration;

        public PrintController(ILogger<PrintController> logger, IConfiguration configuration)
        {

            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("testprint")]
        public async Task<IActionResult> Print([FromBody] Req req)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = System.Text.Encoding.GetEncoding("BIG5"); //traditional chinese
            var printerName = "QX3";
            var portName = "USB001";
            var bytes = new List<byte>();
            int paperWidth = 384;

            string base64String = req.Content.ToString();
            var imageBytes = Convert.FromBase64String(base64String);

            // Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);

            // var b = "";

            // int newHeight = (int)((float)paperWidth / image.Width * image.Height);

            // image.Mutate(x => x.Resize(new ResizeOptions
            // {
            //     Size = new SixLabors.ImageSharp.Size(paperWidth, newHeight),
            //     Mode = ResizeMode.Min,

            // }).Grayscale());


            using (var ms = new MemoryStream(imageBytes))
            {
                //image.Save(ms, new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder());
                //ms.Position = 0;
                Bitmap bitmap = new Bitmap(ms);

                Printer printer = new Printer("QX3");

                printer.AlignCenter();
                printer.Image(bitmap);
                printer.FullPaperCut();
                printer.PrintDocument();
            }

            return Ok();

        }

       

        [HttpPost("test")]
        public async Task<IActionResult> TestPrint()
        {

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                var encoding = System.Text.Encoding.GetEncoding("BIG5"); //traditional chinese

                var url = "https://www.google.com";

                string imagePath = Path.Combine(AppContext.BaseDirectory, "images", "logo.png");
                var chinese = "測試商品 | 繁中繁中繁中繁中繁中 \n";
                var imageBytes = System.IO.File.ReadAllBytes("images/logo.png");

                //todo: generate 2 qrcodes to combine as 1 image 
                //https://blog.miniasp.com/post/2023/08/30/How-to-use-QRCoder-generates-QR-Code-using-dotNet

                var e = new EPSON();

                byte[] bytes = ByteSplicer.Combine(
                e.CenterAlign(),
                //e.PrintImage(imageBytes, false, true, 300, 0), //max width 300
                e.PrintLine(""),
                e.SetBarcodeHeightInDots(360),
                e.SetBarWidth(BarWidth.Default),
                e.SetBarLabelPosition(BarLabelPrintPosition.None),
                e.PrintBarcode(BarcodeType.ITF, "0123456789"),
                e.PrintLine(""),
                encoding.GetBytes("墾丁航空"),
                e.PrintLine("420 NINTH AVE."),
                e.PrintLine("NEW YORK, NY 10001"),
                e.PrintLine("(212) 502-6380 - (800)947-9975"),
                e.SetStyles(PrintStyle.Underline),
                e.PrintLine("www.bhphotovideo.com"),
                e.SetStyles(PrintStyle.None),
                e.PrintLine(""),
                e.LeftAlign(),
                e.PrintLine("Order: 123456789        Date: 02/01/19"),
                e.PrintLine(""),
                e.PrintLine(""),
                e.SetStyles(PrintStyle.FontB),
                encoding.GetBytes(chinese),
                e.PrintLine("    TRFETHEAD/FETHEAD                        89.95         89.95"),
                e.PrintLine("----------------------------------------------------------------"),
                e.RightAlign(),
                e.PrintLine("SUBTOTAL         89.95"),
                e.PrintLine("Total Order:         89.95"),
                e.PrintLine("Total Payment:         89.95"),
                e.PrintLine(""),
                e.LeftAlign(),
                e.PrintQRCode($"{encoding.GetBytes("測試一下")}", TwoDimensionCodeType.QRCODE_MODEL2, Size2DCode.LARGE, CorrectionLevel2DCode.PERCENT_30),
                e.RightAlign(),
                e.PrintQRCode($"{encoding.GetBytes("測試兩下")}", TwoDimensionCodeType.QRCODE_MODEL2, Size2DCode.LARGE, CorrectionLevel2DCode.PERCENT_30),
                e.PrintLine(""),
                e.PrintLine(""),
                e.PrintLine(""),
                e.PrintLine(""),
                e.FullCut()
                );

                var tempFile = Path.GetTempFileName();

                await System.IO.File.WriteAllBytesAsync(tempFile, bytes);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/lp",
                        Arguments = $"-d Q3X -o raw {tempFile}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                //刪除暫存
                System.IO.File.Delete(tempFile);

                if (process.ExitCode == 0)
                {
                    return Ok(new { message = "Test print sent successfully" });

                }
                else
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    return BadRequest(new { error });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test print failed");
                return StatusCode(500, new { message = $"Test failed: {ex.Message}" });
            }
        }

        [HttpPost("raw")]
        public async Task<IActionResult> PrintRawReceipt([FromBody] Req req)
        {
            var bytes = new List<byte>();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var encoding = Encoding.GetEncoding("BIG5");

            int paperWidth = 384;

            string base64String = req.Content.ToString();

            var imageBytes = ImageRender.GetBytes(base64String);

            Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);

            int newHeight = paperWidth / image.Width * image.Height;
            //int newHeight = (int)((float)paperWidth / image.Width * image.Height);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(paperWidth, newHeight),
                Mode = ResizeMode.Min,

            }).Grayscale());


            //var ms = new MemoryStream();
            //image.SaveAsBmp(ms);
            //byte[] imgBytes = ms.ToArray();


            var imgarr = BitmapRender.GetImageBinaryData(image);

            bytes.AddRange(imgarr);

            //var e = new EPSON();
            // byte[] bytes = ByteSplicer.Combine(
            // e.CenterAlign(),
            // e.PrintImage(imageBytes, false, true, -1, 0)
            // );


            var tempFile = Path.GetTempFileName();

            var process = new Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {

                await System.IO.File.WriteAllBytesAsync(tempFile, bytes.ToArray());

                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c copy /b \"{tempFile}\" \"\\\\DESKTOP-F6LR2M9\\Printer\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();
                await process.WaitForExitAsync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {

                //System.IO.File.WriteAllBytes("/dev/usb/lp0", bytes.ToArray());
                // process.StartInfo = new ProcessStartInfo
                // {
                //     //FileName = "/usr/bin/lp",
                //     FileName = "/bin/sh",
                //     //Arguments = $"-d Q3X -o raw {tempFile}",
                //     Arguments = $"-c \"cat {tempFile} > /dev/usb/lp0\"",
                //     RedirectStandardOutput = true,
                //     RedirectStandardError = true,
                //     UseShellExecute = false,
                // };

                using (var client = new System.Net.Sockets.TcpClient("192.168.88.101", 9100))
                using (var stream = client.GetStream())
                {
                    stream.Write(bytes.ToArray(), 0, bytes.ToArray().Length);
            
                // 建議在最後補一個換行符或切刀指令，確保印表機動作
                // byte[] cutCommand = new byte[] { 0x1D, 0x56, 0x42, 0x00 }; // ESC/POS 切刀範例
                // stream.Write(cutCommand, 0, cutCommand.Length);
            
                stream.Flush();
                }

            }

        
            // process.Start();
            // await process.WaitForExitAsync();

            //刪除暫存
            System.IO.File.Delete(tempFile);

            if (process.ExitCode == 0)
            {
                return Ok(new { message = "Test print sent successfully" });

            }
            else
            {

                var error = await process.StandardError.ReadToEndAsync();

                return BadRequest(new { error});
            }
        }
        
        

        // [HttpPost("receipt")]
        // public async Task<IActionResult> PrintReceipt([FromBody] StoreRequest model)
        // {

        //     var escpos = BitmapRender.GetTaiwanReceiptBytes(model);

        //     var e = new EPSON();
        //     var bytes = ByteSplicer.Combine(
        //         e.Initialize(),
        //         escpos,
        //         e.FullCut()
        //     );

        //     var temp = Path.GetTempFileName();
        //     await System.IO.File.WriteAllBytesAsync(temp, bytes);

        //     var process = new Process
        //     {
        //         StartInfo = new ProcessStartInfo
        //         {
        //             FileName = "/usr/bin/lp",
        //             Arguments = $"-d Q3X -o raw {temp}",
        //             RedirectStandardOutput = true,
        //             RedirectStandardError = true,
        //             UseShellExecute = false,
        //         }
        //     };
            
        //     process.Start();
        //     await process.WaitForExitAsync();

        //         //刪除暫存
        //     System.IO.File.Delete(temp);

        //     if (process.ExitCode == 0)
        //     {
        //             return Ok(new { message = "Test print sent successfully" });

        //         }
        //         else
        //         {
        //             var error = await process.StandardError.ReadToEndAsync();
        //             return BadRequest(new { error });
        //         }
        // }
    }
}
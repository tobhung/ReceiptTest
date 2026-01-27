// Controllers/PrintController.cs
using Microsoft.AspNetCore.Mvc;
using ReceiptTest.Services;
using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using System.Drawing.Printing;
using System.Text;
using ESCPOS_NET.Utilities;
using System.Diagnostics;

namespace ReceiptTest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintController : ControllerBase
    {
        private readonly IPrinterService _printerService;
        private readonly ILogger<PrintController> _logger;
        private readonly IConfiguration _configuration;

        public PrintController(IPrinterService printerService, ILogger<PrintController> logger, IConfiguration configuration)
        {
            _printerService = printerService;
            _logger = logger;
            _configuration = configuration;
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
                e.PrintImage(imageBytes, false, true, 300, 0), //max width 300
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
        public async Task<IActionResult> PrintRaw([FromBody] string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return BadRequest(new { message = "Content is required" });
            }

            var result = await _printerService.PrintRaw(content);

            if (result)
            {
                return Ok(new { message = "Print job sent successfully" });
            }

            return StatusCode(500, new { message = "Failed to send print job" });
        }
        
        


        private BasePrinter GetPrinter()
        {
            var connectionType = _configuration["Printer:ConnectionType"] ;

            Console.WriteLine(connectionType);
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                Console.WriteLine(printer);
            }

            if (connectionType.Equals("Network", StringComparison.OrdinalIgnoreCase))
            {
                var ipAddress = _configuration["Printer:IPAddress"] ?? "192.168.1.100";
                var port = int.Parse(_configuration["Printer:Port"] ?? "9100");

                // Use NetworkPrinterSettings
                var settings = new NetworkPrinterSettings
                {
                    ConnectionString = $"{ipAddress}:{port}",
                    PrinterName = "Birch CP-Q3X"
                };

                return new NetworkPrinter(settings);
            }

            else if (connectionType.Equals("Serial", StringComparison.OrdinalIgnoreCase))
            {
                var portName = _configuration["Printer:SerialPort"] ?? "/dev/ttyUSB0";
                var baudRate = int.Parse(_configuration["Printer:BaudRate"] ?? "9600");
                
                return new SerialPrinter(portName, baudRate);
            }
            else // USB
            {
                var printer = new FilePrinter(filePath: "/dev/usb/lp0");

                return new FilePrinter(filePath: "/dev/usb/lp0");
            }
        }
    }
}
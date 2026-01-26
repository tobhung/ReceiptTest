// Controllers/PrintController.cs
using Microsoft.AspNetCore.Mvc;
using ReceiptTest.Models;
using ReceiptTest.Services;
using ESCPOS_NET;
using ESCPOS_NET.Printers;
using ESCPOS_NET.Emitters;
using System;
using System.Drawing.Printing;
using ESC_POS_USB_NET.Printer;
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

        [HttpPost("receipt")]
        public async Task<IActionResult> PrintReceipt([FromBody] Print request)
        {
            if (string.IsNullOrEmpty(request.Content))
            {
                return BadRequest(new { message = "Content is required" });
            }

            var result = await _printerService.PrintReceipt(request);
            
            if (result)
            {
                return Ok(new { message = "Print job sent successfully" });
            }
            
            return StatusCode(500, new { message = "Failed to send print job" });
        }

        [HttpPost("formatted")]
        public async Task<IActionResult> PrintFormatted([FromBody] FormattedPrintRequest request)
        {
            try
            {
                var e = new EPSON();
                var printer = new FilePrinter(filePath: @"\\DESKTOP-F6LR2M9\QX3");
                // BasePrinter printer = GetPrinter();

                printer.Write(e.Initialize());

                // Header
                if (!string.IsNullOrEmpty(request.Header))
                {
                    printer.Write(
                        e.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth),
                        e.CenterAlign(),
                        e.PrintLine(request.Header),
                        e.SetStyles(PrintStyle.None),
                        e.LeftAlign(),
                        e.PrintLine("================================")
                    );
                }

                // Body
                if (!string.IsNullOrEmpty(request.Body))
                {
                    var lines = request.Body.Split('\n');
                    foreach (var line in lines)
                    {
                        printer.Write(e.PrintLine(line));
                    }
                }

                // Footer
                if (!string.IsNullOrEmpty(request.Footer))
                {
                    printer.Write(
                        e.PrintLine("================================"),
                        e.CenterAlign(),
                        e.PrintLine(request.Footer),
                        e.LeftAlign()
                    );
                }

                // Barcode if provided
                if (!string.IsNullOrEmpty(request.Barcode))
                {
                    printer.Write(
                        e.PrintLine(""),
                        e.CenterAlign(),
                        // e.Code128(request.Barcode),
                        e.LeftAlign()
                    );
                }

                // Cut paper
                printer.Write(
                    e.PrintLine(""),
                    e.PrintLine("")
                    // e.FullPaperCut()
                );

                return Ok(new { message = "Formatted receipt printed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing formatted receipt");
                return StatusCode(500, new { message = $"Failed to print: {ex.Message}" });
            }
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestPrint()
        {

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                var encoding = System.Text.Encoding.GetEncoding("GB18030"); //chinese
                Encoding gb18030 = Encoding.GetEncoding("GB18030");
                var url = "https://www.google.com";

                string imagePath = Path.Combine(AppContext.BaseDirectory, "images", "logo.png");
                

                var e = new EPSON();

                var chinese = "測試商品 \n";
                var chinese2 = "測試商品\n";
                byte[] bytes = ByteSplicer.Combine(
                e.CenterAlign(),
                //e.PrintImage(System.IO.File.ReadAllBytes("images/logo.png"), isHiDPI: true, isLegacy: false),
                //e.PrintImage(System.IO.File.ReadAllBytes("images/pd-logo-300.png"), true),
                e.PrintQRCode(url, TwoDimensionCodeType.QRCODE_MODEL2, Size2DCode.LARGE, CorrectionLevel2DCode.PERCENT_30),
                e.PrintLine(""),
                e.SetBarcodeHeightInDots(360),
                e.SetBarWidth(BarWidth.Default),
                e.SetBarLabelPosition(BarLabelPrintPosition.None),
                e.PrintBarcode(BarcodeType.ITF, "0123456789"),
                e.PrintLine(""),
                e.PrintLine("B&H PHOTO & VIDEO"),
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
                e.PrintLine($"    {encoding.GetBytes(chinese2)}                        89.95         89.95"),
                e.PrintLine("----------------------------------------------------------------"),
                e.RightAlign(),
                e.PrintLine("SUBTOTAL         89.95"),
                e.PrintLine("Total Order:         89.95"),
                e.PrintLine("Total Payment:         89.95"),
                e.PrintLine(""),
                e.LeftAlign(),
                e.SetStyles(PrintStyle.Bold | PrintStyle.FontB),
                e.PrintLine("SOLD TO:                        SHIP TO:"),
                e.SetStyles(PrintStyle.FontB),
                e.PrintLine("  FIRSTN LASTNAME                 FIRSTN LASTNAME"),
                e.PrintLine("  123 FAKE ST.                    123 FAKE ST."),
                e.PrintLine("  DECATUR, IL 12345               DECATUR, IL 12345"),
                e.PrintLine("  (123)456-7890                   (123)456-7890"),
                e.PrintLine("  CUST: 87654321"),
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
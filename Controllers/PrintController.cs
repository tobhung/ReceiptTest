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

namespace ReceiptTest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintController : ControllerBase
    {
        private readonly IPrinterService _printerService;
        private readonly ILogger<PrintController> _logger;
        private readonly IConfiguration _configuration;

        public PrintController(
            IPrinterService printerService, 
            ILogger<PrintController> logger,
            IConfiguration configuration)
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
                var e = new EPSON();
                //var printer = new FilePrinter(filePath: @"\\DESKTOP-F6LR2M9\QX3");
                var printer = new FilePrinter(filePath: "LPT1");
                byte[] bytes = ByteSplicer.Combine(
                e.Initialize(),
            e.CenterAlign(),
            e.PrintLine("BIRCH QX3 - USB MODE"),
            e.PrintLine("------------------------"),
            e.LeftAlign(),
            e.PrintLine($"Time: {DateTime.Now:T}"),
            e.PrintLine("Connection: Shared USB Path"),
            e.FullCut()
        );

        // 4. Send to printer
        printer.Write(bytes);

                //Printer printer = new Printer("QX3");

                // printer.Write(
                //     e.Initialize(),
                //     e.CenterAlign(),
                //     e.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth),
                //     e.PrintLine("BIRCH CP-Q3X"),
                //     e.SetStyles(PrintStyle.None),
                //     e.PrintLine("Test Print"),
                //     e.PrintLine(""),
                //     e.LeftAlign(),
                //     e.PrintLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"),
                //     e.PrintLine("Printer: Online"),
                //     e.PrintLine("Status: OK"),
                //     e.PrintLine(""),
                //     e.PrintLine(""),
                //     e.FullCut()
                // );
                
                // printer.TestPrinter();
                // printer.Append("測試測試記憶卡不能用");
                // printer.Append("記憶卡五百塊");
                // printer.Append("testing page");
                // printer.Separator();
                // printer.FullPaperCut();
                // printer.PrintDocument();

                return Ok(new { message = "Test print sent successfully" });
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
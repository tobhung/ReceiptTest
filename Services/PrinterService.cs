// Services/PrinterService.cs
using ReceiptTest.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using System.Text;
using System.Formats.Tar;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;

namespace ReceiptTest.Services
{
    public class PrinterService : IPrinterService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PrinterService> _logger;

        public PrinterService(IConfiguration configuration, ILogger<PrinterService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> PrintReceipt(Print request)
        {
            try
            {
                ICommandEmitter e = new EPSON(); // Birch CP-Q3X uses ESC/POS (EPSON compatible)
                BasePrinter printer = GetPrinter();

                for (int i = 0; i < request.Copies; i++)
                {
                    // Initialize printer
                    printer.Write(e.Initialize());

                    // Print content
                    var lines = request.Content.Split('\n');

                    foreach (var line in lines)
                    {
                        printer.Write(e.PrintLine(line));
                    }
                    
                    if (request.Cut)
                    {
                        printer.Write(
                            e.PrintLine(""),
                            e.PrintLine("")
                        );
                    }
                    else
                    {
                        printer.Write(e.PrintLine(""));
                    }
                }

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing receipt");
                return false;
            }
        }

        public async Task<bool> PrintRaw(string text)
        {
            try
            {
                ICommandEmitter e = new EPSON();
                BasePrinter printer = GetPrinter();

                printer.Write(
                    e.Initialize(),
                    e.PrintLine(text),
                    e.PrintLine("")
                );

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing raw text");
                return false;
            }
        }

        private BasePrinter GetPrinter()
        {
            var connectionType = _configuration["Printer:ConnectionType"] ?? "USB";
            
            if (connectionType.Equals("Network", StringComparison.OrdinalIgnoreCase))
            {
                var ipAddress = _configuration["Printer:IPAddress"] ?? "192.168.1.100";
                var port = int.Parse(_configuration["Printer:Port"] ?? "9100");

                var settings = new NetworkPrinterSettings
                {
                    ConnectionString = $"{ipAddress}:{port}",
                    PrinterName = "Birch CP-Q3X"
                };

                return new NetworkPrinter(settings);
            }
            else if (connectionType.Equals("Serial", StringComparison.OrdinalIgnoreCase))
            {
                var port = _configuration["Printer:SerialPort"] ?? "/dev/ttyUSB0";
                var baudRate = int.Parse(_configuration["Printer:BaudRate"] ?? "9600");
                
                return new SerialPrinter(port, baudRate);
            }
            else //USB 
            {
                //linux setting
                //var devicePath = _configuration["Printer:DevicePath"] ?? "/dev/usb/lp0";
                
                var usbPort = _configuration["Printer:USBPort"] ?? "USB001";

                var printerName = "QX3";

                return new FilePrinter(filePath: $@"\\.\{printerName}");
        };
        }
    }
}
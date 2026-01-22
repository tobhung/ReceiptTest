using ReceiptTest.Models;
namespace ReceiptTest.Services;

public interface IPrinterService
{
    Task<bool> PrintReceipt(Print printReq);
    Task<bool> PrintRaw(string text);

    
}
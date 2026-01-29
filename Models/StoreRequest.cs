namespace ReceiptTest.Models;

public class StoreRequest
{
    public string Name { get; set; }

    public string InvoiceNO { get; set; }

    public DateTime Date { get; set; }

    public List<Item> Items { get; set; } = new();

    public decimal Total { get; set; }

    public int SellerID { get; set; }

    public int BuyerID { get; set; }

    public string RandomCode { get; set; }

    public string LeftQRData { get; set; }

    public string RightQRData { get; set; }
}
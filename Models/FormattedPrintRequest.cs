namespace ReceiptTest.Models
{
    public class FormattedPrintRequest
    {
        public string? Header { get; set; }
        public string? Body { get; set; }
        public string? Footer { get; set; }
        public string? Barcode { get; set; }
    }
}
namespace ReceiptTest.Models;

public class Print
{
    public string Content { get; set; }

    public int Copies { get; set; } = 1;

    public bool Cut { get; set; } = true;
}
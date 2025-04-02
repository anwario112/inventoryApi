namespace InventoryApi.Models
{
    public class InvoiceRequest
    {
        public int InvoiceNum { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string TotalString { get; set; }
        public List<InvoiceItem> InvoiceItems { get; set; }
    }
    }


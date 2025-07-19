namespace InvoiceOcr.Model
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal? Vat { get; set; }

        public virtual List<InvoiceDetail> Details { get; set; } = new List<InvoiceDetail>();
    }
}

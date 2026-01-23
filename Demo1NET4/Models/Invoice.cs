using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Demo1NET4.Models
{
    [Table("Invoices")]
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        public DateTime InvoiceDate { get; set; }

        public DateTime? EndTime { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        public decimal Subtotal { get; set; }

        public decimal VatAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        [StringLength(200)]
        public string CustomerName { get; set; }

        [StringLength(20)]
        public string CustomerPhone { get; set; }

        [StringLength(50)]
        public string CustomerTaxCode { get; set; }

        [StringLength(20)]
        public string CustomerIdCard { get; set; }

        [StringLength(100)]
        public string CustomerEmail { get; set; }

        [StringLength(500)]
        public string CustomerAddress { get; set; }

        public int? TableId { get; set; }

        [ForeignKey("TableId")]
        public virtual Table Table { get; set; }

        [StringLength(100)]
        public string CashierName { get; set; }

        // Navigation property for InvoiceDetails
        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; }

        public Invoice()
        {
            Status = "open";
            InvoiceDetails = new HashSet<InvoiceDetail>();
        }
    }

    [Table("InvoiceDetails")]
    public class InvoiceDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal Total
        {
            get { return Quantity * UnitPrice; }
        }

        public InvoiceDetail()
        {
            Quantity = 1;
        }
    }
}
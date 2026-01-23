using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Demo1NET4.Models
{
    [Table("Customers")]
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }


        [StringLength(50)]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string MaKH { get; set; }  // Mã khách hàng

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        
        [StringLength(20)]
        public string Phone { get; set; }

        // Nullable - cho phép NULL
        [StringLength(50)]
        public string TaxCode { get; set; }

        // Nullable - cho phép NULL
        [StringLength(50)]
        public string IdCard { get; set; }

        // Nullable - cho phép NULL
        [StringLength(200)]
        public string Email { get; set; }

        
        [StringLength(500)]
        public string Address { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // ✅ Sử dụng Newtonsoft.Json (Web API .NET Framework 4 dùng cái này)
        [JsonIgnore]
        public virtual List<Invoice> Invoices { get; set; }

        // ✅ Constructor để set giá trị mặc định
        public Customer()
        {
            Name = string.Empty;
            Phone = string.Empty;
            Address = string.Empty;
            CreatedAt = DateTime.Now;
            Invoices = new List<Invoice>();
        }
        public int? GroupID { get; set; }
    }
}
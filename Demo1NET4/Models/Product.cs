using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Demo1NET4.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Code { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        public decimal Price { get; set; }

        public int? Stock { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; }

        [Required]
        [StringLength(50)]
        public string UnitType { get; set; }

        [Required]
        public int? UnitTypeId { get; set; }

        // ✅ THÊM GroupID
        public int? GroupID { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        // Constructor
        public Product()
        {
            Name = string.Empty;
            Category = string.Empty;
            UnitType = string.Empty;
            Price = 0;
        }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Demo1NET4.Models
{
    [Table("Groups")]
    public class Group
    {
        [Key]
        public int ID { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [StringLength(10)]
        public string MaNhom { get; set; }

        [Required]
        [StringLength(255)]
        public string TenNhom { get; set; }

        [Required]
        [StringLength(20)]
        [Column("type")] // Tên cột trong DB
        public string Type { get; set; } // "customer" hoặc "product"
    }
}
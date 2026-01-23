using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Demo1NET4.Models
{
    [Table("Tables")]
    public class Table
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        // FK nhóm bàn
        public int TableGroupId { get; set; }
        [ForeignKey("TableGroupId")]
        [JsonIgnore]
        public virtual TableGroup TableGroup { get; set; }

        // Invoice đang phục vụ
        public int? CurrentInvoiceId { get; set; }

        public Table()
        {
            Status = "empty";
        }
    }
}
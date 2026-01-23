using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Demo1NET4.Models
{
    [Table("TableGroups")]
    public class TableGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        // Navigation property
        [JsonIgnore]
        public virtual ICollection<Table> Tables { get; set; }

        public TableGroup()
        {
            Tables = new List<Table>();
        }
    }
}
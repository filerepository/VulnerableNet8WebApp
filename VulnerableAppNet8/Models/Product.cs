using System.ComponentModel.DataAnnotations;

namespace VulnerableAppNet8.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string? CreatedByUserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [StringLength(8000)]
        public string Description { get; set; }
    }
}

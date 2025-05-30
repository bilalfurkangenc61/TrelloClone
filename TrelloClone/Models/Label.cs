using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    // Bu sınıf kartlarda kullanılan etiketleri temsil eder
    public class Label
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Color { get; set; } = "#61bd4f"; // Varsayılan yeşil

        // Hangi takımın etiketi
        public int TeamId { get; set; }
        public virtual Team Team { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Bu etiketi kullanan kartlar
        public virtual ICollection<CardLabel> CardLabels { get; set; } = new List<CardLabel>();
    }
}
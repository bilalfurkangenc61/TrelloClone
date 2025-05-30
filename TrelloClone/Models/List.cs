using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    // Bu sınıf panolardaki listeleri temsil eder (Yapılacak, Devam Eden, Tamamlanan gibi)
    public class List
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        // Hangi panoda
        public int BoardId { get; set; }
        public virtual Board Board { get; set; } = null!;

        // Listenin sırası (soldan sağa)
        public int Position { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsArchived { get; set; } = false;

        // Listedeki kartlar
        public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
    }
}
using System.ComponentModel.DataAnnotations;



namespace TrelloClone.Models
{
    // Bu sınıf kartlardaki kontrol listelerini temsil eder
    public class Checklist
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        // Hangi kart
        public int CardId { get; set; }
        public virtual Card Card { get; set; } = null!;

        // Kontrol listesinin sırası
        public int Position { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Kontrol listesindeki öğeler
        public virtual ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
    }
}
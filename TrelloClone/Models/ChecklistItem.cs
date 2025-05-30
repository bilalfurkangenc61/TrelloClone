using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    // Bu sınıf kontrol listelerindeki tek tek öğeleri temsil eder
    public class ChecklistItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Text { get; set; } = string.Empty;

        // Hangi kontrol listesi
        public int ChecklistId { get; set; }
        public virtual Checklist Checklist { get; set; } = null!;

        // Öğenin sırası
        public int Position { get; set; }

        // Tamamlanmış mı?
        public bool IsCompleted { get; set; } = false;

        // Tamamlayan kullanıcı
        public string? CompletedByUserId { get; set; }
        public virtual ApplicationUser? CompletedByUser { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
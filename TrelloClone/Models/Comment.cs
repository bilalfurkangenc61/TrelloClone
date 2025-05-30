using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    // Bu sınıf kartlara yazılan yorumları temsil eder
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        // Hangi kart
        public int CardId { get; set; }
        public virtual Card Card { get; set; } = null!;

        // Yorumu yazan kullanıcı
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Yorum silinmiş mi?
        public bool IsDeleted { get; set; } = false;
    }
}
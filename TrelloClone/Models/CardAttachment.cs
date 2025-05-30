using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    // Bu sınıf kartlara eklenen dosyaları temsil eder
    public class CardAttachment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [StringLength(100)]
        public string? ContentType { get; set; }

        // Hangi kart
        public int CardId { get; set; }
        public virtual Card Card { get; set; } = null!;

        // Dosyayı yükleyen kullanıcı
        public string UploadedByUserId { get; set; } = string.Empty;
        public virtual ApplicationUser UploadedByUser { get; set; } = null!;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
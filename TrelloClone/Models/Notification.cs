using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    // Bu sınıf kullanıcılara gönderilen bildirimleri temsil eder
    public class Notification
    {
        public int Id { get; set; }

        // Bildirimi alan kullanıcı
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        // Bildirim başlığı
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        // Bildirim içeriği
        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        // Bildirim türü
        public NotificationType Type { get; set; }

        // İlgili kart (varsa)
        public int? CardId { get; set; }
        public virtual Card? Card { get; set; }

        // İlgili pano (varsa)
        public int? BoardId { get; set; }
        public virtual Board? Board { get; set; }

        // Okundu mu?
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Bildirim türleri
    public enum NotificationType
    {
        CardAssigned = 0,       // Kart atandı
        CardDueSoon = 1,        // Kart son tarihi yaklaşıyor
        CardOverdue = 2,        // Kart gecikti
        CommentAdded = 3,       // Yorum eklendi
        CardMoved = 4,          // Kart taşındı
        BoardInvited = 5,       // Panoya davet edildi
        TeamInvited = 6,        // Takıma davet edildi
        SprintStarted = 7,      // Sprint başladı
        SprintEnded = 8         // Sprint bitti
    }
}
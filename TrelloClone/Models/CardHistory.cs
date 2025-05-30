using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    // Bu sınıf kartlarda yapılan değişikliklerin geçmişini tutar
    public class CardHistory
    {
        public int Id { get; set; }

        // Hangi kart
        public int CardId { get; set; }
        public virtual Card Card { get; set; } = null!;

        // İşlemi yapan kullanıcı
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        // Ne türde bir işlem yapıldı
        [Required]
        public HistoryActionType ActionType { get; set; }

        // İşlem açıklaması
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        // Eski değer (JSON formatında)
        public string? OldValue { get; set; }

        // Yeni değer (JSON formatında)
        public string? NewValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Geçmiş işlem türleri
    public enum HistoryActionType
    {
        Created = 0,        // Kart oluşturuldu
        Updated = 1,        // Kart güncellendi
        Moved = 2,          // Kart taşındı
        Assigned = 3,       // Kart atandı
        Unassigned = 4,     // Kart ataması kaldırıldı
        CommentAdded = 5,   // Yorum eklendi
        AttachmentAdded = 6, // Dosya eklendi
        LabelAdded = 7,     // Etiket eklendi
        LabelRemoved = 8,   // Etiket kaldırıldı
        Archived = 9,       // Kart arşivlendi
        Restored = 10       // Kart geri yüklendi
    }
}
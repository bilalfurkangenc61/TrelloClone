using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    public class Card
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        // Hangi listede
        public int ListId { get; set; }
        public virtual List List { get; set; } = null!;

        // Kartın sırası (yukarıdan aşağıya)
        public int Position { get; set; }

        // Son tarih
        public DateTime? DueDate { get; set; }

        // Kartın rengi
        public string? Color { get; set; }

        // Kart kapak resmi
        public string? CoverImage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsArchived { get; set; } = false;

        // Sprint ilişkisini kaldırdık - ayrı tablo kullanacağız

        // Karta atanan kişiler
        public virtual ICollection<CardAssignment> Assignments { get; set; } = new List<CardAssignment>();

        // Kartın etiketleri
        public virtual ICollection<CardLabel> Labels { get; set; } = new List<CardLabel>();

        // Kartın yorumları
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        // Kartın kontrol listeleri
        public virtual ICollection<Checklist> Checklists { get; set; } = new List<Checklist>();

        // Kartın dosyaları
        public virtual ICollection<CardAttachment> Attachments { get; set; } = new List<CardAttachment>();

        // Kart geçmişi
        public virtual ICollection<CardHistory> History { get; set; } = new List<CardHistory>();

        // Sprint ilişkisi için ayrı tablo
        public virtual ICollection<CardSprint> CardSprints { get; set; } = new List<CardSprint>();
    }
}
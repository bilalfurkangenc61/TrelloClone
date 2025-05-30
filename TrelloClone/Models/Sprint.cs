using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    public class Sprint
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        // Hangi pano
        public int BoardId { get; set; }
        public virtual Board Board { get; set; } = null!;

        // Sprint başlangıç tarihi
        public DateTime StartDate { get; set; }

        // Sprint bitiş tarihi
        public DateTime EndDate { get; set; }

        // Sprint durumu
        public SprintStatus Status { get; set; } = SprintStatus.Planning;

        // Sprinti oluşturan kullanıcı
        public string CreatedByUserId { get; set; } = string.Empty;
        public virtual ApplicationUser CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Kartlar - ayrı tablo üzerinden
        public virtual ICollection<CardSprint> CardSprints { get; set; } = new List<CardSprint>();
    }

    public enum SprintStatus
    {
        Planning = 0,   // Planlama aşamasında
        Active = 1,     // Aktif sprint
        Completed = 2,  // Tamamlanmış
        Cancelled = 3   // İptal edilmiş
    }
}
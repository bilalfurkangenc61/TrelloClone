using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    // Bu sınıf çalışma panolarını temsil eder
    public class Board
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Hangi takımın panosu
        public int TeamId { get; set; }
        public virtual Team Team { get; set; } = null!;

        // Panoyu oluşturan kullanıcı
        public string CreatedByUserId { get; set; } = string.Empty;
        public virtual ApplicationUser CreatedByUser { get; set; } = null!;

        // Pano rengi (CSS renk kodu)
        public string BackgroundColor { get; set; } = "#0079bf"; // Varsayılan mavi

        // Arka plan resmi
        public string? BackgroundImage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsArchived { get; set; } = false;

        public bool IsActive { get; set; } = true;

        // Panodaki listeler
        public virtual ICollection<List> Lists { get; set; } = new List<List>();

        // Panodaki sprintler
        public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
    }
}
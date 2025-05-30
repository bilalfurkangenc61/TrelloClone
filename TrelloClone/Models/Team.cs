using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    // Bu sınıf çalışma takımlarını temsil eder
    public class Team
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Takım üyeleri
        public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();

        // Takımın panoları
        public virtual ICollection<Board> Boards { get; set; } = new List<Board>();
    }
}
using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    // Bu sınıf hangi kullanıcının hangi takımda olduğunu gösterir
    public class TeamMember
    {
        public int Id { get; set; }

        // Hangi takım
        public int TeamId { get; set; }
        public virtual Team Team { get; set; } = null!;

        // Hangi kullanıcı
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        // Kullanıcının takımdaki rolü
        [Required]
        public UserRole Role { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }

    // Kullanıcı rolleri
    public enum UserRole
    {
        Guest = 0,    // Misafir - sadece görüntüleme
        Member = 1,   // Üye - temel işlemler
        Admin = 2     // Yönetici - tüm yetkiler
    }
}
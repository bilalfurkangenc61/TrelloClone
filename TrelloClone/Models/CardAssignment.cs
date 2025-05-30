namespace TrelloClone.Models
{
    // Bu sınıf hangi kartın hangi kullanıcıya atandığını gösterir
    public class CardAssignment
    {
        public int Id { get; set; }

        // Hangi kart
        public int CardId { get; set; }
        public virtual Card Card { get; set; } = null!;

        // Hangi kullanıcı
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Atamayı yapan kişi
        public string AssignedByUserId { get; set; } = string.Empty;
        public virtual ApplicationUser AssignedByUser { get; set; } = null!;
    }
}
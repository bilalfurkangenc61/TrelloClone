namespace TrelloClone.Models
{
    // Bu sınıf kart-sprint ilişkisini ayrı tablolda tutar
    public class CardSprint
    {
        public int Id { get; set; }

        // Hangi kart
        public int CardId { get; set; }
        public virtual Card Card { get; set; } = null!;

        // Hangi sprint
        public int SprintId { get; set; }
        public virtual Sprint Sprint { get; set; } = null!;

        // Sprinte eklenme tarihi
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Aktif mi (kart birden fazla sprinte eklenebilir, ama sadece biri aktif)
        public bool IsActive { get; set; } = true;
    }
}
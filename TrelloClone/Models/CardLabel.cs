namespace TrelloClone.Models
{
    // Bu sınıf hangi kartın hangi etiketleri kullandığını gösterir
    public class CardLabel
    {
        public int Id { get; set; }

        // Hangi kart
        public int CardId { get; set; }
        public virtual Card Card { get; set; } = null!;

        // Hangi etiket
        public int LabelId { get; set; }
        public virtual Label Label { get; set; } = null!;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
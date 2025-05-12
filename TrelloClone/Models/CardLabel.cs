namespace TrelloClone.Models
{
    public class CardLabel
    {
        public int Id { get; set; }

        // Card reference
        public int CardId { get; set; }
        public virtual Card Card { get; set; }

        // Label reference
        public int LabelId { get; set; }
        public virtual Label Label { get; set; }
    }
}

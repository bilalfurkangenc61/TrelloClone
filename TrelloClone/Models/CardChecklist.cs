namespace TrelloClone.Models
{
    public class CardChecklist
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Position { get; set; }

        // Card reference
        public int CardId { get; set; }
        public virtual Card Card { get; set; }

        // Navigation properties
        public virtual ICollection<ChecklistItem> Items { get; set; }
    }
}
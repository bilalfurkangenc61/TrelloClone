namespace TrelloClone.Models
{
    public class BoardList
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Position { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsArchived { get; set; }
        // Board reference
        public int BoardId { get; set; }
        public virtual Board Board { get; set; }
        // Navigation properties
        public virtual ICollection<Card> Cards { get; set; }
    }
}
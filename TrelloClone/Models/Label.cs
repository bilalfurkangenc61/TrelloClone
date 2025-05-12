namespace TrelloClone.Models
{
    public class Label
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }

        // Board reference
        public int BoardId { get; set; }
        public virtual Board Board { get; set; }

        // Navigation properties
        public virtual ICollection<CardLabel> Cards { get; set; }
    }
}
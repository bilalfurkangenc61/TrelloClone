namespace TrelloClone.Models
{
    public class CardComment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Card reference
        public int CardId { get; set; }
        public virtual Card Card { get; set; }

        // User reference
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}
namespace TrelloClone.Models
{
    public class BoardMember
    {
        public int Id { get; set; }

        // Board reference
        public int BoardId { get; set; }
        public virtual Board Board { get; set; }

        // User reference
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public string Role { get; set; } // Admin, Editor, Viewer
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
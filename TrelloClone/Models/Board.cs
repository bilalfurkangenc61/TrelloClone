namespace TrelloClone.Models
{
    public class Board
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string BackgroundColor { get; set; }
      
        public string BackgroundImage { get; set; } = null; 
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Owner reference
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Navigation properties
        public virtual ICollection<BoardList> Lists { get; set; }
        public virtual ICollection<BoardMember> Members { get; set; }
        public ICollection<Sprint> Sprints { get; set; }
    }
}
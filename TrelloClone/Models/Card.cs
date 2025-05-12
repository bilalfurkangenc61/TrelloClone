namespace TrelloClone.Models
{
    public class Card
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; } = "";  // Null değil, boş string
        public int Position { get; set; }
        public string? CoverColor { get; set; } = "";  // Null değil, boş string
        public string CoverImage { get; set; } = "";  // Null değil, boş string
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; } = false;  // Varsayılan olarak false
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public int? SprintId { get; set; }
        public Sprint Sprint { get; set; }

        // Story Point özelliği (Scrum için)
        public int? StoryPoints { get; set; }

        // BoardList reference
        public int BoardListId { get; set; }
        public virtual BoardList BoardList { get; set; }

        // User reference (card creator)
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Navigation properties
        public virtual ICollection<CardMember> Members { get; set; } = new List<CardMember>();  // Boş liste
        public virtual ICollection<CardLabel> Labels { get; set; } = new List<CardLabel>();  // Boş liste
        public virtual ICollection<CardAttachment> Attachments { get; set; } = new List<CardAttachment>();  // Boş liste
        public virtual ICollection<CardComment> Comments { get; set; } = new List<CardComment>();  // Boş liste
        public virtual ICollection<CardChecklist> Checklists { get; set; } = new List<CardChecklist>();  // Boş liste
    }
}
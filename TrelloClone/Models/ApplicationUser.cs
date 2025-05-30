using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public string? ProfilePicture { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties - düzeltildi
        // Bu navigation property'ler ApplicationUser'da olmalı:
        public virtual ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
        public virtual ICollection<Board> CreatedBoards { get; set; } = new List<Board>();
        public virtual ICollection<CardAssignment> AssignedCards { get; set; } = new List<CardAssignment>();
        public virtual ICollection<CardAssignment> MadeAssignments { get; set; } = new List<CardAssignment>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Sprint> CreatedSprints { get; set; } = new List<Sprint>();
        public virtual ICollection<ChecklistItem> CompletedChecklistItems { get; set; } = new List<ChecklistItem>();
        public virtual ICollection<CardAttachment> UploadedAttachments { get; set; } = new List<CardAttachment>();
        public virtual ICollection<CardHistory> CardHistories { get; set; } = new List<CardHistory>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
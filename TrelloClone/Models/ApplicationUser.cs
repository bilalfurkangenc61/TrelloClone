// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace TrelloClone.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Abonelik özellikleri
        public string SubscriptionType { get; set; } // Free, Basic, Premium
        public DateTime? SubscriptionEndDate { get; set; }

        // Navigation properties
        public virtual ICollection<Board> Boards { get; set; }
        public virtual ICollection<BoardMember> BoardMembers { get; set; }
        public virtual ICollection<CardMember> CardMembers { get; set; }
    }
}
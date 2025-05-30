using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrelloClone.Models;

namespace TrelloClone.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<List> Lists { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<CardAssignment> CardAssignments { get; set; }
        public DbSet<Label> Labels { get; set; }
        public DbSet<CardLabel> CardLabels { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Checklist> Checklists { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }
        public DbSet<CardAttachment> CardAttachments { get; set; }
        public DbSet<CardHistory> CardHistories { get; set; }
        public DbSet<Sprint> Sprints { get; set; }
        public DbSet<CardSprint> CardSprints { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // TÜM İLİŞKİLERİ MANUEL TANIMLA - CASCADE YOK

            // Team - TeamMember
            builder.Entity<TeamMember>()
                .HasOne(tm => tm.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TeamMember>()
                .HasOne(tm => tm.User)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Board - Team
            builder.Entity<Board>()
                .HasOne(b => b.Team)
                .WithMany(t => t.Boards)
                .HasForeignKey(b => b.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Board - User (CreatedBy)
            builder.Entity<Board>()
                .HasOne(b => b.CreatedByUser)
                .WithMany(u => u.CreatedBoards)
                .HasForeignKey(b => b.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // List - Board
            builder.Entity<List>()
                .HasOne(l => l.Board)
                .WithMany(b => b.Lists)
                .HasForeignKey(l => l.BoardId)
                .OnDelete(DeleteBehavior.Restrict);

            // Card - List
            builder.Entity<Card>()
                .HasOne(c => c.List)
                .WithMany(l => l.Cards)
                .HasForeignKey(c => c.ListId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sprint - Board
            builder.Entity<Sprint>()
                .HasOne(s => s.Board)
                .WithMany(b => b.Sprints)
                .HasForeignKey(s => s.BoardId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sprint - User (CreatedBy)
            builder.Entity<Sprint>()
                .HasOne(s => s.CreatedByUser)
                .WithMany(u => u.CreatedSprints)
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // CardSprint - Card
            builder.Entity<CardSprint>()
                .HasOne(cs => cs.Card)
                .WithMany(c => c.CardSprints)
                .HasForeignKey(cs => cs.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            // CardSprint - Sprint
            builder.Entity<CardSprint>()
                .HasOne(cs => cs.Sprint)
                .WithMany(s => s.CardSprints)
                .HasForeignKey(cs => cs.SprintId)
                .OnDelete(DeleteBehavior.Restrict);

            // CardAssignment - Card
            builder.Entity<CardAssignment>()
                .HasOne(ca => ca.Card)
                .WithMany(c => c.Assignments)
                .HasForeignKey(ca => ca.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            // CardAssignment - User (Assigned to)
            builder.Entity<CardAssignment>()
                .HasOne(ca => ca.User)
                .WithMany(u => u.AssignedCards)
                .HasForeignKey(ca => ca.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // CardAssignment - User (Assigned by)
            builder.Entity<CardAssignment>()
                .HasOne(ca => ca.AssignedByUser)
                .WithMany(u => u.MadeAssignments)
                .HasForeignKey(ca => ca.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Label - Team
            builder.Entity<Label>()
                .HasOne(l => l.Team)
                .WithMany()
                .HasForeignKey(l => l.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // CardLabel - Card
            builder.Entity<CardLabel>()
                .HasOne(cl => cl.Card)
                .WithMany(c => c.Labels)
                .HasForeignKey(cl => cl.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            // CardLabel - Label
            builder.Entity<CardLabel>()
                .HasOne(cl => cl.Label)
                .WithMany(l => l.CardLabels)
                .HasForeignKey(cl => cl.LabelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment - Card
            builder.Entity<Comment>()
                .HasOne(c => c.Card)
                .WithMany(card => card.Comments)
                .HasForeignKey(c => c.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment - User
            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Checklist - Card
            builder.Entity<Checklist>()
                .HasOne(cl => cl.Card)
                .WithMany(c => c.Checklists)
                .HasForeignKey(cl => cl.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            // ChecklistItem - Checklist
            builder.Entity<ChecklistItem>()
                .HasOne(cli => cli.Checklist)
                .WithMany(cl => cl.Items)
                .HasForeignKey(cli => cli.ChecklistId)
                .OnDelete(DeleteBehavior.Restrict);

            // ChecklistItem - User (CompletedBy) - nullable
            builder.Entity<ChecklistItem>()
                .HasOne(cli => cli.CompletedByUser)
                .WithMany(u => u.CompletedChecklistItems)
                .HasForeignKey(cli => cli.CompletedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // CardAttachment - Card
            builder.Entity<CardAttachment>()
                .HasOne(ca => ca.Card)
                .WithMany(c => c.Attachments)
                .HasForeignKey(ca => ca.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            // CardAttachment - User (UploadedBy)
            builder.Entity<CardAttachment>()
                .HasOne(ca => ca.UploadedByUser)
                .WithMany(u => u.UploadedAttachments)
                .HasForeignKey(ca => ca.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // CardHistory - Card
            builder.Entity<CardHistory>()
                .HasOne(ch => ch.Card)
                .WithMany(c => c.History)
                .HasForeignKey(ch => ch.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            // CardHistory - User
            builder.Entity<CardHistory>()
                .HasOne(ch => ch.User)
                .WithMany(u => u.CardHistories)
                .HasForeignKey(ch => ch.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification - User
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification - Card (nullable)
            builder.Entity<Notification>()
                .HasOne(n => n.Card)
                .WithMany()
                .HasForeignKey(n => n.CardId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification - Board (nullable)
            builder.Entity<Notification>()
                .HasOne(n => n.Board)
                .WithMany()
                .HasForeignKey(n => n.BoardId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.Entity<TeamMember>()
                .HasIndex(tm => new { tm.TeamId, tm.UserId })
                .IsUnique();

            builder.Entity<CardAssignment>()
                .HasIndex(ca => new { ca.CardId, ca.UserId })
                .IsUnique();

            builder.Entity<CardLabel>()
                .HasIndex(cl => new { cl.CardId, cl.LabelId })
                .IsUnique();

            builder.Entity<CardSprint>()
                .HasIndex(cs => new { cs.CardId, cs.SprintId })
                .IsUnique();
        }
    }
}
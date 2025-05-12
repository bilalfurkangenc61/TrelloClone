// Data/ApplicationDbContext.cs
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

        public DbSet<Sprint> Sprints { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<BoardList> BoardLists { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<CardMember> CardMembers { get; set; }
        public DbSet<BoardMember> BoardMembers { get; set; }
        public DbSet<Label> Labels { get; set; }
        public DbSet<CardLabel> CardLabels { get; set; }
        public DbSet<CardAttachment> CardAttachments { get; set; }
        public DbSet<CardComment> CardComments { get; set; }
        public DbSet<CardChecklist> CardChecklists { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionTransaction> SubscriptionTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Card - BoardList ilişkisi - CASCADE olarak değiştirildi
            builder.Entity<Card>()
                   .HasOne(c => c.BoardList)
                   .WithMany(bl => bl.Cards)
                   .HasForeignKey(c => c.BoardListId)
                   .OnDelete(DeleteBehavior.Cascade);

            // BoardList - Board ilişkisi - CASCADE olarak değiştirildi
            builder.Entity<BoardList>()
                .HasOne(bl => bl.Board)
                .WithMany(b => b.Lists)
                .HasForeignKey(bl => bl.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            // Board - User ilişkisi
            builder.Entity<Board>()
                .HasOne(b => b.User)
                .WithMany(u => u.Boards)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // BoardMember ilişkileri - CASCADE olarak değiştirildi
            builder.Entity<BoardMember>()
                .HasOne(bm => bm.Board)
                .WithMany(b => b.Members)
                .HasForeignKey(bm => bm.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BoardMember>()
                .HasOne(bm => bm.User)
                .WithMany(u => u.BoardMembers)
                .HasForeignKey(bm => bm.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // CardMember ilişkileri - CASCADE olarak değiştirildi
            builder.Entity<CardMember>()
                .HasOne(cm => cm.Card)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CardMember>()
                .HasOne(cm => cm.User)
                .WithMany(u => u.CardMembers)
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Card - User ilişkisi (creator)
            builder.Entity<Card>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // CardLabel ilişkileri - CASCADE olarak değiştirildi
            builder.Entity<CardLabel>()
                .HasOne(cl => cl.Card)
                .WithMany(c => c.Labels)
                .HasForeignKey(cl => cl.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CardLabel>()
                .HasOne(cl => cl.Label)
                .WithMany(l => l.Cards)
                .HasForeignKey(cl => cl.LabelId)
                .OnDelete(DeleteBehavior.NoAction);

            // Label - Board ilişkisi - CASCADE olarak değiştirildi
            builder.Entity<Label>()
                .HasOne(l => l.Board)
                .WithMany()
                .HasForeignKey(l => l.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            // CardAttachment ilişkileri - CASCADE olarak değiştirildi
            builder.Entity<CardAttachment>()
                .HasOne(ca => ca.Card)
                .WithMany(c => c.Attachments)
                .HasForeignKey(ca => ca.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CardAttachment>()
                .HasOne(ca => ca.User)
                .WithMany()
                .HasForeignKey(ca => ca.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // CardComment ilişkileri - CASCADE olarak değiştirildi
            builder.Entity<CardComment>()
                .HasOne(cc => cc.Card)
                .WithMany(c => c.Comments)
                .HasForeignKey(cc => cc.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CardComment>()
                .HasOne(cc => cc.User)
                .WithMany()
                .HasForeignKey(cc => cc.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // CardChecklist ilişkisi - CASCADE olarak değiştirildi
            builder.Entity<CardChecklist>()
                .HasOne(cc => cc.Card)
                .WithMany(c => c.Checklists)
                .HasForeignKey(cc => cc.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChecklistItem ilişkisi - CASCADE olarak değiştirildi
            builder.Entity<ChecklistItem>()
                .HasOne(ci => ci.Checklist)
                .WithMany(cl => cl.Items)
                .HasForeignKey(ci => ci.ChecklistId)
                .OnDelete(DeleteBehavior.Cascade);

            // SubscriptionTransaction ilişkileri
            builder.Entity<SubscriptionTransaction>()
                .HasOne(st => st.User)
                .WithMany()
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<SubscriptionTransaction>()
                .HasOne(st => st.Subscription)
                .WithMany()
                .HasForeignKey(st => st.SubscriptionId)
                .OnDelete(DeleteBehavior.NoAction);

            // Sprint ilişkileri
            builder.Entity<Sprint>()
                .HasOne(s => s.Board)
                .WithMany(b => b.Sprints)
                .HasForeignKey(s => s.BoardId)
                .OnDelete(DeleteBehavior.NoAction);  // Cascade yerine NoAction

            // Card-Sprint ilişkisi
            builder.Entity<Card>()
                .HasOne(c => c.Sprint)
                .WithMany(s => s.Cards)
                .HasForeignKey(c => c.SprintId)
                .IsRequired(false)  // SprintId null olabilir
                .OnDelete(DeleteBehavior.ClientSetNull);  // NoAction yerine ClientSetNull

            // Seed data for Subscriptions
            builder.Entity<Subscription>().HasData(
                new Subscription
                {
                    Id = 1,
                    Name = "Free",
                    Price = 0,
                    Description = "Basic Kanban functionality for individuals",
                    MaxBoards = 3,
                    MaxMembersPerBoard = 5,
                    AllowAttachments = false,
                    MaxAttachmentSize = 0,
                    AllowCustomBackgrounds = false,
                    AdvancedReporting = false
                },
                new Subscription
                {
                    Id = 2,
                    Name = "Basic",
                    Price = 9.99m,
                    Description = "Enhanced features for small teams",
                    MaxBoards = 10,
                    MaxMembersPerBoard = 15,
                    AllowAttachments = true,
                    MaxAttachmentSize = 10 * 1024 * 1024, // 10MB
                    AllowCustomBackgrounds = true,
                    AdvancedReporting = false
                },
                new Subscription
                {
                    Id = 3,
                    Name = "Premium",
                    Price = 24.99m,
                    Description = "Full-featured solution for organizations",
                    MaxBoards = 100,
                    MaxMembersPerBoard = 50,
                    AllowAttachments = true,
                    MaxAttachmentSize = 100 * 1024 * 1024, // 100MB
                    AllowCustomBackgrounds = true,
                    AdvancedReporting = true
                }
            );
        }
    }
}
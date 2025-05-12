namespace TrelloClone.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string Name { get; set; } // Free, Basic, Premium
        public decimal Price { get; set; }
        public string Description { get; set; }
        public int MaxBoards { get; set; }
        public int MaxMembersPerBoard { get; set; }
        public bool AllowAttachments { get; set; }
        public long MaxAttachmentSize { get; set; } // in bytes
        public bool AllowCustomBackgrounds { get; set; }
        public bool AdvancedReporting { get; set; }
    }
}
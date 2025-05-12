namespace TrelloClone.Models
{
    public class CardAttachment
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.Now;

        // Card reference
        public int CardId { get; set; }
        public virtual Card Card { get; set; }

        // User reference
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}
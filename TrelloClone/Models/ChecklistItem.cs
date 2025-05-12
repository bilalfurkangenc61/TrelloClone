namespace TrelloClone.Models
{
    public class ChecklistItem
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsCompleted { get; set; }
        public int Position { get; set; }

        // Checklist reference
        public int ChecklistId { get; set; }
        public virtual CardChecklist Checklist { get; set; }
    }
}
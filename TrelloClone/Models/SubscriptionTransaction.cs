namespace TrelloClone.Models
{
    public class SubscriptionTransaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; } // Success, Failed, Pending, Refunded
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        // User reference
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Subscription reference
        public int SubscriptionId { get; set; }
        public virtual Subscription Subscription { get; set; }
    }
}
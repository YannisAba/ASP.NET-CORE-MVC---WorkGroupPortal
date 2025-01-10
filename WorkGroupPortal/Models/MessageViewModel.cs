namespace WorkGroupPortal.Models
{
    public class MessageViewModel
    {
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsCurrentUser { get; set; }
    }
}

namespace WorkGroupPortal.Models
{
    public class SeeMessagesOfGroupViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string GroupLeader { get; set; }
        public List<User> AcceptedMembers { get; set; }
        public List<User> PendingMembers { get; set; }
        public List<MessageViewModel> Messages { get; set; }
    }
}

namespace SimpleChatBe.Models
{
    public class CreateMessageRequest
    {
        public string RoomId { get; set; } = default!;
        public string FromUserId { get; set; } = default!;
        public string Text { get; set; } = string.Empty;
    }
}

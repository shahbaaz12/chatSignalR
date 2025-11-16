using System;
using System.Collections.Generic;

namespace SimpleChatBe.Models
{
    // In-memory DTO/entity used by the repo
    public class MessageDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RoomId { get; set; } = default!;
        public string FromUserId { get; set; } = default!;
        public string Text { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Per-user read receipts (usernames)
        public List<string> SeenBy { get; set; } = new List<string>();
    }
}

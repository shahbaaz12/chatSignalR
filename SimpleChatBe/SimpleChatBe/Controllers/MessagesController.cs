using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SimpleChatBe.Hubs;
using SimpleChatBe.Models;
using SimpleChatBe.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleChatBe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IChatRepository _repo;
        private readonly IHubContext<ChatHub> _hub;

        public MessagesController(IChatRepository repo, IHubContext<ChatHub> hub)
        {
            _repo = repo;
            _hub = hub;
        }

        // GET: api/messages/{roomId}?limit=100
        [HttpGet("{roomId}")]
        public IActionResult GetMessages(string roomId, [FromQuery] int limit = 100)
        {
            var messages = _repo.GetMessages(roomId, limit);
            return Ok(messages);
        }

        // POST: api/messages
        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] CreateMessageRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.RoomId) || string.IsNullOrWhiteSpace(req.FromUserId))
                return BadRequest("roomId and fromUserId required.");

            var message = new MessageDto
            {
                Id = Guid.NewGuid().ToString(),
                RoomId = req.RoomId,
                FromUserId = req.FromUserId,
                Text = req.Text ?? string.Empty,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _repo.SaveMessage(message);

            // Broadcast to room
            await _hub.Clients.Group(req.RoomId).SendAsync("NewMessage", message);

            return Ok(message);
        }

        // POST: api/messages/seen
        // Body: { "roomId":"room1", "messageIds":["id1","id2"], "username":"Alice" }
        [HttpPost("seen")]
        public async Task<IActionResult> MarkSeen([FromBody] MarkSeenRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.RoomId) || req.MessageIds == null || req.MessageIds.Length == 0 || string.IsNullOrWhiteSpace(req.Username))
                return BadRequest("roomId, messageIds and username required.");

            var updated = _repo.MarkSeen(req.RoomId, req.MessageIds, req.Username).ToList();

            // Broadcast MessageSeen for each updated message
            foreach (var mid in updated)
            {
                await _hub.Clients.Group(req.RoomId).SendAsync("MessageSeen", new { messageId = mid, username = req.Username });
            }

            return Ok(new { updated = updated.Count, ids = updated });
        }
    }

    // Request DTOs used by controller
    public class CreateMessageRequest
    {
        public string RoomId { get; set; } = default!;
        public string FromUserId { get; set; } = default!;
        public string Text { get; set; } = string.Empty;
    }

    public class MarkSeenRequest
    {
        public string RoomId { get; set; } = default!;
        public string[] MessageIds { get; set; } = default!;
        public string Username { get; set; } = default!;
    }
}

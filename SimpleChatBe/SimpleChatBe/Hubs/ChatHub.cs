using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleChatBe.Hubs
{
    public class ChatHub : Hub
    {
        // optional: presence mapping (connectionId -> username)
        // If you have RegisterUser logic in frontend, you can store it here.
        private static Dictionary<string, string> _users = new();

        public async Task RegisterUser(string username)
        {
            _users[Context.ConnectionId] = username;
            await Clients.All.SendAsync("UserListUpdated", _users.Values.ToList());
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_users.ContainsKey(Context.ConnectionId))
            {
                _users.Remove(Context.ConnectionId);
                await Clients.All.SendAsync("UserListUpdated", _users.Values.ToList());
            }

            await base.OnDisconnectedAsync(exception);
        }

        public Task JoinRoom(string roomId) => Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        public Task LeaveRoom(string roomId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

        public Task Typing(string roomId, string userId, bool isTyping)
        {
            return Clients.OthersInGroup(roomId)
                .SendAsync("UserTyping", new { roomId, userId, isTyping });
        }

        // Controller can call this to broadcast a new message to the room.
        public Task BroadcastMessage(string roomId, object messageDto)
        {
            return Clients.Group(roomId).SendAsync("NewMessage", messageDto);
        }

        // Controller will call this when message(s) are marked seen
        public Task BroadcastMessageSeen(string roomId, string messageId, string username)
        {
            return Clients.Group(roomId).SendAsync("MessageSeen", new { messageId, username });
        }
    }
}

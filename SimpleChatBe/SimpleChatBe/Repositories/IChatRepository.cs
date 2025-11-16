using SimpleChatBe.Models;
using System.Collections.Generic;

namespace SimpleChatBe.Repositories
{
    public interface IChatRepository
    {
        void SaveMessage(MessageDto message);
        IEnumerable<MessageDto> GetMessages(string roomId, int limit = 100);

        // Mark message(s) as seen by username. Returns list of messageIds actually updated.
        IEnumerable<string> MarkSeen(string roomId, string[] messageIds, string username);
    }
}

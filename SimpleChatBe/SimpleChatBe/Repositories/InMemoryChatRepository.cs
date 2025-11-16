using SimpleChatBe.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SimpleChatBe.Repositories
{
    public class InMemoryChatRepository : IChatRepository
    {
        // roomId -> queue of messages (chronological order by enqueue)
        private readonly ConcurrentDictionary<string, ConcurrentQueue<MessageDto>> _store
            = new();

        public void SaveMessage(MessageDto message)
        {
            var queue = _store.GetOrAdd(message.RoomId, _ => new ConcurrentQueue<MessageDto>());
            queue.Enqueue(message);

            // Limit memory to latest ~1000 messages per room
            while (queue.Count > 1000)
            {
                queue.TryDequeue(out _);
            }
        }

        public IEnumerable<MessageDto> GetMessages(string roomId, int limit = 100)
        {
            if (!_store.TryGetValue(roomId, out var queue))
                return Enumerable.Empty<MessageDto>();

            // Return last N messages in chronological order
            return queue.Reverse().Take(limit).Reverse().ToList();
        }

        /// <summary>
        /// Mark the requested message ids as seen by given username (for the specified room).
        /// Returns the list of messageIds that were actually updated (i.e., where username was newly added).
        /// </summary>
        public IEnumerable<string> MarkSeen(string roomId, string[] messageIds, string username)
        {
            if (!_store.TryGetValue(roomId, out var queue))
                return Enumerable.Empty<string>();

            // We'll snapshot the queue and operate on the snapshot, but update the actual objects inside it.
            var arr = queue.ToArray();
            var updated = new List<string>();

            // For each message id requested
            var set = messageIds?.ToHashSet() ?? new HashSet<string>();
            if (set.Count == 0) return updated;

            foreach (var m in arr)
            {
                if (set.Contains(m.Id))
                {
                    // Ensure thread-safety per-message using lock on the message instance
                    lock (m)
                    {
                        if (!m.SeenBy.Contains(username))
                        {
                            m.SeenBy.Add(username);
                            updated.Add(m.Id);
                        }
                    }
                }
            }

            return updated;
        }
    }
}

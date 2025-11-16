import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import axios from "axios";

function App() {
  const [log, setLog] = useState([]);
  const [messages, setMessages] = useState([]);
  const [text, setText] = useState("");

  const [typingUsers, setTypingUsers] = useState([]);
  const [onlineUsers, setOnlineUsers] = useState([]);

  let typingTimeout = null;

  // login
  const [username, setUsername] = useState(localStorage.getItem("username") || "");
  const [isLoggedIn, setIsLoggedIn] = useState(!!localStorage.getItem("username"));

  const rooms = ["room1", "room2", "room3"];
  const [selectedRoom, setSelectedRoom] = useState("room1");

  const apiBase = "https://localhost:32771"; // update if needed
  const listRef = useRef(null);

  function addLog(message) {
    setLog(prev => [...prev, message]);
  }

  function sendTyping() {
    if (!window._connection) return;
    window._connection.invoke("Typing", selectedRoom, username, true);

    if (typingTimeout) clearTimeout(typingTimeout);
    typingTimeout = setTimeout(() => {
      window._connection.invoke("Typing", selectedRoom, username, false);
    }, 1000);
  }

  // mark message ids as seen by this username
  async function markMessagesSeen(msgIds) {
    if (!msgIds || msgIds.length === 0) return;
    try {
      await axios.post(`${apiBase}/api/messages/seen`, {
        roomId: selectedRoom,
        messageIds: msgIds,
        username: username
      });
    } catch (err) {
      addLog("MarkSeen error: " + err);
    }
  }

  // mark all currently loaded messages not yet seen by this user
  async function markAllLoadedSeen() {
    const notSeen = messages.filter(m => !m.seenBy || !m.seenBy.includes(username)).map(m => m.id);
    if (notSeen.length > 0) {
      // optimistic UI update
      setMessages(prev => prev.map(m => notSeen.includes(m.id) ? { ...m, seenBy: [...(m.seenBy || []), username] } : m));
      await markMessagesSeen(notSeen);
    }
  }

  async function switchRoom(newRoom) {
    if (!window._connection) return;
    await window._connection.invoke("LeaveRoom", selectedRoom);
    await window._connection.invoke("JoinRoom", newRoom);

    setSelectedRoom(newRoom);
    setMessages([]);
    setTypingUsers([]);

    const res = await axios.get(`${apiBase}/api/messages/${newRoom}`);
    setMessages(res.data || []);
    addLog(`Switched to room: ${newRoom}`);

    // mark seen after a short delay so UI renders
    setTimeout(() => markAllLoadedSeen(), 300);
  }

  useEffect(() => {
    if (!isLoggedIn) return;

    // load history
    axios.get(`${apiBase}/api/messages/${selectedRoom}`)
      .then(res => {
        setMessages(res.data || []);
        addLog("Loaded message history");
        setTimeout(() => markAllLoadedSeen(), 300);
      })
      .catch(err => addLog("History load error: " + err));

    // create connection
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiBase}/hubs/chat`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    window._connection = connection;

    // presence (if your backend uses RegisterUser)
    connection.on("UserListUpdated", users => setOnlineUsers(users || []));

    connection.on("UserTyping", data => {
      if (data.isTyping) {
        setTypingUsers(prev => {
          if (!prev.includes(data.userId)) return [...prev, data.userId];
          return prev;
        });
      } else {
        setTypingUsers(prev => prev.filter(u => u !== data.userId));
      }
    });

    connection.on("NewMessage", msg => {
      setMessages(prev => [...prev, msg]);

      // if user is in the room, mark the new message as seen
      setTimeout(() => {
        // optimistic update
        setMessages(prev => prev.map(m => m.id === msg.id ? { ...m, seenBy: [...(m.seenBy || []), username] } : m));
        markMessagesSeen([msg.id]);
      }, 200);
    });

    connection.on("MessageSeen", payload => {
      // payload: { messageId, username }
      const { messageId, username: seenUser } = payload;
      setMessages(prev => prev.map(m => {
        if (m.id === messageId) {
          const current = m.seenBy || [];
          if (!current.includes(seenUser)) {
            return { ...m, seenBy: [...current, seenUser] };
          }
        }
        return m;
      }));
    });

    connection.start()
      .then(() => connection.invoke("RegisterUser", username))
      .then(() => connection.invoke("JoinRoom", selectedRoom))
      .then(() => addLog("Connected"))
      .catch(err => addLog("SignalR error: " + err));

    return () => connection.stop();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLoggedIn]);

  async function sendMessage() {
    if (!text.trim()) return;

    await axios.post(`${apiBase}/api/messages`, {
      roomId: selectedRoom,
      fromUserId: username,
      text: text
    });

    setText("");
  }

  if (!isLoggedIn) {
    return (
      <div style={{ padding: 40, fontFamily: "Arial" }}>
        <h2>Enter Your Username</h2>

        <input
          style={{ padding: 10, fontSize: 16 }}
          value={username}
          onChange={e => setUsername(e.target.value)}
          placeholder="Your name..."
          onKeyDown={e => {
            if (e.key === "Enter") {
              localStorage.setItem("username", username);
              setIsLoggedIn(true);
            }
          }}
        />

        <button
          onClick={() => {
            localStorage.setItem("username", username);
            setIsLoggedIn(true);
          }}
          style={{ marginLeft: 10, padding: "10px 20px" }}
        >
          Join Chat
        </button>
      </div>
    );
  }

  return (
    <div style={{ padding: 20, fontFamily: "Arial" }}>
      <h2>React Chat</h2>

      {/* Rooms */}
      <div style={{ marginBottom: 20 }}>
        <b>Rooms:</b>
        <div style={{ display: "flex", gap: 10, marginTop: 5 }}>
          {rooms.map(r => (
            <button
              key={r}
              onClick={() => switchRoom(r)}
              style={{
                padding: "5px 10px",
                background: r === selectedRoom ? "#4caf50" : "#eee",
                color: r === selectedRoom ? "white" : "black",
                border: "1px solid #ccc",
                borderRadius: 4
              }}
            >
              {r}
            </button>
          ))}
        </div>
      </div>

      {/* Online users */}
      <div style={{ marginBottom: 10 }}>
        <b>Online Users:</b>
        <div style={{ marginTop: 5 }}>
          {onlineUsers.length === 0 ? "Nobody online" : onlineUsers.map(u => <div key={u} style={{ color: "#2a7" }}>{u}</div>)}
        </div>
      </div>

      {/* Messages */}
      <div
        ref={listRef}
        style={{
          border: "1px solid #ccc",
          padding: 10,
          height: "300px",
          overflowY: "auto",
          marginBottom: 10
        }}
      >
        {messages.map(m => (
          <div key={m.id} style={{ marginBottom: 6 }}>
            <div><b>{m.fromUserId}</b> <small style={{ color: "#888" }}>{new Date(m.createdAt).toLocaleTimeString()}</small></div>
            <div>{m.text}</div>
            <div style={{ fontSize: 12, color: "#666" }}>
              {m.seenBy && m.seenBy.length > 0 ? `Seen by: ${m.seenBy.join(", ")}` : <span>Not seen</span>}
            </div>
          </div>
        ))}
      </div>

      {/* Typing */}
      {typingUsers.length > 0 && (
        <div style={{ fontStyle: "italic", color: "#555", marginBottom: 10 }}>
          {typingUsers.join(", ")} typing...
        </div>
      )}

      {/* Input */}
      <div style={{ display: "flex", gap: "10px" }}>
        <input
          style={{ flex: 1, padding: 10, fontSize: 16 }}
          type="text"
          value={text}
          onChange={e => {
            setText(e.target.value);
            sendTyping();
          }}
          onKeyDown={e => {
            if (e.key === "Enter") sendMessage();
          }}
          placeholder="Type a message..."
        />

        <button onClick={sendMessage} style={{ padding: "10px 20px" }}>Send</button>
      </div>

      <h3>Logs:</h3>
      <pre>{log.join("\n")}</pre>
    </div>
  );
}

export default App;

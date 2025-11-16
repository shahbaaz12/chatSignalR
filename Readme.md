README.md (complete file)
# Chat Solution

A simple real-time chat application built with:

- ASP.NET Core (backend)
- SignalR (real-time communication)
- React + Vite (frontend)
- In-memory message storage
- Typing indicators
- Online presence
- Per-user read receipts
- Multi-room support

This solution contains both backend and frontend:



SimpleChatBe/ - Backend (.NET 8 + SignalR)
chat-fe/ - Frontend (React + Vite)


Both run locally and communicate over HTTPS using WebSockets.

---

## Prerequisites

Install the following:

- .NET SDK 8
- Node.js (v18 or newer)
- npm  
 ---

# 1. Backend Setup (SimpleChatBe)

Navigate to:



cd SimpleChatBe


Restore dependencies:



dotnet restore


Run the backend:



dotnet run


The console will output something like:



Now listening on: https://localhost:32769

Now listening on: http://localhost:5148


Note the **HTTPS port** (32769 in this example).
The React frontend will use this port to connect.

SignalR Hub:



/hubs/chat


API endpoints:

- GET /api/messages/{roomId}
- POST /api/messages
- POST /api/messages/seen

---

# 2. Frontend Setup (chat-fe)

Open a new terminal:



cd chat-fe


Install dependencies:



npm install


### Important Step Before Running React

Open:



chat-fe/src/App.jsx


Find:

```js
const apiBase = "https://localhost:32769";


Replace 32769 with the actual HTTPS port shown in the backend output.

Example:

If backend shows:

https://localhost:44347


Change to:

const apiBase = "https://localhost:44347";


Save the file.

Run the frontend:

npm run dev


Vite will display:

Local: http://localhost:5173


Open that URL in the browser.

3. Running Both Together

Start the backend

dotnet run


Confirm the backend HTTPS port

Update the port in App.jsx

Start the frontend

npm run dev


Both must stay running.

4. Testing the Chat Application

To test real-time features:

Steps

Open http://localhost:5173 in Tab A

Open the same URL in Tab B

Enter different usernames

Both users will appear as online

Join a room (default is room1)

Type in Tab A

Tab B shows “typing...”

Send a message from Tab A

Tab B receives it instantly

Message becomes “Seen by: <username>” when read

Switch rooms

Messages remain isolated by room

Read receipts update accordingly

Real-time updates are instant and require no refresh.
 
6. Notes

All data is stored in memory. Restarting the backend clears history.

SignalR automatically uses WebSockets if possible.

Real-time features (typing, read receipts, online status) are driven by the hub.
# Codebase Map — WebMessenger

> Generated: 2026-04-28. Read-only analysis.

---

## System Overview

**WebMessenger** is a real-time web messenger application. It combines a REST API for CRUD operations with a SignalR hub for real-time push events.

**High-level flow:**

```
1. Auth      → POST /api/auth/login  → JWT issued as HttpOnly cookie "auth-token"
2. Chat list → GET  /api/chats       → paginated list of chat previews
3. Messages  → GET  /api/chats/{id}/messages  → paginated message history
4. Send msg  → POST /api/chats/direct/{userId}/messages
                 └─► Server calls ChatEvents → SignalR pushes "MessageCreated"
                     to chat group, DM group, and both user groups
5. Realtime  → Client subscribes to SignalR hub (/hubs/chat)
                 ├─ JoinChat(chatId)   → receives MessageCreated, Typing, ReadReceipt
                 └─ JoinDirect(peerId) → receives MessageCreated for DM
```

---

## Repository Structure

```
WebMessenger/
├── Client/                     Next.js 15 App Router (TypeScript + Tailwind)
│   └── src/
│       ├── app/                Routes + Next.js Route Handler proxies
│       ├── components/         UI components
│       ├── hooks/              Data-fetching & realtime hooks
│       ├── lib/                API clients, hub singleton, auth utils
│       ├── middleware/         Next.js route protection
│       ├── providers/          AuthProvider (client-side auth gate)
│       ├── styles/             globals.scss
│       └── types/              Shared TypeScript types
├── Server/
│   ├── WebMessenger.Api/       ASP.NET Core 8 Web API + SignalR
│   │   ├── Controllers/        REST endpoints
│   │   ├── Hubs/               SignalR ChatHub + ChatEvents dispatcher
│   │   ├── Infrastructure/     ICurrentUser (claims extraction)
│   │   ├── Services/           Business logic layer
│   │   └── Program.cs          Composition root / DI
│   ├── WebMessenger.Contracts/ Shared DTOs, SignalR event name constants, group helpers
│   └── WebMessenger.DAL/       EF Core 8 + MySQL, entities, migrations, Repository/UoW
└── docs/
```

---

## Client (Next.js)

### Entry Points

| File | Role |
|---|---|
| `Client/src/app/layout.tsx` | Root layout — wraps all pages in `AuthProvider` |
| `Client/src/app/page.tsx` | Main messenger page (chat list + message area; all top-level state lives here) |
| `Client/src/middleware/auth.ts` | Next.js middleware — redirects unauthenticated requests to `/auth/login`; passes `/api/*` through |
| `Client/src/middleware/config.ts` | Matcher config for the middleware |
| `Client/next.config.ts` | Next.js config |

### Routing (App Router)

```
src/app/
├── layout.tsx              Root layout (AuthProvider)
├── page.tsx                / → MessengerPage (main UI)
├── auth/
│   ├── layout.tsx          Auth shell layout
│   ├── login/              /auth/login
│   └── register/           /auth/register
└── api/                    Next.js Route Handlers (proxy to .NET API)
    ├── auth/               Token bridge — /api/auth/token used by hub accessTokenFactory
    ├── chats/
    ├── contacts/
    └── users/
```

> Client-side `fetch` calls hit `/api/*` Next.js Route Handlers, which proxy to the .NET backend. The SignalR hub uses `accessTokenFactory` (calling `/api/auth/token`) to retrieve the JWT for WebSocket authentication.

### State Management & Data Fetching

No React Query, Redux, or SWR. All data fetching uses **plain `fetch` + `useState`/`useEffect`** in `page.tsx` and dedicated custom hooks:

| Hook | File | Purpose |
|---|---|---|
| `useChatList` | `src/hooks/useChatList.ts` | Fetches paginated chat list |
| `useMessages` | `src/hooks/useMessages.ts` | Fetches paginated messages for a chat |
| `useChatRealtime` | `src/hooks/useChatRealtime.ts` | Per-chat SignalR subscription (JoinChat / JoinDirect, receives MessageCreated / Typing / ReadReceipt) |
| `useChatInboxRealtime` | `src/hooks/useChatInboxRealtime.ts` | Inbox-level realtime (updates chat list on new messages) |
| `useMarkRead` | `src/hooks/useMarkRead.ts` | POST to mark messages read |
| `useDebounce` | `src/hooks/useDebounce.ts` | Generic debounce utility |

### Realtime Layer (SignalR)

| File | Role |
|---|---|
| `src/lib/hubs/chatHubClient.ts` | **Singleton** `HubConnection` factory (`getChatConnection()`). Connects to `/hubs/chat`, uses `accessTokenFactory` fetching JWT from `/api/auth/token`. Auto-reconnect with exponential back-off (capped at 30 s). Transport fallback: WebSockets → SSE → LongPolling. |

Connection lifecycle is managed inside hooks: `useChatRealtime` calls `conn.invoke('JoinChat', chatId)` or `conn.invoke('JoinDirect', peerId)` after confirming the connection state is `Connected`.

### UI Modules

```
src/components/features/messenger/
├── layout/
│   ├── MessengerSidebar.tsx    Left panel: chat list + search + avatar
│   └── MessengerMainArea.tsx   Right panel: chat header + message list + composer
├── chat/
│   ├── ChatHeader.tsx          Chat title bar, peer info
│   ├── MessageList.tsx         Scrollable message history
│   ├── MessageBubble.tsx       Individual message rendering
│   └── UserProfilePanel.tsx    Side panel showing a user's profile
├── ChatList/                   Chat list item components
├── ContactList/                Contact list + add-contact flow
├── SearchResults/              User search results UI
├── UserSettings/               Profile edit + avatar upload
└── EmptyState/                 Empty chat placeholder

src/components/features/auth/   Login / register forms
```

---

## Server (.NET 8 Web API + SignalR)

### Entry Point & DI Composition

`Server/WebMessenger.Api/Program.cs`

- **DB**: EF Core + Pomelo MySQL provider (`DefaultConnection` from config), retry-on-failure enabled
- **Auth**: JWT Bearer. For SignalR connections, token is read from `?access_token` query param first, then the `auth-token` cookie
- **CORS**: `http(s)://localhost:3000` with `AllowCredentials`
- **SignalR**: `EnableDetailedErrors = true`
- **Services registered (Scoped)**:

| Interface | Implementation |
|---|---|
| `IUnitOfWork` | `UnitOfWork` |
| `IUserService` | `UserService` |
| `IAuthService` | `AuthService` |
| `IContactsService` | `ContactsService` |
| `IAvatarService` | `AvatarService` |
| `IChatService` | `ChatService` |
| `IChatEvents` | `ChatEvents` |
| `ICurrentUser` | `CurrentUser` |
| `IFileStorage` | `DropboxFileStorage` |

### REST Endpoints

| Controller | Base Route | Key Endpoints |
|---|---|---|
| `AuthController` | `api/auth` | `POST /register`, `POST /login` (sets `auth-token` cookie + returns token body), `GET /verify` |
| `ChatController` | `api/chats` | `GET /` (paged), `GET /{id}/header`, `GET /direct/{userId}/header`, `GET /{id}/messages` (paged), `POST /direct/{userId}/messages`, `POST /{id}/read`, `GET /{id}/read-state` |
| `ContactController` | `api/contacts` | `GET /?query` (search), `POST /add` |
| `UserController` | `api/users` | `GET /?query&limit` (search), `GET /profile`, `GET /profile/{id}`, `PUT /profile`, `POST /avatar` |

All routes except `auth/register` and `auth/login` require `[Authorize]`.

### SignalR Hub

`Server/WebMessenger.Api/Hubs/ChatHub.cs` — mapped to `/hubs/chat`

**`OnConnectedAsync`**: adds the connection to `user:{userId}` group immediately.

**Client → Server (invokable methods):**

| Method | Description |
|---|---|
| `JoinChat(chatId)` | Joins `chat:{chatId}` group; validates membership via `ChatService.GetMessagesAsync` |
| `JoinDirect(otherUserId)` | Joins `dm:{min}:{max}` group |
| `LeaveChat(chatId)` | Leaves `chat:{chatId}` group |
| `LeaveDirect(otherUserId)` | Leaves `dm:{min}:{max}` group |
| `Typing(chatId, isTyping)` | Broadcasts `Typing` event to the chat group |
| `MarkRead(chatId, upToUtc)` | Broadcasts `ReadReceipt` to the chat group (hub-only — no DB write) |

**Server → Client (push events) via `ChatEvents`:**

`Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs` — injected as `IChatEvents` into controllers.

| Event | Trigger | Target Groups |
|---|---|---|
| `MessageCreated` | After `POST .../messages` REST call | `chat:{id}` + `user:{senderId}` + `user:{peerId}` (or `dm:a:b` for first message) |
| `ReadReceipt` | After `POST /{id}/read` REST call | `chat:{id}` |
| `Typing` | Hub `Typing` method | `chat:{id}` |

**Group naming** (`WebMessenger.Contracts/Helpers/SignalRGroups.cs`):

| Group | Key |
|---|---|
| Per-user inbox | `user:{userId}` |
| Active chat | `chat:{chatId}` |
| DM pair | `dm:{minId}:{maxId}` (IDs sorted ascending) |

### Domain / Application Layer

Services in `WebMessenger.Api/Services/`:

| Service | Responsibility |
|---|---|
| `AuthService` | Password validation (hash compare), JWT generation |
| `UserService` | Registration, profile CRUD, user search |
| `ChatService` | Chat list, message history, send message (creates `Chat`+`ChatMember` rows if first DM), mark-read, read state |
| `ContactsService` | Contact list retrieval, add contact |
| `AvatarService` | Upload avatar via `IFileStorage`, update `User.AvatarUrl` in DB |

### Persistence

`Server/WebMessenger.DAL/Data/ApplicationDbContext.cs` — EF Core, MySQL

**Entities:**

| Entity | Key Fields | Notes |
|---|---|---|
| `User` | `Id` (Guid), `Username` (unique), `PasswordHash`, `Email`, `AvatarUrl`, `IsOnline`, `LastSeenAt` | |
| `Chat` | `Id`, `Name`, `IsGroup`, `CreatedAt` | `Name` nullable for DMs |
| `Message` | `Id`, `Content` (max 5000), `SenderId`, `ChatId`, `SentAt`, `EditedAt` | Composite index `(ChatId, SentAt)` for cursor pagination |
| `ChatMember` | `UserId`, `ChatId` (unique pair), `JoinedAt`, `LastReadAt` | `LastReadAt` drives unread counts |
| `Contact` | Links two `User` rows | |

Pattern: generic `Repository<T>` + `UnitOfWork` in `WebMessenger.DAL/`. EF migrations in `WebMessenger.DAL/Migrations/`.

### AuthN / AuthZ

- **Scheme**: JWT Bearer; symmetric key from `Jwt:Key` in config
- **Claims**: `ClaimTypes.NameIdentifier` = `User.Id`, `ClaimTypes.Name` = username
- **`ICurrentUser`** (`Infrastructure/CurrentUser.cs`) — thin wrapper that reads `NameIdentifier` claim; injected into all controllers and services as `_currentUser.Id`
- **SignalR token flow**: query-string `access_token` → `auth-token` cookie (evaluated in `JwtBearerEvents.OnMessageReceived`)
- **Avatar upload boundary validation**: content-type must start with `image/`; max 5 MB enforced in controller

---

## Contracts Between Client and Server

### REST Endpoints (brief list)

```
POST   /api/auth/register
POST   /api/auth/login
GET    /api/auth/verify

GET    /api/chats?limit&before
GET    /api/chats/{id}/header
GET    /api/chats/direct/{userId}/header
GET    /api/chats/{id}/messages?limit&before
POST   /api/chats/direct/{userId}/messages      body: { content }
POST   /api/chats/{id}/read                     body: { at? }
GET    /api/chats/{id}/read-state

GET    /api/contacts?query
POST   /api/contacts/add                        body: { contactUserId }

GET    /api/users?query&limit
GET    /api/users/profile
GET    /api/users/profile/{id}
PUT    /api/users/profile
POST   /api/users/avatar                        multipart/form-data
```

### SignalR Events

```
Client → Server  (invoke)
  JoinChat(chatId: Guid)
  JoinDirect(otherUserId: Guid)
  LeaveChat(chatId: Guid)
  LeaveDirect(otherUserId: Guid)
  Typing(chatId: Guid, isTyping: bool)
  MarkRead(chatId: Guid, upToUtc: DateTime)

Server → Client  (on)
  "MessageCreated"  { chatId, peerUserId?, message: ChatMessageDto }
  "ReadReceipt"     { chatId, userId, lastReadAt }
  "Typing"          { chatId, userId, isTyping }
```

Event name string constants are defined in `WebMessenger.Contracts/Helpers/Events.cs`. The client mirrors them as string literals inside the hooks.

### Shared DTOs

All DTOs live in `WebMessenger.Contracts/Models/` (server project). The client duplicates the shapes in `Client/src/types/` (`chat.ts`, `contact.ts`, `user.ts`, `index.ts`).

> **No code-gen or versioning scheme** is in place. DTO changes must be applied manually to both the server contract project and the client type files.

Key DTOs:

| DTO | Purpose |
|---|---|
| `ChatListItemDto` | Chat list entry (id, title, isGroup, lastMessage preview, unreadCount, peerUserId/AvatarUrl) |
| `ChatMessageDto` | Full message (id, chatId, senderId, content, sentAt, editedAt) |
| `ChatMessagePreviewDto` | Snippet used inside `ChatListItemDto.lastMessage` |
| `DirectChatHeaderDto` | Header info for a DM (peer username, avatar, online state) |
| `PagedResult<T>` | `{ items, hasMore, nextBefore }` — cursor pagination via `before` (DateTime) |
| `SendMessageRequest` / `SendMessageResponse` | Send message payload / confirmation |
| `MarkReadRequest` | `{ at? }` — optional timestamp |
| `ReadStateDto` | `{ lastReadAt, unreadCount }` |
| `UserProfileDto` / `UserSearchResultDto` | Profile and search result shapes |
| `LoginDto` / `RegisterDto` / `UpdateProfileDto` | Auth and profile update bodies |

---

## Cross-Cutting Concerns

### Logging
`ILogger<T>` injected throughout. Hub logs: `Information` on connect/disconnect, `Debug` on group join/leave, `Trace` on every event dispatch. No structured log sink or APM configured — default ASP.NET Core console provider.

### Error Handling
- `app.UseExceptionHandler()` + `AddProblemDetails` (appends `traceId` to all ProblemDetails responses)
- Hub auth failures throw `HubException("Unauthorized")`
- Client uses `try/catch` silently around all fetch and hub operations

### Telemetry
None configured.

### Secrets / Config

| Secret | Location |
|---|---|
| `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` | `appsettings.Development.json` (dev) / env vars (prod) |
| `ConnectionStrings:DefaultConnection` | `appsettings.Development.json` / env vars |
| Dropbox credentials | `appsettings.Development.json` / env vars |

`appsettings.Development.json` should be in `.gitignore`.

### File Storage
`IFileStorage` abstraction in `Services/FileStorage/`. Current implementation: `DropboxFileStorage`. To swap providers, re-register `IFileStorage` in `Program.cs`.

### Testing
No test projects found in the solution. No `*.Tests` project, test runner configuration, or CI test step present.

---

## "Where to Implement X"

### Add a new message type (e.g. image, file attachment)

1. **`Message` entity** — `WebMessenger.DAL/Entities/Message.cs`: add `MessageType` enum + optional metadata columns; add EF migration
2. **`ChatMessageDto`** — `WebMessenger.Contracts/Models/ChatMessageDto.cs`: add the new field to the contract
3. **`ChatService`** — update `SendMessageToUserAsync` to persist the new type
4. **`SendMessageRequest`** — `WebMessenger.Contracts/Models/SendMessageRequest.cs`: extend the request body
5. **Client types** — `Client/src/types/chat.ts`: mirror the DTO change
6. **`MessageBubble.tsx`** — `Client/src/components/features/messenger/chat/MessageBubble.tsx`: render the new type

---

### Add chat reactions

1. New `Reaction` entity (DAL) + EF migration
2. New REST endpoint `POST /api/chats/{id}/messages/{msgId}/react` in `ChatController`
3. New `IChatEvents.ReactionAddedAsync(...)` method and implementation in `ChatEvents.cs`
4. New event name constant in `WebMessenger.Contracts/Helpers/Events.cs` (e.g. `"ReactionAdded"`)
5. Client: subscribe in `useChatRealtime.ts` (`conn.on("ReactionAdded", ...)`) and update message state
6. Render in `MessageBubble.tsx`

---

### Add typing indicator

> The **server side is already fully implemented** — `ChatHub.Typing()` broadcasts a `Typing` event to the relevant chat group.

Client work remaining:

1. **Send**: in the message composer component, call `conn.invoke('Typing', chatId, true)` on keystroke and `conn.invoke('Typing', chatId, false)` on blur or message send (debounce recommended — use `useDebounce.ts`)
2. **Receive**: `useChatRealtime` already accepts an `onTyping` callback — wire it from `MessengerMainArea.tsx` and display a "… is typing" indicator in `ChatHeader.tsx` or above `MessageList.tsx`

# System overview

WebMessenger is a full-stack messenger application.

High-level runtime flow:
1. Auth
Client calls Next.js route handlers under Client/src/app/api/auth, server validates in Server/WebMessenger.Api/Controllers/AuthController.cs, JWT is stored in auth-token cookie.
2. Chat list
Client loads list via Client/src/app/api/chats/route.ts and Server/WebMessenger.Api/Controllers/ChatController.cs (GET api/chats).
3. Message send and receive
Client posts to Client/src/app/api/chats/direct/[userId]/messages/route.ts, server writes through Server/WebMessenger.Api/Services/ChatService.cs, returns SendMessageResponse.
4. Realtime updates
Client SignalR connection from Client/src/lib/hubs/chatHubClient.ts subscribes through hooks in Client/src/hooks, server broadcasts from Server/WebMessenger.Api/Hubs/ChatHub.cs and Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs.

Client and Server communication model:
- REST: Client/src/app/api/** proxies to ASP.NET API controllers.
- SignalR: Client hub client connects to /hubs/chat, joins chat or dm groups, receives MessageCreated, Typing, ReadReceipt.

# Repository structure

Key tree:
- Client/
  - next.config.ts
  - src/
    - app/
      - layout.tsx
      - page.tsx
      - auth/
      - api/
    - middleware/
    - components/
      - features/
    - hooks/
    - lib/
    - providers/
    - types/
    - styles/
- Server/
  - WebMessenger.sln
  - WebMessenger.Api/
    - Program.cs
    - Controllers/
    - Hubs/
    - Infrastructure/
    - Services/
    - appsettings.json
  - WebMessenger.Contracts/
    - Helpers/
    - Models/
  - WebMessenger.DAL/
    - Data/
    - Entities/
    - Interfaces/
    - Migrations/
    - Repository.cs
    - UnitOfWork.cs
- docs/
  - CODEBASE_MAP.md
  - plans/
  - specs/
- .github/
  - prompts/

# Client (Next.js)

Entry points:
- Client/next.config.ts
Loads environment from .env.<NODE_ENV>, configures Next images and strict mode.
- Client/src/app/layout.tsx
Root layout with AuthProvider and UserProvider.
- Client/src/app/page.tsx
Main messenger screen with sidebar, chat area, settings panel, profile panel.
- Client/src/middleware/config.ts
Exports middleware matcher for all routes except static assets.
- Client/src/middleware/auth.ts
Protects non-public pages, redirects unauthenticated users to /auth/login.

Routing structure:
- App Router is used.
- Main routes live under Client/src/app.
- Auth pages: Client/src/app/auth/login/page.tsx and Client/src/app/auth/register/page.tsx.
- API route handlers: Client/src/app/api/**.

State management and data fetching:
- No Redux/RTK, React Query, or SWR found.
- Uses React state and custom hooks.
- Main hooks:
  - Client/src/hooks/useChatListManagement.ts
  - Client/src/hooks/useChatList.ts
  - Client/src/hooks/useMessages.ts
  - Client/src/hooks/useDirectChatResolution.ts
  - Client/src/hooks/useMarkRead.ts
  - Client/src/hooks/useReadStateTracking.ts

Realtime layer (SignalR client):
- Client/src/lib/hubs/chatHubClient.ts
Creates singleton HubConnection and accessTokenFactory using Client/src/app/api/auth/token/route.ts.
- Client/src/lib/hubs/chatHubOperations.ts
Contains join and leave operations (JoinChat, JoinDirect, LeaveChat, LeaveDirect) and connection guard.
- Client/src/hooks/useChatRealtime.ts
Subscribes to MessageCreated, Typing, ReadReceipt and manages target join lifecycle.
- Client/src/hooks/useChatInboxRealtime.ts exists and also contains a useChatRealtime export; verify intended usage before edits.

UI modules:
- Chat list and sidebar:
  - Client/src/components/features/messenger/layout/MessengerSidebar.tsx
  - Client/src/components/features/messenger/ChatList/
- Chat view and composer:
  - Client/src/components/features/messenger/layout/MessengerMainArea.tsx
  - Client/src/components/features/messenger/chat/MessageList.tsx
  - Client/src/components/features/messenger/chat/MessageComposer.tsx
  - Client/src/components/features/messenger/chat/MessageBubble.tsx
  - Client/src/components/features/messenger/chat/ChatHeader.tsx
- Settings and profile:
  - Client/src/components/features/messenger/UserSettings/
  - Client/src/components/features/messenger/chat/UserProfilePanel.tsx
- Search and contacts:
  - Client/src/components/features/messenger/SearchResults/
  - Client/src/components/features/messenger/ContactList/

# Server (.NET 8 Web API + SignalR)

Entry points and DI composition:
- Server/WebMessenger.Api/Program.cs
Configures controllers, swagger, ProblemDetails, EF Core MySQL, CORS policy Front, JWT bearer auth, SignalR hub, DI for services and infra abstractions.

REST endpoints (key areas):
- AuthController at Server/WebMessenger.Api/Controllers/AuthController.cs
  - POST api/auth/register
  - POST api/auth/login
  - GET api/auth/verify
- ChatController at Server/WebMessenger.Api/Controllers/ChatController.cs
  - GET api/chats
  - GET api/chats/{chatId}/header
  - GET api/chats/direct/{userId}/header
  - GET api/chats/{chatId}/messages
  - POST api/chats/direct/{userId}/messages
  - POST api/chats/{chatId}/read
  - GET api/chats/{chatId}/read-state
- ContactController at Server/WebMessenger.Api/Controllers/ContactController.cs
  - GET api/contacts
  - POST api/contacts/add
- UserController at Server/WebMessenger.Api/Controllers/UserController.cs
  - GET api/users
  - GET api/users/profile
  - GET api/users/profile/{id}
  - PUT api/users/profile
  - POST api/users/avatar

SignalR:
- Hub class: Server/WebMessenger.Api/Hubs/ChatHub.cs
- Hub route: /hubs/chat (mapped in Program.cs)
- Hub methods:
  - JoinChat(chatId)
  - JoinDirect(otherUserId)
  - LeaveChat(chatId)
  - LeaveDirect(otherUserId)
  - Typing(chatId, isTyping)
  - MarkRead(chatId, upToUtc)
- Group naming strategy:
  - user:{userId}
  - chat:{chatId}
  - dm:{minId}:{maxId}
  Implemented in Server/WebMessenger.Contracts/Helpers/SignalRGroups.cs.
- Event names:
  - MessageCreated
  - Typing
  - ReadReceipt
  Defined in Server/WebMessenger.Contracts/Helpers/Events.cs.
- Server event dispatcher:
  - Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs

Domain and application layer:
- Server/WebMessenger.Api/Services/AuthService.cs
JWT generation and credential validation.
- Server/WebMessenger.Api/Services/ChatService.cs
Chat list, headers, messages, send message, read-state updates, direct-peer discovery.
- Server/WebMessenger.Api/Services/UserService.cs
Registration, profile read and update, user search.
- Server/WebMessenger.Api/Services/ContactsService.cs
Contacts query and add-contact flow.
- Server/WebMessenger.Api/Services/AvatarService.cs
Avatar upload and storage update.

Persistence:
- DbContext: Server/WebMessenger.DAL/Data/ApplicationDbContext.cs
- Entities:
  - Server/WebMessenger.DAL/Entities/User.cs
  - Server/WebMessenger.DAL/Entities/Chat.cs
  - Server/WebMessenger.DAL/Entities/Message.cs
  - Server/WebMessenger.DAL/Entities/ChatMember.cs
  - Server/WebMessenger.DAL/Entities/Contact.cs
- Data access pattern:
  - Generic repository: Server/WebMessenger.DAL/Repository.cs
  - Unit of work: Server/WebMessenger.DAL/UnitOfWork.cs
- Migrations:
  - Server/WebMessenger.DAL/Migrations/

AuthN and AuthZ:
- JWT bearer is configured in Server/WebMessenger.Api/Program.cs.
- Claims extraction wrapper:
  - Server/WebMessenger.Api/Infrastructure/CurrentUser.cs
- Controllers other than public auth routes are protected with Authorize.
- SignalR token extraction supports:
  - access_token query parameter
  - auth-token cookie

# Contracts between Client and Server

REST endpoints list (brief):
- Auth
  - POST /api/auth/register
  - POST /api/auth/login
  - GET /api/auth/verify
- Chats
  - GET /api/chats
  - GET /api/chats/{chatId}/header
  - GET /api/chats/direct/{userId}/header
  - GET /api/chats/{chatId}/messages
  - POST /api/chats/direct/{userId}/messages
  - POST /api/chats/{chatId}/read
  - GET /api/chats/{chatId}/read-state
- Contacts
  - GET /api/contacts
  - POST /api/contacts/add
- Users
  - GET /api/users
  - GET /api/users/profile
  - GET /api/users/profile/{id}
  - PUT /api/users/profile
  - POST /api/users/avatar

SignalR events and messages list (brief):
- Server to client:
  - MessageCreated
  - Typing
  - ReadReceipt
- Client to server:
  - JoinChat
  - JoinDirect
  - LeaveChat
  - LeaveDirect
  - Typing
  - MarkRead

Shared DTOs location and versioning approach:
- DTO and contract source is Server/WebMessenger.Contracts/Models/.
- Event and group constants are in Server/WebMessenger.Contracts/Helpers/.
- Explicit API versioning was not found in routes; current approach is path-stable endpoints without version segment.

# Cross-cutting concerns

Logging, error handling, telemetry:
- Logging configured via appsettings in Server/WebMessenger.Api/appsettings.json and appsettings.Development.json.
- Central exception handling enabled with UseExceptionHandler in Program.cs.
- ProblemDetails is enabled and includes traceId extension.
- Telemetry integration (for example OpenTelemetry/Application Insights) was not found.

Security boundaries, secrets, config:
- Backend secrets and connection strings are currently present in Server/WebMessenger.Api/appsettings.json.
- Client proxy reads PUBLIC_API_URL and forwards auth via Bearer from cookie in Client/src/app/api/utils/proxy.ts.
- Auth cookie name used by both sides: auth-token.
- CORS allows localhost:3000 with credentials in Program.cs.

Testing (unit/integration/e2e):
- No test projects or test files were found in current repository scan.

# Where to implement X

Add new message type:
- Contracts payload:
  - Server/WebMessenger.Contracts/Models/ChatMessageDto.cs
  - Server/WebMessenger.Contracts/Models/SendMessageRequest.cs
  - Server/WebMessenger.Contracts/Models/SendMessageResponse.cs
- Persistence:
  - Server/WebMessenger.DAL/Entities/Message.cs
  - Server/WebMessenger.DAL/Data/ApplicationDbContext.cs
  - Server/WebMessenger.DAL/Migrations/
- Server flow:
  - Server/WebMessenger.Api/Services/ChatService.cs
  - Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs
- Client rendering:
  - Client/src/components/features/messenger/chat/MessageBubble.tsx
  - Client/src/components/features/messenger/chat/MessageList.tsx
  - Client/src/types/chat.ts

Add chat reactions:
- Server contract and storage:
  - Server/WebMessenger.Contracts/Models/
  - Server/WebMessenger.DAL/Entities/
  - Server/WebMessenger.DAL/Data/ApplicationDbContext.cs
  - Server/WebMessenger.Api/Services/ChatService.cs
- Realtime push:
  - Server/WebMessenger.Contracts/Helpers/Events.cs
  - Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs
  - Server/WebMessenger.Api/Hubs/ChatHub.cs
- Client consumption:
  - Client/src/hooks/useChatRealtime.ts
  - Client/src/hooks/useChatListManagement.ts
  - Client/src/components/features/messenger/chat/MessageBubble.tsx

Add typing indicator:
- Existing server endpoint already exists:
  - Server/WebMessenger.Api/Hubs/ChatHub.cs method Typing
- Existing client subscription points:
  - Client/src/hooks/useChatRealtime.ts
- UI display location:
  - Client/src/components/features/messenger/chat/ChatHeader.tsx
  - Client/src/components/features/messenger/chat/MessageList.tsx

Add read receipts:
- Existing server read flow:
  - Server/WebMessenger.Api/Controllers/ChatController.cs (POST read, GET read-state)
  - Server/WebMessenger.Api/Services/ChatService.cs
  - Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs
- Existing client integration:
  - Client/src/hooks/useMarkRead.ts
  - Client/src/hooks/useReadStateTracking.ts
  - Client/src/hooks/useChatRealtime.ts
  - Client/src/hooks/useChatListManagement.ts

# Glossary

- Direct chat
A non-group chat between two users.
- DM group
SignalR group with key dm:{minId}:{maxId} for pair communication.
- Chat group
SignalR group with key chat:{chatId} for chat-specific broadcasts.
- User group
SignalR group with key user:{userId} used for inbox-level fan-out.
- Read state
Per-user state in a chat, based on ChatMember.LastReadAt.
- Route handler
Next.js server route under Client/src/app/api that proxies to ASP.NET API.

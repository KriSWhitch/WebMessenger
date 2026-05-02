# System overview

WebMessenger is a full-stack real-time messenger application. The user-facing flow is:

1. Auth: the client signs in through Next.js route handlers under `Client/src/app/api/auth/*`, the ASP.NET API validates credentials in `Server/WebMessenger.Api/Controllers/AuthController.cs`, and the browser receives an `auth-token` cookie.
2. Chat list: the main screen in `Client/src/app/page.tsx` loads chats from `Client/src/app/api/chats/route.ts`, which proxies to `Server/WebMessenger.Api/Controllers/ChatController.cs`.
3. Message send/receive: the composer in `Client/src/components/features/messenger/chat/MessageComposer.tsx` posts to `Client/src/app/api/chats/direct/[userId]/messages/route.ts`, which proxies to `POST /api/chats/direct/{userId}/messages` on the API.
4. Realtime updates: the SignalR client from `Client/src/lib/hubs/chatHubClient.ts` connects to `/hubs/chat`, joins a chat or direct-message group, and receives `MessageCreated`, `Typing`, and `ReadReceipt` events published by `Server/WebMessenger.Api/Hubs/ChatHub.cs` and `Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs`.

Client and Server communicate in two ways:

- REST: Next.js route handlers under `Client/src/app/api/**` act as a thin proxy/facade over the ASP.NET controllers.
- SignalR: the browser connects directly from the client app to `Server/WebMessenger.Api/Hubs/ChatHub.cs` at `/hubs/chat` using a JWT fetched from `Client/src/app/api/auth/token/route.ts`.

# Repository structure

Key directories and top-level files:

- `Client/`
  - `next.config.ts`: Next.js configuration.
  - `src/app/`: App Router pages and route handlers.
  - `src/components/`: UI modules.
  - `src/hooks/`: client-side data loading and realtime orchestration.
  - `src/lib/`: auth, hub, and utility helpers.
  - `src/middleware/`: route protection.
  - `src/providers/`: auth and current-user context.
  - `src/types/`: client-side DTO/view-model typings.
  - `src/styles/`: global styling.
- `Server/`
  - `WebMessenger.sln`: .NET solution entry point.
  - `WebMessenger.Api/`: ASP.NET Core Web API + SignalR host.
  - `WebMessenger.DAL/`: EF Core DbContext, entities, migrations, repository/unit-of-work layer.
  - `WebMessenger.Contracts/`: shared DTOs and SignalR constants.
  - `WebMessenger.Api.Tests/`: server unit tests.
  - `WebMessenger.Contracts.Tests/`: contract/validation tests.
- `docker/`
  - `mysql/init/00-import-optional-dump.sh`: optional first-run dump import.
  - `mysql/dumps/`: SQL dumps mounted into MySQL init directory.
- `docs/`
  - `CODEBASE_MAP.md`: this repository map.
  - `guides/`: operational and testing guides.
  - `plans/`: work plans.
  - `specs/`: feature and architecture specs.
- `scripts/`
  - currently empty.
- Root Docker files
  - `docker-compose.yml`
  - `Dockerfile.api`
  - `Dockerfile.client`
- Root env templates
  - `.env.docker.example`

Condensed tree:

```text
WebMessenger/
|- Client/
|  |- next.config.ts
|  |- src/app/
|  |- src/components/
|  |- src/hooks/
|  |- src/lib/
|  |- src/middleware/
|  |- src/providers/
|  \- src/types/
|- Server/
|  |- WebMessenger.Api/
|  |- WebMessenger.DAL/
|  |- WebMessenger.Contracts/
|  |- WebMessenger.Api.Tests/
|  \- WebMessenger.Contracts.Tests/
|- docker/
|- docs/
|- scripts/
|- docker-compose.yml
|- Dockerfile.api
|- Dockerfile.client
\- README.md
```

# Client (Next.js)

Entry points:

- `Client/next.config.ts`
  - Next.js config; the app uses the App Router, not Pages Router.
- `Client/src/app/layout.tsx`
  - Root layout; wires `AuthProvider` and `UserProvider`, and loads global styles.
- `Client/src/app/page.tsx`
  - Main messenger shell; composes sidebar, main chat area, settings panel, and profile panel.
- `Client/src/middleware/config.ts`
  - Exports the project middleware and matcher for all non-static routes.
- `Client/src/middleware/auth.ts`
  - Redirects unauthenticated page requests to `/auth/login`, but skips `/api/*` routes.

Routing structure:

- App Router is used throughout `Client/src/app/`.
- Main screen:
  - `Client/src/app/page.tsx`
- Auth pages:
  - `Client/src/app/auth/login/page.tsx`
  - `Client/src/app/auth/register/page.tsx`
- Internal API facade:
  - `Client/src/app/api/auth/**`
  - `Client/src/app/api/chats/**`
  - `Client/src/app/api/contacts/**`
  - `Client/src/app/api/users/**`
- Common proxy helper:
  - `Client/src/app/api/utils/proxy.ts`
  - Reads the `auth-token` cookie, forwards Bearer auth to the ASP.NET API, and normalizes upstream responses.

State management and data fetching:

- No Redux, Zustand, React Query, or SWR was found.
- State is managed with React state, context providers, and custom hooks.
- Auth/current user:
  - `Client/src/providers/AuthProvider.tsx`
  - `Client/src/providers/UserProvider.tsx`
  - `Client/src/hooks/useCurrentUser.ts`
- Chat list and pagination:
  - `Client/src/hooks/useChatListManagement.ts`
  - `Client/src/hooks/useChatList.ts`
- Chat selection and DM resolution:
  - `Client/src/hooks/useDirectChatResolution.ts`
- Message fetching and read-state handling:
  - `Client/src/hooks/useMessages.ts`
  - `Client/src/hooks/useMarkRead.ts`
  - `Client/src/hooks/useReadStateTracking.ts`
- Search:
  - `Client/src/hooks/useSearch.ts`

Realtime layer (SignalR client):

- `Client/src/lib/hubs/chatHubClient.ts`
  - Builds a singleton `HubConnection` to `${PUBLIC_API_URL}/hubs/chat`.
  - Uses `Client/src/app/api/auth/token/route.ts` to fetch the JWT for `accessTokenFactory`.
  - Enables automatic reconnect.
- `Client/src/lib/hubs/chatHubOperations.ts`
  - Encapsulates `JoinChat`, `JoinDirect`, `LeaveChat`, and `LeaveDirect` behavior.
- `Client/src/hooks/useChatRealtime.ts`
  - Subscribes to `MessageCreated`, `Typing`, and `ReadReceipt`.
  - Joins or leaves the appropriate chat/direct target based on the selected conversation.
- `Client/src/hooks/useChatListManagement.ts`
  - Also listens to `MessageCreated` and `ReadReceipt` to keep the sidebar chat list fresh.

UI modules:

- Messenger layout:
  - `Client/src/components/features/messenger/layout/MessengerSidebar.tsx`
  - `Client/src/components/features/messenger/layout/MessengerMainArea.tsx`
- Chat list:
  - `Client/src/components/features/messenger/ChatList/`
- Chat view and composer:
  - `Client/src/components/features/messenger/chat/ChatHeader.tsx`
  - `Client/src/components/features/messenger/chat/MessageList.tsx`
  - `Client/src/components/features/messenger/chat/MessageBubble.tsx`
  - `Client/src/components/features/messenger/chat/MessageComposer.tsx`
  - `Client/src/components/features/messenger/chat/UserProfilePanel.tsx`
- Contacts and search:
  - `Client/src/components/features/messenger/ContactList/`
  - `Client/src/components/features/messenger/SearchResults/`
- Settings and empty states:
  - `Client/src/components/features/messenger/UserSettings/`
  - `Client/src/components/features/messenger/EmptyState/`

# Server (.NET 8 Web API + SignalR)

Entry points, startup, and DI composition:

- `Server/WebMessenger.Api/Program.cs`
  - Configures Serilog, controllers, Swagger, ProblemDetails, EF Core MySQL, JWT Bearer auth, CORS, and SignalR.
  - Maps controllers and `ChatHub`.
  - Applies EF Core migrations on startup with retry logic.
- `Server/WebMessenger.Api/appsettings.json`
  - Configures Serilog console + rolling file sinks and baseline application settings.
- `Server/WebMessenger.Api/appsettings.Development.json`
  - Overrides logging levels for development.

REST endpoints (controller areas):

- `Server/WebMessenger.Api/Controllers/AuthController.cs`
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `GET /api/auth/verify`
- `Server/WebMessenger.Api/Controllers/ChatController.cs`
  - `GET /api/chats`
  - `GET /api/chats/{chatId}/header`
  - `GET /api/chats/direct/{userId}/header`
  - `GET /api/chats/{chatId}/messages`
  - `POST /api/chats/direct/{userId}/messages`
  - `POST /api/chats/{chatId}/read`
  - `GET /api/chats/{chatId}/read-state`
- `Server/WebMessenger.Api/Controllers/ContactController.cs`
  - `GET /api/contacts`
  - `POST /api/contacts/add`
- `Server/WebMessenger.Api/Controllers/UserController.cs`
  - `GET /api/users`
  - `GET /api/users/profile`
  - `GET /api/users/profile/{id}`
  - `PUT /api/users/profile`
  - `POST /api/users/avatar`

SignalR:

- Hub host:
  - `Server/WebMessenger.Api/Hubs/ChatHub.cs`
- Hub route:
  - `/hubs/chat`
- Hub methods exposed to clients:
  - `JoinChat(Guid chatId)`
  - `JoinDirect(Guid otherUserId)`
  - `LeaveChat(Guid chatId)`
  - `LeaveDirect(Guid otherUserId)`
  - `Typing(Guid chatId, bool isTyping)`
  - `MarkRead(Guid chatId, DateTime upToUtc)`
- Group naming strategy:
  - `SignalRGroups.User(userId)` -> `user:{userId}`
  - `SignalRGroups.Chat(chatId)` -> `chat:{chatId}`
  - `SignalRGroups.Direct(a, b)` -> `dm:{smallerGuid}:{largerGuid}`
  - Implemented in `Server/WebMessenger.Contracts/Helpers/SignalRGroups.cs`
- Event names:
  - `MessageCreated`
  - `ReadReceipt`
  - `Typing`
  - Defined in `Server/WebMessenger.Contracts/Helpers/Events.cs`
- Event dispatch from application layer:
  - `Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs`
  - `ChatService` emits `MessageCreatedAsync`
  - `ChatController.MarkRead` emits `ReadReceiptAsync`

Domain/application layer:

- `Server/WebMessenger.Api/Services/AuthService.cs`
  - Auth workflow and JWT issuance.
- `Server/WebMessenger.Api/Services/ChatService.cs`
  - Chat list loading, message retrieval, direct chat discovery/creation, send message, read state, and direct-peer lookup.
- `Server/WebMessenger.Api/Services/UserService.cs`
  - Registration, user search, profile read/update.
- `Server/WebMessenger.Api/Services/ContactsService.cs`
  - Contact retrieval and add-contact behavior.
- `Server/WebMessenger.Api/Services/AvatarService.cs`
  - Avatar upload/update orchestration.
- `Server/WebMessenger.Api/Services/FileStorage/DropboxFileStorage.cs`
  - File storage implementation for avatars/images.

Persistence:

- DbContext:
  - `Server/WebMessenger.DAL/Data/ApplicationDbContext.cs`
- Core entities:
  - `Server/WebMessenger.DAL/Entities/User.cs`
  - `Server/WebMessenger.DAL/Entities/Chat.cs`
  - `Server/WebMessenger.DAL/Entities/Message.cs`
  - `Server/WebMessenger.DAL/Entities/ChatMember.cs`
  - `Server/WebMessenger.DAL/Entities/Contact.cs`
- Data access style:
  - Generic repository: `Server/WebMessenger.DAL/Repository.cs`
  - Unit of work: `Server/WebMessenger.DAL/UnitOfWork.cs`
  - Repository abstractions: `Server/WebMessenger.DAL/Interfaces/`
- Migrations:
  - `Server/WebMessenger.DAL/Migrations/`

AuthN/AuthZ:

- JWT Bearer auth is configured in `Server/WebMessenger.Api/Program.cs`.
- Most controllers are protected with `[Authorize]`; auth endpoints are the public entry points.
- `Server/WebMessenger.Api/Infrastructure/CurrentUser.cs` extracts the current user from claims.
- SignalR auth supports JWT from:
  - query string `access_token`
  - cookie `auth-token`
- Frontend route protection exists in both:
  - `Client/src/middleware/auth.ts`
  - `Client/src/providers/AuthProvider.tsx`

# Contracts between Client and Server

REST endpoints used by the client (brief):

- Auth
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `GET /api/auth/verify`
- Chats
  - `GET /api/chats`
  - `GET /api/chats/{chatId}/header`
  - `GET /api/chats/direct/{userId}/header`
  - `GET /api/chats/{chatId}/messages`
  - `POST /api/chats/direct/{userId}/messages`
  - `POST /api/chats/{chatId}/read`
  - `GET /api/chats/{chatId}/read-state`
- Contacts
  - `GET /api/contacts`
  - `POST /api/contacts/add`
- Users
  - `GET /api/users`
  - `GET /api/users/profile`
  - `GET /api/users/profile/{id}`
  - `PUT /api/users/profile`
  - `POST /api/users/avatar`

How REST is wired:

- Browser components call Next route handlers under `Client/src/app/api/**`.
- Those handlers use `Client/src/app/api/utils/proxy.ts` or direct `fetch(...)` calls to forward requests to the ASP.NET API using `API_BASE_URL` or `PUBLIC_API_URL`.
- Example paths:
  - `Client/src/app/api/chats/route.ts` -> `GET /api/chats`
  - `Client/src/app/api/auth/login/route.ts` -> `POST /api/auth/login`

SignalR events/messages (brief):

- Client to server methods:
  - `JoinChat`
  - `JoinDirect`
  - `LeaveChat`
  - `LeaveDirect`
  - `Typing`
  - `MarkRead`
- Server to client events:
  - `MessageCreated`
  - `Typing`
  - `ReadReceipt`

Shared DTOs and constants:

- Shared DTOs live in `Server/WebMessenger.Contracts/Models/`, including:
  - `ChatListItemDto.cs`
  - `ChatMessageDto.cs`
  - `DirectChatHeaderDto.cs`
  - `SendMessageRequest.cs`
  - `SendMessageResponse.cs`
  - `ReadStateDto.cs`
  - `UserDto.cs`
  - `UserProfileDto.cs`
- SignalR constants live in `Server/WebMessenger.Contracts/Helpers/`.
- Client-side transport/view types live in `Client/src/types/chat.ts`, `Client/src/types/user.ts`, and `Client/src/types/contact.ts`.
- Explicit API versioning was not found; the contract strategy is currently path-stable endpoints without `/v1` style version segments.

# Cross-cutting concerns

Logging, error handling, telemetry:

- Logging:
  - Bootstrap and structured logging are configured in `Server/WebMessenger.Api/Program.cs`.
  - Serilog sinks are configured in `Server/WebMessenger.Api/appsettings.json`.
  - Docker bind-mount for file logs exists in `docker-compose.yml` (`./Server/WebMessenger.Api/logs:/app/logs`).
- Error handling:
  - `AddProblemDetails(...)` and `UseExceptionHandler()` are enabled in `Program.cs`.
  - Controllers also contain local `try/catch` logging and status mapping.
- Telemetry:
  - No OpenTelemetry or Application Insights integration was found.

Security boundaries, secrets, config:

- Client-side env usage:
  - `PUBLIC_API_URL` for browser-visible API base.
  - `API_BASE_URL` for server-side Next.js proxy calls.
- Server-side secrets/config:
  - JWT and Dropbox settings are bound from configuration in `Program.cs`.
  - Docker-local values come from `.env.docker` generated from `.env.docker.example`.
- Cookie/JWT boundary:
  - login route writes the `auth-token` cookie in `Client/src/app/api/auth/login/route.ts`.
  - server validates JWT for controllers and hubs.

Testing:

- API unit tests:
  - `Server/WebMessenger.Api.Tests/Unit/Controllers/`
  - `Server/WebMessenger.Api.Tests/Unit/Services/`
  - `Server/WebMessenger.Api.Tests/Unit/ErrorHandling/`
  - `Server/WebMessenger.Api.Tests/Unit/Debugging/`
- Test utilities/shared fixtures:
  - `Server/WebMessenger.Api.Tests/Shared/`
- Contracts tests:
  - `Server/WebMessenger.Contracts.Tests/Unit/Validation/`
- Not found:
  - dedicated browser e2e test project
  - integration-test host exercising the full HTTP pipeline against a live app instance

# Where to implement X

Add new message type:

- Server contract shape:
  - `Server/WebMessenger.Contracts/Models/ChatMessageDto.cs`
  - `Server/WebMessenger.Contracts/Models/SendMessageRequest.cs`
  - possibly `Server/WebMessenger.DAL/Entities/Message.cs`
- Server business logic:
  - `Server/WebMessenger.Api/Services/ChatService.cs`
  - `Server/WebMessenger.Api/Controllers/ChatController.cs`
  - `Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs`
- Client transport and rendering:
  - `Client/src/types/chat.ts`
  - `Client/src/lib/utils/normalization.ts`
  - `Client/src/components/features/messenger/chat/MessageBubble.tsx`
  - `Client/src/components/features/messenger/chat/MessageList.tsx`
  - `Client/src/components/features/messenger/chat/MessageComposer.tsx`

Add chat reactions:

- Persistence likely starts in:
  - `Server/WebMessenger.DAL/Entities/Message.cs`
  - new reaction entity under `Server/WebMessenger.DAL/Entities/`
  - `Server/WebMessenger.DAL/Data/ApplicationDbContext.cs`
- API/service layer likely in:
  - `Server/WebMessenger.Api/Services/ChatService.cs`
  - `Server/WebMessenger.Api/Controllers/ChatController.cs`
- Realtime fan-out likely in:
  - `Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs`
  - `Server/WebMessenger.Contracts/Helpers/Events.cs`
- Client UI likely in:
  - `Client/src/components/features/messenger/chat/MessageBubble.tsx`
  - `Client/src/hooks/useChatRealtime.ts`
  - `Client/src/types/chat.ts`

Add typing indicator:

- Existing server-side entry points already exist in:
  - `Server/WebMessenger.Api/Hubs/ChatHub.cs`
  - `Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs`
  - `Server/WebMessenger.Contracts/Helpers/Events.cs`
- Existing client-side subscription already exists in:
  - `Client/src/hooks/useChatRealtime.ts`
- Likely UI and emit points:
  - `Client/src/components/features/messenger/chat/MessageComposer.tsx`
  - `Client/src/components/features/messenger/chat/ChatHeader.tsx` or message area components to show typing state

Add read receipts:

- Existing backend logic already exists in:
  - `Server/WebMessenger.Api/Controllers/ChatController.cs` (`POST /api/chats/{chatId}/read`)
  - `Server/WebMessenger.Api/Services/ChatService.cs`
  - `Server/WebMessenger.Api/Hubs/Events/ChatEvents.cs`
  - `Server/WebMessenger.Api/Hubs/ChatHub.cs`
- Existing client logic already exists in:
  - `Client/src/hooks/useMarkRead.ts`
  - `Client/src/hooks/useReadStateTracking.ts`
  - `Client/src/hooks/useChatRealtime.ts`
  - `Client/src/hooks/useChatListManagement.ts`
- Likely UI surface for deeper read-receipt UX:
  - `Client/src/components/features/messenger/chat/MessageBubble.tsx`
  - `Client/src/components/features/messenger/chat/MessageList.tsx`

# Glossary

- `auth-token`: HTTP-only cookie set by the Next.js login route and used for authenticated API and hub access.
- direct chat: a one-to-one conversation; the client may temporarily represent it with a synthetic DM key before the server chat id is known.
- DM key: a deterministic client-side identifier derived from two user ids, used before or alongside a persisted direct chat id.
- `peerUserId`: the other participant in a one-to-one chat.
- `serverChatId`: the actual persisted chat id from the ASP.NET backend.
- `MessageCreated`: SignalR event emitted when a new message is stored.
- `ReadReceipt`: SignalR event emitted when a chat read position changes.
- `Typing`: SignalR event emitted when a user starts or stops typing in a chat.
- `IUnitOfWork`: DAL abstraction used by services to access repositories and commit changes.
- `PagedResult<T>`: shared contract wrapper used for paginated API responses.

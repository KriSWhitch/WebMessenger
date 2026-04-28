# TRELLO-1 Client App Refactoring: Structure Review, Deduplication & Optimization

## Objective

Refactor the client application to improve maintainability, readability, and performance without changing any existing user-visible behavior, server contracts, authentication model, or realtime behavior.

## Scope

### In scope
- Review the client architecture and document/refine ownership boundaries between routes, hooks, hub integration, auth wrappers, and messenger UI modules.
- Extract duplicated client-side logic into reusable helpers, hooks, or smaller UI units.
- Reduce unnecessary re-renders and simplify state/effect flow where the current implementation is overly broad.
- Remove dead or unused client code where safe.
- Break down overly large client files where it improves clarity and reviewability.

### Out of scope
- Any changes under `Server/`
- API contract changes
- SignalR event name or payload changes
- Database changes or migrations
- New product features
- UI redesign or intentional UX changes
- New external services or dependencies unless explicitly approved

## Constraints

- No new external services unless approved.
- Keep changes small, local, and reviewable.
- Respect the current auth model: JWT cookie `auth-token` and SignalR `access_token` flow must continue working.
- Preserve current REST and SignalR integration behavior.
- Maintain zero functional regressions.
- Prefer incremental extraction over large rewrites.

## Relevant code areas & files

### Primary client entry and orchestration
- `Client/src/app/page.tsx`
- `Client/src/app/layout.tsx`

### Auth and request gating
- `Client/src/providers/AuthProvider.tsx`
- `Client/src/middleware/auth.ts`

### Realtime / hub integration
- `Client/src/lib/hubs/chatHubClient.ts`
- `Client/src/hooks/useChatRealtime.ts`
- `Client/src/hooks/useChatInboxRealtime.ts`

### Client data flows and utility hooks
- `Client/src/hooks/useChatList.ts`
- `Client/src/hooks/useMessages.ts`
- `Client/src/hooks/useMarkRead.ts`
- `Client/src/hooks/useDebounce.ts`

### Messenger UI modules
- `Client/src/components/features/messenger/layout/MessengerSidebar.tsx`
- `Client/src/components/features/messenger/layout/MessengerMainArea.tsx`
- `Client/src/components/features/messenger/chat/ChatHeader.tsx`
- `Client/src/components/features/messenger/chat/MessageList.tsx`
- `Client/src/components/features/messenger/chat/MessageBubble.tsx`
- `Client/src/components/features/messenger/chat/UserProfilePanel.tsx`

### Client DTO mirrors
- `Client/src/types/chat.ts`
- `Client/src/types/contact.ts`
- `Client/src/types/user.ts`

### Architecture reference
- `docs/CODEBASE_MAP.md`

## Implementation expectations

### Refactor targets
- Reduce orchestration weight in `Client/src/app/page.tsx` by moving clearly separable logic into focused hooks or helper modules.
- Consolidate duplicated SignalR subscription / join / leave handling between realtime hooks where possible without changing event semantics.
- Consolidate duplicated fetch normalization, pagination merging, and DTO-to-view-model mapping logic where safe.
- Remove dead code and unused imports/exports in touched areas.
- Keep naming consistent across hooks, state variables, and messenger UI modules.

### Non-goals during implementation
- Do not alter REST paths, request bodies, or response handling contracts.
- Do not alter SignalR event names: `MessageCreated`, `ReadReceipt`, `Typing`.
- Do not change hub URL or auth token acquisition flow.
- Do not introduce behavioral changes in login redirects, profile flows, avatar upload, chat ordering, unread counts, message send, or realtime updates.

## API + SignalR expectations

### REST endpoints that must remain unchanged
- `POST /api/auth/login`
- `GET /api/auth/verify`
- `GET /api/users/profile`
- `GET /api/chats?limit&before`
- `GET /api/chats/{id}/header`
- `GET /api/chats/direct/{userId}/header`
- `GET /api/chats/{id}/messages?limit&before`
- `POST /api/chats/direct/{userId}/messages`
- `POST /api/chats/{id}/read`
- `GET /api/chats/{id}/read-state`
- `GET /api/contacts?query`
- `PUT /api/users/profile`
- `POST /api/users/avatar`

### SignalR client behavior that must remain unchanged
- Hub path remains `/hubs/chat`
- Token retrieval continues through `/api/auth/token`
- Existing transport fallback and reconnect strategy remain behaviorally equivalent
- Existing event subscriptions remain intact:
  - `MessageCreated`
  - `ReadReceipt`
  - `Typing`
- Existing invoke flow remains intact where used:
  - `JoinChat`
  - `JoinDirect`
  - `LeaveChat`
  - `LeaveDirect`
  - `Typing`
  - `MarkRead`

## Acceptance Criteria

- [ ] Client architecture review completed before substantive refactor work.
- [ ] Refactor touches only `Client/` code.
- [ ] No REST endpoint usage or payload shape is changed.
- [ ] No SignalR event names, payload expectations, or connection/auth flow is changed.
- [ ] Duplicated client logic in touched areas is reduced through reusable helpers/hooks/components.
- [ ] Large orchestration logic is split into smaller focused units where this reduces complexity.
- [ ] Dead code and unused imports/exports in touched areas are removed.
- [ ] Core messenger flows behave identically before and after the refactor.
- [ ] Auth redirect behavior remains unchanged.
- [ ] Chat ordering, message loading, unread counts, read receipts, and realtime updates remain unchanged.
- [ ] No new dependency or external service is introduced without approval.
- [ ] Resulting changes are reviewable and logically grouped.

## Validation checklist

### Automated
- [ ] Run client lint/typecheck/build commands available in the repo.
- [ ] Verify touched code compiles cleanly.
- [ ] If tests exist for touched areas, run them unchanged.

### Manual
- [ ] Login works and redirect logic is unchanged.
- [ ] Chat list loads and pagination still works.
- [ ] Opening an existing chat still loads header and message history correctly.
- [ ] Sending a direct message still updates the thread and chat list.
- [ ] Realtime `MessageCreated` updates still appear as before.
- [ ] Realtime `Typing` behavior remains unchanged where currently wired.
- [ ] Read receipts / mark-read behavior still works.
- [ ] Profile panel and settings still open and function correctly.
- [ ] Avatar upload still works.
- [ ] Search / contacts behavior remains unchanged.
- [ ] No visible UI regressions on the main messenger page.

## Risks / watch areas

- `Client/src/app/page.tsx` appears to hold broad UI/data/realtime orchestration and is the highest regression-risk area.
- Realtime join/leave and inbox update flows are easy to regress if extracted without keeping lifecycle order intact.
- Auth and redirect handling must remain behaviorally identical even if code is simplified.
- Cursor pagination and merge-order logic for chats/messages should be treated as contract-like behavior.

## Open questions

- Are auth-related client files fully in scope for internal cleanup?
- What exact validation commands should be treated as required for completion?
- Is this expected as one pass or split into multiple small PR-sized slices?
- Are invisible accessibility-only improvements acceptable?

## Links

- Trello card: https://trello.com/c/ZUYfLUHz/1-client-app-refactoring-structure-review-deduplication-optimization
- Short URL: https://trello.com/c/ZUYfLUHz
- Architecture map: `docs/CODEBASE_MAP.md`

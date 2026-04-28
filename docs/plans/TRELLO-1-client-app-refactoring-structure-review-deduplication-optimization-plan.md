# WebMessenger Client Refactoring Plan (Revised, Strict)

## Objective
Refactor the client codebase for maintainability and clarity, with zero behavior changes and no contract changes.

## Scope
- **In scope:** `Client/` only.
- **Out of scope:** `Server/`, API contracts, SignalR contracts, database, new features, UI redesign.

## Scope Freeze (Hard Rule)
After Phase 0, no new initiatives are allowed. Work must stay within the defined phase tasks only.

---

## Global Invariants (Must Not Change)

### API usage invariants
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

### SignalR invariants
- Hub path remains `/hubs/chat`
- Token path remains `/api/auth/token`
- Event names remain:
  - `MessageCreated`
  - `ReadReceipt`
  - `Typing`
- Invoke names remain:
  - `JoinChat`, `JoinDirect`, `LeaveChat`, `LeaveDirect`, `Typing`, `MarkRead`
- Reconnect/fallback behavior must remain equivalent.

### Auth/security invariants
- JWT cookie flow (`auth-token`) unchanged.
- Middleware redirect semantics unchanged.
- Hub authorization behavior unchanged.

### DTO parity invariant
- `Client/src/types/chat.ts`, `Client/src/types/contact.ts`, `Client/src/types/user.ts` must remain contract-compatible with existing payloads.

---

## Hard Gates (Applied After Every Phase)

1. **Gate A (API):** endpoint usage, request shape, and response expectations unchanged.
2. **Gate B (SignalR):** event/invoke names and lifecycle behavior unchanged.
3. **Gate C (Auth):** cookie flow, middleware redirects, `/api/auth/token`, and hub auth unchanged.
4. **Gate D (DTO Parity):** client type shapes remain compatible with runtime payloads.
5. **Gate E (Build Quality):** lint + typecheck + build pass.

If any gate fails: stop, rollback current phase, and do not continue.

---

## Execution Commands (Windows / VS Code Terminal)

Run from `Client/`:
- `npm run lint --if-present`
- `npm run typecheck --if-present`
- `npm run build --if-present`
- `npm test --if-present`

If the repo uses another package manager, use equivalent commands.

---

## File and Domain Limits (Hard Rules)

- One PR = one phase.
- Max one domain area per phase.
- Max 12 touched files per phase.
- If limits are exceeded, split into sub-phases.

---

## Phase 0 — Baseline and Invariant Lock (Mandatory)

**Goal:** establish a reproducible baseline before refactor work.

### Tasks
- Document invariants and hard gates in PR notes.
- Capture baseline manual smoke flow:
  - Login + protected route behavior
  - Chat list load + pagination
  - Open chat + message history load
  - Send message
  - Realtime receive (`MessageCreated`)
  - Read state flow
  - Profile update + avatar upload
- Capture realtime lifecycle baseline:
  - connect -> join -> receive -> leave -> reconnect -> rejoin
- Capture pagination integrity baseline:
  - cursor progression
  - stable ordering
  - no duplicates

### DoD
- Baseline checklist is documented and reusable.
- Scope Freeze is confirmed.

---

## Phase 1 — Pagination and Normalization Consolidation Only

**Domain:** shared list/data utilities only.

### Target files
- `Client/src/lib/utils/pagination.ts` (new)
- `Client/src/lib/utils/normalization.ts` (new)
- `Client/src/hooks/useMessages.ts`
- `Client/src/hooks/useChatList.ts`
- `Client/src/app/page.tsx`

### Tasks
- Extract duplicated pagination and merge helpers.
- Extract DTO-to-view-model normalization helper.
- Replace local duplicates with shared helpers.
- Keep logic identical.

### Validation
- Unit tests for:
  - normalize page parsing
  - dedupe merge behavior
  - stable sorting behavior
  - DTO normalization behavior
- Manual pagination integrity checks:
  - no missing items
  - no duplicate items
  - stable ordering across page loads

### DoD
- Duplicate utility logic removed from target files.
- Behavior parity confirmed by gates and smoke checks.

---

## Phase 2 — Profile State Centralization Only

**Domain:** profile state ownership only.

### Target files
- `Client/src/providers/UserProvider.tsx` (new)
- `Client/src/hooks/useCurrentUser.ts` (new)
- `Client/src/app/layout.tsx`
- `Client/src/app/page.tsx`
- `Client/src/components/features/messenger/layout/MessengerMainArea.tsx`

### Tasks
- Introduce a single source of truth for current user profile state.
- Remove duplicate profile fetch calls where applicable.
- Keep auth behavior unchanged.

### Validation
- Verify profile endpoint call count is reduced without behavior change.
- Verify login, route guard, and page load behavior remain unchanged.

### DoD
- Single profile source in client runtime.
- Auth and routing parity confirmed.

---

## Phase 3 — Realtime Lifecycle Consolidation Only

**Domain:** SignalR connection/join/leave lifecycle only.

### Target files
- `Client/src/lib/hubs/chatHubOperations.ts` (new)
- `Client/src/hooks/useChatRealtime.ts`
- `Client/src/hooks/useChatInboxRealtime.ts`

### Tasks
- Consolidate shared connect/join/leave operations.
- Remove duplicated listener lifecycle code.
- Preserve event handling semantics.

### Mandatory realtime checks
- connect/join works
- chat switch leaves previous and joins next
- reconnect restores expected subscriptions
- no duplicate listeners or duplicate UI updates
- `MessageCreated`, `Typing`, `ReadReceipt` behavior unchanged

### DoD
- Realtime lifecycle logic consolidated.
- Full realtime regression checks pass.

---

## Phase 4 — `app/page.tsx` Orchestration Decomposition Only

**Domain:** page orchestration structure only.

### Target files
- `Client/src/hooks/useChatListManagement.ts` (new)
- `Client/src/hooks/useDirectChatResolution.ts` (new)
- `Client/src/app/page.tsx`
- Verify compatibility with:
  - `Client/src/components/features/messenger/layout/MessengerSidebar.tsx`
  - `Client/src/components/features/messenger/layout/MessengerMainArea.tsx`

### Tasks
- Move orchestration logic into focused hooks.
- Keep data flow and callbacks behavior identical.
- No UI redesign, no feature additions.

### DoD
- `page.tsx` complexity reduced structurally.
- End-to-end behavior parity preserved.

---

## Phase 5 — Targeted Large-Component Split Only

**Domain:** component decomposition only for proven hotspots.

### Candidate files (only if justified by size/duplication)
- `Client/src/components/features/messenger/layout/MessengerMainArea.tsx`
- `Client/src/components/features/messenger/layout/MessengerSidebar.tsx`
- `Client/src/components/features/messenger/chat/MessageBubble.tsx`
- Optional new files only when required:
  - `Client/src/components/features/messenger/chat/MessageComposer.tsx`
  - `Client/src/components/features/messenger/chat/ChatReadIndicator.tsx`
  - `Client/src/components/features/messenger/SearchBox.tsx`
  - `Client/src/hooks/useReadStateTracking.ts`
  - `Client/src/hooks/useSearch.ts`

### Rules
- No optional abstractions unless there is explicit duplication or excessive complexity.
- No behavior changes in send/read/search flows.

### DoD
- Only justified splits merged.
- UI and interaction parity confirmed.

---

## Phase 6 — Cleanup, Naming Consistency, Final Parity Checks

**Domain:** cleanup and consistency only.

### Tasks
- Remove dead code and unused imports in touched areas.
- Normalize naming conventions.
- Add brief inline documentation/JSDoc where needed.
- Run final DTO parity check against runtime payloads.

### DoD
- No dead code in modified areas.
- All gates pass.
- Final baseline smoke checklist passes.

---

## Explicit No-Touch Areas (Unless Strictly Mechanical and Behavior-Neutral)

- `Server/**`
- API contract definitions and payload semantics
- SignalR event/invoke names and semantics
- Auth token flow semantics
- `Client/src/app/api/*` route handler behavior (proxy logic must remain equivalent)

---

## Relevant Code Areas (From CODEBASE_MAP)

- `Client/src/app/page.tsx`
- `Client/src/app/layout.tsx`
- `Client/src/providers/AuthProvider.tsx`
- `Client/src/hooks/useChatList.ts`
- `Client/src/hooks/useMessages.ts`
- `Client/src/hooks/useChatRealtime.ts`
- `Client/src/hooks/useChatInboxRealtime.ts`
- `Client/src/hooks/useMarkRead.ts`
- `Client/src/hooks/useDebounce.ts`
- `Client/src/lib/hubs/chatHubClient.ts`
- `Client/src/components/features/messenger/layout/MessengerSidebar.tsx`
- `Client/src/components/features/messenger/layout/MessengerMainArea.tsx`
- `Client/src/components/features/messenger/chat/MessageList.tsx`
- `Client/src/components/features/messenger/chat/MessageBubble.tsx`
- `Client/src/components/features/messenger/chat/ChatHeader.tsx`
- `Client/src/types/chat.ts`
- `Client/src/types/contact.ts`
- `Client/src/types/user.ts`
- `docs/CODEBASE_MAP.md`
- `docs/specs/TRELLO-1-client-app-refactoring-structure-review-deduplication-optimization.md`

---

## Acceptance Criteria Coverage

- Architecture reviewed before refactor: **Phase 0**
- Duplication reduced in client: **Phases 1, 3, 4, 5**
- Behavior unchanged: **all phases + hard gates**
- Auth model preserved: **all phases + Gate C**
- API/SignalR contracts preserved: **all phases + Gates A/B**
- Validation complete (manual + automated): **all phases**
- DTO parity preserved: **Gate D + Phase 6 final check**

---

## Estimated Effort (Story Points)

- Phase 0: 1
- Phase 1: 2
- Phase 2: 3
- Phase 3: 3
- Phase 4: 5
- Phase 5: 4
- Phase 6: 2

**Total: ~20 story points**
# Client-Side SignalR Migration Guide

This document lists all changes required on the frontend to match the server-side SignalR refactoring.

---

## 1. Payload shapes changed — all events now use named DTOs

All three server-pushed events now deliver strongly-typed record payloads. Update your event handler registrations accordingly.

### `MessageCreated`

**Before:**
```json
{ "chatId": "...", "peerUserId": "...", "message": { ... } }
```

**After** (same fields, same JSON keys — no change needed):
```json
{ "chatId": "...", "peerUserId": "...", "message": { ... } }
```
> ✅ No client change required — shape is identical.

---

### `ReadReceipt`

**Before:**
```json
{ "chatId": "...", "userId": "...", "lastReadAt": "..." }
```

**After** (same fields, same JSON keys — no change needed):
```json
{ "chatId": "...", "userId": "...", "lastReadAt": "..." }
```
> ✅ No client change required — shape is identical.

---

### `Typing`

**Before:**
```json
{ "chatId": "...", "userId": "...", "isTyping": true }
```

**After** (same fields, same JSON keys — no change needed):
```json
{ "chatId": "...", "userId": "...", "isTyping": true }
```
> ✅ No client change required — shape is identical.

---

## 2. `Typing` event is no longer echoed back to the sender

The server now calls `GroupExcept(chatId, senderConnectionId)` for `Typing` broadcasts.

**Impact:** If the client was filtering out its own typing events manually, that logic can be removed. If it was relying on receiving its own typing echo — stop relying on it.

> ⚠️ **Action:** Remove any client-side self-filtering for `Typing` events if present.

---

## 3. Hub methods now require a `CancellationToken`-compatible call

Server hub methods signatures:

| Method | Parameters |
|--------|-----------|
| `JoinChat` | `(chatId: string)` |
| `JoinDirect` | `(otherUserId: string)` |
| `Typing` | `(chatId: string, isTyping: boolean)` |
| `MarkRead` | `(chatId: string, upToUtc: string)` |
| `LeaveChat` | `(chatId: string)` |
| `LeaveDirect` | `(otherUserId: string)` |

`CancellationToken` is a server-only concept and is transparent to the SignalR JS/TS client — no changes needed in invocation calls.

> ✅ No client change required for method invocations.

---

## 4. SignalR connection behaviour changes

| Setting | Old value | New value |
|---------|-----------|-----------|
| `EnableDetailedErrors` | `true` (always) | `true` only in **Development**, `false` in Production |
| `KeepAliveInterval` | default (15 s) | **15 s** (explicit) |
| `ClientTimeoutInterval` | default (30 s) | **30 s** (explicit) |
| `MaximumReceiveMessageSize` | default (32 KB) | **32 KB** (explicit) |

**Impact:**
- In production, SignalR hub exceptions will no longer include stack traces in error messages sent to the client. Client code that was parsing `HubException` message strings for debugging purposes should be updated.
- Message size is capped at 32 KB. If the client sends messages larger than that (e.g., large file content via hub), the connection will be dropped.

> ⚠️ **Action:** Ensure no hub invocation sends a payload larger than **32 KB**. All file uploads must go through the HTTP REST API (`POST /api/...`), never through the hub.

---

## 5. Recommended: adopt `connection.serverTimeoutInMilliseconds`

To align with the server's `ClientTimeoutInterval = 30 s` and `KeepAliveInterval = 15 s`, set matching values on the JS client:

```typescript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", {
        accessTokenFactory: () => getAccessToken()
    })
    .withAutomaticReconnect()
    .build();

connection.serverTimeoutInMilliseconds = 30_000;  // match server ClientTimeoutInterval
connection.keepAliveIntervalInMilliseconds = 15_000; // match server KeepAliveInterval
```

> ⚠️ **Recommended action:** Add these two settings to your hub connection builder.

---

## 6. Recommended: `withAutomaticReconnect()`

If not already present, add `.withAutomaticReconnect()` to the connection builder (shown above). This handles transient disconnects without user intervention.

---

## Summary of required client changes

| # | Change | Required |
|---|--------|----------|
| 1 | Payload shapes for all 3 events | ✅ No change |
| 2 | Remove self-filtering of own `Typing` events | ⚠️ If applicable |
| 3 | Hub method call signatures | ✅ No change |
| 4 | Ensure no hub payload > 32 KB | ⚠️ Verify |
| 5 | Set `serverTimeoutInMilliseconds` / `keepAliveIntervalInMilliseconds` | ⚠️ Recommended |
| 6 | Add `withAutomaticReconnect()` | ⚠️ Recommended |

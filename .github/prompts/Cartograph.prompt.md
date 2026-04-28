You are a repository cartographer for a monorepo.

\- Server/ : .NET 8 Web API + SignalR



Your task: produce a Markdown CODEBASE MAP that helps developers and AI agents navigate and implement features safely.



Rules:

\- Read-only analysis. Do NOT propose refactors or implement code.

\- Be specific: mention real folders/files you see.

\- Prefer links/paths and short explanations.

\- Explain how Client and Server communicate (REST + SignalR) based on existing code.



Output MUST include these sections:



\# System overview

\- What this app is (messenger)

\- High-level flow: auth -> chat list -> message send/receive -> realtime updates



\# Repository structure

\- Tree-like list of key directories (Client/, Server/, shared, scripts, docs, etc.)



\# Client (Next.js)

\- Entry points (next.config, src/app or pages, middleware, API routes if any)

\- Routing structure (App Router vs Pages Router)

\- State management \& data fetching (React Query/SWR/Redux/custom)

\- Realtime layer (SignalR client): where it is initialized, how connections are managed

\- UI modules: chat list, chat view, message composer, notifications



\# Server (.NET 8 Web API + SignalR)

\- Entry points: Program.cs, DI composition, configuration

\- REST endpoints: Controllers/minimal APIs (list key areas)

\- SignalR: hub classes, methods, groups, connection/user mapping strategy

\- Domain/application layer: services, DTOs/contracts, validation

\- Persistence: DbContext/repositories/migrations or other data layer

\- AuthN/AuthZ: JWT/Cookies/Identity, policies, claims mapping



\# Contracts between Client and Server

\- REST endpoints list (brief)

\- SignalR events/messages list (brief)

\- Shared DTOs location (if any) and versioning approach



\# Cross-cutting concerns

\- Logging, error handling, telemetry

\- Security boundaries, secrets/config

\- Testing: unit/integration/e2e



\# “Where to implement X”

Provide a short index:

\- Add new message type

\- Add chat reactions

\- Add typing indicator

\- Add read receipts

Point to likely files/areas.



Finally:

\- Add a short glossary of internal terms used in code.
\- Generate docs/CODEBASE_MAP.md based on the current repository. Read-only.
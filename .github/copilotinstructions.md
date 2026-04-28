# Copilot project instructions

This repository is a monorepo:
- Client/ = Next.js (React)
- Server/ = .NET 8 Web API + SignalR

Always use the architectural map:
docs/CODEBASE_MAP.md

Rules:
- Respect current architecture and folder boundaries (Client vs Server).
- When planning or coding, explicitly reference relevant sections/files from CODEBASE_MAP.
- Prefer small, reviewable changes.
- Keep security in mind: do not log secrets or tokens; do not weaken auth.
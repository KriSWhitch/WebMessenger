
# 💬 Web Messenger App

## 📺 Desktop & Mobile Demo

<details>
<summary><b>Click here to see a demo</b></summary>

https://github.com/user-attachments/assets/a3900797-dd0a-4b3f-8523-06d577ee1a45
</details>

## Overview

WebMessenger is a real-time full-stack messenger built as a portfolio project.
It includes authentication, contact management, chat history, and live updates powered by SignalR.

## Why This Project

- Demonstrates end-to-end product thinking: auth, messaging flow, realtime behavior, and UX.
- Shows modern full-stack architecture with clear client/server boundaries.
- Runs locally with Docker in a repeatable way without installing local runtimes.

## Highlights

- JWT authentication with protected routes
- Real-time messaging via SignalR
- Dynamic direct chats and chat list updates
- Read state and unread counters
- User search and contact management
- Profile and avatar support (Dropbox integration)
- Responsive UI for desktop and mobile

## User Flow

1. User logs in via auth endpoints and receives `auth-token` cookie.
2. Client loads chat list and chat headers from API.
3. User sends a message in direct chat.
4. Server persists message and broadcasts realtime update.
5. Client updates chat list, message thread, unread/read state.

## Architecture

- Client: Next.js 15 (App Router) + React 19 + TypeScript
- API: ASP.NET Core 8 Web API + SignalR Hub
- Data: EF Core + MySQL
- Contracts: shared DTOs in `Server/WebMessenger.Contracts`
- Dev runtime: Docker Compose (client + api + db)

Communication model:

- REST: Next.js route handlers in `Client/src/app/api/**` proxy requests to ASP.NET API.
- SignalR: browser client connects to `/hubs/chat` for `MessageCreated`, `Typing`, `ReadReceipt`.

## Tech Stack

Frontend:

- Next.js 15
- React 19
- TypeScript
- Tailwind CSS
- SignalR

Backend:

- ASP.NET Core 8
- Entity Framework Core
- MySQL
- SignalR
- Serilog
- Swagger (Swashbuckle)
- Dropbox.Api (file storage integration)

Quality and testing:

- xUnit
- Moq
- AutoFixture + AutoMoq
- Bogus

## Quick Start (Docker)

Requirements:

- Docker Desktop

1. Clone repository

```bash
git clone <your-repository-url>
cd WebMessenger
```

2. Create local env file from template

```bash
# macOS / Linux
cp .env.docker.example .env.docker

# Windows
copy .env.docker.example .env.docker
```

3. Fill required values in `.env.docker`

- `JWT_KEY`
- Optional: `DROPBOX_ACCESS_TOKEN`, `DROPBOX_CLIENT_ID`, `DROPBOX_CLIENT_SECRET`

4. Build and run

```bash
docker compose --env-file .env.docker up -d --build
```

5. Open app

- Client: http://localhost:3000
- API: http://localhost:5227
- MySQL: localhost:3307

Stop stack:

```bash
docker compose --env-file .env.docker down
```

Stop and reset DB volume:

```bash
docker compose --env-file .env.docker down -v
```

## Testing

- API tests: `Server/WebMessenger.Api.Tests`
- Contracts tests: `Server/WebMessenger.Contracts.Tests`

Run all backend tests:

```bash
dotnet test Server/WebMessenger.sln
```

## Repository Map

- `Client/` - Next.js app (UI, hooks, API route handlers)
- `Server/WebMessenger.Api/` - ASP.NET API, controllers, SignalR hub
- `Server/WebMessenger.DAL/` - entities, DbContext, migrations, repository/UoW
- `Server/WebMessenger.Contracts/` - shared DTOs and realtime constants
- `Server/WebMessenger.Api.Tests/` - API unit tests
- `Server/WebMessenger.Contracts.Tests/` - contract/validation tests
- `docker/` - MySQL init script and optional SQL dumps
- `docs/CODEBASE_MAP.md` - detailed codebase navigation map

## Notes

- This project is optimized for Docker-based local development.
- You do not need local Node.js, .NET runtime, or MySQL installation to run the stack.
- SQL dump import is supported on first DB initialization via `docker/mysql/init/00-import-optional-dump.sh`.
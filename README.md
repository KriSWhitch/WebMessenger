
# 💬 Web Messenger App

## 📺 Desktop & Mobile Demo

<details>
<summary><b>Click here to see a demo</b></summary>

https://github.com/user-attachments/assets/a3900797-dd0a-4b3f-8523-06d577ee1a45
</details>

## 🚀 About

A modern real-time web messenger application built as a full-stack portfolio project.

It features **instant messaging**, **real-time updates via SignalR**, and a **responsive UI** optimized for both desktop and mobile.

## ✨ Features

- JWT-based authorization and authentication system
- User search
- Contact management functionality
- Dynamic chat creation
- Real-time messaging with SignalR
- Unread message indicator
- Responsive design
- Image handling using Dropbox

## 🛠 Technologies

### **Frontend**

- **Next.js 15** 
- **React 19**
- **TypeScript**
- **Tailwind CSS**
- **SignalR**

### **Backend**

- **ASP.NET Core 8**
- **Entity Framework Core**
- **SignalR**
- **JWT Authentication**
- **MySQL**

## Setup

Run the full stack locally with Docker: **MySQL**, **ASP.NET Core API**, and **Next.js client**.

### 1. Clone the repository

```bash
git clone https://github.com/<your-username>/WebMessenger.git
cd WebMessenger
```

### 2. Prepare environment variables

The project already includes a `.env.docker` file for local Docker startup. Update it only if you need your own credentials or tokens.

### 3. Build and start the app

```bash
docker compose --env-file .env.docker up -d --build
```

This starts:

- **Client** at `http://localhost:3000`
- **API** at `http://localhost:5227`
- **MySQL** at `localhost:3307`

### 4. Stop the app

```bash
docker compose --env-file .env.docker down
```

To also remove the database volume and start from a clean state next time:

```bash
docker compose --env-file .env.docker down -v
```

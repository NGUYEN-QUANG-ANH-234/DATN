# Dockerizing HICAS HRM

This setup runs:

- MySQL on host port `3307`
- Redis on host port `6379`
- Backend API on host port `5107`
- Frontend gateway on host port `8080`

The frontend serves the built React app with Nginx and proxies `/api`, `/uploads`, `/contract-documents`, and `/swagger` to the backend container.

## 1. Prepare environment

```powershell
Copy-Item .env.docker.example .env.docker
```

Edit `.env.docker` and change at least:

- `MYSQL_ROOT_PASSWORD`
- `MYSQL_PASSWORD`
- `JWT_SECRET_KEY`
- Google OAuth values if you use Google login
- Email values if you send email from the system

## 2. Start database and Redis

```powershell
docker compose --env-file .env.docker up -d mysql redis
```

## 3. Apply EF migrations

The backend does not auto-run migrations on startup, so apply migrations before starting the full stack:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3307;Database=hrm_hicas;User=hrm_user;Password=change-db-password;AllowPublicKeyRetrieval=True;SslMode=None;CharSet=utf8mb4;"
$env:ConnectionStrings__Redis="127.0.0.1:6379"
$env:JwtSettings__SecretKey="change-this-development-jwt-secret-key-at-least-32-chars"
dotnet ef database update --project HRM.backend/HRM.backend.csproj --startup-project HRM.backend/HRM.backend.csproj
```

Use the same database/user/password values that you set in `.env.docker`.

## 4. Start the full stack

```powershell
docker compose --env-file .env.docker up -d --build
```

Open:

- Frontend: http://localhost:8080
- Backend API: http://localhost:5107
- Swagger through gateway: http://localhost:8080/swagger

## Useful commands

```powershell
docker compose --env-file .env.docker logs -f backend
docker compose --env-file .env.docker logs -f frontend
docker compose --env-file .env.docker down
docker compose --env-file .env.docker down -v
```

`down -v` removes database, Redis, upload, and log volumes. Use it only when you intentionally want to reset local Docker data.

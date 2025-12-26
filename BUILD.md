# IncidentScope Build Guide

This document provides step-by-step instructions for building and running IncidentScope locally.

## Prerequisites

- .NET 8 SDK
- Node.js 18+ and pnpm (or npm)
- **Docker Desktop** (must be running - see troubleshooting below)
- Git

> **Important**: Docker Desktop must be running before starting infrastructure. If you see connection errors, ensure Docker Desktop is started and fully initialized.

## Initial Setup

### 1. Clone and Navigate

```bash
cd IncidentScope
```

### 2. Start Infrastructure

**First, ensure Docker Desktop is running** (check system tray for Docker icon).

Start all required services (PostgreSQL, Redis, ClickHouse, Redpanda, OTel Collector):

```bash
docker-compose -f scripts/local/docker-compose.yml up -d
```

Wait for services to be healthy (check with `docker ps`).

### 3. Initialize Databases

Run the initialization script:

```bash
# On Windows (PowerShell)
docker exec incidentscope-postgres psql -U incidentscope -d incidentscope -f /docker-entrypoint-initdb.d/init.sql

# On Linux/Mac
./scripts/local/init-databases.sh
```

ClickHouse initialization happens automatically via the init script mounted in docker-compose.

### 4. Build Backend Services

```bash
cd backend/src/IncidentService
dotnet restore
dotnet build
dotnet run --urls "http://localhost:5001"
```

In another terminal:

```bash
cd backend/src/ApiGateway
dotnet restore
dotnet build
dotnet run --urls "http://localhost:5000"
```

### 5. Build and Run Frontend

```bash
cd frontend
pnpm install  # or npm install
pnpm dev      # or npm run dev
```

Frontend will be available at http://localhost:3000

## Testing the System

### Create an Incident

1. Open http://localhost:3000
2. Use the API directly:

```bash
curl -X POST http://localhost:5000/api/incidents \
  -H "Content-Type: application/json" \
  -H "x-tenant-id: 00000000-0000-0000-0000-000000000001" \
  -d '{
    "envId": "00000000-0000-0000-0000-000000000001",
    "severity": 2,
    "title": "Test Incident",
    "detectedAtUnixMs": 1700000000000
  }'
```

### Verify Data

Check PostgreSQL:

```bash
docker exec -it incidentscope-postgres psql -U incidentscope -d incidentscope -c "SELECT * FROM incidents;"
```

## Development Workflow

1. **Backend Changes**: Services auto-reload on file changes (use `dotnet watch run`)
2. **Frontend Changes**: Next.js hot-reloads automatically
3. **Database Migrations**: Add SQL scripts to `scripts/local/init/` and re-run init

## Troubleshooting

### Services Won't Start

- Check Docker is running: `docker ps`
- Check ports aren't in use: `netstat -an | findstr "5000 5001 5432 6379 8123 19092"` (Windows) or `netstat -an | grep -E '5000|5001|5432|6379|8123|19092'` (Linux/Mac)
- View logs: `docker-compose -f scripts/local/docker-compose.yml logs`

### Database Connection Issues

- Verify connection strings in `appsettings.json` match docker-compose
- Check PostgreSQL is ready: `docker exec incidentscope-postgres pg_isready`

### Frontend Can't Connect to API

- Verify API Gateway is running on port 5000
- Check CORS settings in `backend/src/ApiGateway/Program.cs`
- Verify Next.js rewrites in `frontend/next.config.js`

## Next Steps

- Implement OTel consumers (Phase 2)
- Add correlation engine (Phase 2)
- Set up CI/CD pipelines (Phase 4)
- Deploy to Kubernetes (Phase 4)

See [README.md](./README.md) for architecture overview.


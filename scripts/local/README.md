# Local Development Scripts

This directory contains scripts and configurations for local development.

## Files

- `docker-compose.yml` - Local infrastructure (Postgres, Redis, ClickHouse, Redpanda, OTel Collector)
- `init-databases.sh` - Database initialization helper
- `config/otel-collector-config.yaml` - OTel Collector configuration
- `init/postgres-init.sql` - PostgreSQL schema and seed data
- `init/clickhouse-init.sql` - ClickHouse schema and materialized views

## Usage

### Start Infrastructure

```bash
docker-compose -f scripts/local/docker-compose.yml up -d
```

### Stop Infrastructure

```bash
docker-compose -f scripts/local/docker-compose.yml down
```

### View Logs

```bash
docker-compose -f scripts/local/docker-compose.yml logs -f
```

### Initialize Databases

```bash
# Databases are auto-initialized on first start via init scripts
# To re-initialize, restart containers:
docker-compose -f scripts/local/docker-compose.yml restart postgres clickhouse
```

## Service URLs

- **PostgreSQL**: `localhost:5432`
- **Redis**: `localhost:6379`
- **ClickHouse HTTP**: `http://localhost:8123`
- **ClickHouse Native**: `localhost:9000`
- **Redpanda Kafka**: `localhost:19092`
- **OTel Collector gRPC**: `localhost:4317`
- **OTel Collector HTTP**: `localhost:4318`

## Connection Strings

### PostgreSQL
```
Host=localhost;Port=5432;Database=incidentscope;Username=incidentscope;Password=incidentscope-dev
```

### Redis
```
localhost:6379
```

### ClickHouse
```
Host=localhost;Port=9000;Database=incidentscope;Username=incidentscope;Password=incidentscope-dev
```

### Kafka/Redpanda
```
localhost:19092
```

## Troubleshooting

### Port Conflicts

If ports are already in use, modify `docker-compose.yml` to use different ports.

### Database Not Ready

Wait a few seconds after starting containers, then check:

```bash
docker exec incidentscope-postgres pg_isready
docker exec incidentscope-clickhouse clickhouse-client --query "SELECT 1"
```

### Reset Everything

```bash
docker-compose -f scripts/local/docker-compose.yml down -v
docker-compose -f scripts/local/docker-compose.yml up -d
```

This will delete all volumes and recreate them with fresh data.


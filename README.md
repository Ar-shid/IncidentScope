# IncidentScope

Production-ready observability and incident management platform with AI-powered root cause analysis.

## Architecture

IncidentScope is a microservices-based platform that ingests OpenTelemetry signals (logs, metrics, traces), correlates them with deployment events, and provides intelligent incident management with root cause hypotheses.

### Key Components

- **Control Plane**: Next.js UI, API Gateway (BFF), PostgreSQL (metadata), Redis (hot cache), RAG service
- **Data Plane**: OpenTelemetry Collector, Kafka/Redpanda, ClickHouse (telemetry storage), signal consumers
- **Intelligence**: Correlation engine, anomaly detection, SLO engine, AI/RAG assistant

See [docs/architecture/](./docs/architecture/) for detailed architecture documentation.

## Quick Start

### Prerequisites

- .NET 8 SDK
- Node.js 18+ and pnpm
- Docker and Docker Compose
- Terraform (for infrastructure)

### Local Development

1. Start infrastructure:
   ```bash
   docker-compose -f scripts/local/docker-compose.yml up -d
   ```

2. Initialize databases:
   ```bash
   ./scripts/local/init-databases.sh
   ```

3. Run backend services:
   ```bash
   cd backend/src/ApiGateway && dotnet run
   ```

4. Run frontend:
   ```bash
   cd frontend && pnpm install && pnpm dev
   ```

## Repository Structure

```
incidentscope/
  frontend/          # Next.js application
  backend/           # .NET 8 microservices
  infra/             # Terraform, Helm, ArgoCD
  proto/             # Protocol Buffer definitions
  docs/              # Architecture docs, ADRs, runbooks
  scripts/           # Local dev and deployment scripts
```

## Development Phases

- **Phase 1**: MVP foundations (incident service, API gateway, basic UI)
- **Phase 2**: Advanced correlation (ingestion pipeline, suspect hypotheses)
- **Phase 3**: AI intelligence (RAG, grounded assistant)
- **Phase 4**: Scale & hardening (multi-tenancy, reliability patterns)

## License

MIT

This project is intentionally released under the MIT License to maximize reuse, reviewability, and learning.  
IncidentScope is designed as a reference implementation of production-grade observability and incident management patterns, not as a commercial product.

The permissive license allows engineers and teams to study, fork, and adapt the architecture and techniques without legal friction.



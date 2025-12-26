# IncidentScope

Production-ready observability and incident management platform with AI-powered root cause analysis.

## Tech Stack

### Frontend
- **Next.js 14** - React framework with App Router
- **TypeScript** - Type-safe development
- **TanStack Query** - Data fetching, caching, and synchronization
- **Tailwind CSS** - Utility-first CSS framework
- **Recharts** - Charting library for visualizations

### Backend
- **.NET 8** - Modern C# runtime and framework
- **Minimal APIs** - Lightweight HTTP endpoints
- **gRPC** - High-performance inter-service communication
- **Protocol Buffers** - Schema-first API contracts

### Data Storage
- **PostgreSQL 16** - Relational database for metadata (incidents, services, SLOs)
- **ClickHouse** - Columnar OLAP database for high-volume telemetry (logs, metrics, traces)
- **Redis 7** - In-memory cache for hot data and correlation results
- **pgvector** - Vector similarity search for RAG embeddings

### Messaging & Streaming
- **Kafka/Redpanda** - Distributed event streaming platform
- **OpenTelemetry Collector** - Unified telemetry collection and routing

### Observability
- **OpenTelemetry** - Vendor-neutral observability standard
- **OTLP** - OpenTelemetry Protocol for signal export
- **Prometheus** - Metrics collection (dogfooding)
- **Grafana** - Visualization and dashboards (dogfooding)
- **Tempo/Jaeger** - Distributed tracing (dogfooding)
- **Loki/ELK** - Log aggregation (dogfooding)

### Infrastructure & DevOps
- **Docker & Docker Compose** - Containerization and local development
- **Kubernetes** - Container orchestration (AKS/EKS)
- **Helm** - Kubernetes package management
- **Terraform** - Infrastructure as Code
- **ArgoCD** - GitOps continuous delivery
- **GitHub Actions** - CI/CD pipelines

### AI/ML
- **RAG (Retrieval-Augmented Generation)** - Grounded AI responses with citations
- **Vector Embeddings** - Semantic search over runbooks and postmortems
- **Lightweight Ranking Models** - Hypothesis prioritization

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

- **Phase 1**: ✅ **COMPLETE** - MVP foundations (incident service, API gateway, basic UI)
  - ✅ Incident Service with PostgreSQL integration
  - ✅ API Gateway (BFF) with tenant isolation
  - ✅ Next.js frontend with incident list and detail pages
  - ✅ OpenTelemetry instrumentation
  - ✅ Database schemas (PostgreSQL + ClickHouse)
  - ✅ Local development environment (Docker Compose)
  
- **Phase 2**: 🚧 **IN PROGRESS** - Advanced correlation (ingestion pipeline, suspect hypotheses)
  - [ ] OTel consumers (logs, metrics, traces)
  - [ ] Correlation engine (suspect services, hypotheses)
  - [ ] Deploy events service
  - [ ] Evidence explorer UI
  
- **Phase 3**: ⏳ **PLANNED** - AI intelligence (RAG, grounded assistant)
  - [ ] RAG service with vector embeddings
  - [ ] Incident assistant with citations
  - [ ] Runbook/postmortem indexing
  
- **Phase 4**: ⏳ **PLANNED** - Scale & hardening (multi-tenancy, reliability patterns)
  - [ ] Remaining services (Catalog, SLO, Alert Router, Anomaly)
  - [ ] Infrastructure as Code (Terraform, Helm, ArgoCD)
  - [ ] Enhanced CI/CD pipelines
  - [ ] Load testing and chaos engineering

**Current Status**: Phase 1 complete. Working on Phase 2 - implementing telemetry ingestion and correlation engine.

## License

MIT

This project is intentionally released under the MIT License to maximize reuse, reviewability, and learning.  
IncidentScope is designed as a reference implementation of production-grade observability and incident management patterns, not as a commercial product.

The permissive license allows engineers and teams to study, fork, and adapt the architecture and techniques without legal friction.



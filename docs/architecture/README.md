# IncidentScope Architecture

This directory contains architecture documentation, ADRs (Architecture Decision Records), and system diagrams.

## Structure

- `decisions/` - Architecture Decision Records (ADRs)
- `diagrams/` - System architecture diagrams (Mermaid, PlantUML, etc.)

## Key Architecture Decisions

See individual ADR files for detailed decisions on:
- Data partitioning strategy
- Tenant isolation model
- Deduplication approach
- SLO burn-rate policies
- Correlation window definitions

## System Overview

IncidentScope is a microservices-based observability platform that:

1. **Ingests** OpenTelemetry signals (logs, metrics, traces) via OTel Collector → Kafka/Redpanda
2. **Stores** telemetry in ClickHouse, metadata in PostgreSQL
3. **Correlates** signals with deployment events to generate root cause hypotheses
4. **Manages** incident lifecycle with AI-powered assistance via RAG

See the main [README.md](../../README.md) for more details.


# ADR-002: Data Partitioning Strategy

## Status
Accepted

## Context
We need to partition high-volume telemetry data (logs, metrics, traces) in ClickHouse for performance and retention management.

## Decision
Partition ClickHouse tables by `(toDate(ts), tenant_id)` using MergeTree engine with TTL-based retention.

## Rationale

### Partition Key: `(toDate(ts), tenant_id)`
- **Time-based**: Enables efficient time-range queries and TTL cleanup
- **Tenant isolation**: Keeps tenant data physically separated in partitions
- **Query performance**: Most queries filter by time range and tenant_id

### Retention Strategy
- **Logs**: 14 days raw, optionally 30 days for sampled/compacted
- **Traces**: 7 days raw spans, 30 days for rollups (latency p95, error rate)
- **Metrics**: 30 days raw points, 90 days for 5-minute rollups

### Ordering Key
- `(tenant_id, service_name, ts, ingest_id)` for logs
- `(tenant_id, service_name, ts, trace_id, span_id)` for spans
- Optimizes queries that filter by tenant + service + time range

## Consequences
- **Positive**: Efficient time-range queries, automatic cleanup, tenant isolation
- **Negative**: Large number of partitions if many tenants; requires monitoring partition count
- **Mitigation**: Monitor partition count, consider monthly partitions for low-volume tenants

## References
- See `scripts/local/init/clickhouse-init.sql` for schema definitions


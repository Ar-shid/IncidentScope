# ADR-001: Tenant Isolation Model

## Status
Accepted

## Context
IncidentScope needs to support multi-tenancy. We must choose between namespace-per-tenant isolation vs shared namespace with data-level isolation.

## Decision
We will implement **Option B: Shared namespace with data-level isolation** for the MVP and portfolio demonstration, while documenting Option A as an alternative for stronger isolation requirements.

## Rationale

### Option B Benefits
- **Operational simplicity**: Single set of services to manage
- **Resource efficiency**: Better utilization of cluster resources
- **Scalability**: Can support hundreds of tenants without namespace explosion
- **Cost-effective**: Lower operational overhead

### Implementation Requirements
- All tables include `tenant_id` as first column in primary/composite keys
- All queries MUST filter by `tenant_id` at application layer
- ClickHouse partitions by `tenant_id` for query isolation
- Redis keys prefixed with `is:{tenant}:{env}:...`
- API Gateway enforces tenant context via `x-tenant-id` header
- Services validate tenant_id on every request

### Option A (Documented Alternative)
- Namespace-per-tenant provides stronger blast radius isolation
- Suitable for enterprise customers requiring strict compliance
- Higher operational complexity (namespaces, RBAC, resource quotas per tenant)

## Consequences
- **Positive**: Faster development, easier operations, better resource utilization
- **Negative**: Requires strict enforcement of tenant_id filtering; potential for cross-tenant data leaks if bugs exist
- **Mitigation**: Automated tests for cross-tenant access prevention, code review focus on tenant_id usage

## References
- See `backend/src/Shared/Security/Tenant/` for implementation
- See `scripts/local/init/postgres-init.sql` for schema patterns


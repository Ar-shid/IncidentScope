-- IncidentScope PostgreSQL Schema

-- Tenants
CREATE TABLE IF NOT EXISTS tenants (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Services
CREATE TABLE IF NOT EXISTS services (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  name TEXT NOT NULL,
  namespace TEXT,
  team TEXT,
  criticality INT NOT NULL DEFAULT 3,
  tags JSONB NOT NULL DEFAULT '{}'::jsonb,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (tenant_id, name)
);

CREATE INDEX idx_services_tenant ON services(tenant_id);

-- Environments
CREATE TABLE IF NOT EXISTS environments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  name TEXT NOT NULL,
  cluster TEXT NOT NULL,
  region TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (tenant_id, name)
);

CREATE INDEX idx_environments_tenant ON environments(tenant_id);

-- Deploy Events
CREATE TABLE IF NOT EXISTS deploy_events (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  service_id UUID NOT NULL REFERENCES services(id),
  env_id UUID NOT NULL REFERENCES environments(id),
  version TEXT NOT NULL,
  commit_sha TEXT,
  status TEXT NOT NULL, -- started/succeeded/failed
  started_at TIMESTAMPTZ NOT NULL,
  finished_at TIMESTAMPTZ,
  actor TEXT,
  metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_deploy_events_tenant_service_env ON deploy_events(tenant_id, service_id, env_id, started_at DESC);
CREATE INDEX idx_deploy_events_time ON deploy_events(started_at DESC);

-- Incidents
CREATE TABLE IF NOT EXISTS incidents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  env_id UUID NOT NULL REFERENCES environments(id),
  primary_service_id UUID REFERENCES services(id),
  severity INT NOT NULL, -- 1..4
  status TEXT NOT NULL,  -- open/mitigating/resolved
  title TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  detected_at TIMESTAMPTZ,
  resolved_at TIMESTAMPTZ,
  created_by TEXT,
  assignee TEXT,
  labels JSONB NOT NULL DEFAULT '{}'::jsonb
);

CREATE INDEX idx_incidents_tenant_status ON incidents(tenant_id, status, created_at DESC);
CREATE INDEX idx_incidents_tenant_env ON incidents(tenant_id, env_id, created_at DESC);

-- Incident Events
CREATE TABLE IF NOT EXISTS incident_events (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  incident_id UUID NOT NULL REFERENCES incidents(id),
  type TEXT NOT NULL, -- detection/anomaly/deploy_correlated/hypothesis/mitigation/resolution/postmortem
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  payload JSONB NOT NULL
);

CREATE INDEX idx_incident_events_incident ON incident_events(tenant_id, incident_id, created_at);

-- Incident Hypotheses
CREATE TABLE IF NOT EXISTS incident_hypotheses (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  incident_id UUID NOT NULL REFERENCES incidents(id),
  rank INT NOT NULL,
  hypothesis_type TEXT NOT NULL, -- deploy_regression/error_signature/dependency_down/latency_spike
  summary TEXT NOT NULL,
  confidence REAL NOT NULL, -- 0..1
  evidence JSONB NOT NULL,  -- pointers to CH queries, logs, spans, deploys
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_incident_hypotheses_incident ON incident_hypotheses(tenant_id, incident_id, rank);

-- SLOs
CREATE TABLE IF NOT EXISTS slos (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  service_id UUID NOT NULL REFERENCES services(id),
  env_id UUID NOT NULL REFERENCES environments(id),
  name TEXT NOT NULL,
  sli_type TEXT NOT NULL,          -- availability/latency/error_rate
  objective REAL NOT NULL,         -- e.g. 0.999
  window_minutes INT NOT NULL,     -- e.g. 43200 for 30d
  query_spec JSONB NOT NULL,       -- how to compute SLI from ClickHouse
  alert_policies JSONB NOT NULL,   -- burn-rate thresholds
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_slos_tenant_service ON slos(tenant_id, service_id, env_id);

-- SLO Snapshots
CREATE TABLE IF NOT EXISTS slo_snapshots (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  slo_id UUID NOT NULL REFERENCES slos(id),
  computed_at TIMESTAMPTZ NOT NULL,
  error_budget_remaining REAL NOT NULL,
  burn_rate_1h REAL NOT NULL,
  burn_rate_6h REAL NOT NULL,
  details JSONB NOT NULL
);

CREATE INDEX idx_slo_snapshots_slo ON slo_snapshots(tenant_id, slo_id, computed_at DESC);

-- Outbox Pattern (for exactly-once event publishing)
CREATE TABLE IF NOT EXISTS outbox_messages (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  occurred_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  type TEXT NOT NULL,
  payload JSONB NOT NULL,
  dispatched_at TIMESTAMPTZ
);

CREATE INDEX idx_outbox_dispatched ON outbox_messages(dispatched_at, occurred_at) WHERE dispatched_at IS NULL;

-- Postmortems
CREATE TABLE IF NOT EXISTS postmortems (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  incident_id UUID NOT NULL REFERENCES incidents(id),
  summary TEXT NOT NULL,
  root_cause TEXT,
  action_items JSONB NOT NULL DEFAULT '[]'::jsonb,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_postmortems_incident ON postmortems(tenant_id, incident_id);

-- RAG Documents (with pgvector extension)
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS rag_documents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  type TEXT NOT NULL, -- runbook/postmortem/playbook
  title TEXT NOT NULL,
  source_uri TEXT,
  content_hash TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_rag_documents_tenant ON rag_documents(tenant_id, type);

-- RAG Embeddings
CREATE TABLE IF NOT EXISTS rag_embeddings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  doc_id UUID NOT NULL REFERENCES rag_documents(id) ON DELETE CASCADE,
  chunk_id INT NOT NULL,
  chunk_text TEXT NOT NULL,
  embedding vector(1536), -- OpenAI ada-002 dimension
  metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (doc_id, chunk_id)
);

CREATE INDEX idx_rag_embeddings_vector ON rag_embeddings USING ivfflat (embedding vector_cosine_ops);

-- Seed default tenant for development
INSERT INTO tenants (id, name) VALUES ('00000000-0000-0000-0000-000000000001', 'Default Tenant')
ON CONFLICT (id) DO NOTHING;


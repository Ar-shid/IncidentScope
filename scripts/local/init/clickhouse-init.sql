-- IncidentScope ClickHouse Schema

-- Logs table
CREATE TABLE IF NOT EXISTS logs_raw
(
  tenant_id           UUID,
  ts                  DateTime64(9),
  service_name        LowCardinality(String),
  env                 LowCardinality(String),
  severity_text       LowCardinality(String),
  severity_number     UInt8,
  trace_id            FixedString(32),
  span_id             FixedString(16),
  body                String,
  http_method         LowCardinality(String),
  http_route          String,
  http_status_code     UInt16,
  exception_type      LowCardinality(String),
  attributes          Map(String, String),
  ingest_id           String,
  received_at         DateTime64(9)
)
ENGINE = MergeTree
PARTITION BY (toDate(ts), tenant_id)
ORDER BY (tenant_id, service_name, ts, ingest_id)
TTL ts + INTERVAL 14 DAY
SETTINGS index_granularity = 8192;

-- Spans table
CREATE TABLE IF NOT EXISTS spans_raw
(
  tenant_id           UUID,
  ts                  DateTime64(9),
  end_ts              DateTime64(9),
  duration_ms         UInt32,
  trace_id            FixedString(32),
  span_id             FixedString(16),
  parent_span_id      FixedString(16),
  service_name        LowCardinality(String),
  env                 LowCardinality(String),
  span_name           String,
  span_kind           LowCardinality(String),
  status_code         LowCardinality(String),
  status_message      String,
  http_method         LowCardinality(String),
  http_route          String,
  http_status_code    UInt16,
  attributes          Map(String, String),
  ingest_id           String,
  received_at         DateTime64(9)
)
ENGINE = MergeTree
PARTITION BY (toDate(ts), tenant_id)
ORDER BY (tenant_id, service_name, ts, trace_id, span_id)
TTL ts + INTERVAL 7 DAY
SETTINGS index_granularity = 8192;

-- Metrics timeseries table
CREATE TABLE IF NOT EXISTS metrics_timeseries
(
  tenant_id           UUID,
  ts                  DateTime64(3),
  service_name        LowCardinality(String),
  env                 LowCardinality(String),
  metric_name         LowCardinality(String),
  metric_type         LowCardinality(String),
  value               Float64,
  attributes          Map(String, String),
  ingest_id           String,
  received_at         DateTime64(3)
)
ENGINE = MergeTree
PARTITION BY (toDate(ts), tenant_id)
ORDER BY (tenant_id, metric_name, service_name, ts)
TTL ts + INTERVAL 30 DAY
SETTINGS index_granularity = 8192;

-- Materialized view: Latency p95 rollup (per service, per 1m)
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_latency_p95_1m
ENGINE = SummingMergeTree
PARTITION BY (toDate(ts), tenant_id)
ORDER BY (tenant_id, service_name, env, toStartOfMinute(ts))
AS
SELECT
  tenant_id,
  service_name,
  env,
  toStartOfMinute(ts) AS ts,
  quantileTDigest(0.95)(duration_ms) AS p95_ms
FROM spans_raw
WHERE span_kind = 'SERVER'
GROUP BY tenant_id, service_name, env, ts;

-- Materialized view: Error rate rollup (per service, per 1m)
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_error_rate_1m
ENGINE = SummingMergeTree
PARTITION BY (toDate(ts), tenant_id)
ORDER BY (tenant_id, service_name, env, toStartOfMinute(ts))
AS
SELECT
  tenant_id,
  service_name,
  env,
  toStartOfMinute(ts) AS ts,
  count() AS total,
  countIf(status_code = 'ERROR' OR http_status_code >= 500) AS errors
FROM spans_raw
WHERE span_kind = 'SERVER'
GROUP BY tenant_id, service_name, env, ts;

-- Service edges (dependency map)
CREATE TABLE IF NOT EXISTS service_edges_1m
(
  tenant_id UUID,
  ts DateTime,
  env LowCardinality(String),
  caller LowCardinality(String),
  callee LowCardinality(String),
  calls UInt64,
  errors UInt64,
  p95_ms UInt32
)
ENGINE = SummingMergeTree
PARTITION BY (toDate(ts), tenant_id)
ORDER BY (tenant_id, env, caller, callee, ts)
TTL ts + INTERVAL 30 DAY
SETTINGS index_granularity = 8192;


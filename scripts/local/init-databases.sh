#!/bin/bash

set -e

echo "Initializing databases..."

# Wait for Postgres
until docker exec incidentscope-postgres pg_isready -U incidentscope > /dev/null 2>&1; do
  echo "Waiting for Postgres..."
  sleep 2
done

# Wait for ClickHouse
until docker exec incidentscope-clickhouse clickhouse-client --query "SELECT 1" > /dev/null 2>&1; do
  echo "Waiting for ClickHouse..."
  sleep 2
done

echo "Databases are ready!"

# Run migrations if needed
# docker exec incidentscope-postgres psql -U incidentscope -d incidentscope -f /docker-entrypoint-initdb.d/init.sql

echo "Database initialization complete."


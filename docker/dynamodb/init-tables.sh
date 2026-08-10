#!/bin/bash
set -eu

ENDPOINT_URL="http://db:8000"
REGION="us-east-1"
TABLES_DIR="/dynamodb/tables"
SEED_DIR="/dynamodb/seed"

echo "Waiting for DynamoDB Local..."
until aws dynamodb list-tables --endpoint-url "$ENDPOINT_URL" --region "$REGION" >/dev/null 2>&1; do
  sleep 1
done

for file in "$TABLES_DIR"/*.yaml; do
  [ -e "$file" ] || continue
  table_name=$(basename "$file" .yaml)
  existing=$(aws dynamodb list-tables --endpoint-url "$ENDPOINT_URL" --region "$REGION" --output text --query "TableNames")

  if echo "$existing" | grep -qw "$table_name"; then
    echo "Table '$table_name' already exists. Skipping."
    continue
  fi

  echo "Creating table '$table_name'..."
  aws dynamodb create-table --cli-input-yaml "file://$file" --endpoint-url "$ENDPOINT_URL" --region "$REGION" >/dev/null
  echo "Table '$table_name' created."
done

echo "All tables ready."

for file in "$SEED_DIR"/*.yaml; do
  [ -e "$file" ] || continue
  echo "Seeding '$file'..."
  aws dynamodb batch-write-item --cli-input-yaml "file://$file" --endpoint-url "$ENDPOINT_URL" --region "$REGION" >/dev/null
done

echo "Seed data ready."

EF Migration guidance: AddAuditMetadata

This repository may not have dotnet-ef CLI available in the current environment.
If you have a local dev environment with dotnet SDK and EF tools, generate a proper EF Core migration with:

  cd milk-api-manager-system/backend/MilkApiManager
  dotnet tool restore
  dotnet ef migrations add AddAuditMetadata
  dotnet ef migrations script -o ./Migrations/20260214_AddAuditMetadata.sql

If you cannot run dotnet-ef here, apply the provided SQL file directly to PostgreSQL 17:

  psql -d mydb -f ./Migrations/20260214_AddAuditMetadata.sql

Rollback example (psql):
  -- run the DROP COLUMN lines from the same SQL file or keep separate rollback script.

Notes:
- The SQL adds nullable columns to avoid blocking existing inserts. If you prefer non-nullable, add default values and then backfill.
- After applying migration, run application smoke tests to confirm AuditLog writing works and that indexes are usable.

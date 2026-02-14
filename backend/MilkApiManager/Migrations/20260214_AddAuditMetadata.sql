-- Migration: AddAuditMetadata (2026-02-14)
-- Adds CorrelationId, OperatorIp, RequestId to AuditLogEntries table

BEGIN;

ALTER TABLE "AuditLogEntries"
  ADD COLUMN IF NOT EXISTS "CorrelationId" varchar(128);

ALTER TABLE "AuditLogEntries"
  ADD COLUMN IF NOT EXISTS "RequestId" varchar(128);

ALTER TABLE "AuditLogEntries"
  ADD COLUMN IF NOT EXISTS "OperatorIp" varchar(64);

-- Optional: add index to CorrelationId and RequestId for faster lookups
CREATE INDEX IF NOT EXISTS IX_AuditLogEntries_CorrelationId ON "AuditLogEntries" ("CorrelationId");
CREATE INDEX IF NOT EXISTS IX_AuditLogEntries_RequestId ON "AuditLogEntries" ("RequestId");

COMMIT;

-- Rollback (if needed):
-- ALTER TABLE "AuditLogEntries" DROP COLUMN IF EXISTS "CorrelationId";
-- ALTER TABLE "AuditLogEntries" DROP COLUMN IF EXISTS "RequestId";
-- ALTER TABLE "AuditLogEntries" DROP COLUMN IF EXISTS "OperatorIp";

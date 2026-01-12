-- Platform Database Initialization Script
-- Creates schemas for each bounded context

-- IAM Schema
CREATE SCHEMA IF NOT EXISTS iam_schema;

-- Application Schema
CREATE SCHEMA IF NOT EXISTS application_schema;

-- Payment Schema
CREATE SCHEMA IF NOT EXISTS payment_schema;

-- Ledger Schema
CREATE SCHEMA IF NOT EXISTS ledger_schema;

-- Audit Schema
CREATE SCHEMA IF NOT EXISTS audit_schema;

-- Document Schema
CREATE SCHEMA IF NOT EXISTS document_schema;

-- Notification Schema
CREATE SCHEMA IF NOT EXISTS notification_schema;

-- Reference Data Schema
CREATE SCHEMA IF NOT EXISTS reference_schema;

-- Grant permissions to platform user
GRANT ALL PRIVILEGES ON SCHEMA iam_schema TO platform;
GRANT ALL PRIVILEGES ON SCHEMA application_schema TO platform;
GRANT ALL PRIVILEGES ON SCHEMA payment_schema TO platform;
GRANT ALL PRIVILEGES ON SCHEMA ledger_schema TO platform;
GRANT ALL PRIVILEGES ON SCHEMA audit_schema TO platform;
GRANT ALL PRIVILEGES ON SCHEMA document_schema TO platform;
GRANT ALL PRIVILEGES ON SCHEMA notification_schema TO platform;
GRANT ALL PRIVILEGES ON SCHEMA reference_schema TO platform;

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create audit trigger function for created_at/updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

COMMENT ON SCHEMA iam_schema IS 'Identity and Access Management - users, roles, sessions, policies';
COMMENT ON SCHEMA application_schema IS 'Application Case Management - cases, tasks, workflows, attachments';
COMMENT ON SCHEMA payment_schema IS 'Payment Processing - intents, transactions, bank callbacks';
COMMENT ON SCHEMA ledger_schema IS 'Financial Ledger - immutable entries, reversals, hash chain';
COMMENT ON SCHEMA audit_schema IS 'Audit Trail - immutable events, hash chain, WORM export';
COMMENT ON SCHEMA document_schema IS 'Document Management - templates, PDFs, archives';
COMMENT ON SCHEMA notification_schema IS 'Notification System - SMS, email, delivery tracking';
COMMENT ON SCHEMA reference_schema IS 'Reference Data - static lookups, organization codes';

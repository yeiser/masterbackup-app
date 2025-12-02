-- SQL Script to reset tenant database schema
-- Database: tenant_ffa0a0030206433bb0fd0a2ef0ae0f2b
-- This will drop and recreate the public schema, removing all tables

-- Drop the public schema (this will drop all tables, views, etc.)
DROP SCHEMA IF EXISTS public CASCADE;

-- Recreate the public schema
CREATE SCHEMA public;

-- Grant permissions to the database user
GRANT ALL ON SCHEMA public TO siscolsi;
GRANT ALL ON SCHEMA public TO PUBLIC;

-- Verify schema exists
SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'public';

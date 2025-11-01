-- Create database
--CREATE DATABASE grsdb;

\c grsdb;

CREATE SCHEMA IF NOT EXISTS grsdb;

CREATE ROLE grs_admin_user LOGIN PASSWORD 'ChangeThisAdminPassword1234';
CREATE ROLE grs_app_user LOGIN PASSWORD 'ChangeThisUserPassword1234';

-- Grant grs_admin_user privileges: can manage schemas/tables but no data manipulation
GRANT CREATE ON DATABASE grsdb TO grs_admin_user;
ALTER SCHEMA grsdb OWNER TO grs_admin_user;
GRANT ALL ON SCHEMA grsdb TO grs_admin_user;
REVOKE ALL ON ALL TABLES IN SCHEMA grsdb FROM grs_admin_user;

-- Grant grs_app_user privileges: data manipulation only
GRANT CONNECT ON DATABASE grsdb TO grs_app_user;
GRANT USAGE ON SCHEMA grsdb TO grs_app_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA grsdb TO grs_app_user;

SET ROLE grs_admin_user;

-- Future tables: automatic privileges for grs_app_user
ALTER DEFAULT PRIVILEGES FOR ROLE grs_admin_user IN SCHEMA grsdb
   GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO grs_app_user;

-- Future sequences : automatic privileges for grs_app_user
-- Useful if you have serial/identity columns
ALTER DEFAULT PRIVILEGES IN SCHEMA grsdb
GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO grs_app_user;

RESET ROLE;
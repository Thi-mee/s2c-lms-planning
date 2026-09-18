-- Synthetic, loopback-only local development credentials; never use for customer deployments.
CREATE ROLE variable_migrator LOGIN PASSWORD 'variable-local-migrator-only'
    NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS;
CREATE ROLE variable_runtime LOGIN PASSWORD 'variable-local-runtime-only'
    NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOBYPASSRLS;
GRANT CREATE ON DATABASE variable_lms TO variable_migrator;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;

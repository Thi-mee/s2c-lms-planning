CREATE SCHEMA licensing;

CREATE TABLE licensing.license_documents (
    organization_id uuid NOT NULL,
    revision bigint NOT NULL CHECK (revision > 0),
    license_id varchar(200) NOT NULL,
    schema_version integer NOT NULL,
    key_id varchar(100) NOT NULL,
    issued_at timestamptz NOT NULL,
    not_before timestamptz NOT NULL,
    expires_at timestamptz NOT NULL,
    max_active_learners integer NOT NULL CHECK (max_active_learners >= 0),
    compact_jws text NOT NULL,
    document_hash varchar(64) NOT NULL,
    loaded_by uuid NOT NULL,
    loaded_at timestamptz NOT NULL,
    PRIMARY KEY (organization_id, revision),
    UNIQUE (organization_id, document_hash),
    FOREIGN KEY (organization_id, loaded_by) REFERENCES identity.users(organization_id, id),
    CHECK (not_before < expires_at)
);

CREATE TABLE licensing.license_state (
    organization_id uuid PRIMARY KEY REFERENCES identity.organizations(id),
    current_revision bigint NOT NULL CHECK (current_revision > 0),
    FOREIGN KEY (organization_id, current_revision)
        REFERENCES licensing.license_documents(organization_id, revision)
);

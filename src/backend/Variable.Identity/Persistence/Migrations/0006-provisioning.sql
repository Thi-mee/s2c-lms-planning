CREATE TABLE identity.invitations (
    organization_id uuid NOT NULL,
    user_id uuid NOT NULL,
    token_hash varchar(64) NOT NULL,
    expires_at timestamptz NOT NULL,
    used_at timestamptz,
    version integer NOT NULL CHECK (version > 0),
    issued_by uuid NOT NULL,
    request_id uuid NOT NULL,
    request_hash varchar(64) NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    PRIMARY KEY (organization_id, user_id),
    UNIQUE (token_hash),
    FOREIGN KEY (organization_id, user_id) REFERENCES identity.users(organization_id, id),
    FOREIGN KEY (organization_id, issued_by) REFERENCES identity.users(organization_id, id)
);
CREATE INDEX invitations_expiry ON identity.invitations(organization_id, expires_at) WHERE used_at IS NULL;

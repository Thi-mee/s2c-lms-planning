CREATE SCHEMA identity;

CREATE TABLE identity.organizations (
    id uuid PRIMARY KEY,
    name varchar(200) NOT NULL CHECK (length(trim(name)) > 0),
    created_at timestamptz NOT NULL
);

CREATE TABLE identity.installation (
    singleton boolean PRIMARY KEY DEFAULT true CHECK (singleton),
    organization_id uuid NOT NULL UNIQUE REFERENCES identity.organizations(id),
    bootstrapped_at timestamptz NOT NULL
);

CREATE TABLE identity.users (
    id uuid PRIMARY KEY,
    organization_id uuid NOT NULL REFERENCES identity.organizations(id),
    name varchar(200) NOT NULL,
    email varchar(320) NOT NULL,
    normalized_email varchar(320) NOT NULL,
    password_hash text NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('pending', 'active', 'deactivated')),
    roles text[] NOT NULL CHECK (roles <@ ARRAY['Learner','CourseAuthor','CohortCoordinator','LearningFacilitator','OrganizationManager','Administrator']::text[] AND array_position(roles, NULL) IS NULL),
    security_version bigint NOT NULL CHECK (security_version > 0),
    failed_access_count integer NOT NULL DEFAULT 0 CHECK (failed_access_count >= 0),
    lockout_end timestamptz,
    deleted_at timestamptz,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    UNIQUE (organization_id, id),
    UNIQUE (organization_id, normalized_email)
);

CREATE TABLE identity.sessions (
    key_hash varchar(64) PRIMARY KEY,
    organization_id uuid NOT NULL,
    user_id uuid NOT NULL,
    security_version bigint NOT NULL,
    ticket bytea NOT NULL,
    expires_at timestamptz NOT NULL,
    FOREIGN KEY (organization_id, user_id) REFERENCES identity.users(organization_id, id)
);
CREATE INDEX sessions_user ON identity.sessions(organization_id, user_id);
CREATE INDEX sessions_expiry ON identity.sessions(expires_at);

CREATE TABLE identity.audit_log (
    id uuid PRIMARY KEY,
    organization_id uuid NOT NULL REFERENCES identity.organizations(id),
    actor_user_id uuid,
    actor_kind varchar(20) NOT NULL CHECK (actor_kind IN ('user', 'system', 'operator')),
    action varchar(100) NOT NULL,
    target_id uuid NOT NULL,
    metadata jsonb NOT NULL,
    created_at timestamptz NOT NULL,
    FOREIGN KEY (organization_id, actor_user_id) REFERENCES identity.users(organization_id, id)
);
CREATE INDEX audit_organization_time ON identity.audit_log(organization_id, created_at);

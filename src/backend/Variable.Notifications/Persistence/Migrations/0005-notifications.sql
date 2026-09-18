CREATE SCHEMA notifications;

CREATE TABLE notifications.email_intents (
    id uuid PRIMARY KEY,
    organization_id uuid NOT NULL REFERENCES identity.organizations(id),
    recipient_user_id uuid NOT NULL,
    event_type varchar(40) NOT NULL CHECK (event_type IN ('invitation')),
    recipient_email varchar(320) NOT NULL,
    protected_payload text NOT NULL,
    dedupe_key varchar(300) NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('pending', 'processing', 'sent', 'failed')),
    attempts integer NOT NULL DEFAULT 0 CHECK (attempts >= 0),
    next_attempt_at timestamptz NOT NULL,
    lease_until timestamptz,
    lease_token uuid,
    last_error_code varchar(100),
    created_at timestamptz NOT NULL,
    sent_at timestamptz,
    UNIQUE (organization_id, dedupe_key),
    FOREIGN KEY (organization_id, recipient_user_id) REFERENCES identity.users(organization_id, id)
);
CREATE INDEX email_intents_due ON notifications.email_intents(status, next_attempt_at, created_at);

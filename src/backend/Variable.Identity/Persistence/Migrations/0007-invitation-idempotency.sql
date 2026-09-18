ALTER TABLE identity.invitations
    ADD CONSTRAINT invitations_request_id UNIQUE (organization_id, request_id);

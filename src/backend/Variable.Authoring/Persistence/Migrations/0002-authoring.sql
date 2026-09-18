CREATE SCHEMA authoring;

CREATE TABLE authoring.courses (
    organization_id uuid NOT NULL REFERENCES identity.organizations(id),
    id uuid NOT NULL,
    title varchar(200) NOT NULL CHECK (length(trim(title)) > 0),
    description varchar(4000) NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('draft', 'published', 'archived')),
    revision bigint NOT NULL DEFAULT 1 CHECK (revision > 0),
    requirements_version bigint NOT NULL DEFAULT 1 CHECK (requirements_version > 0),
    created_by uuid NOT NULL,
    create_hash varchar(64) NOT NULL,
    last_save_hash varchar(64),
    last_save_revision bigint,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    published_at timestamptz,
    PRIMARY KEY (organization_id, id),
    FOREIGN KEY (organization_id, created_by) REFERENCES identity.users(organization_id, id)
);

CREATE TABLE authoring.course_authors (
    id uuid NOT NULL UNIQUE,
    organization_id uuid NOT NULL,
    course_id uuid NOT NULL,
    user_id uuid NOT NULL,
    author_role text NOT NULL DEFAULT 'owner' CHECK (author_role = 'owner'),
    created_at timestamptz NOT NULL,
    PRIMARY KEY (organization_id, course_id),
    FOREIGN KEY (organization_id, course_id) REFERENCES authoring.courses(organization_id, id),
    FOREIGN KEY (organization_id, user_id) REFERENCES identity.users(organization_id, id)
);
-- A committed Course must have exactly one grant; inserting both is one local transaction.
ALTER TABLE authoring.courses ADD CONSTRAINT courses_require_owner
    FOREIGN KEY (organization_id, id) REFERENCES authoring.course_authors(organization_id, course_id)
    DEFERRABLE INITIALLY DEFERRED;
CREATE INDEX course_authors_user ON authoring.course_authors(organization_id, user_id, course_id);
CREATE INDEX courses_listing ON authoring.courses(organization_id, created_at DESC, id);

CREATE TABLE authoring.modules (
    organization_id uuid NOT NULL,
    course_id uuid NOT NULL,
    id uuid NOT NULL,
    title varchar(200) NOT NULL CHECK (length(trim(title)) > 0),
    position integer NOT NULL CHECK (position >= 0),
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    PRIMARY KEY (organization_id, id),
    UNIQUE (organization_id, course_id, id),
    UNIQUE (organization_id, course_id, position) DEFERRABLE INITIALLY DEFERRED,
    FOREIGN KEY (organization_id, course_id) REFERENCES authoring.courses(organization_id, id)
);

CREATE TABLE authoring.lessons (
    organization_id uuid NOT NULL,
    course_id uuid NOT NULL,
    module_id uuid NOT NULL,
    id uuid NOT NULL,
    title varchar(200) NOT NULL CHECK (length(trim(title)) > 0),
    notes text NOT NULL CHECK (length(notes) <= 50000),
    notes_format text NOT NULL DEFAULT 'plain_text' CHECK (notes_format = 'plain_text'),
    required boolean NOT NULL,
    content_version bigint NOT NULL DEFAULT 1 CHECK (content_version > 0),
    position integer NOT NULL CHECK (position >= 0),
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    PRIMARY KEY (organization_id, id),
    UNIQUE (organization_id, module_id, position) DEFERRABLE INITIALLY DEFERRED,
    FOREIGN KEY (organization_id, course_id, module_id) REFERENCES authoring.modules(organization_id, course_id, id)
);

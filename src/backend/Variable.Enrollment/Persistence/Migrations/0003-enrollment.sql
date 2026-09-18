CREATE SCHEMA enrollment;

CREATE TABLE enrollment.cohorts (
    organization_id uuid NOT NULL,
    id uuid NOT NULL,
    course_id uuid NOT NULL,
    title varchar(200) NOT NULL CHECK (length(trim(title)) > 0),
    start_at timestamptz NOT NULL,
    end_at timestamptz NOT NULL,
    revision bigint NOT NULL DEFAULT 1 CHECK (revision > 0),
    created_by uuid NOT NULL,
    create_hash varchar(64) NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    PRIMARY KEY (organization_id, id),
    CHECK (end_at >= start_at),
    FOREIGN KEY (organization_id, course_id) REFERENCES authoring.courses(organization_id, id),
    FOREIGN KEY (organization_id, created_by) REFERENCES identity.users(organization_id, id)
);
CREATE INDEX cohorts_listing ON enrollment.cohorts(organization_id, created_at DESC, id);
CREATE INDEX cohorts_schedule ON enrollment.cohorts(organization_id, start_at, end_at);

CREATE TABLE enrollment.cohort_staff (
    id uuid NOT NULL UNIQUE,
    organization_id uuid NOT NULL,
    cohort_id uuid NOT NULL,
    user_id uuid NOT NULL,
    capability varchar(20) NOT NULL CHECK (capability IN ('coordinator', 'facilitator')),
    created_at timestamptz NOT NULL,
    PRIMARY KEY (organization_id, cohort_id, user_id, capability),
    FOREIGN KEY (organization_id, cohort_id) REFERENCES enrollment.cohorts(organization_id, id),
    FOREIGN KEY (organization_id, user_id) REFERENCES identity.users(organization_id, id)
);
CREATE INDEX cohort_staff_user ON enrollment.cohort_staff(organization_id, user_id, cohort_id);

import { useEffect, useState, type FormEvent } from 'react';
import { ApiError, request, type Session } from './api';
import {
  Alert,
  Button,
  Card,
  Input,
  PageHeader,
  StatusBadge,
  Textarea,
} from './components/ui';

type Account = Session['account'];
type Course = {
  id: string;
  title: string;
  description: string;
  status: string;
  ownerId: string;
  revision: number;
  requirementsVersion: number;
};
type Lesson = { id: string; title: string; notes: string; required: boolean };
type Module = { id: string; title: string; lessons: Lesson[] };
type Detail = { course: Course; modules: Module[] };

export function explain(error: unknown): string {
  const messages: Record<string, string> = {
    course_requires_lessons: 'Add at least one module, with a lesson in every module, before publishing.',
    lesson_requires_content: 'Write lesson notes for every lesson before publishing.',
    course_requires_required_lesson: 'Mark at least one lesson as required before publishing.',
    owner_not_eligible: 'Choose an active Course Author or Administrator in your organization.',
    revision_conflict:
      'Someone changed this course. Your edits are still here. Reload the saved course before trying again.',
    published_content_read_only: 'Published content is read-only in this release.',
    forbidden: 'Your current role does not allow this action.',
    course_not_found: 'This course is unavailable or you no longer have access.',
    reauthentication_failed: 'Your current password could not be verified.',
    request_conflict:
      'This request was already used with different details. Return to the course list and try again.',
    invalid_lessons:
      'Check lesson titles and notes. A draft supports up to 200 lessons and 50,000 characters per lesson.',
    invalid_modules: 'Each module needs a title. A draft supports up to 50 modules.',
    invalid_course_details:
      'Enter a title of up to 200 characters and a description of up to 4,000 characters.',
  };
  if (error instanceof ApiError) {
    return error.status === 401
      ? 'Your session changed. Sign in again to continue.'
      : error.status === 429
        ? 'Too many requests. Wait a minute and try again.'
        : messages[error.code] ??
          'We couldn’t complete that action. Your edits are still here; please try again.';
  }
  return 'We couldn’t reach your workspace. Your edits are still here; please try again.';
}

export function CourseLibrary({
  session,
  onDirty,
}: {
  session: Session;
  onDirty: (dirty: boolean) => void;
}) {
  const [courses, setCourses] = useState<Course[]>([]);
  const [offset, setOffset] = useState(0);
  const [selected, setSelected] = useState<Detail | null>(null);
  const [creating, setCreating] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const canCreate = session.account.roles.some(role =>
    ['Administrator', 'CourseAuthor'].includes(role)
  );

  async function loadList() {
    setLoading(true);
    setError('');
    try {
      setCourses(await request<Course[]>(`/api/authoring/courses?offset=${offset}`));
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadList();
  }, [offset]);

  async function open(id: string) {
    setLoading(true);
    setError('');
    try {
      setSelected(await request<Detail>(`/api/authoring/courses/${id}`));
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setLoading(false);
    }
  }

  if (selected) {
    return (
      <CourseEditor
        key={selected.course.id}
        initial={selected}
        session={session}
        onDirty={onDirty}
        onBack={() => {
          setSelected(null);
          void loadList();
        }}
      />
    );
  }

  if (creating) {
    return (
      <NewCourse
        session={session}
        onCreated={value => {
          setCreating(false);
          setSelected(value);
        }}
        onBack={() => setCreating(false)}
      />
    );
  }

  return (
    <section aria-labelledby="courses-title">
      <div className="section-heading">
        <div>
          <p className="eyebrow">SHARE WHAT YOU KNOW</p>
          <h1 id="courses-title">Courses</h1>
          <p className="intro">Build thoughtful learning, one lesson at a time.</p>
        </div>
        {canCreate && (
          <Button
            variant="primary"
            onClick={() => setCreating(true)}
            iconRight={<span aria-hidden="true">＋</span>}
          >
            New course
          </Button>
        )}
      </div>

      {error && (
        <Alert type="error">
          {error} <Button variant="text" size="sm" onClick={() => void loadList()}>Try again</Button>
        </Alert>
      )}

      {loading ? (
        <p role="status" className="intro">Loading courses…</p>
      ) : courses.length ? (
        <div className="course-grid">
          {courses.map(course => (
            <button
              key={course.id}
              className="course-card"
              onClick={() => void open(course.id)}
            >
              <StatusBadge status={course.status} />
              <h2>{course.title}</h2>
              <p>{course.description || 'No description yet.'}</p>
              <span className="card-link">
                Open course <span aria-hidden="true">↗</span>
              </span>
            </button>
          ))}
        </div>
      ) : (
        <div className="empty-state">
          <span className="empty-symbol" aria-hidden="true">↗</span>
          <h2>{offset ? 'No more courses.' : 'Your first course starts here.'}</h2>
          <p>
            {canCreate
              ? 'Create a draft, add your lessons, and publish when you’re ready.'
              : 'Courses available to your role will appear here.'}
          </p>
        </div>
      )}

      {(offset > 0 || courses.length === 50) && (
        <div className="actions" style={{ marginTop: '24px' }}>
          <Button
            variant="secondary"
            disabled={loading || offset === 0}
            onClick={() => setOffset(offset - 50)}
          >
            Previous
          </Button>
          <Button
            variant="secondary"
            disabled={loading || courses.length < 50}
            onClick={() => setOffset(offset + 50)}
          >
            Next
          </Button>
        </div>
      )}
    </section>
  );
}

function NewCourse({
  session,
  onCreated,
  onBack,
}: {
  session: Session;
  onCreated: (value: Detail) => void;
  onBack: () => void;
}) {
  const [id] = useState(() => crypto.randomUUID());
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [ownerId, setOwnerId] = useState(session.account.id);
  const [owners, setOwners] = useState<Account[]>([session.account]);
  const [search, setSearch] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const administrator = session.account.roles.includes('Administrator');

  async function findOwners() {
    setBusy(true);
    setError('');
    try {
      setOwners(
        await request<Account[]>(`/api/authoring/owners?search=${encodeURIComponent(search)}`)
      );
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setBusy(false);
    }
  }

  async function create(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      onCreated(
        await request<Detail>('/api/authoring/courses', {
          id,
          title,
          description,
          ownerId,
        })
      );
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setBusy(false);
    }
  }

  return (
    <section>
      <PageHeader
        backLink={{ label: '← Back to courses', onClick: onBack, disabled: busy }}
        eyebrow="A NEW BEGINNING"
        title="Create a course"
        description="Start with the essentials. Your course stays a draft until you publish it."
      />

      <Card className="editor-panel">
        <form onSubmit={event => void create(event)}>
          <fieldset disabled={busy}>
            <Input
              autoFocus
              id="course-title"
              label="Course title"
              required
              maxLength={200}
              value={title}
              onChange={event => setTitle(event.target.value)}
              placeholder="e.g. Getting started at s2c"
            />
            <Textarea
              id="course-description"
              label="Description"
              maxLength={4000}
              rows={4}
              value={description}
              onChange={event => setDescription(event.target.value)}
              placeholder="What will people learn?"
            />

            {administrator && (
              <>
                <label htmlFor="owner-search" className="field-label">
                  Find an owner by email
                </label>
                <div className="inline-fields" style={{ marginBottom: '16px' }}>
                  <input
                    id="owner-search"
                    value={search}
                    maxLength={320}
                    onChange={event => setSearch(event.target.value)}
                    placeholder="Search by email..."
                  />
                  <Button type="button" variant="secondary" onClick={() => void findOwners()}>
                    Find owners
                  </Button>
                </div>
              </>
            )}

            <label htmlFor="course-owner" className="field-label">
              Course owner
            </label>
            <select
              id="course-owner"
              value={ownerId}
              onChange={event => setOwnerId(event.target.value)}
              disabled={!administrator}
            >
              {!owners.some(owner => owner.id === ownerId) && (
                <option value={ownerId}>Selected owner</option>
              )}
              {owners.map(owner => (
                <option key={owner.id} value={owner.id}>
                  {owner.name} — {owner.email}
                </option>
              ))}
            </select>
            <p className="field-help">
              One owner, with Course Author or Administrator access. Ownership does not grant an account role.
            </p>

            {error && <Alert type="error">{error}</Alert>}

            <Button
              variant="primary"
              type="submit"
              loading={busy}
              disabled={busy}
              iconRight={<span aria-hidden="true">→</span>}
            >
              Create draft
            </Button>
          </fieldset>
        </form>
      </Card>
    </section>
  );
}

function CourseEditor({
  initial,
  session,
  onDirty,
  onBack,
}: {
  initial: Detail;
  session: Session;
  onDirty: (dirty: boolean) => void;
  onBack: () => void;
}) {
  const [saved, setSaved] = useState(initial);
  const [draft, setDraft] = useState(initial);
  const [dirty, setDirty] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const canEdit =
    session.account.roles.includes('Administrator') ||
    (session.account.roles.includes('CourseAuthor') && draft.course.ownerId === session.account.id);
  const editable = canEdit && saved.course.status === 'draft';

  useEffect(() => {
    onDirty(dirty);
    const guard = (event: BeforeUnloadEvent) => {
      event.preventDefault();
    };
    if (dirty) window.addEventListener('beforeunload', guard);
    return () => {
      onDirty(false);
      window.removeEventListener('beforeunload', guard);
    };
  }, [dirty, onDirty]);

  function change(value: Detail) {
    setDraft(value);
    setDirty(true);
    setNotice('');
  }

  function updateModule(index: number, update: Partial<Module>) {
    change({
      ...draft,
      modules: draft.modules.map((module, position) =>
        position === index ? { ...module, ...update } : module
      ),
    });
  }

  function updateLesson(moduleIndex: number, lessonIndex: number, update: Partial<Lesson>) {
    updateModule(moduleIndex, {
      lessons: draft.modules[moduleIndex].lessons.map((lesson, index) =>
        index === lessonIndex ? { ...lesson, ...update } : lesson
      ),
    });
  }

  async function save() {
    setBusy(true);
    setError('');
    setNotice('');
    try {
      const result = await request<Detail>(`/api/authoring/courses/${draft.course.id}/draft`, {
        expectedRevision: saved.course.revision,
        title: draft.course.title,
        description: draft.course.description,
        modules: draft.modules,
      });
      setSaved(result);
      setDraft(result);
      setDirty(false);
      setNotice('Draft saved.');
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setBusy(false);
    }
  }

  async function publish() {
    setBusy(true);
    setError('');
    setNotice('');
    try {
      const result = await request<Detail>(`/api/authoring/courses/${draft.course.id}/publish`, {
        expectedRevision: saved.course.revision,
      });
      setSaved(result);
      setDraft(result);
      setNotice('Course published. It can now be selected for a cohort.');
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setBusy(false);
    }
  }

  async function reload() {
    setBusy(true);
    setError('');
    try {
      const result = await request<Detail>(`/api/authoring/courses/${draft.course.id}`);
      setSaved(result);
      setDraft(result);
      setDirty(false);
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setBusy(false);
    }
  }

  return (
    <section>
      <PageHeader
        backLink={{
          label: '← Back to courses',
          onClick: onBack,
          disabled: busy || dirty,
        }}
        title={saved.course.title}
        status={<span className={`status ${saved.course.status}`}>{saved.course.status}</span>}
        description={
          editable
            ? 'Shape your course. Save your draft, then publish when it’s ready.'
            : 'Course content'
        }
        actions={
          editable ? (
            <div className="actions">
              <Button
                variant="secondary"
                disabled={busy || !dirty}
                onClick={() => void save()}
              >
                Save draft
              </Button>
              <Button
                variant="primary"
                disabled={busy || dirty}
                onClick={() => void publish()}
                iconRight={<span aria-hidden="true">↗</span>}
              >
                Publish course
              </Button>
            </div>
          ) : undefined
        }
      />

      {error && <Alert type="error">{error}</Alert>}
      {notice && <Alert type="success">{notice}</Alert>}

      {dirty && (
        <div className="draft-notice">
          <span>Unsaved changes. Save or discard before leaving this page.</span>
          <Button
            variant="secondary"
            size="sm"
            disabled={busy}
            onClick={() => {
              setDraft(saved);
              setDirty(false);
              setError('');
            }}
          >
            Discard changes
          </Button>
        </div>
      )}

      {error && (
        <Button
          variant="text"
          disabled={busy || dirty}
          onClick={() => void reload()}
          style={{ marginBottom: '16px' }}
        >
          Reload saved course
        </Button>
      )}

      {editable ? (
        <fieldset disabled={busy} className="course-editor">
          <Card className="editor-panel">
            <Input
              id="edit-title"
              label="Course title"
              maxLength={200}
              value={draft.course.title}
              onChange={event =>
                change({ ...draft, course: { ...draft.course, title: event.target.value } })
              }
            />
            <Textarea
              id="edit-description"
              label="Description"
              rows={3}
              maxLength={4000}
              value={draft.course.description}
              onChange={event =>
                change({ ...draft, course: { ...draft.course, description: event.target.value } })
              }
            />
          </Card>

          {draft.modules.map((module, moduleIndex) => (
            <section
              className="module-panel"
              key={module.id}
              aria-label={`Module ${moduleIndex + 1}`}
            >
              <div className="module-heading">
                <p className="eyebrow">MODULE {String(moduleIndex + 1).padStart(2, '0')}</p>
                <Button
                  type="button"
                  variant="text"
                  size="sm"
                  onClick={() =>
                    change({
                      ...draft,
                      modules: draft.modules.filter(x => x.id !== module.id),
                    })
                  }
                >
                  Remove module
                </Button>
              </div>

              <Input
                id={`module-${module.id}`}
                label="Module title"
                maxLength={200}
                value={module.title}
                onChange={event => updateModule(moduleIndex, { title: event.target.value })}
              />

              {module.lessons.map((lesson, lessonIndex) => (
                <div className="lesson-panel" key={lesson.id}>
                  <div className="module-heading">
                    <h2>Lesson {lessonIndex + 1}</h2>
                    <Button
                      type="button"
                      variant="text"
                      size="sm"
                      onClick={() =>
                        updateModule(moduleIndex, {
                          lessons: module.lessons.filter(x => x.id !== lesson.id),
                        })
                      }
                    >
                      Remove lesson
                    </Button>
                  </div>

                  <Input
                    id={`lesson-${lesson.id}`}
                    label="Lesson title"
                    maxLength={200}
                    value={lesson.title}
                    onChange={event =>
                      updateLesson(moduleIndex, lessonIndex, { title: event.target.value })
                    }
                  />

                  <Textarea
                    id={`notes-${lesson.id}`}
                    label="Lesson notes"
                    rows={8}
                    maxLength={50000}
                    value={lesson.notes}
                    onChange={event =>
                      updateLesson(moduleIndex, lessonIndex, { notes: event.target.value })
                    }
                    placeholder="Share the knowledge your learners need…"
                    helpText="Plain text in this release. Line breaks are preserved."
                  />

                  <label className="checkbox">
                    <input
                      type="checkbox"
                      checked={lesson.required}
                      onChange={event =>
                        updateLesson(moduleIndex, lessonIndex, { required: event.target.checked })
                      }
                    />
                    Required for completion
                  </label>
                </div>
              ))}

              <Button
                type="button"
                variant="add"
                onClick={() =>
                  updateModule(moduleIndex, {
                    lessons: [
                      ...module.lessons,
                      {
                        id: crypto.randomUUID(),
                        title: `Lesson ${module.lessons.length + 1}`,
                        notes: '',
                        required: true,
                      },
                    ],
                  })
                }
              >
                ＋ Add lesson
              </Button>
            </section>
          ))}

          <Button
            type="button"
            variant="add"
            disabled={draft.modules.length >= 50}
            onClick={() =>
              change({
                ...draft,
                modules: [
                  ...draft.modules,
                  {
                    id: crypto.randomUUID(),
                    title: `Module ${draft.modules.length + 1}`,
                    lessons: [],
                  },
                ],
              })
            }
          >
            ＋ Add module
          </Button>
        </fieldset>
      ) : (
        <>
          <p className="intro" style={{ marginBottom: '24px' }}>
            {draft.course.description}
          </p>
          {draft.modules.map((module, index) => (
            <section className="module-panel" key={module.id}>
              <p className="eyebrow">MODULE {String(index + 1).padStart(2, '0')}</p>
              <h2>{module.title}</h2>
              {module.lessons.map(lesson => (
                <article className="lesson-panel" key={lesson.id}>
                  <StatusBadge status={lesson.required ? 'Required' : 'Optional'} />
                  <h3>{lesson.title}</h3>
                  <div className="lesson-notes">{lesson.notes}</div>
                </article>
              ))}
            </section>
          ))}
        </>
      )}
    </section>
  );
}

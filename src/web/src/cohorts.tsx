import { useEffect, useState, type FormEvent } from 'react';
import { request, type Session } from './api';
import { explain } from './authoring';

type Account = Session['account'];
type Course = { id: string; title: string; status: string };
type Staff = { userId: string; name: string; email: string; capability: 'coordinator' | 'facilitator'; effective: boolean };
type Cohort = { id: string; courseId: string; courseTitle: string; title: string; startAt: string; endAt: string; lifecycle: string; revision: number; staff: Staff[] };

export function Cohorts({ session }: { session: Session }) {
  const [cohorts, setCohorts] = useState<Cohort[]>([]); const [selected, setSelected] = useState<Cohort | null>(null);
  const [creating, setCreating] = useState(false); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  const canCreate = session.account.roles.some(role => ['Administrator', 'OrganizationManager', 'CohortCoordinator'].includes(role));
  async function load() {
    setLoading(true); setError('');
    try { setCohorts(await request<Cohort[]>('/api/cohorts')); }
    catch (failure) { setError(explainCohort(failure)); } finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, []);
  if (selected) return <CohortDetail cohort={selected} onBack={() => setSelected(null)} />;
  if (creating) return <NewCohort session={session} onCreated={setSelected} onBack={() => setCreating(false)} />;
  return <section aria-labelledby="cohorts-title"><div className="section-heading"><div><p className="eyebrow">LEARN TOGETHER</p><h1 id="cohorts-title">Cohorts</h1><p className="intro">Schedule a published course and establish its learning team.</p></div>
    {canCreate && <button className="primary-button" onClick={() => setCreating(true)}>New cohort <span aria-hidden="true">＋</span></button>}</div>
    {error && <p role="alert" className="error">{error} <button onClick={() => void load()}>Try again</button></p>}
    {loading ? <p role="status">Loading cohorts…</p> : cohorts.length ? <div className="course-grid">{cohorts.map(cohort => <button key={cohort.id} className="course-card" onClick={() => setSelected(cohort)}>
      <span className={`status ${cohort.lifecycle}`}>{cohort.lifecycle}</span><h2>{cohort.title}</h2><p>{cohort.courseTitle}</p><p>{schedule(cohort)}</p><span className="card-link">View staff <span aria-hidden="true">↗</span></span>
    </button>)}</div> : <div className="empty-state"><h2>No cohorts yet.</h2><p>{canCreate ? 'Publish a course, make an account eligible to coordinate, then schedule the first cohort.' : 'Cohorts assigned to you will appear here.'}</p></div>}
  </section>;
}

function NewCohort({ session, onCreated, onBack }: { session: Session; onCreated: (cohort: Cohort) => void; onBack: () => void }) {
  const [id] = useState(() => crypto.randomUUID()); const [courses, setCourses] = useState<Course[]>([]);
  const [coordinators, setCoordinators] = useState<Account[]>([]); const [facilitators, setFacilitators] = useState<Account[]>([]);
  const [courseId, setCourseId] = useState(''); const [coordinatorId, setCoordinatorId] = useState(''); const [facilitatorId, setFacilitatorId] = useState('');
  const [title, setTitle] = useState(''); const [startAt, setStartAt] = useState(localInput(new Date())); const [endAt, setEndAt] = useState(localInput(new Date(Date.now() + 7 * 86400000)));
  const [busy, setBusy] = useState(true); const [error, setError] = useState('');
  useEffect(() => { void (async () => {
    try {
      const [availableCourses, eligibleCoordinators, eligibleFacilitators] = await Promise.all([
        request<Course[]>('/api/authoring/courses'), request<Account[]>('/api/cohorts/eligible-staff?capability=coordinator'), request<Account[]>('/api/cohorts/eligible-staff?capability=facilitator')]);
      const published = availableCourses.filter(course => course.status === 'published'); setCourses(published); setCourseId(published[0]?.id ?? '');
      setCoordinators(eligibleCoordinators); setCoordinatorId(eligibleCoordinators.find(person => person.id === session.account.id)?.id ?? eligibleCoordinators[0]?.id ?? '');
      setFacilitators(eligibleFacilitators);
    } catch (failure) { setError(explainCohort(failure)); } finally { setBusy(false); }
  })(); }, []);
  async function create(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError('');
    try { onCreated(await request<Cohort>('/api/cohorts', { id, courseId, title, startAt: new Date(startAt).toISOString(), endAt: new Date(endAt).toISOString(), coordinatorId, facilitatorId: facilitatorId || null })); }
    catch (failure) { setError(explainCohort(failure)); } finally { setBusy(false); }
  }
  return <section><button className="text-button" disabled={busy} onClick={onBack}>← Back to cohorts</button><p className="eyebrow">A SHARED LEARNING WINDOW</p><h1>Schedule a cohort</h1><p className="intro">A Coordinator is assigned in the same transaction, so the cohort is never launched unstaffed.</p>
    <form className="editor-panel" onSubmit={event => void create(event)}><fieldset disabled={busy}><label htmlFor="cohort-title">Cohort title</label><input autoFocus id="cohort-title" required maxLength={200} value={title} onChange={event => setTitle(event.target.value)} />
      <label htmlFor="cohort-course">Published course</label><select id="cohort-course" required value={courseId} onChange={event => setCourseId(event.target.value)}><option value="" disabled>Select a course</option>{courses.map(course => <option key={course.id} value={course.id}>{course.title}</option>)}</select>
      <div className="inline-fields schedule-fields"><div><label htmlFor="cohort-start">Starts</label><input id="cohort-start" type="datetime-local" required value={startAt} onChange={event => setStartAt(event.target.value)} /></div><div><label htmlFor="cohort-end">Ends</label><input id="cohort-end" type="datetime-local" required value={endAt} onChange={event => setEndAt(event.target.value)} /></div></div>
      <label htmlFor="cohort-coordinator">Coordinator</label><select id="cohort-coordinator" required value={coordinatorId} onChange={event => setCoordinatorId(event.target.value)}><option value="" disabled>Select an eligible Coordinator</option>{coordinators.map(person => <option key={person.id} value={person.id}>{person.name} — {person.email}</option>)}</select>
      <label htmlFor="cohort-facilitator">Facilitator (optional)</label><select id="cohort-facilitator" value={facilitatorId} onChange={event => setFacilitatorId(event.target.value)}><option value="">None</option>{facilitators.map(person => <option key={person.id} value={person.id}>{person.name} — {person.email}</option>)}</select>
      <p className="field-help">Account roles establish eligibility; these cohort assignments establish scope. Neither one implies the other.</p>
      {(!courses.length || !coordinators.length) && !busy && <p className="error" role="alert">You need a published course and at least one active account with Cohort Coordinator access.</p>}{error && <p className="error" role="alert">{error}</p>}
      <button className="primary-button" type="submit" disabled={!courses.length || !coordinators.length}>{busy ? 'Scheduling…' : 'Schedule cohort'} <span aria-hidden="true">→</span></button>
    </fieldset></form></section>;
}

function CohortDetail({ cohort, onBack }: { cohort: Cohort; onBack: () => void }) {
  return <section><button className="text-button" onClick={onBack}>← Back to cohorts</button><span className={`status ${cohort.lifecycle}`}>{cohort.lifecycle}</span><h1>{cohort.title}</h1><p className="intro">{cohort.courseTitle}</p><p>{schedule(cohort)}</p>
    <section className="editor-panel" aria-labelledby="cohort-staff-title"><h2 id="cohort-staff-title">Cohort staff</h2>{cohort.staff.map(person => <article key={`${person.userId}-${person.capability}`} className="person-row"><div><h3>{person.name}</h3><p>{person.email}</p></div><div className="roles"><span>{person.capability}</span>{!person.effective && <span>role inactive</span>}</div></article>)}</section>
  </section>;
}

function localInput(date: Date) { const offset = date.getTimezoneOffset() * 60000; return new Date(date.getTime() - offset).toISOString().slice(0, 16); }
function schedule(cohort: Cohort) { return `${new Date(cohort.startAt).toLocaleString()} – ${new Date(cohort.endAt).toLocaleString()}`; }
function explainCohort(error: unknown) {
  const messages: Record<string, string> = {
    published_course_not_found: 'Choose a published course available to your organization.', staff_not_eligible: 'Choose an active account with the matching staff role.',
    invalid_schedule: 'The end must be at or after the start.', invalid_cohort: 'Check the cohort title, course, dates, and Coordinator.',
    cohort_requires_coordinator: 'This scheduled cohort must retain at least one active Coordinator.', revision_conflict: 'Someone changed this cohort. Reload it before trying again.',
  };
  const generic = explain(error); return error instanceof Error && 'code' in error ? messages[(error as { code: string }).code] ?? generic : generic;
}

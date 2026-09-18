import { StrictMode, useEffect, useRef, useState, type FormEvent } from 'react';
import { createRoot } from 'react-dom/client';
import { ApiError, request, type Session } from './api';
import { CourseLibrary } from './authoring';
import { People } from './people';
import { Cohorts } from './cohorts';
import { Licensing } from './licensing';
import {
  Alert,
  Button,
  Card,
  Input,
} from './components/ui';
import './styles.css';

export function Brand() {
  return (
    <div className="brand">
      <span className="brand-mark" aria-hidden="true">
        v<span>·</span>
      </span>
      <span>
        Variable <small>LMS</small>
      </span>
    </div>
  );
}

function InvitationAcceptance() {
  const token = new URLSearchParams(window.location.search).get('token') ?? '';
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState('');
  const [done, setDone] = useState(false);
  const [busy, setBusy] = useState(false);

  async function accept(event: FormEvent) {
    event.preventDefault();
    if (password !== confirm) {
      setError('Passwords do not match.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      await request<void>('/api/invitations/accept', { token, password });
      setDone(true);
    } catch (failure) {
      setError(
        failure instanceof ApiError && failure.code === 'learner_capacity_full'
          ? 'No learner capacity is currently available. Ask your administrator to retry after capacity is available.'
          : 'This invitation is invalid, expired, or no longer current.'
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="entry">
      <aside className="story" aria-label="About Variable LMS">
        <Brand />
        <div className="story-copy">
          <p className="eyebrow">YOU’RE INVITED</p>
          <h2>
            Start learning
            <br />
            with your team.
          </h2>
          <p>
            Join your organization’s private workspace to explore courses, participate in
            cohorts, and advance your skills.
          </p>
        </div>
        <p className="story-footer">Learning belongs here.</p>
      </aside>

      <main className="sign-in">
        <div className="form-wrap">
          <p className="eyebrow">CREATE YOUR ACCOUNT</p>
          <h1>Accept your invitation.</h1>

          {done ? (
            <>
              <Alert type="success">Your account is active.</Alert>
              <a className="primary-button" href="/">
                Continue to sign in
              </a>
            </>
          ) : (
            <form onSubmit={event => void accept(event)}>
              <Input
                id="new-password"
                type="password"
                label="Password"
                minLength={14}
                maxLength={128}
                autoComplete="new-password"
                required
                value={password}
                onChange={event => setPassword(event.target.value)}
                helpText="At least 14 characters."
              />

              <Input
                id="confirm-password"
                type="password"
                label="Confirm password"
                minLength={14}
                maxLength={128}
                autoComplete="new-password"
                required
                value={confirm}
                onChange={event => setConfirm(event.target.value)}
              />

              {error && <Alert type="error">{error}</Alert>}

              <Button
                variant="primary"
                type="submit"
                loading={busy}
                disabled={busy || !token}
                iconRight={<span aria-hidden="true">→</span>}
              >
                Activate account
              </Button>
            </form>
          )}
        </div>
        <p className="entry-footer">Powered by Variable</p>
      </main>
    </div>
  );
}

function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [page, setPage] = useState<'overview' | 'courses' | 'cohorts' | 'people' | 'licensing'>('overview');
  const [dirty, setDirty] = useState(false);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const heading = useRef<HTMLHeadingElement>(null);

  async function refresh() {
    setLoading(true);
    try {
      setSession(await request<Session>('/api/auth/session'));
      setError('');
      setPage('overview');
      setDirty(false);
    } catch (failure) {
      setSession(null);
      if (!(failure instanceof ApiError && failure.status === 401)) {
        setError('We couldn’t reach your workspace. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void refresh();
  }, []);

  useEffect(() => {
    if (!loading) heading.current?.focus();
  }, [session, loading, page]);

  async function login(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      await request<void>('/api/auth/login', { email, password });
      setPassword('');
      await refresh();
    } catch (failure) {
      setError(
        failure instanceof ApiError && failure.status === 401
          ? 'Sign-in failed. Check your details or try again later.'
          : failure instanceof ApiError && failure.status === 429
            ? 'Too many attempts. Please wait a minute before trying again.'
            : 'Your workspace is temporarily unavailable. Please try again.'
      );
      setPassword('');
    } finally {
      setBusy(false);
    }
  }

  async function logout() {
    setBusy(true);
    setError('');
    try {
      await request<void>('/api/auth/logout', {});
      setSession(null);
      setPassword('');
    } catch (failure) {
      if (failure instanceof ApiError && failure.status === 401) {
        setSession(null);
      } else {
        setError('We couldn’t sign you out. Please try again.');
      }
    } finally {
      setBusy(false);
    }
  }

  if (window.location.pathname === '/accept-invitation') {
    return <InvitationAcceptance />;
  }

  if (loading) {
    return (
      <main className="loading" aria-busy="true">
        <Brand />
        <p role="status">Opening your workspace…</p>
      </main>
    );
  }

  if (session) {
    const roles = session.account.roles;
    const canViewCourses = roles.some(role =>
      ['Administrator', 'OrganizationManager', 'CourseAuthor', 'CohortCoordinator'].includes(role)
    );
    const canViewCohorts = roles.some(role =>
      ['Administrator', 'OrganizationManager', 'CohortCoordinator', 'LearningFacilitator'].includes(role)
    );
    const canManagePeople = roles.some(role =>
      ['Administrator', 'OrganizationManager'].includes(role)
    );
    const canManageLicensing = roles.some(role =>
      ['Administrator', 'OrganizationManager'].includes(role)
    );

    return (
      <div className="workspace">
        <a className="skip" href="#main">
          Skip to content
        </a>

        <header className="workspace-header">
          <Brand />
          <span className="organization">{session.organization.name}</span>
          <button
            type="button"
            className="quiet-button"
            disabled={busy || dirty}
            title={dirty ? 'Save or discard your draft first' : undefined}
            onClick={() => void logout()}
          >
            Sign out <span aria-hidden="true">↗</span>
          </button>
        </header>

        <nav className="workspace-nav" aria-label="Workspace">
          <button
            type="button"
            disabled={dirty}
            aria-current={page === 'overview' ? 'page' : undefined}
            onClick={() => setPage('overview')}
          >
            Overview
          </button>
          {canViewCourses && (
            <button
              type="button"
              disabled={dirty}
              aria-current={page === 'courses' ? 'page' : undefined}
              onClick={() => setPage('courses')}
            >
              Courses
            </button>
          )}
          {canViewCohorts && (
            <button
              type="button"
              disabled={dirty}
              aria-current={page === 'cohorts' ? 'page' : undefined}
              onClick={() => setPage('cohorts')}
            >
              Cohorts
            </button>
          )}
          {canManagePeople && (
            <button
              type="button"
              disabled={dirty}
              aria-current={page === 'people' ? 'page' : undefined}
              onClick={() => setPage('people')}
            >
              People
            </button>
          )}
          {canManageLicensing && (
            <button
              type="button"
              disabled={dirty}
              aria-current={page === 'licensing' ? 'page' : undefined}
              onClick={() => setPage('licensing')}
            >
              Licensing
            </button>
          )}
        </nav>

        <main id="main" className="workspace-main">
          {page === 'courses' ? (
            <CourseLibrary session={session} onDirty={setDirty} />
          ) : page === 'cohorts' ? (
            <Cohorts session={session} />
          ) : page === 'people' ? (
            <People session={session} onSessionChanged={refresh} />
          ) : page === 'licensing' ? (
            <Licensing session={session} />
          ) : (
            <>
              <p className="eyebrow">YOUR WORKSPACE</p>
              <h1 tabIndex={-1} ref={heading}>
                Welcome, {session.account.name}.
              </h1>
              <p className="intro">A place for your organization to learn and grow.</p>

              {error && <Alert type="error">{error}</Alert>}

              <Card as="section" className="account-panel" aria-labelledby="account-title">
                <div className="account-avatar" aria-hidden="true">
                  {session.account.name.charAt(0).toUpperCase()}
                </div>
                <div>
                  <p className="eyebrow">SIGNED IN AS</p>
                  <h2 id="account-title">{session.account.name}</h2>
                  <p>{session.account.email}</p>
                  <div className="roles">
                    {session.account.roles.map(role => (
                      <span key={role}>{role.replace(/([a-z])([A-Z])/g, '$1 $2')}</span>
                    ))}
                  </div>
                </div>
              </Card>

              <section className="empty-state" aria-labelledby="learning-title">
                <span className="empty-symbol" aria-hidden="true">
                  ↗
                </span>
                <h2 id="learning-title">Your learning workspace is taking shape.</h2>
                <p>
                  Authorized staff can publish courses, schedule staffed cohorts, and invite
                  Learners within licensed capacity.
                </p>
              </section>
            </>
          )}
        </main>

        <footer className="workspace-footer">
          <span>Variable LMS</span>
          <span>Built for learning, together.</span>
        </footer>
      </div>
    );
  }

  return (
    <div className="entry">
      <a className="skip" href="#main">
        Skip to content
      </a>

      <aside className="story" aria-label="About Variable LMS">
        <Brand />
        <div className="story-copy">
          <p className="eyebrow">A LITTLE PROGRESS. EVERY DAY.</p>
          <h2>
            Room to learn.
            <br />
            Space to grow.
          </h2>
          <p>
            Your people, your knowledge,
            <br />
            your next chapter.
          </p>
          <div className="growth" aria-hidden="true">
            <span />
            <span />
            <span />
            <span />
          </div>
        </div>
        <p className="story-footer">Learning belongs here.</p>
      </aside>

      <main id="main" className="sign-in">
        <div className="mobile-brand">
          <Brand />
        </div>

        <div className="form-wrap">
          <p className="eyebrow">WELCOME BACK</p>
          <h1 tabIndex={-1} ref={heading}>
            Sign in to your workspace.
          </h1>
          <p className="intro">Continue with your organization’s account.</p>

          <form onSubmit={event => void login(event)} aria-busy={busy}>
            <Input
              id="email"
              name="email"
              type="email"
              label="Email address"
              autoComplete="username"
              required
              maxLength={320}
              value={email}
              onChange={event => setEmail(event.target.value)}
              disabled={busy}
              placeholder="you@organization.com"
            />

            <Input
              id="password"
              name="password"
              type="password"
              label="Password"
              autoComplete="current-password"
              required
              maxLength={128}
              value={password}
              onChange={event => setPassword(event.target.value)}
              disabled={busy}
            />

            {error && <Alert type="error">{error}</Alert>}

            <Button
              variant="primary"
              type="submit"
              loading={busy}
              disabled={busy}
              iconRight={<span aria-hidden="true">→</span>}
            >
              Sign in
            </Button>
          </form>

          <p className="help">Need access? Contact your organization’s administrator.</p>
        </div>

        <p className="entry-footer">Powered by Variable</p>
      </main>
    </div>
  );
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>
);

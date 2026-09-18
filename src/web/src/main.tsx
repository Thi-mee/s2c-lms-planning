import { StrictMode, useEffect, useRef, useState, type FormEvent } from 'react';
import { createRoot } from 'react-dom/client';
import { ApiError, request, type Session } from './api';
import './styles.css';

function Brand() {
  return <div className="brand"><span className="brand-mark" aria-hidden="true">v<span>·</span></span><span>Variable <small>LMS</small></span></div>;
}

function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const heading = useRef<HTMLHeadingElement>(null);

  async function refresh() {
    setLoading(true);
    try { setSession(await request<Session>('/api/auth/session')); setError(''); }
    catch (failure) {
      setSession(null);
      if (!(failure instanceof ApiError && failure.status === 401)) setError('We couldn’t reach your workspace. Please try again.');
    } finally { setLoading(false); }
  }
  useEffect(() => { void refresh(); }, []);
  useEffect(() => { if (!loading) heading.current?.focus(); }, [session, loading]);

  async function login(event: FormEvent) {
    event.preventDefault();
    setBusy(true); setError('');
    try {
      await request<void>('/api/auth/login', { email, password });
      setPassword('');
      await refresh();
    } catch (failure) {
      setError(failure instanceof ApiError && failure.status === 401
        ? 'Sign-in failed. Check your details or try again later.'
        : failure instanceof ApiError && failure.status === 429
          ? 'Too many attempts. Please wait a minute before trying again.'
          : 'Your workspace is temporarily unavailable. Please try again.');
      setPassword('');
    } finally { setBusy(false); }
  }
  async function logout() {
    setBusy(true); setError('');
    try { await request<void>('/api/auth/logout', {}); setSession(null); setPassword(''); }
    catch (failure) {
      if (failure instanceof ApiError && failure.status === 401) setSession(null);
      else setError('We couldn’t sign you out. Please try again.');
    } finally { setBusy(false); }
  }

  if (loading) return <main className="loading" aria-busy="true"><Brand /><p role="status">Opening your workspace…</p></main>;

  if (session) return <div className="workspace">
    <a className="skip" href="#main">Skip to content</a>
    <header className="workspace-header"><Brand /><span className="organization">{session.organization.name}</span><button className="quiet-button" disabled={busy} onClick={() => void logout()}>Sign out <span aria-hidden="true">↗</span></button></header>
    <main id="main" className="workspace-main">
      <p className="eyebrow">YOUR WORKSPACE</p>
      <h1 tabIndex={-1} ref={heading}>Welcome, {session.account.name}.</h1>
      <p className="intro">A place for your organization to learn and grow.</p>
      {error && <p role="alert" className="error">{error}</p>}
      <section className="account-panel" aria-labelledby="account-title">
        <div className="account-avatar" aria-hidden="true">{session.account.name.charAt(0).toUpperCase()}</div>
        <div><p className="eyebrow">SIGNED IN AS</p><h2 id="account-title">{session.account.name}</h2><p>{session.account.email}</p><div className="roles">{session.account.roles.map(role => <span key={role}>{role.replace(/([a-z])([A-Z])/g, '$1 $2')}</span>)}</div></div>
      </section>
      <section className="empty-state" aria-labelledby="learning-title"><span className="empty-symbol" aria-hidden="true">↗</span><h2 id="learning-title">Your learning workspace is taking shape.</h2><p>Course authoring and cohort learning are coming next. Your account is ready.</p></section>
    </main><footer className="workspace-footer">Variable LMS <span>Built for learning, together.</span></footer>
  </div>;

  return <div className="entry">
    <a className="skip" href="#main">Skip to content</a>
    <aside className="story" aria-label="About Variable LMS"><Brand /><div className="story-copy"><p className="eyebrow">A LITTLE PROGRESS. EVERY DAY.</p><h2>Room to learn.<br />Space to grow.</h2><p>Your people, your knowledge,<br />your next chapter.</p><div className="growth" aria-hidden="true"><span /><span /><span /><span /></div></div><p className="story-footer">Learning belongs here.</p></aside>
    <main id="main" className="sign-in"><div className="mobile-brand"><Brand /></div><div className="form-wrap"><p className="eyebrow">WELCOME BACK</p><h1 tabIndex={-1} ref={heading}>Sign in to your workspace.</h1><p className="intro">Continue with your organization’s account.</p>
      <form onSubmit={event => void login(event)} aria-busy={busy}>
        <label htmlFor="email">Email address</label><input id="email" name="email" type="email" autoComplete="username" required maxLength={320} value={email} onChange={event => setEmail(event.target.value)} disabled={busy} placeholder="you@organization.com" />
        <label htmlFor="password">Password</label><input id="password" name="password" type="password" autoComplete="current-password" required maxLength={128} value={password} onChange={event => setPassword(event.target.value)} disabled={busy} />
        {error && <p className="error" role="alert">{error}</p>}
        <button className="primary-button" type="submit" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}<span aria-hidden="true">→</span></button>
      </form>
      <p className="help">Need access? Contact your organization’s administrator.</p>
    </div><p className="entry-footer">Powered by Variable</p></main>
  </div>;
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);

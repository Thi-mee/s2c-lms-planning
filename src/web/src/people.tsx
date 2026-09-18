import { useEffect, useState, type FormEvent } from 'react';
import { request, type Session } from './api';
import { explain } from './authoring';

export function People({ session, onSessionChanged }: { session: Session; onSessionChanged: () => Promise<void> }) {
  const [people, setPeople] = useState<Session['account'][]>([]); const [search, setSearch] = useState('');
  const [target, setTarget] = useState<{ person: Session['account']; role: StaffRole } | null>(null); const [reason, setReason] = useState(''); const [password, setPassword] = useState('');
  const [error, setError] = useState(''); const [notice, setNotice] = useState(''); const [busy, setBusy] = useState(false);
  async function load() {
    setBusy(true); setError('');
    try { setPeople(await request<Session['account'][]>(`/api/administration/users?search=${encodeURIComponent(search)}`)); }
    catch (failure) { setError(explain(failure)); } finally { setBusy(false); }
  }
  useEffect(() => { void load(); }, []);
  async function change(event: FormEvent) {
    event.preventDefault(); if (!target) return; setBusy(true); setError(''); setNotice('');
    try {
      const definition = roleDefinitions[target.role];
      await request<void>(`/api/administration/users/${target.person.id}/${definition.path}`, { granted: !target.person.roles.includes(target.role), reason, currentPassword: password || null });
      if (target.person.id === session.account.id) { await onSessionChanged(); return; }
      setTarget(null); setReason(''); setPassword(''); setNotice(`${definition.label} access updated. The account must sign in again.`); await load();
    } catch (failure) { setError(explain(failure)); setPassword(''); } finally { setBusy(false); }
  }
  return <section><p className="eyebrow">THE PEOPLE BEHIND THE LEARNING</p><h1>People</h1><p className="intro">Manage authoring and cohort staff eligibility for active accounts. Invitations are coming in a later slice.</p>
    <form className="inline-fields search-form" onSubmit={event => { event.preventDefault(); void load(); }}><label htmlFor="people-search">Find by email</label><input id="people-search" maxLength={320} value={search} onChange={event => setSearch(event.target.value)} /><button disabled={busy}>Search</button></form>
    {error && <p role="alert" className="error">{error}</p>}{notice && <p role="status" className="success">{notice}</p>}
    {target && <form className="editor-panel" onSubmit={event => void change(event)}><fieldset disabled={busy}><h2>{target.person.roles.includes(target.role) ? 'Revoke' : 'Grant'} {roleDefinitions[target.role].label}</h2><p>{target.person.name} · {target.person.email}</p>
      <label htmlFor="grant-reason">Reason for this change</label><input id="grant-reason" required maxLength={1000} value={reason} onChange={event => setReason(event.target.value)} />
      {target.person.roles.includes('Administrator') && target.person.id !== session.account.id && <><label htmlFor="grant-password">Your current password</label><input id="grant-password" type="password" autoComplete="current-password" required value={password} onChange={event => setPassword(event.target.value)} /></>}
      <p className="field-help">This change is audited and signs the affected account out. Resource assignments remain separate and never grant an account role.</p><div className="actions"><button type="button" onClick={() => { setTarget(null); setPassword(''); }}>Cancel</button><button className="primary-button" type="submit">Confirm access change</button></div>
    </fieldset></form>}
    <div className="people-list">{people.map(person => {
      const allowed = session.account.roles.includes('Administrator') || person.id === session.account.id || !person.roles.some(role => ['Administrator', 'OrganizationManager'].includes(role));
      return <article key={person.id} className="person-row"><div><h2>{person.name}</h2><p>{person.email}</p><div className="roles">{person.roles.map(role => <span key={role}>{role.replace(/([a-z])([A-Z])/g, '$1 $2')}</span>)}</div></div>
        {allowed && <div className="actions">{(Object.keys(roleDefinitions) as StaffRole[]).map(role => <button key={role} disabled={busy} onClick={() => { setTarget({ person, role }); setReason(''); setPassword(''); setError(''); }}>{person.roles.includes(role) ? 'Revoke' : 'Grant'} {roleDefinitions[role].label}</button>)}</div>}</article>;
    })}</div>{people.length === 50 && <p className="field-help">Showing the first 50 matches. Narrow your email search to find another account.</p>}{!busy && !people.length && <p>No active accounts match this search.</p>}
  </section>;
}

type StaffRole = 'CourseAuthor' | 'CohortCoordinator' | 'LearningFacilitator';
const roleDefinitions: Record<StaffRole, { label: string; path: string }> = {
  CourseAuthor: { label: 'Course Author', path: 'course-author-role' },
  CohortCoordinator: { label: 'Cohort Coordinator', path: 'cohort-coordinator-role' },
  LearningFacilitator: { label: 'Learning Facilitator', path: 'learning-facilitator-role' },
};

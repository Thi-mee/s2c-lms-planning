import { useEffect, useState, type FormEvent } from 'react';
import { request, type Session } from './api';
import { explain } from './authoring';
import {
  Alert,
  Button,
  Card,
  Checkbox,
  Input,
  Modal,
  PageHeader,
  StatusBadge,
} from './components/ui';

type Person = Session['account'] & {
  status: 'pending' | 'active' | 'deactivated';
  invitationExpiresAt?: string | null;
};
type ManagedRole = 'Learner' | 'CourseAuthor' | 'CohortCoordinator' | 'LearningFacilitator';

export function People({
  session,
  onSessionChanged,
}: {
  session: Session;
  onSessionChanged: () => Promise<void>;
}) {
  const [people, setPeople] = useState<Person[]>([]);
  const [search, setSearch] = useState('');
  const [target, setTarget] = useState<{ person: Person; role: ManagedRole } | null>(null);
  const [reason, setReason] = useState('');
  const [password, setPassword] = useState('');
  const [invite, setInvite] = useState({ name: '', email: '', roles: ['Learner'] as string[] });
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);

  async function load() {
    setBusy(true);
    setError('');
    try {
      setPeople(
        await request<Person[]>(`/api/administration/users?search=${encodeURIComponent(search)}`)
      );
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function sendInvite(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError('');
    setNotice('');
    try {
      await request('/api/provisioning/invitations', {
        requestId: crypto.randomUUID(),
        ...invite,
      });
      setInvite({ name: '', email: '', roles: ['Learner'] });
      setNotice('Invitation queued. Capacity will be checked when it is accepted.');
      await load();
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setBusy(false);
    }
  }

  async function resend(person: Person) {
    setBusy(true);
    setError('');
    setNotice('');
    try {
      await request('/api/provisioning/invitations', {
        requestId: crypto.randomUUID(),
        name: person.name,
        email: person.email,
        roles: person.roles,
      });
      setNotice('A fresh invitation was queued; the previous link is no longer valid.');
      await load();
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setBusy(false);
    }
  }

  async function reactivate(person: Person) {
    setBusy(true);
    setError('');
    try {
      await request<void>(`/api/administration/users/${person.id}/reactivate`, {
        reason: 'Reactivated from People administration',
      });
      setNotice('Account reactivated.');
      await load();
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setBusy(false);
    }
  }

  async function change(event: FormEvent) {
    event.preventDefault();
    if (!target) return;
    setBusy(true);
    setError('');
    setNotice('');
    try {
      const definition = roleDefinitions[target.role];
      await request<void>(`/api/administration/users/${target.person.id}/${definition.path}`, {
        granted: !target.person.roles.includes(target.role),
        reason,
        currentPassword: password || null,
      });
      if (target.person.id === session.account.id) {
        await onSessionChanged();
        return;
      }
      setTarget(null);
      setReason('');
      setPassword('');
      setNotice(`${definition.label} access updated. The account must sign in again.`);
      await load();
    } catch (failure) {
      setError(explain(failure));
      setPassword('');
    } finally {
      setBusy(false);
    }
  }

  function toggleInviteRole(role: string) {
    setInvite(current => ({
      ...current,
      roles: current.roles.includes(role)
        ? current.roles.filter(item => item !== role)
        : [...current.roles, role],
    }));
  }

  const inviteRoles = [
    ...Object.keys(roleDefinitions),
    ...(session.account.roles.includes('Administrator') ? ['OrganizationManager'] : []),
  ];

  return (
    <section aria-labelledby="people-title">
      <PageHeader
        eyebrow="THE PEOPLE BEHIND THE LEARNING"
        title="People"
        titleId="people-title"
        description="Invite accounts, manage role sets, and activate Learners within the installed license."
      />

      {error && <Alert type="error">{error}</Alert>}
      {notice && <Alert type="success">{notice}</Alert>}

      {/* Invite Form Card */}
      <Card className="editor-panel" as="section" aria-labelledby="invite-title">
        <form onSubmit={event => void sendInvite(event)}>
          <fieldset disabled={busy}>
            <h2 id="invite-title">Invite a person</h2>
            <Input
              id="invite-name"
              label="Name"
              required
              maxLength={200}
              value={invite.name}
              onChange={event => setInvite({ ...invite, name: event.target.value })}
              placeholder="e.g. Maya Chen"
            />
            <Input
              id="invite-email"
              type="email"
              label="Email address"
              required
              maxLength={320}
              value={invite.email}
              onChange={event => setInvite({ ...invite, email: event.target.value })}
              placeholder="colleague@organization.com"
            />

            <fieldset className="role-options">
              <legend>Roles</legend>
              <div className="role-options-grid">
                {inviteRoles.map(role => (
                  <Checkbox
                    key={role}
                    cardStyle
                    id={`role-${role}`}
                    label={role.replace(/([a-z])([A-Z])/g, '$1 $2')}
                    checked={invite.roles.includes(role)}
                    onChange={() => toggleInviteRole(role)}
                  />
                ))}
              </div>
            </fieldset>

            <p className="field-help">Pending invitations use no learner capacity.</p>
            <Button
              type="submit"
              variant="primary"
              disabled={!invite.roles.length || busy}
              loading={busy}
              iconRight={<span aria-hidden="true">→</span>}
            >
              Send invitation
            </Button>
          </fieldset>
        </form>
      </Card>

      {/* Directory Filter / Search Toolbar */}
      <div className="directory-toolbar">
        <div className="directory-title">
          <h2>Accounts</h2>
          <span className="directory-count">
            {people.length} {people.length === 1 ? 'member' : 'members'}
          </span>
        </div>
        <form
          className="search-box"
          role="search"
          onSubmit={event => {
            event.preventDefault();
            void load();
          }}
        >
          <label htmlFor="people-search" className="sr-only">
            Find by email
          </label>
          <input
            id="people-search"
            maxLength={320}
            value={search}
            onChange={event => setSearch(event.target.value)}
            placeholder="Search by email..."
          />
          <Button type="submit" variant="secondary" size="sm" disabled={busy}>
            Search
          </Button>
        </form>
      </div>

      {/* Audited Role Change Modal */}
      <Modal
        isOpen={Boolean(target)}
        onClose={() => {
          setTarget(null);
          setReason('');
          setPassword('');
        }}
        title={
          target
            ? `${target.person.roles.includes(target.role) ? 'Revoke' : 'Grant'} ${
                roleDefinitions[target.role].label
              }`
            : ''
        }
        description={target ? `${target.person.name} · ${target.person.email}` : ''}
      >
        {target && (
          <form onSubmit={event => void change(event)}>
            <fieldset disabled={busy}>
              <Input
                id="grant-reason"
                label="Reason for this change"
                required
                maxLength={1000}
                value={reason}
                onChange={event => setReason(event.target.value)}
                placeholder="Required for organizational audit log"
              />

              {target.person.roles.includes('Administrator') &&
                target.person.id !== session.account.id && (
                  <Input
                    id="grant-password"
                    type="password"
                    label="Your current password"
                    autoComplete="current-password"
                    required
                    value={password}
                    onChange={event => setPassword(event.target.value)}
                    placeholder="Enter your administrator password"
                  />
                )}

              <p className="field-help">
                This change is audited and signs the affected account out.
              </p>

              <div className="modal-footer actions">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => {
                    setTarget(null);
                    setReason('');
                    setPassword('');
                  }}
                >
                  Cancel
                </Button>
                <Button type="submit" variant="primary" loading={busy} disabled={busy || !reason.trim()}>
                  Confirm access change
                </Button>
              </div>
            </fieldset>
          </form>
        )}
      </Modal>

      {/* People List */}
      <div className="people-list">
        {people.map(person => {
          const allowed =
            session.account.roles.includes('Administrator') ||
            person.id === session.account.id ||
            !person.roles.some(role => ['Administrator', 'OrganizationManager'].includes(role));

          return (
            <article key={person.id} className="person-row">
              <div>
                <div className="person-row-header">
                  <h2>{person.name}</h2>
                  <StatusBadge status={person.status} />
                </div>
                <p>
                  {person.email} · {person.status}
                </p>
                <div className="roles">
                  {person.roles.map(role => (
                    <span key={role}>{role.replace(/([a-z])([A-Z])/g, '$1 $2')}</span>
                  ))}
                </div>
              </div>

              <div className="actions">
                {person.status === 'pending' && (
                  <Button
                    variant="secondary"
                    size="sm"
                    disabled={busy}
                    onClick={() => void resend(person)}
                  >
                    Resend invitation
                  </Button>
                )}
                {person.status === 'deactivated' && allowed && (
                  <Button
                    variant="secondary"
                    size="sm"
                    disabled={busy}
                    onClick={() => void reactivate(person)}
                  >
                    Reactivate
                  </Button>
                )}
                {person.status === 'active' && allowed && (
                  <div className="person-actions-grid">
                    {(Object.keys(roleDefinitions) as ManagedRole[]).map(role => {
                      const isGranted = person.roles.includes(role);
                      return (
                        <button
                          key={role}
                          type="button"
                          className={`person-action-btn ${
                            isGranted ? 'person-action-revoke' : 'person-action-grant'
                          }`}
                          disabled={busy}
                          onClick={() => {
                            setTarget({ person, role });
                            setReason('');
                            setError('');
                          }}
                        >
                          {isGranted ? 'Revoke' : 'Grant'} {roleDefinitions[role].label}
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            </article>
          );
        })}
      </div>

      {people.length === 50 && <p className="field-help">Showing the first 50 matches.</p>}
      {!busy && !people.length && (
        <div className="empty-state">
          <h2>No accounts match this search.</h2>
          <p>Try searching for a different email address or invite a new team member.</p>
        </div>
      )}
    </section>
  );
}

const roleDefinitions: Record<ManagedRole, { label: string; path: string }> = {
  Learner: { label: 'Learner', path: 'learner-role' },
  CourseAuthor: { label: 'Course Author', path: 'course-author-role' },
  CohortCoordinator: { label: 'Cohort Coordinator', path: 'cohort-coordinator-role' },
  LearningFacilitator: { label: 'Learning Facilitator', path: 'learning-facilitator-role' },
};

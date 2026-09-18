import { useEffect, useState, type FormEvent } from 'react';
import { request, type Session } from './api';
import { explain } from './authoring';
import {
  Alert,
  Button,
  PageHeader,
  ProgressBar,
  StatusBadge,
  Textarea,
} from './components/ui';

type Status = {
  status: string;
  licenseId?: string;
  revision?: number;
  maxActiveLearners?: number;
  activeLearners: number;
  remaining?: number;
  expiresAt?: string;
};

export function Licensing({ session }: { session: Session }) {
  const [status, setStatus] = useState<Status | null>(null);
  const [document, setDocument] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);

  async function load() {
    setBusy(true);
    try {
      setStatus(await request<Status>('/api/licensing/status'));
      setError('');
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function install(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError('');
    setNotice('');
    try {
      setStatus(
        await request<Status>('/api/licensing/license', {
          compactJws: document.trim(),
        })
      );
      setDocument('');
      setNotice('Signed license verified and installed.');
    } catch (failure) {
      setError(explain(failure));
    } finally {
      setBusy(false);
    }
  }

  const isAdmin = session.account.roles.includes('Administrator');

  return (
    <section aria-labelledby="licensing-title">
      <PageHeader
        eyebrow="OFFLINE LICENSE CONTROL"
        title="Licensing"
        titleId="licensing-title"
        description="Learner capacity comes only from a locally verified, organization-bound signed document."
      />

      {error && <Alert type="error">{error}</Alert>}
      {notice && <Alert type="success">{notice}</Alert>}

      {status && (
        <section className="license-status-card" aria-label="Current License Status">
          <div className="license-header-row">
            <div>
              <p className="eyebrow">CURRENT LICENSE ENTITLEMENT</p>
              <h2>
                {status.activeLearners} active Learners / {status.maxActiveLearners ?? 'no license'}
              </h2>
            </div>
            <StatusBadge status={status.status.toUpperCase()} />
          </div>

          {status.maxActiveLearners && status.maxActiveLearners > 0 && (
            <ProgressBar
              value={status.activeLearners}
              max={status.maxActiveLearners}
              label="Learner seat utilization"
            />
          )}

          <div className="license-metrics">
            <div className="metric-item">
              <span className="metric-label">Active Seats</span>
              <span className="metric-value">{status.activeLearners}</span>
            </div>
            <div className="metric-item">
              <span className="metric-label">Available Capacity</span>
              <span className="metric-value">
                {status.remaining === undefined ? '—' : status.remaining}
              </span>
            </div>
            <div className="metric-item">
              <span className="metric-label">Total Provisioned</span>
              <span className="metric-value">{status.maxActiveLearners ?? 0}</span>
            </div>
          </div>

          <div className="license-meta-footer">
            {status.expiresAt && (
              <span>
                Expires: <strong>{new Date(status.expiresAt).toLocaleString()}</strong>
              </span>
            )}
            {status.licenseId && (
              <span className="license-meta-tag">
                ID: {status.licenseId}
              </span>
            )}
            {status.revision !== undefined && (
              <span className="license-meta-tag">
                Revision: {status.revision}
              </span>
            )}
          </div>
        </section>
      )}

      {isAdmin && (
        <form className="card-base" onSubmit={event => void install(event)}>
          <fieldset disabled={busy}>
            <h2>Install signed license</h2>
            <Textarea
              id="license-document"
              label="Compact JWS"
              required
              maxLength={32768}
              rows={5}
              monospace
              value={document}
              onChange={event => setDocument(event.target.value)}
              placeholder="eyJhbGciOiJFUzI1NiIsImtpZCI6Li4ufQ..."
              helpText="Invalid, expired, replayed, foreign, or unsupported documents are rejected without replacing the current license."
            />
            <Button
              type="submit"
              variant="primary"
              loading={busy}
              disabled={busy || !document.trim()}
              iconRight={<span aria-hidden="true">→</span>}
            >
              Verify and install
            </Button>
          </fieldset>
        </form>
      )}
    </section>
  );
}

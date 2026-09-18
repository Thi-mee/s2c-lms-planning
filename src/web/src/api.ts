export type Session = {
  account: { id: string; name: string; email: string; roles: string[] };
  organization: { id: string; name: string };
};

export class ApiError extends Error {
  constructor(public readonly status: number, public readonly code: string) { super(code); }
}

export async function request<T>(path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = {};
  if (body !== undefined) {
    const csrf = await fetch('/api/auth/csrf', { credentials: 'same-origin', cache: 'no-store' });
    if (!csrf.ok) throw new ApiError(csrf.status, 'service_unavailable');
    headers['X-CSRF-TOKEN'] = (await csrf.json() as { token: string }).token;
    headers['Content-Type'] = 'application/json';
  }
  const response = await fetch(path, { method: body === undefined ? 'GET' : 'POST', headers,
    credentials: 'same-origin', cache: 'no-store', body: body === undefined ? undefined : JSON.stringify(body) });
  if (!response.ok) {
    const error = await response.json().catch(() => ({ code: response.status === 429 ? 'rate_limited' : 'request_failed' })) as { code?: string };
    throw new ApiError(response.status, error.code ?? 'request_failed');
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}

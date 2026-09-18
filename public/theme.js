// Two-state theme toggle: system default ↔ pinned opposite. Persisted in localStorage.
// The FOUC-avoidance read happens inline in each page <head>; this only handles clicks.
(() => {
  const meta = document.querySelector('meta[name="color-scheme"]');
  const btn = document.querySelector('[data-theme-toggle]');
  if (!meta || !btn) return;

  const resolved = () => {
    const c = meta.content.trim();
    if (c === 'light' || c === 'dark') return c;
    return matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  };
  const sync = () => {
    const r = resolved();
    btn.setAttribute('aria-pressed', String(r === 'dark'));
    btn.querySelector('[data-label]').textContent = r === 'dark' ? 'Dark' : 'Light';
  };

  btn.addEventListener('click', () => {
    const next = resolved() === 'dark' ? 'light' : 'dark';
    meta.content = next;
    localStorage.setItem('color-scheme', next);
    sync();
  });
  // React to OS changes while following system.
  matchMedia('(prefers-color-scheme: dark)').addEventListener('change', sync);
  sync();
})();

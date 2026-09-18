import { readFile } from 'node:fs/promises';
import { test, expect, type Page } from '@playwright/test';

async function login(page: Page) {
  await page.goto('/');
  await page.getByLabel('Email address').fill('administrator@example.test');
  await page.getByLabel('Password', { exact: true }).fill((await readFile('../../.local/bootstrap-password', 'utf8')).trim());
  await page.getByRole('button', { name: 'Sign in', exact: false }).click();
  await expect(page.getByRole('heading', { name: 'Welcome, Development Administrator.' })).toBeVisible();
}

test('install a signed license, invite a Learner, and accept the emailed token', async ({ page, request }, info) => {
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await login(page);
  await page.getByRole('navigation').getByRole('button', { name: 'Licensing', exact: true }).click();
  await page.getByLabel('Compact JWS').fill((await readFile('../../.local/development-license/license.jwt', 'utf8')).trim());
  await page.getByRole('button', { name: 'Verify and install' }).click();
  await expect(page.getByRole('status')).toHaveText('Signed license verified and installed.');
  await expect(page.getByRole('heading', { name: /active Learners \/ 25/ })).toBeVisible();

  const email = `browser-learner-${crypto.randomUUID().slice(0, 8)}@example.test`;
  await page.getByRole('navigation').getByRole('button', { name: 'People', exact: true }).click();
  await page.getByLabel('Name').fill('Browser Learner'); await page.getByLabel('Email address').fill(email);
  await page.getByRole('button', { name: 'Send invitation' }).click();
  await expect(page.getByRole('status')).toContainText('Invitation queued');
  const row = page.getByRole('article').filter({ hasText: email }); await expect(row).toContainText('pending');

  let messageId = '';
  await expect.poll(async () => {
    const response = await request.get(`http://127.0.0.1:8025/api/v1/search?query=${encodeURIComponent(`to:${email}`)}`);
    if (!response.ok()) return 0;
    const body = await response.json() as { messages: Array<{ ID: string }> };
    messageId = body.messages[0]?.ID ?? ''; return body.messages.length;
  }, { timeout: 15_000 }).toBe(1);
  const message = await (await request.get(`http://127.0.0.1:8025/api/v1/message/${messageId}`)).json() as { Text: string };
  const link = message.Text.match(/http:\/\/localhost:5178\/accept-invitation\?token=\S+/)?.[0];
  expect(link).toBeTruthy();
  await page.goto(link!.replace('http://localhost:5178', 'http://localhost:5078'));
  await page.getByLabel('Password', { exact: true }).fill('Browser-test-password-726!');
  await page.getByLabel('Confirm password').fill('Browser-test-password-726!');
  await page.getByRole('button', { name: 'Activate account' }).click();
  await expect(page.getByRole('status')).toHaveText('Your account is active.');
  await page.screenshot({ path: info.outputPath('learner-activated.png'), fullPage: true });
  expect(errors).toEqual([]);
});

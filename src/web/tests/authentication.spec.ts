import { readFile } from 'node:fs/promises';
import { test, expect } from '@playwright/test';

test('administrator can sign in, reload the persistent session, and sign out', async ({ page }, info) => {
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Sign in to your workspace.' })).toBeVisible();
  await page.screenshot({ path: info.outputPath('sign-in.png'), fullPage: true });
  await page.getByLabel('Email address').fill('administrator@example.test');
  await page.getByLabel('Password', { exact: true }).fill((await readFile('../../.local/bootstrap-password', 'utf8')).trim());
  await page.getByRole('button', { name: 'Sign in', exact: false }).click();
  await expect(page.getByRole('heading', { name: 'Welcome, Development Administrator.' })).toBeVisible();
  await expect(page.getByText('Variable development', { exact: true })).toBeVisible();
  await expect(page.getByText('Administrator', { exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath('workspace.png'), fullPage: true });
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Welcome, Development Administrator.' })).toBeVisible();
  await page.getByRole('button', { name: 'Sign out', exact: false }).click();
  await expect(page.getByRole('heading', { name: 'Sign in to your workspace.' })).toBeVisible();
  expect((await page.request.get('/api/auth/session')).status()).toBe(401);
  expect(errors).toEqual([]);
});

test('failed login is accessible and does not reveal an account or keep its password', async ({ page }) => {
  await page.goto('/');
  await page.getByLabel('Email address').fill('unknown@example.test');
  await page.getByLabel('Password', { exact: true }).fill('Synthetic-wrong-password');
  await page.getByRole('button', { name: 'Sign in', exact: false }).click();
  await expect(page.getByRole('alert')).toHaveText('Sign-in failed. Check your details or try again later.');
  await expect(page.getByLabel('Password', { exact: true })).toHaveValue('');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
});

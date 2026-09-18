import { readFile } from 'node:fs/promises';
import { test, expect, type Page } from '@playwright/test';

async function login(page: Page) {
  await page.goto('/');
  await page.getByLabel('Email address').fill('administrator@example.test');
  await page.getByLabel('Password', { exact: true }).fill((await readFile('../../.local/bootstrap-password', 'utf8')).trim());
  await page.getByRole('button', { name: 'Sign in', exact: false }).click();
  await expect(page.getByRole('heading', { name: 'Welcome, Development Administrator.' })).toBeVisible();
}

test('create, validate, edit and publish a course through the web app', async ({ page }, info) => {
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await login(page); await page.getByRole('navigation').getByRole('button', { name: 'Courses', exact: true }).click();
  await page.getByRole('button', { name: 'New course', exact: false }).click();
  const title = `Synthetic onboarding ${crypto.randomUUID().slice(0, 8)}`;
  await page.getByLabel('Course title', { exact: true }).fill(title);
  await page.getByLabel('Description', { exact: true }).fill('Our first course, from draft to publication.');
  await page.getByRole('button', { name: 'Create draft', exact: false }).click();
  await expect(page.getByRole('heading', { name: title })).toBeVisible();
  await page.getByRole('button', { name: 'Publish course', exact: false }).click();
  await expect(page.getByRole('alert')).toContainText('Add at least one module');
  await page.getByRole('button', { name: 'Add module', exact: false }).click();
  await page.getByLabel('Module title').fill('Welcome to s2c');
  await page.getByRole('button', { name: 'Add lesson', exact: false }).click();
  await page.getByLabel('Lesson title').fill('A shared way of working');
  const notes = 'Welcome to the team.\n\n<svg onload="window.untrustedLesson = true"> is plain text, never executable markup.';
  await page.getByLabel('Lesson notes').fill(notes);
  await expect(page.getByLabel('Required for completion')).toBeChecked();
  await expect(page.getByRole('button', { name: 'Publish course', exact: false })).toBeDisabled();
  await page.getByRole('button', { name: 'Save draft', exact: true }).click();
  await expect(page.getByRole('status')).toHaveText('Draft saved.');
  await page.screenshot({ path: info.outputPath('course-editor.png'), fullPage: true });
  await page.getByRole('button', { name: 'Back to courses', exact: false }).click();
  await page.getByRole('button', { name: new RegExp(title) }).click();
  await expect(page.getByLabel('Lesson notes')).toHaveValue(notes);
  await page.getByRole('button', { name: 'Publish course', exact: false }).click();
  await expect(page.getByRole('status')).toContainText('Course published.');
  await expect(page.getByText(notes, { exact: true })).toBeVisible();
  await expect(page.getByLabel('Lesson notes')).toHaveCount(0);
  expect(await page.evaluate(() => 'untrustedLesson' in window)).toBe(false);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath('published-course.png'), fullPage: true });
  expect(errors).toEqual([]);
});

test('granting or revoking own Course Author access requires signing in again', async ({ page }) => {
  await login(page);
  await page.getByRole('navigation').getByRole('button', { name: 'People', exact: true }).click();
  const person = page.getByRole('article').filter({ hasText: 'administrator@example.test' });
  const button = person.getByRole('button', { name: /(?:Grant|Revoke) Course Author/ });
  const wasGranted = (await button.textContent())?.startsWith('Revoke');
  await button.click();
  await page.getByLabel('Reason for this change').fill('Synthetic browser workflow validation');
  await page.getByRole('button', { name: 'Confirm access change' }).click();
  await expect(page.getByRole('heading', { name: 'Sign in to your workspace.' })).toBeVisible();
  await login(page);
  if (wasGranted) await expect(page.getByText('Course Author', { exact: true })).toHaveCount(0);
  else await expect(page.getByText('Course Author', { exact: true })).toBeVisible();
});

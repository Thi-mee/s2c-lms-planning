import { readFile } from 'node:fs/promises';
import { test, expect, type Page } from '@playwright/test';

async function login(page: Page) {
  await page.goto('/');
  await page.getByLabel('Email address').fill('administrator@example.test');
  await page.getByLabel('Password', { exact: true }).fill((await readFile('../../.local/bootstrap-password', 'utf8')).trim());
  await page.getByRole('button', { name: 'Sign in', exact: false }).click();
  await expect(page.getByRole('heading', { name: 'Welcome, Development Administrator.' })).toBeVisible();
}

async function ensureRole(page: Page, label: string) {
  if (await page.getByText(label, { exact: true }).count()) return;
  await page.getByRole('navigation').getByRole('button', { name: 'People', exact: true }).click();
  const person = page.getByRole('article').filter({ hasText: 'administrator@example.test' });
  await person.getByRole('button', { name: `Grant ${label}` }).click();
  await page.getByLabel('Reason for this change').fill('Synthetic cohort browser workflow');
  await page.getByRole('button', { name: 'Confirm access change' }).click();
  await expect(page.getByRole('heading', { name: 'Sign in to your workspace.' })).toBeVisible();
  await login(page);
}

async function publishCourse(page: Page, title: string) {
  await page.getByRole('navigation').getByRole('button', { name: 'Courses', exact: true }).click();
  await page.getByRole('button', { name: 'New course', exact: false }).click();
  await page.getByLabel('Course title', { exact: true }).fill(title);
  await page.getByRole('button', { name: 'Create draft', exact: false }).click();
  await page.getByRole('button', { name: 'Add module', exact: false }).click();
  await page.getByLabel('Module title').fill('Welcome');
  await page.getByRole('button', { name: 'Add lesson', exact: false }).click();
  await page.getByLabel('Lesson title').fill('Start here');
  await page.getByLabel('Lesson notes').fill('Required cohort lesson.');
  await page.getByRole('button', { name: 'Save draft', exact: true }).click();
  await page.getByRole('button', { name: 'Publish course', exact: false }).click();
  await expect(page.getByRole('status')).toContainText('Course published.');
}

test('schedule a published course with one person holding both staff capabilities', async ({ page }, info) => {
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await login(page);
  await ensureRole(page, 'Cohort Coordinator');
  await ensureRole(page, 'Learning Facilitator');
  const suffix = crypto.randomUUID().slice(0, 8); const course = `Cohort course ${suffix}`; const cohort = `September cohort ${suffix}`;
  await publishCourse(page, course);
  await page.getByRole('navigation').getByRole('button', { name: 'Cohorts', exact: true }).click();
  await page.getByRole('button', { name: 'New cohort', exact: false }).click();
  await page.getByLabel('Cohort title').fill(cohort);
  await page.getByLabel('Published course').selectOption({ label: course });
  await page.getByLabel('Facilitator (optional)').selectOption({ label: 'Development Administrator — administrator@example.test' });
  await page.getByRole('button', { name: 'Schedule cohort', exact: false }).click();
  await expect(page.getByRole('heading', { name: cohort })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Cohort staff' })).toBeVisible();
  await expect(page.getByText('coordinator', { exact: true })).toBeVisible();
  await expect(page.getByText('facilitator', { exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath('staffed-cohort.png'), fullPage: true });
  expect(errors).toEqual([]);
});

import { readFile } from 'node:fs/promises';
import { test } from '@playwright/test';

async function login(page: any) {
  await page.goto('/');
  await page.getByLabel('Email address').fill('administrator@example.test');
  await page.getByLabel('Password', { exact: true }).fill(
    (await readFile('../../.local/bootstrap-password', 'utf8')).trim()
  );
  await page.getByRole('button', { name: 'Sign in', exact: false }).click();
  await page.waitForLoadState('networkidle');
}

test('capture visual audit screenshots', async ({ page }) => {
  await login(page);

  // 1. Overview
  await page.screenshot({ path: 'test-results/visual-verification/01-overview.png', fullPage: true });

  // 2. Licensing
  await page.getByRole('navigation').getByRole('button', { name: 'Licensing', exact: true }).click();
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-results/visual-verification/02-licensing.png', fullPage: true });

  // 3. People
  await page.getByRole('navigation').getByRole('button', { name: 'People', exact: true }).click();
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-results/visual-verification/03-people-list.png', fullPage: true });

  // 4. People - Role Change Modal
  const person = page.getByRole('article').first();
  const grantOrRevoke = person.getByRole('button', { name: /(?:Grant|Revoke) Learner/ });
  if (await grantOrRevoke.count()) {
    await grantOrRevoke.click();
    await page.waitForTimeout(300);
    await page.screenshot({ path: 'test-results/visual-verification/04-people-modal.png', fullPage: true });
    await page.getByRole('button', { name: 'Cancel' }).click();
    await page.waitForTimeout(300);
  }

  // 5. Courses
  await page.getByRole('navigation').getByRole('button', { name: 'Courses', exact: true }).click();
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-results/visual-verification/06-courses.png', fullPage: true });

  // 6. Cohorts
  await page.getByRole('navigation').getByRole('button', { name: 'Cohorts', exact: true }).click();
  await page.waitForTimeout(500);
  const firstCohort = page.locator('.course-card').first();
  if (await firstCohort.count()) {
    await firstCohort.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: 'test-results/visual-verification/05-cohort-detail.png', fullPage: true });
  }
});

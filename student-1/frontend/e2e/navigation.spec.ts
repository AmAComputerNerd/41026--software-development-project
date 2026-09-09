import { test, expect } from '@playwright/test'

test.describe('Navigation & Shell Integration', () => {
  test.beforeEach(async ({ page }) => {
    await page.route(/^http:\/\/localhost:5101\/notifications/, async (route) => {
      if (!route.request().url().includes('/stream')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: '11111111-1111-1111-1111-111111111101',
              studentId: '11111111-1111-1111-1111-111111111111',
              type: 'Deadline',
              sourceMicroservice: 'student-3-backend',
              message: 'Upcoming assignment deadline',
              isRead: false,
              createdAtUtc: new Date().toISOString(),
              relatedEntityType: null,
              relatedEntityId: null,
              actionPayload: null,
            },
          ]),
        })
      } else {
        await route.fulfill({
          status: 200,
          contentType: 'text/event-stream',
          body: 'event: connected\ndata: {}\n\n',
        })
      }
    })

    await page.route(/^http:\/\/localhost:5101\/preferences/, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      })
    })

    await page.route(/^http:\/\/localhost:5101\/digest/, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      })
    })

    await page.goto('./')
  })

  test('switches between tab views correctly', async ({ page }) => {
    // Starts on 01 LIST
    await expect(page.locator('.nb-tabstrip__tab--active')).toContainText('01 LIST')
    await expect(page.locator('.nb-list__title')).toHaveText('ALL NOTIFICATIONS')

    // Navigate to 02 PREFS
    await page.getByRole('link', { name: '02 PREFS' }).click()
    await expect(page).toHaveURL(/.*\/preferences/)
    await expect(page.locator('.nb-tabstrip__tab--active')).toContainText('02 PREFS')
    await expect(page.locator('.nb-prefs__title')).toHaveText('PREFERENCES')

    // Navigate to 03 DIGEST
    await page.getByRole('link', { name: '03 DIGEST' }).click()
    await expect(page).toHaveURL(/.*\/digest/)
    await expect(page.locator('.nb-tabstrip__tab--active')).toContainText('03 DIGEST')
    await expect(page.locator('.nb-digest__title')).toHaveText('AI DIGEST')

    // Navigate back to 01 LIST
    await page.getByRole('link', { name: '01 LIST' }).click()
    await expect(page).toHaveURL(/.*\/notifications\/?$/)
    await expect(page.locator('.nb-tabstrip__tab--active')).toContainText('01 LIST')
  })

  test('navbar NotificationCentre displays unread badge and dropdown', async ({ page }) => {
    const bellBtn = page.locator('.nb-bell')
    await expect(bellBtn).toBeVisible()

    // Unread badge is 1
    const badge = page.locator('.nb-bell .nb-badge')
    await expect(badge).toHaveText('1')

    // Open dropdown
    await bellBtn.click()
    const dropdown = page.locator('.nb-centre')
    await expect(dropdown).toBeVisible()
    await expect(dropdown).toContainText('Upcoming assignment deadline')

    // Close dropdown
    await bellBtn.click()
    await expect(dropdown).not.toBeVisible()
  })
})

import { test, expect } from '@playwright/test'

test.describe('Notification Preferences View', () => {
  const mockPreferences = [
    {
      id: '44444444-4444-4444-4444-444444444401',
      studentId: '11111111-1111-1111-1111-111111111111',
      type: 'Deadline',
      channel: 'InApp',
      enabled: true,
      updatedAtUtc: new Date().toISOString(),
    },
    {
      id: '44444444-4444-4444-4444-444444444402',
      studentId: '11111111-1111-1111-1111-111111111111',
      type: 'Deadline',
      channel: 'Email',
      enabled: false,
      updatedAtUtc: new Date().toISOString(),
    },
  ]

  test.beforeEach(async ({ page }) => {
    await page.route(/^http:\/\/localhost:5101\/notifications/, async (route) => {
      if (!route.request().url().includes('/stream')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([]),
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
      const request = route.request()
      if (request.method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(mockPreferences),
        })
      } else if (request.method() === 'POST' || request.method() === 'PUT') {
        const body = JSON.parse(request.postData() || '{}')
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: '44444444-4444-4444-4444-444444444403',
            studentId: body.studentId,
            type: body.type,
            channel: body.channel,
            enabled: body.enabled,
            updatedAtUtc: new Date().toISOString(),
          }),
        })
      } else {
        await route.continue()
      }
    })

    await page.goto('./preferences')
  })

  test('renders preferences table and channel columns', async ({ page }) => {
    await expect(page.locator('.nb-prefs__title')).toHaveText('PREFERENCES')
    await expect(page.locator('.nb-table__head')).toContainText('TYPE')
    await expect(page.locator('.nb-table__head')).toContainText('IN-APP')
    await expect(page.locator('.nb-table__head')).toContainText('EMAIL')

    // 5 types: Deadlines, Grades, Automations, Account, AI Digests
    await expect(page.locator('.nb-table__row')).toHaveCount(5)
  })

  test('toggles channel preference state', async ({ page }) => {
    // Find the toggle for Deadline Email
    const toggleGroup = page.locator('div[role="group"][aria-label="DEADLINE EMAIL"]')
    await expect(toggleGroup).toBeVisible()

    // Deadlines Email starts OFF in mock data
    const offBtn = toggleGroup.getByRole('button', { name: 'OFF' })
    await expect(offBtn).toHaveClass(/nb-toggle__cell--active/)

    // Click ON to toggle
    const onBtn = toggleGroup.getByRole('button', { name: 'ON' })
    await onBtn.click()
    await expect(onBtn).toHaveClass(/nb-toggle__cell--active/)
  })
})

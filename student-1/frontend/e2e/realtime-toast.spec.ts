import { test, expect } from '@playwright/test'

test.describe('Real-Time SSE Toast Alerts', () => {
  const realtimeNotification = {
    id: '99999999-9999-9999-9999-999999999999',
    studentId: '11111111-1111-1111-1111-111111111111',
    type: 'Deadline',
    sourceMicroservice: 'student-3-backend',
    message: 'URGENT: Physics Lab Report due in 1 hour!',
    isRead: false,
    createdAtUtc: new Date().toISOString(),
    relatedEntityType: 'Assignment',
    relatedEntityId: '88888888-8888-8888-8888-888888888888',
    actionPayload: null,
  }

  test.beforeEach(async ({ page }) => {
    await page.route(/^http:\/\/localhost:5101\/notifications/, async (route) => {
      const request = route.request()
      if (request.method() === 'GET' && request.url().includes('/stream')) {
        const ssePayload = `event: connected\ndata: {}\n\ndata: ${JSON.stringify(realtimeNotification)}\n\n`
        await route.fulfill({
          status: 200,
          contentType: 'text/event-stream',
          body: ssePayload,
        })
      } else if (request.method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([]),
        })
      } else if (request.method() === 'PUT' && request.url().includes('/read')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ ...realtimeNotification, isRead: true }),
        })
      } else {
        await route.continue()
      }
    })

    await page.route(/^http:\/\/localhost:5101\/preferences/, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      })
    })

    await page.goto('./')
  })

  test('displays real-time toast alert when SSE event is received', async ({ page }) => {
    const toast = page.locator('.nb-toast')
    await expect(toast).toBeVisible()
    await expect(toast.locator('.nb-toast__live-badge')).toHaveText('LIVE EVENT')
    await expect(toast.locator('.nb-toast__message')).toContainText('Physics Lab Report due in 1 hour!')

    // Dismiss button closes toast
    const dismissBtn = toast.getByRole('button', { name: 'DISMISS' })
    await dismissBtn.click()
    await expect(toast).not.toBeVisible()
  })

  test('marks notification as read directly from toast alert', async ({ page }) => {
    const toast = page.locator('.nb-toast')
    await expect(toast).toBeVisible()

    const markReadBtn = toast.getByRole('button', { name: 'MARK READ' })
    await markReadBtn.click()
    await expect(toast).not.toBeVisible()
  })
})

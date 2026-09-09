import { test, expect } from '@playwright/test'

test.describe('Notifications View', () => {
  const mockNotifications = [
    {
      id: '11111111-1111-1111-1111-111111111101',
      studentId: '11111111-1111-1111-1111-111111111111',
      type: 'Deadline',
      sourceMicroservice: 'student-3-backend',
      message: 'Assignment 1 is due tomorrow at 5:00 PM',
      isRead: false,
      createdAtUtc: new Date(Date.now() - 3600000).toISOString(),
      relatedEntityType: 'Assignment',
      relatedEntityId: '22222222-2222-2222-2222-222222222222',
      actionPayload: '{"taskId":"t-101"}',
    },
    {
      id: '11111111-1111-1111-1111-111111111102',
      studentId: '11111111-1111-1111-1111-111111111111',
      type: 'Grade',
      sourceMicroservice: 'student-5-backend',
      message: 'Quiz 2 marks released: 18/20 (90%)',
      isRead: true,
      createdAtUtc: new Date(Date.now() - 86400000).toISOString(),
      relatedEntityType: 'Assignment',
      relatedEntityId: '33333333-3333-3333-3333-333333333333',
      actionPayload: null,
    },
    {
      id: '11111111-1111-1111-1111-111111111103',
      studentId: '11111111-1111-1111-1111-111111111111',
      type: 'Account',
      sourceMicroservice: 'student-4-backend',
      message: 'Student account profile details updated successfully',
      isRead: false,
      createdAtUtc: new Date(Date.now() - 172800000).toISOString(),
      relatedEntityType: null,
      relatedEntityId: null,
      actionPayload: null,
    },
  ]

  test.beforeEach(async ({ page }) => {
    await page.route(/^http:\/\/localhost:5101\/notifications/, async (route) => {
      const request = route.request()
      if (request.method() === 'GET' && !request.url().includes('/stream')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(mockNotifications),
        })
      } else if (request.method() === 'GET' && request.url().includes('/stream')) {
        await route.fulfill({
          status: 200,
          contentType: 'text/event-stream',
          body: 'event: connected\ndata: {}\n\n',
        })
      } else if (request.method() === 'PUT' && request.url().includes('/read')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ ...mockNotifications[0], isRead: true }),
        })
      } else if (request.method() === 'PUT' && request.url().includes('/unread')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ ...mockNotifications[1], isRead: false }),
        })
      } else if (request.method() === 'DELETE') {
        await route.fulfill({ status: 200 })
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

    // Mock cross-service endpoints for dialogs
    await page.route('**/api/tasks/**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      })
    })

    await page.route('**/api/grades/**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      })
    })

    await page.goto('./')
  })

  test('renders page title and notifications count', async ({ page }) => {
    await expect(page.locator('.nb-list__title')).toHaveText('ALL NOTIFICATIONS')
    await expect(page.locator('.nb-list__count')).toContainText('3 SHOWN')
    await expect(page.locator('.nb-row')).toHaveCount(3)
  })

  test('filters notifications by type chips', async ({ page }) => {
    // Toggling DEADLINE chip off filters out Deadline notifications
    await page.getByRole('button', { name: 'DEADLINE', exact: true }).click()
    await expect(page.locator('.nb-row')).toHaveCount(2)
    await expect(page.locator('.nb-list__panel')).not.toContainText('Assignment 1 is due tomorrow')

    // Click ALL filter chip to restore all filters
    await page.getByRole('button', { name: 'ALL', exact: true }).click()
    await expect(page.locator('.nb-row')).toHaveCount(3)
    await expect(page.locator('.nb-list__panel')).toContainText('Assignment 1 is due tomorrow')
  })

  test('sorts notifications by newest and oldest', async ({ page }) => {
    const oldestButton = page.getByRole('button', { name: 'OLDEST FIRST' })
    await oldestButton.click()
    await expect(oldestButton).toHaveClass(/nb-chip--active/)

    const firstRowMessage = page.locator('.nb-row .nb-row__message').first()
    await expect(firstRowMessage).toContainText('Student account profile')

    const newestButton = page.getByRole('button', { name: 'NEWEST FIRST' })
    await newestButton.click()
    await expect(newestButton).toHaveClass(/nb-chip--active/)
    await expect(page.locator('.nb-row .nb-row__message').first()).toContainText('Assignment 1 is due')
  })

  test('marks an unread notification as read', async ({ page }) => {
    const markReadBtn = page.getByRole('button', { name: 'MARK READ' }).first()
    await expect(markReadBtn).toBeVisible()
    await markReadBtn.click()

    await expect(page.getByRole('button', { name: /READ — UNMARK/ }).first()).toBeVisible()
  })

  test('opens AI Task Breakdown dialog on actionable deadline', async ({ page }) => {
    const breakdownBtn = page.getByRole('button', { name: 'AI BREAK DOWN' })
    await expect(breakdownBtn).toBeVisible()
    await breakdownBtn.click()

    // Dialog appears
    await expect(page.locator('.nb-dialog')).toBeVisible()
    await expect(page.locator('.nb-dialog__header-title')).toContainText('AI TASK BREAKDOWN')

    // Close dialog
    await page.locator('.nb-dialog__close').click()
    await expect(page.locator('.nb-dialog')).not.toBeVisible()
  })

  test('opens Grade Impact dialog on grade notification', async ({ page }) => {
    const gradeImpactBtn = page.getByRole('button', { name: 'GRADE IMPACT' })
    await expect(gradeImpactBtn).toBeVisible()
    await gradeImpactBtn.click()

    // Dialog appears
    await expect(page.locator('.nb-dialog')).toBeVisible()
    await expect(page.locator('.nb-dialog__header-title')).toContainText('GRADE IMPACT SIMULATOR')

    // Close dialog
    await page.locator('.nb-dialog__close').click()
    await expect(page.locator('.nb-dialog')).not.toBeVisible()
  })
})

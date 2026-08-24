// Simple fetch-based client for the student-1 Notification Service.
// No axios: this is the only API surface the frontend needs so far.

import { buildQuery, request } from './http'

export function getNotifications({ studentId, type, isRead } = {}) {
  const query = buildQuery({ studentId, type, isRead })
  return request(`/notifications${query}`)
}

export function markNotificationRead(id) {
  return request(`/notifications/${id}/read`, { method: 'PUT' })
}

export function markAllNotificationsRead(studentId) {
  const query = buildQuery({ studentId })
  return request(`/notifications/read-all${query}`, { method: 'PUT' })
}

// Fetch-based client for the NotificationPreference CRUD endpoints.

import { buildQuery, request } from './http'

export function getPreferences(studentId) {
  const query = buildQuery({ studentId })
  return request(`/preferences${query}`)
}

export function createPreference({ studentId, type, channel, enabled }) {
  return request('/preferences', {
    method: 'POST',
    body: JSON.stringify({ studentId, type, channel, enabled }),
  })
}

export function updatePreference(id, { studentId, type, channel, enabled }) {
  return request(`/preferences/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ studentId, type, channel, enabled }),
  })
}

export function deletePreference(id) {
  return request(`/preferences/${id}`, { method: 'DELETE' })
}

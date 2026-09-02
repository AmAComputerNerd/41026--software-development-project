// Minimal fetch-based client for the student-3 Deadlines/Tasks service.
// Used only to complete a task inline from a Deadline notification.

const DEADLINES_BASE_URL = import.meta.env.VITE_DEADLINES_API_BASE_URL || 'http://localhost:5103/api'

export async function completeTask(id) {
  const response = await fetch(`${DEADLINES_BASE_URL}/tasks/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      newTitle: null,
      updateDescription: false,
      newDescription: null,
      updateDueDate: false,
      newDueDate: null,
      newPriority: null,
      newStatus: 'Completed',
    }),
  })

  if (!response.ok && response.status !== 404) {
    throw new Error(`Task API request failed: ${response.status} ${response.statusText}`)
  }

  return response.status === 404 ? null : response.json()
}

export async function breakdownTask(id, prompt = 'Break down this task into actionable subtasks', priority = 'Medium') {
  const response = await fetch(`${DEADLINES_BASE_URL}/tasks/${id}/ai-breakdown`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt, priority }),
  })

  if (!response.ok) {
    throw new Error(`Task breakdown failed: ${response.status} ${response.statusText}`)
  }

  return response.json()
}


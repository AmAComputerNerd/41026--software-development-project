import { onBeforeUnmount, onMounted, ref } from 'vue'
import { CURRENT_STUDENT_ID, NOTIFICATIONS_API_BASE_URL } from '@/config'

export function useUnreadCount() {
  const count = ref(0)
  let eventSource: EventSource | null = null

  async function load() {
    try {
      const query = new URLSearchParams({ studentId: CURRENT_STUDENT_ID, isRead: 'false' })
      const response = await fetch(`${NOTIFICATIONS_API_BASE_URL}/notifications?${query}`)
      if (!response.ok) throw new Error(`Notification API request failed: ${response.status}`)
      const unread = await response.json()
      count.value = Array.isArray(unread) ? unread.length : 0
    } catch (err) {
      console.error('Failed to load unread notification count:', err)
      count.value = 0
    }
  }

  function startStream() {
    try {
      const url = `${NOTIFICATIONS_API_BASE_URL}/notifications/stream?studentId=${CURRENT_STUDENT_ID}`
      eventSource = new EventSource(url)
      eventSource.onmessage = (e) => {
        if (e.data) {
          try {
            const data = JSON.parse(e.data)
            if (data && data.id) {
              count.value++
            }
          } catch {
            load()
          }
        }
      }
    } catch (err) {
      console.warn('Notification SSE stream not available in shared shell:', err)
    }
  }

  onMounted(() => {
    load()
    startStream()
  })

  onBeforeUnmount(() => {
    if (eventSource) {
      eventSource.close()
      eventSource = null
    }
  })

  return { count, load }
}

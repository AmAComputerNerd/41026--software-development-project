import { ref } from 'vue'
import { CURRENT_STUDENT_ID, NOTIFICATIONS_API_BASE_URL } from '@/config'

export interface NotificationDto {
  id: string
  studentId: string
  type: string
  sourceMicroservice: string
  message: string
  isRead: boolean
  createdAtUtc: string
}

const MAX_ITEMS = 10

export function useNotifications() {
  const notifications = ref<NotificationDto[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function load() {
    loading.value = true
    error.value = null
    try {
      const query = new URLSearchParams({ studentId: CURRENT_STUDENT_ID })
      const response = await fetch(`${NOTIFICATIONS_API_BASE_URL}/notifications?${query}`)
      if (!response.ok) throw new Error(`Notification API request failed: ${response.status}`)
      const all: NotificationDto[] = await response.json()
      notifications.value = all
        .sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())
        .slice(0, MAX_ITEMS)
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load notifications'
      notifications.value = []
    } finally {
      loading.value = false
    }
  }

  return { notifications, loading, error, load }
}

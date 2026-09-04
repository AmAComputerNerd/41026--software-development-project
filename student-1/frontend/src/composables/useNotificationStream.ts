import { onBeforeUnmount, onMounted, ref } from 'vue'
import { CURRENT_STUDENT_ID, NOTIFICATIONS_API_BASE_URL } from '@/config'
import type { NotificationDto } from './useNotifications'

export function useNotificationStream(onNotification?: (n: NotificationDto) => void) {
  const isConnected = ref(false)
  let eventSource: EventSource | null = null
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null

  function connect() {
    if (eventSource) return

    const streamUrl = `${NOTIFICATIONS_API_BASE_URL}/notifications/stream?studentId=${CURRENT_STUDENT_ID}`
    eventSource = new EventSource(streamUrl)

    eventSource.onopen = () => {
      isConnected.value = true
    }

    eventSource.onmessage = (event) => {
      if (!event.data) return
      try {
        const notification: NotificationDto = JSON.parse(event.data)
        if (notification && notification.id) {
          onNotification?.(notification)
        }
      } catch (err) {
        console.error('Failed to parse SSE notification:', err)
      }
    }

    eventSource.onerror = () => {
      isConnected.value = false
      eventSource?.close()
      eventSource = null

      if (!reconnectTimer) {
        reconnectTimer = setTimeout(() => {
          reconnectTimer = null
          connect()
        }, 3000)
      }
    }
  }

  function disconnect() {
    if (reconnectTimer) {
      clearTimeout(reconnectTimer)
      reconnectTimer = null
    }
    if (eventSource) {
      eventSource.close()
      eventSource = null
    }
    isConnected.value = false
  }

  onMounted(connect)
  onBeforeUnmount(disconnect)

  return { isConnected, connect, disconnect }
}

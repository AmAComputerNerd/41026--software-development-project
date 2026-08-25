import { computed, ref } from 'vue'
import {
  deleteNotification as deleteNotificationRequest,
  getNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  markNotificationUnread,
} from '@/api/notifications'
import { CURRENT_STUDENT_ID } from '@/config'

export interface NotificationDto {
  id: string
  studentId: string
  type: string
  sourceMicroservice: string
  message: string
  isRead: boolean
  createdAtUtc: string
}

// Matches the backend `NotificationType` enum names exactly — the API
// filters with a plain string equality check against `Type.ToString()`.
export const NOTIFICATION_TYPES = [
  { value: 'Deadline', label: 'DEADLINE', tagClass: 'nb-tag--deadline' },
  { value: 'Grade', label: 'GRADE', tagClass: 'nb-tag--grade' },
  { value: 'Automation', label: 'AUTOMATION', tagClass: 'nb-tag--automation' },
  { value: 'Account', label: 'ACCOUNT', tagClass: 'nb-tag--account' },
  { value: 'AI', label: 'AI', tagClass: 'nb-tag--ai' },
]

export function tagClassForType(type: string) {
  return NOTIFICATION_TYPES.find((t) => t.value === type)?.tagClass ?? 'nb-tag--automation'
}

// Module-level state shared by every call site, so NotificationCentre and
// NotificationsView stay in sync without a page reload.
const notifications = ref<NotificationDto[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

const activeFilters = ref<Record<string, boolean>>(
  Object.fromEntries(NOTIFICATION_TYPES.map((t) => [t.value, true])),
)

export type SortOrder = 'newest' | 'oldest'

const sortOrder = ref<SortOrder>('newest')

export function useNotifications() {
  const unreadCount = computed(() => notifications.value.filter((n) => !n.isRead).length)

  const filteredNotifications = computed(() => {
    const filtered = notifications.value.filter((n) => activeFilters.value[n.type])
    const direction = sortOrder.value === 'newest' ? -1 : 1
    return [...filtered].sort(
      (a, b) =>
        direction * (new Date(a.createdAtUtc).getTime() - new Date(b.createdAtUtc).getTime()),
    )
  })

  async function fetchNotifications() {
    loading.value = true
    error.value = null
    try {
      notifications.value = await getNotifications({ studentId: CURRENT_STUDENT_ID })
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load notifications'
    } finally {
      loading.value = false
    }
  }

  function toggleFilter(type: string) {
    activeFilters.value[type] = !activeFilters.value[type]
  }

  function setAllFilters(value: boolean) {
    NOTIFICATION_TYPES.forEach((t) => (activeFilters.value[t.value] = value))
  }

  function setSortOrder(value: SortOrder) {
    sortOrder.value = value
  }

  async function markAsRead(id: string) {
    const notification = notifications.value.find((n) => n.id === id)
    if (!notification || notification.isRead) return

    notification.isRead = true
    try {
      await markNotificationRead(id)
    } catch (err) {
      notification.isRead = false
      error.value = err instanceof Error ? err.message : 'Failed to mark notification as read'
    }
  }

  async function markAsUnread(id: string) {
    const notification = notifications.value.find((n) => n.id === id)
    if (!notification || !notification.isRead) return

    notification.isRead = false
    try {
      await markNotificationUnread(id)
    } catch (err) {
      notification.isRead = true
      error.value = err instanceof Error ? err.message : 'Failed to mark notification as unread'
    }
  }

  async function markAllAsRead() {
    const unread = notifications.value.filter((n) => !n.isRead)
    if (unread.length === 0) return

    unread.forEach((n) => (n.isRead = true))
    try {
      await markAllNotificationsRead(CURRENT_STUDENT_ID)
    } catch (err) {
      unread.forEach((n) => (n.isRead = false))
      error.value = err instanceof Error ? err.message : 'Failed to mark all notifications as read'
    }
  }

  async function deleteNotification(id: string) {
    const index = notifications.value.findIndex((n) => n.id === id)
    if (index === -1) return

    const [removed] = notifications.value.splice(index, 1)
    try {
      await deleteNotificationRequest(id)
    } catch (err) {
      notifications.value.splice(index, 0, removed)
      error.value = err instanceof Error ? err.message : 'Failed to delete notification'
    }
  }

  return {
    notifications,
    loading,
    error,
    activeFilters,
    sortOrder,
    unreadCount,
    filteredNotifications,
    fetchNotifications,
    toggleFilter,
    setAllFilters,
    setSortOrder,
    markAsRead,
    markAsUnread,
    markAllAsRead,
    deleteNotification,
  }
}

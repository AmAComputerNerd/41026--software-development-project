<script lang="ts">
export interface NotificationItem {
  id: string
  studentId: string
  type: string
  sourceMicroservice: string
  message: string
  isRead: boolean
  createdAtUtc: string
}
</script>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import NotificationDropdown from './NotificationDropdown.vue'

const studentId = import.meta.env.VITE_STUDENT_ID || '11111111-1111-1111-1111-111111111111'
const apiBase = import.meta.env.VITE_NOTIFICATIONS_API_BASE_URL || '/api/notifications'
const root = ref<HTMLElement | null>(null)
const notifications = ref<NotificationItem[]>([])
const loading = ref(false)
const error = ref('')
const open = ref(false)

const unreadCount = computed(() => notifications.value.filter((item) => !item.isRead).length)
const recentNotifications = computed(() =>
  [...notifications.value]
    .sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())
    .slice(0, 10),
)

async function loadNotifications() {
  loading.value = true
  error.value = ''
  try {
    const query = new URLSearchParams({ studentId })
    const response = await fetch(`${apiBase}/notifications?${query}`)
    if (!response.ok) throw new Error(`Notification API request failed: ${response.status}`)
    notifications.value = (await response.json()) as NotificationItem[]
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to load notifications.'
  } finally {
    loading.value = false
  }
}

function toggle() {
  open.value = !open.value
  if (open.value) loadNotifications()
}

function handleOutsideClick(event: MouseEvent) {
  if (open.value && root.value && !root.value.contains(event.target as Node)) open.value = false
}

onMounted(() => {
  loadNotifications()
  document.addEventListener('click', handleOutsideClick)
})
onBeforeUnmount(() => document.removeEventListener('click', handleOutsideClick))
</script>

<template>
  <div ref="root" class="nb-notifications">
    <button
      class="nb-btn nb-btn--outline nb-notifications__button"
      type="button"
      aria-label="Notifications"
      :aria-expanded="open"
      @click="toggle"
    >
      <svg aria-hidden="true" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
        <path d="M13.73 21a2 2 0 0 1-3.46 0" />
      </svg>
      <span v-if="unreadCount" class="nb-badge nb-notifications__badge">{{ unreadCount > 99 ? '99+' : unreadCount }}</span>
    </button>
    <NotificationDropdown
      v-if="open"
      :notifications="recentNotifications"
      :loading="loading"
      :error="error || null"
    />
  </div>
</template>

<style scoped>
.nb-notifications { position: relative; }
.nb-notifications__button { position: relative; display: inline-flex; align-items: center; justify-content: center; width: 44px; height: 44px; padding: 0; }
.nb-notifications__badge { position: absolute; top: -8px; right: -8px; }
</style>

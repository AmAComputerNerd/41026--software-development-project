<script setup lang="ts">
import type { NotificationItem } from './NotificationButton.vue'

defineProps<{
  notifications: NotificationItem[]
  loading: boolean
  error: string | null
}>()

function formatRelativeTime(isoDate: string) {
  const hasTimezone = /[zZ]|[+-]\d\d:\d\d$/.test(isoDate)
  const date = new Date(hasTimezone ? isoDate : `${isoDate}Z`)
  const diffMinutes = Math.round((Date.now() - date.getTime()) / 60_000)
  if (diffMinutes < 1) return 'now'
  if (diffMinutes < 60) return `${diffMinutes}m ago`
  const diffHours = Math.round(diffMinutes / 60)
  if (diffHours < 24) return `${diffHours}h ago`
  const diffDays = Math.round(diffHours / 24)
  if (diffDays < 7) return `${diffDays}d ago`
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}
</script>

<template>
  <div class="nb-panel nb-notif-dropdown">
    <div v-if="loading" class="nb-notif-dropdown__empty nb-mono">LOADING&hellip;</div>
    <div v-else-if="error" class="nb-notif-dropdown__empty nb-mono">FAILED TO LOAD</div>
    <div v-else-if="notifications.length === 0" class="nb-notif-dropdown__empty nb-mono">
      NO NOTIFICATIONS YET
    </div>
    <ul v-else class="nb-notif-dropdown__list">
      <li
        v-for="notification in notifications"
        :key="notification.id"
        class="nb-notif-dropdown__item"
        :class="{ 'nb-notif-dropdown__item--unread': !notification.isRead }"
      >
        <p class="nb-notif-dropdown__message">{{ notification.message }}</p>
        <div class="nb-notif-dropdown__meta nb-mono">
          <span>{{ notification.sourceMicroservice }}</span>
          <span>{{ formatRelativeTime(notification.createdAtUtc) }}</span>
        </div>
      </li>
    </ul>
    <a href="/notifications/" class="nb-btn nb-notif-dropdown__footer">VIEW ALL &rarr;</a>
  </div>
</template>

<style scoped>
.nb-notif-dropdown { position: absolute; z-index: 20; top: calc(100% + 12px); right: 0; width: 380px; max-width: 90vw; max-height: 70vh; overflow-y: auto; padding: var(--nb-space-4); }
.nb-notif-dropdown__empty { padding: 24px 0; color: var(--nb-color-muted); text-align: center; }
.nb-notif-dropdown__list { margin: 0; padding: 0; list-style: none; }
.nb-notif-dropdown__item { border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink); padding: var(--nb-space-3) 0; }
.nb-notif-dropdown__item:last-child { border-bottom: 0; }
.nb-notif-dropdown__item--unread { border-left: var(--nb-border-width-md) solid var(--nb-color-accent-orange); padding-left: var(--nb-space-2); }
.nb-notif-dropdown__message { margin: 0 0 4px; }
.nb-notif-dropdown__meta { display: flex; justify-content: space-between; gap: 12px; color: var(--nb-color-muted); font-size: 11px; }
.nb-notif-dropdown__footer { display: block; width: 100%; margin-top: 12px; text-align: center; text-decoration: none; }
</style>

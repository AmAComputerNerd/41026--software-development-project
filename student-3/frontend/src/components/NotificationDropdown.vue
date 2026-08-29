<script setup lang="ts">
import type { NotificationItem } from '@/types/notification'

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

    <TransitionGroup
      v-else
      appear
      tag="ul"
      name="notification-item"
      class="nb-notif-dropdown__list"
    >
      <li
        v-for="(notification, index) in notifications"
        :key="notification.id"
        class="nb-notif-dropdown__item"
        :class="{ 'nb-notif-dropdown__item--unread': !notification.isRead }"
        :style="{ transitionDelay: `${Math.min(index, 7) * 30}ms` }"
      >
        <p class="nb-notif-dropdown__message">{{ notification.message }}</p>
        <div class="nb-notif-dropdown__meta nb-mono">
          <span>{{ notification.sourceMicroservice }}</span>
          <span>{{ formatRelativeTime(notification.createdAtUtc) }}</span>
        </div>
      </li>
    </TransitionGroup>

    <a href="/notifications/" class="nb-btn nb-notif-dropdown__footer">
      VIEW ALL &rarr;
    </a>
  </div>
</template>

<style scoped>
.nb-notif-dropdown {
  position: absolute;
  z-index: 20;
  top: calc(100% + 12px);
  right: 0;
  width: 380px;
  max-width: 90vw;
  max-height: 70vh;
  overflow-y: auto;
  padding: var(--nb-space-4, 16px);
}

.nb-notif-dropdown__empty {
  padding: 24px 0;
  color: var(--nb-color-muted);
  text-align: center;
}

.nb-notif-dropdown__list {
  margin: 0;
  padding: 0;
  list-style: none;
}

.nb-notif-dropdown__item {
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
  padding: var(--nb-space-3, 12px) 0;
}

.nb-notif-dropdown__item:last-child {
  border-bottom: 0;
}

.nb-notif-dropdown__item--unread {
  border-left: var(--nb-border-width-md) solid var(--nb-color-accent-orange);
  padding-left: var(--nb-space-2, 8px);
}

.nb-notif-dropdown__message {
  margin: 0 0 4px;
}

.nb-notif-dropdown__meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  color: var(--nb-color-muted);
  font-size: 11px;
  letter-spacing: 0.5px;
}

.nb-notif-dropdown__footer {
  display: block;
  width: 100%;
  margin-top: 12px;
  text-align: center;
  text-decoration: none;
}

.notification-item-enter-active {
  transition:
    opacity 180ms ease-out,
    transform 220ms ease-out;
}

.notification-item-enter-from {
  opacity: 0;
  transform: translateX(10px);
}

@media (prefers-reduced-motion: reduce) {
  .notification-item-enter-active {
    transition: none;
  }
}
</style>

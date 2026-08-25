<script lang="ts" setup>
import type { NotificationDto } from '@/composables/useNotifications'

defineProps<{
  notifications: NotificationDto[]
  loading: boolean
  error: string | null
}>()

function formatRelativeTime(isoDate: string): string {
  const hasTimezone = /[zZ]|[+-]\d\d:\d\d$/.test(isoDate)
  const date = new Date(hasTimezone ? isoDate : `${isoDate}Z`)
  const diffMs = Date.now() - date.getTime()
  const diffMin = Math.round(diffMs / 60_000)

  if (diffMin < 1) return 'now'
  if (diffMin < 60) return `${diffMin}m ago`

  const diffHr = Math.round(diffMin / 60)
  if (diffHr < 24) return `${diffHr}h ago`

  const diffDay = Math.round(diffHr / 24)
  if (diffDay < 7) return `${diffDay}d ago`

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
        v-for="n in notifications"
        :key="n.id"
        class="nb-notif-dropdown__item"
        :class="{ 'nb-notif-dropdown__item--unread': !n.isRead }"
      >
        <p class="nb-notif-dropdown__message">{{ n.message }}</p>
        <div class="nb-notif-dropdown__meta nb-mono">
          <span>{{ n.sourceMicroservice }}</span>
          <span>{{ formatRelativeTime(n.createdAtUtc) }}</span>
        </div>
      </li>
    </ul>

    <a href="/notifications/" class="nb-btn nb-notif-dropdown__footer">
      VIEW ALL &rarr;
    </a>
  </div>
</template>

<style scoped>
.nb-notif-dropdown {
  position: absolute;
  top: calc(100% + 12px);
  right: 0;
  width: 380px;
  max-width: 90vw;
  max-height: 70vh;
  overflow-y: auto;
  padding: var(--nb-space-4, 16px);
  z-index: 20;
}

.nb-notif-dropdown__empty {
  padding: 24px 0;
  text-align: center;
  color: var(--nb-color-muted);
}

.nb-notif-dropdown__list {
  list-style: none;
  margin: 0;
  padding: 0;
}

.nb-notif-dropdown__item {
  padding: var(--nb-space-3, 12px) 0;
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.nb-notif-dropdown__item:last-child {
  border-bottom: none;
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
  font-size: 11px;
  letter-spacing: 0.5px;
  color: var(--nb-color-muted);
}

.nb-notif-dropdown__footer {
  display: block;
  width: 100%;
  margin-top: 12px;
  text-align: center;
  text-decoration: none;
}
</style>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import NotificationRow from './NotificationRow.vue'
import { useNotifications } from '@/composables/useNotifications'

const { notifications, unreadCount, fetchNotifications, markAsRead, markAllAsRead } =
  useNotifications()

const recentNotifications = computed(() =>
  [...notifications.value]
    .sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())
    .slice(0, 5),
)

const open = ref(false)
const root = ref<HTMLElement | null>(null)

function toggle() {
  open.value = !open.value
}

function onClickOutside(event: MouseEvent) {
  if (open.value && root.value && !root.value.contains(event.target as Node)) {
    open.value = false
  }
}

onMounted(() => {
  fetchNotifications()
  document.addEventListener('click', onClickOutside)
})

onBeforeUnmount(() => document.removeEventListener('click', onClickOutside))
</script>

<template>
  <div ref="root" class="nb-centre-wrap">
    <button type="button" class="nb-bell" aria-label="Notifications" @click="toggle">
      <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
        <path d="M13.73 21a2 2 0 0 1-3.46 0" />
      </svg>
      <span v-if="unreadCount > 0" class="nb-badge">{{ unreadCount }}</span>
    </button>

    <div v-if="open" class="nb-panel nb-centre">
      <div class="nb-centre__header">
        <span class="nb-mono nb-centre__heading">NOTIFICATIONS</span>
        <button
          type="button"
          class="nb-btn"
          :disabled="unreadCount === 0"
          @click="markAllAsRead"
        >
          MARK ALL READ
        </button>
      </div>

      <div v-if="recentNotifications.length === 0" class="nb-centre__empty nb-mono">
        NO NOTIFICATIONS YET
      </div>
      <NotificationRow
        v-for="n in recentNotifications"
        :key="n.id"
        :notification="n"
        variant="dropdown"
        @click="markAsRead(n.id)"
      />

      <RouterLink to="/" class="nb-btn nb-centre__footer" @click="open = false">
        VIEW ALL NOTIFICATIONS &rarr;
      </RouterLink>
    </div>
  </div>
</template>

<style scoped>
.nb-centre-wrap {
  position: relative;
}

.nb-bell {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: var(--nb-border-width-md) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  width: 44px;
  height: 44px;
  transition:
    transform 140ms ease,
    box-shadow 140ms ease;
}

.nb-bell:hover {
  transform: translateY(-2px);
  box-shadow: 2px 2px 0 var(--nb-color-ink);
}

.nb-bell .nb-badge {
  position: absolute;
  top: -8px;
  right: -8px;
}

.nb-centre {
  position: absolute;
  top: calc(100% + 12px);
  right: 0;
  width: 460px;
  max-width: 90vw;
  padding: 16px;
  z-index: 20;
  animation: nb-dialog-in 200ms cubic-bezier(0.2, 0.9, 0.25, 1.15) both;
}


.nb-centre__heading {
  font-weight: 700;
}

.nb-centre__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-bottom: 12px;
  margin-bottom: 8px;
  border-bottom: var(--nb-border-width-md) solid var(--nb-color-ink);
}

.nb-centre__empty {
  padding: 24px 0;
  text-align: center;
  color: var(--nb-color-muted);
}

.nb-centre__footer {
  display: block;
  width: 100%;
  margin-top: 12px;
  text-align: center;
  text-decoration: none;
}
</style>

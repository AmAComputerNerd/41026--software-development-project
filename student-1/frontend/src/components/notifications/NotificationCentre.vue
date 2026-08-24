<script setup lang="ts">
import { computed, onMounted } from 'vue'
import NotificationRow from './NotificationRow.vue'
import { useNotifications } from '@/composables/useNotifications'

const { notifications, unreadCount, fetchNotifications, markAsRead, markAllAsRead } =
  useNotifications()

const recentNotifications = computed(() =>
  [...notifications.value]
    .sort((a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime())
    .slice(0, 5),
)

onMounted(fetchNotifications)
</script>

<template>
  <v-menu :close-on-content-click="false" location="bottom end" offset="12">
    <template #activator="{ props: menuProps }">
      <button type="button" class="nb-bell" v-bind="menuProps" aria-label="Notifications">
        <v-icon icon="mdi-bell-outline" />
        <span v-if="unreadCount > 0" class="nb-badge">{{ unreadCount }}</span>
      </button>
    </template>

    <div class="nb-panel nb-centre">
      <div class="nb-centre__header">
        <span class="nb-mono font-weight-bold">NOTIFICATIONS</span>
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

      <RouterLink to="/notifications" class="nb-btn nb-centre__footer">
        VIEW ALL NOTIFICATIONS &rarr;
      </RouterLink>
    </div>
  </v-menu>
</template>

<style scoped>
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

  .nb-badge {
    position: absolute;
    top: -8px;
    right: -8px;
  }
}

.nb-centre {
  width: 460px;
  max-width: 90vw;
  padding: 16px;
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

<script setup lang="ts">
import { onMounted } from 'vue'
import NotificationFilterChips from '@/components/notifications/NotificationFilterChips.vue'
import NotificationRow from '@/components/notifications/NotificationRow.vue'
import { useNotifications } from '@/composables/useNotifications'

const {
  loading,
  error,
  activeFilters,
  filteredNotifications,
  fetchNotifications,
  toggleFilter,
  setAllFilters,
  markAsRead,
  markAsUnread,
} = useNotifications()

onMounted(fetchNotifications)
</script>

<template>
  <div class="nb-list">
    <div class="nb-list__header">
      <h1 class="nb-list__title">ALL NOTIFICATIONS</h1>
      <span class="nb-mono nb-list__count">{{ filteredNotifications.length }} SHOWN</span>
    </div>

    <NotificationFilterChips
      :active-filters="activeFilters"
      @toggle="toggleFilter"
      @toggle-all="setAllFilters"
    />

    <p v-if="error" class="nb-list__error nb-mono">{{ error }}</p>
    <p v-else-if="loading" class="nb-mono">LOADING...</p>

    <div v-else class="nb-panel nb-list__panel">
      <p v-if="filteredNotifications.length === 0" class="nb-list__empty nb-mono">
        NO NOTIFICATIONS MATCH THESE FILTERS
      </p>
      <NotificationRow
        v-for="n in filteredNotifications"
        :key="n.id"
        :notification="n"
        variant="list"
        @mark-read="markAsRead"
        @mark-unread="markAsUnread"
      />
    </div>
  </div>
</template>

<style scoped>
.nb-list__header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  margin-bottom: 16px;
}

.nb-list__title {
  font-size: 28px;
  font-weight: 700;
}

.nb-list__count {
  color: var(--nb-color-muted);
}

.nb-list__panel {
  margin-top: 16px;
}

.nb-list__empty,
.nb-list__error {
  padding: 24px;
  text-align: center;
  color: var(--nb-color-muted);
}
</style>

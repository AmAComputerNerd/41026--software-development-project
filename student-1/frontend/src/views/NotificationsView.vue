<script setup lang="ts">
import { onMounted } from 'vue'
import NotificationFilterChips from '@/components/notifications/NotificationFilterChips.vue'
import NotificationRow from '@/components/notifications/NotificationRow.vue'
import { useNotifications } from '@/composables/useNotifications'

const {
  loading,
  error,
  activeFilters,
  sortOrder,
  filteredNotifications,
  fetchNotifications,
  toggleFilter,
  setAllFilters,
  setSortOrder,
  markAsRead,
  markAsUnread,
  deleteNotification,
} = useNotifications()

onMounted(fetchNotifications)
</script>

<template>
  <div class="nb-list">
    <div class="nb-list__header">
      <h1 class="nb-list__title">ALL NOTIFICATIONS</h1>
      <span class="nb-mono nb-list__count">{{ filteredNotifications.length }} SHOWN</span>
    </div>

    <div class="nb-list__controls">
      <NotificationFilterChips
        :active-filters="activeFilters"
        @toggle="toggleFilter"
        @toggle-all="setAllFilters"
      />

      <div class="nb-chips nb-list__sort">
        <button
          type="button"
          class="nb-chip"
          :class="{ 'nb-chip--active': sortOrder === 'newest' }"
          @click="setSortOrder('newest')"
        >
          NEWEST FIRST
        </button>
        <button
          type="button"
          class="nb-chip"
          :class="{ 'nb-chip--active': sortOrder === 'oldest' }"
          @click="setSortOrder('oldest')"
        >
          OLDEST FIRST
        </button>
      </div>
    </div>

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
        @delete="deleteNotification"
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

.nb-list__controls {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.nb-list__sort {
  flex-shrink: 0;
}

.nb-list__empty,
.nb-list__error {
  padding: 24px;
  text-align: center;
  color: var(--nb-color-muted);
}
</style>

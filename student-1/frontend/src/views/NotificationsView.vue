<script setup lang="ts">
import { onMounted, ref } from 'vue'
import NotificationFilterChips from '@/components/notifications/NotificationFilterChips.vue'
import NotificationRow from '@/components/notifications/NotificationRow.vue'
import NotificationToast from '@/components/notifications/NotificationToast.vue'
import BreakdownDialog from '@/components/notifications/BreakdownDialog.vue'
import GradeImpactDialog from '@/components/notifications/GradeImpactDialog.vue'
import RagQueryModal from '@/components/notifications/RagQueryModal.vue'
import McpBroadcastModal from '@/components/notifications/McpBroadcastModal.vue'
import { useNotifications, type NotificationDto } from '@/composables/useNotifications'
import { useNotificationStream } from '@/composables/useNotificationStream'

const ragModalOpen = ref(false)
const mcpModalOpen = ref(false)

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
  markTaskComplete,
  deleteNotification,
  addRealtimeNotification,
} = useNotifications()

// Real-time toast state
const toastNotification = ref<NotificationDto | null>(null)
let toastTimer: ReturnType<typeof setTimeout> | null = null

function showToast(n: NotificationDto) {
  if (toastTimer) clearTimeout(toastTimer)
  toastNotification.value = n
  toastTimer = setTimeout(() => {
    toastNotification.value = null
  }, 6000)
}

function dismissToast() {
  if (toastTimer) clearTimeout(toastTimer)
  toastNotification.value = null
}

// Connect SSE stream
useNotificationStream((n) => {
  addRealtimeNotification(n)
  showToast(n)
})

// Dialog state
const breakdownTaskId = ref<string | null>(null)
const breakdownTaskTitle = ref<string | undefined>(undefined)
const gradeAssignmentId = ref<string | null>(null)
const gradeMessage = ref<string | undefined>(undefined)

function handleBreakdownTask(taskId: string, message: string) {
  breakdownTaskId.value = taskId
  breakdownTaskTitle.value = message
}

function handleSimulateGrade(assignmentId: string | null, message: string) {
  gradeAssignmentId.value = assignmentId
  gradeMessage.value = message
}

onMounted(fetchNotifications)
</script>

<template>
  <div class="nb-list">
    <div class="nb-list__header">
      <div class="nb-list__title-group">
        <h1 class="nb-list__title">ALL NOTIFICATIONS</h1>
        <span class="nb-mono nb-list__count">{{ filteredNotifications.length }} SHOWN</span>
      </div>

      <div class="nb-list__actions">
        <button
          type="button"
          class="nb-btn nb-btn--rag"
          @click="ragModalOpen = true"
        >
          🧠 QUERY COURSE RAG
        </button>
        <button
          type="button"
          class="nb-btn nb-btn--mcp"
          @click="mcpModalOpen = true"
        >
          ⚡ MCP BROADCAST TOOL
        </button>
      </div>
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
        @complete-task="markTaskComplete"
        @breakdown-task="handleBreakdownTask"
        @simulate-grade="handleSimulateGrade"
        @delete="deleteNotification"
      />
    </div>

    <!-- AI Task Breakdown Dialog (Student-3) -->
    <BreakdownDialog
      v-if="breakdownTaskId"
      :task-id="breakdownTaskId"
      :task-title="breakdownTaskTitle"
      @close="breakdownTaskId = null"
    />

    <!-- Grade Impact Simulator Dialog (Student-5) -->
    <GradeImpactDialog
      v-if="gradeAssignmentId !== null"
      :assignment-id="gradeAssignmentId"
      :message="gradeMessage"
      @close="gradeAssignmentId = null"
    />

    <!-- Real-time SSE Notification Toast -->
    <NotificationToast
      :notification="toastNotification"
      @dismiss="dismissToast"
      @mark-read="(id) => { markAsRead(id); dismissToast(); }"
    />

    <!-- Shared Local RAG Course Knowledge Query Modal -->
    <RagQueryModal :open="ragModalOpen" @close="ragModalOpen = false" />

    <!-- Shared Local MCP Tool Dispatch Modal -->
    <McpBroadcastModal :open="mcpModalOpen" @close="mcpModalOpen = false" />
  </div>
</template>

<style scoped>
.nb-list__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
  animation: nb-rise-in 320ms ease-out both;
  flex-wrap: wrap;
  gap: 12px;
}

.nb-list__title-group {
  display: flex;
  align-items: baseline;
  gap: 12px;
}

.nb-list__actions {
  display: flex;
  gap: 10px;
  align-items: center;
}

.nb-btn--rag {
  background: var(--nb-color-accent-purple, #b388ff);
  color: var(--nb-color-ink, #000);
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink, #000);
  padding: 8px 14px;
  font-weight: 800;
  font-size: 13px;
  cursor: pointer;
  box-shadow: 3px 3px 0 var(--nb-color-ink, #000);
}

.nb-btn--mcp {
  background: var(--nb-color-accent-orange, #ff9100);
  color: var(--nb-color-ink, #000);
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink, #000);
  padding: 8px 14px;
  font-weight: 800;
  font-size: 13px;
  cursor: pointer;
  box-shadow: 3px 3px 0 var(--nb-color-ink, #000);
}

.nb-btn--rag:hover,
.nb-btn--mcp:hover {
  transform: translate(-1px, -1px);
  box-shadow: 4px 4px 0 var(--nb-color-ink, #000);
}

.nb-list__title {
  font-size: 28px;
  font-weight: 700;
}

.nb-list__count {
  color: var(--nb-color-muted);
}

.nb-list__controls {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  animation: nb-rise-in 360ms 60ms ease-out both;
}

.nb-list__sort {
  flex-shrink: 0;
}

.nb-list__panel {
  margin-top: 16px;
  animation: nb-rise-in 380ms 100ms ease-out both;
}

.nb-list__empty,
.nb-list__error {
  padding: 24px;
  text-align: center;
  color: var(--nb-color-muted);
}

.nb-list__error {
  animation: nb-alert-in 260ms ease-out both;
}
</style>


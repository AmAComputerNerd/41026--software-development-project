<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import AssignmentBreakdownDialog from '@/components/AssignmentBreakdownDialog.vue'
import TaskDialog from '@/components/TaskDialog.vue'
import { useTasks } from '@/composables/useTasks'
import { dueLabel, formatDueDate } from '@/utils/dates'
import type { CanvasSyncResult, TaskItem } from '@/types/task'

const { tasks, loading, error, load, sync } = useTasks()
const selectedAssignment = ref<TaskItem | null>(null)
const breakdownOpen = ref(false)
const subtaskOpen = ref(false)
const syncing = ref(false)
const syncMessage = ref('')
const actionError = ref('')

const assignments = computed(() =>
  tasks.value
    .filter((task) => task.canvasAssignmentId !== null && !task.parentTaskId)
    .sort((a, b) => (a.dueDate ?? '').localeCompare(b.dueDate ?? '')),
)

function childrenFor(assignment: TaskItem) {
  return tasks.value.filter((task) => task.parentTaskId === assignment.id)
}

function progressFor(assignment: TaskItem) {
  const children = childrenFor(assignment)
  if (!children.length) return 0
  return Math.round(
    (children.filter((task) => task.status === 'Completed').length / children.length) * 100,
  )
}

onMounted(() => load().catch(() => undefined))

function openBreakdown(assignment: TaskItem) {
  selectedAssignment.value = assignment
  breakdownOpen.value = true
}

function openSubtask(assignment: TaskItem) {
  selectedAssignment.value = assignment
  subtaskOpen.value = true
}

async function syncAssignments() {
  syncing.value = true
  actionError.value = ''
  syncMessage.value = ''
  try {
    const result: CanvasSyncResult = await sync()
    syncMessage.value =
      `Canvas synced: ${result.tasksCreated} assignment(s) added, ` +
      `${result.tasksUpdated} updated.`
  } catch (reason) {
    actionError.value = reason instanceof Error ? reason.message : 'Unable to sync Canvas.'
  } finally {
    syncing.value = false
  }
}
</script>

<template>
  <section class="nb-page">
    <header class="nb-page-header">
      <div>
        <p class="nb-eyebrow nb-mono">CANVAS → ACTION PLAN</p>
        <h1>Assignments</h1>
        <p>Turn synced Canvas assignments into reusable, practical sub-task plans.</p>
      </div>
      <button class="nb-btn nb-btn--accent" type="button" :disabled="syncing" @click="syncAssignments">
        {{ syncing ? 'Syncing...' : '↻ Sync Canvas' }}
      </button>
    </header>

    <v-alert v-if="error || actionError" type="error" variant="outlined" class="mb-5">
      {{ actionError || error }}
    </v-alert>
    <v-alert v-if="syncMessage" type="success" variant="outlined" class="mb-5">
      {{ syncMessage }}
    </v-alert>

    <div v-if="loading" class="nb-panel nb-empty">Loading assignments...</div>
    <div v-else-if="!assignments.length" class="nb-panel nb-empty">
      <strong>No active Canvas assignments yet.</strong>
      <span>Use “Sync Canvas” to import assignments through the shared Canvas service.</span>
    </div>
    <div v-else class="nb-assignment-grid">
      <article v-for="assignment in assignments" :key="assignment.id" class="nb-panel nb-assignment">
        <div class="nb-assignment__topline">
          <span class="nb-tag nb-tag--canvas">Canvas</span>
          <span class="nb-tag" :class="`nb-tag--${assignment.priority.toLowerCase()}`">
            {{ assignment.priority }}
          </span>
        </div>
        <div>
          <p class="nb-eyebrow nb-mono">{{ assignment.courseName ?? 'COURSE' }}</p>
          <h2>{{ assignment.title }}</h2>
          <p class="nb-mono" :class="{ 'nb-overdue': dueLabel(assignment.dueDate).includes('OVERDUE') }">
            {{ dueLabel(assignment.dueDate) }} · {{ formatDueDate(assignment.dueDate) }}
          </p>
        </div>
        <div class="nb-assignment__progress">
          <div class="nb-assignment__progress-label nb-mono">
            <span>{{ childrenFor(assignment).length }} SUB-TASKS</span>
            <span>{{ progressFor(assignment) }}%</span>
          </div>
          <div class="nb-progress-track">
            <span :style="{ width: `${progressFor(assignment)}%` }" />
          </div>
        </div>
        <div class="nb-assignment__actions">
          <button class="nb-btn nb-btn--accent" type="button" @click="openBreakdown(assignment)">
            Use template
          </button>
          <button class="nb-btn nb-btn--outline" type="button" @click="openSubtask(assignment)">
            + One sub-task
          </button>
        </div>
      </article>
    </div>

    <AssignmentBreakdownDialog
      v-model="breakdownOpen"
      :assignment="selectedAssignment"
    />
    <TaskDialog
      v-model="subtaskOpen"
      :parent-task="selectedAssignment"
    />
  </section>
</template>

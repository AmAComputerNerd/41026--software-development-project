<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import AssignmentBreakdownDialog from '@/components/AssignmentBreakdownDialog.vue'
import TaskDialog from '@/components/TaskDialog.vue'
import { useTasks } from '@/composables/useTasks'
import { dueLabel, formatDueDate } from '@/utils/dates'
import type { CanvasSyncResult, TaskItem } from '@/types/task'

const { tasks, courses, loading, error, load, sync } = useTasks()
const selectedAssignment = ref<TaskItem | null>(null)
const selectedCourseId = ref<string | 'All'>('All')
const breakdownOpen = ref(false)
const subtaskOpen = ref(false)
const syncing = ref(false)
const syncMessage = ref('')
const actionError = ref('')

const assignments = computed(() =>
  tasks.value
    .filter(
      (task) =>
        task.canvasAssignmentId !== null &&
        !task.parentTaskId &&
        task.status !== 'Completed',
    )
    .sort((a, b) => (a.dueDate ?? '').localeCompare(b.dueDate ?? '')),
)

const courseOptions = computed(() => [
  { id: 'All', name: 'All courses' },
  ...[...courses.value].sort((a, b) => a.name.localeCompare(b.name)),
])

const filteredAssignments = computed(() =>
  selectedCourseId.value === 'All'
    ? assignments.value
    : assignments.value.filter((assignment) => assignment.courseId === selectedCourseId.value),
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
        <p>Turn synced Canvas assignments into tailored, AI-generated sub-task plans.</p>
      </div>
      <button class="nb-btn nb-btn--accent" type="button" :disabled="syncing" @click="syncAssignments">
        {{ syncing ? 'Syncing...' : '↻ Sync Canvas' }}
      </button>
    </header>

    <div class="nb-panel nb-assignment-filter">
      <label class="nb-field">
        <span>Course</span>
        <select v-model="selectedCourseId">
          <option v-for="course in courseOptions" :key="course.id" :value="course.id">
            {{ course.name }}
          </option>
        </select>
      </label>
    </div>

    <div v-if="error || actionError" class="nb-alert nb-alert--error" role="alert">
      {{ actionError || error }}
    </div>
    <div v-if="syncMessage" class="nb-alert nb-alert--success" role="status">
      {{ syncMessage }}
    </div>

    <div v-if="loading" class="nb-panel nb-empty">Loading assignments...</div>
    <div v-else-if="!assignments.length" class="nb-panel nb-empty">
      <strong>No active Canvas assignments yet.</strong>
      <span>Use “Sync Canvas” to import assignments through the shared Canvas service.</span>
    </div>
    <div v-else-if="!filteredAssignments.length" class="nb-panel nb-empty">
      <strong>No assignments for this course.</strong>
      <span>Select another course or choose “All courses”.</span>
    </div>
    <div v-else class="nb-assignment-grid">
      <article
        v-for="assignment in filteredAssignments"
        :key="assignment.id"
        class="nb-panel nb-assignment"
      >
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
            AI breakdown
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

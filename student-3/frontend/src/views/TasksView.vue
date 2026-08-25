<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import TaskDialog from '@/components/TaskDialog.vue'
import TaskRow from '@/components/TaskRow.vue'
import { useTasks } from '@/composables/useTasks'
import type { TaskItem, TaskPriority, TaskStatus } from '@/types/task'

const { tasks, courses, loading, error, load, update, remove } = useTasks()
const route = useRoute()
const router = useRouter()
const search = ref('')
const status = ref<TaskStatus | 'Active' | 'All'>('Active')
const priority = ref<TaskPriority | 'All'>('All')
const courseId = ref<string | 'All'>('All')
const dialogOpen = ref(false)
const selectedTask = ref<TaskItem | null>(null)
const parentTask = ref<TaskItem | null>(null)
const actionError = ref('')

const filtered = computed(() => {
  const needle = search.value.trim().toLowerCase()
  return tasks.value.filter((task) => {
    const matchesSearch =
      !needle ||
      task.title.toLowerCase().includes(needle) ||
      task.description?.toLowerCase().includes(needle) ||
      task.courseName?.toLowerCase().includes(needle)
    return (
      matchesSearch &&
      (status.value === 'All' ||
        (status.value === 'Active' && task.status !== 'Completed') ||
        task.status === status.value) &&
      (priority.value === 'All' || task.priority === priority.value) &&
      (courseId.value === 'All' || task.courseId === courseId.value)
    )
  })
})

const visibleTasks = computed(() => {
  const matchingIds = new Set(filtered.value.map((task) => task.id))
  let foundAncestor = true
  while (foundAncestor) {
    foundAncestor = false
    for (const task of tasks.value) {
      if (matchingIds.has(task.id) && task.parentTaskId && !matchingIds.has(task.parentTaskId)) {
        matchingIds.add(task.parentTaskId)
        foundAncestor = true
      }
    }
  }
  return tasks.value.filter((task) => matchingIds.has(task.id))
})

const roots = computed(() => {
  const visibleIds = new Set(visibleTasks.value.map((task) => task.id))
  return visibleTasks.value
    .filter((task) => !task.parentTaskId || !visibleIds.has(task.parentTaskId))
    .sort((a, b) => {
      if (!a.dueDate) return 1
      if (!b.dueDate) return -1
      return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
    })
})

onMounted(async () => {
  try {
    await load()
  } catch {
    return
  }

  const editQuery = route.query.edit
  const taskId = Array.isArray(editQuery) ? editQuery[0] : editQuery
  if (!taskId) return

  const task = tasks.value.find((item) => item.id === taskId)
  if (task) openEdit(task)

  const query = { ...route.query }
  delete query.edit
  await router.replace({ query })
})

function openCreate(parent: TaskItem | null = null) {
  selectedTask.value = null
  parentTask.value = parent
  dialogOpen.value = true
}

function openEdit(task: TaskItem) {
  selectedTask.value = task
  parentTask.value = null
  dialogOpen.value = true
}

async function changeStatus(task: TaskItem, newStatus: TaskStatus) {
  actionError.value = ''
  try {
    await update(task.id, {
      newTitle: null,
      updateDescription: false,
      newDescription: null,
      updateDueDate: false,
      newDueDate: null,
      newPriority: null,
      newStatus,
    })
  } catch (reason) {
    actionError.value = reason instanceof Error ? reason.message : 'Unable to update task status.'
  }
}

async function removeTask(task: TaskItem) {
  const childCount = tasks.value.filter((item) => item.parentTaskId === task.id).length
  const warning = childCount
    ? `Delete "${task.title}" and its ${childCount} sub-task${childCount === 1 ? '' : 's'}?`
    : `Delete "${task.title}"?`
  if (!window.confirm(warning)) return

  actionError.value = ''
  try {
    await remove(task.id)
  } catch (reason) {
    actionError.value = reason instanceof Error ? reason.message : 'Unable to delete this task.'
  }
}
</script>

<template>
  <section class="nb-page">
    <header class="nb-page-header">
      <div>
        <p class="nb-eyebrow nb-mono">DEADLINE CONTROL</p>
        <h1>Tasks</h1>
        <p>Plan coursework, track priorities and turn big assignments into achievable steps.</p>
      </div>
      <button class="nb-btn nb-btn--accent" type="button" @click="openCreate()">+ New task</button>
    </header>

    <div class="nb-panel nb-filterbar">
      <v-text-field v-model="search" label="Search tasks" prepend-inner-icon="mdi-magnify" hide-details />
      <v-select
        v-model="status"
        label="Status"
        :items="['Active', 'All', 'Todo', 'InProgress', 'Completed']"
        hide-details
      />
      <v-select v-model="priority" label="Priority" :items="['All', 'Low', 'Medium', 'High']" hide-details />
      <v-select
        v-model="courseId"
        label="Course"
        :items="[{ id: 'All', name: 'All courses' }, ...courses]"
        item-title="name"
        item-value="id"
        hide-details
      />
    </div>

    <v-alert v-if="error || actionError" type="error" variant="outlined" class="mb-5">
      {{ actionError || error }}
    </v-alert>

    <div v-if="loading" class="nb-panel nb-empty">Loading tasks...</div>
    <div v-else-if="!roots.length" class="nb-panel nb-empty">
      <strong>No tasks match this view.</strong>
      <span>Adjust the filters or create a new task.</span>
    </div>
    <div v-else class="nb-task-list">
      <TaskRow
        v-for="task in roots"
        :key="task.id"
        :task="task"
        :all-tasks="visibleTasks"
        @edit="openEdit"
        @add-child="openCreate"
        @remove="removeTask"
        @status="changeStatus"
      />
    </div>

    <TaskDialog
      v-model="dialogOpen"
      :task="selectedTask"
      :parent-task="parentTask"
    />
  </section>
</template>

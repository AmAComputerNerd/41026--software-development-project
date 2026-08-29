<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import TaskDialog from '@/components/TaskDialog.vue'
import { useTasks } from '@/composables/useTasks'
import type { TaskItem } from '@/types/task'

interface CalendarDay {
  key: string
  date: Date
  inMonth: boolean
  isToday: boolean
}

const { tasks, loading, error, load } = useTasks()
const cursor = ref(new Date(new Date().getFullYear(), new Date().getMonth(), 1))
const dialogOpen = ref(false)
const selectedTask = ref<TaskItem | null>(null)
const selectedDate = ref('')
const weekdays = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN']

const monthLabel = computed(() =>
  new Intl.DateTimeFormat('en-AU', { month: 'long', year: 'numeric' }).format(cursor.value),
)

const days = computed<CalendarDay[]>(() => {
  const first = new Date(cursor.value.getFullYear(), cursor.value.getMonth(), 1)
  const mondayOffset = (first.getDay() + 6) % 7
  const start = new Date(first)
  start.setDate(first.getDate() - mondayOffset)
  const today = new Date()

  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(start)
    date.setDate(start.getDate() + index)
    return {
      key: localDateKey(date),
      date,
      inMonth: date.getMonth() === cursor.value.getMonth(),
      isToday: localDateKey(date) === localDateKey(today),
    }
  })
})

const tasksByDay = computed(() => {
  const result = new Map<string, TaskItem[]>()
  for (const task of tasks.value) {
    if (!task.dueDate || task.status === 'Completed') continue
    const key = localDateKey(new Date(task.dueDate))
    const dayTasks = result.get(key) ?? []
    dayTasks.push(task)
    dayTasks.sort((a, b) => new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime())
    result.set(key, dayTasks)
  }
  return result
})

onMounted(() => load().catch(() => undefined))

function localDateKey(date: Date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function moveMonth(offset: number) {
  cursor.value = new Date(cursor.value.getFullYear(), cursor.value.getMonth() + offset, 1)
}

function isOverdue(task: TaskItem) {
  return task.dueDate !== null && new Date(task.dueDate).getTime() < Date.now()
}

function openCreate(day: CalendarDay) {
  selectedTask.value = null
  selectedDate.value = day.key
  dialogOpen.value = true
}

function openEdit(task: TaskItem) {
  selectedTask.value = task
  selectedDate.value = ''
  dialogOpen.value = true
}
</script>

<template>
  <section class="nb-page">
    <header class="nb-page-header">
      <div>
        <h1>CALENDAR</h1>
        <p class="nb-page-subtitle">
          See every deadline in context and add work directly to a day.
        </p>
      </div>
      <div class="nb-calendar-controls">
        <button class="nb-btn nb-btn--outline" type="button" @click="moveMonth(-1)">←</button>
        <strong>{{ monthLabel }}</strong>
        <button class="nb-btn nb-btn--outline" type="button" @click="moveMonth(1)">→</button>
      </div>
    </header>

    <div v-if="error" class="nb-alert nb-alert--error" role="alert">{{ error }}</div>
    <div v-if="loading" class="nb-panel nb-empty">Loading calendar...</div>
    <div v-else class="nb-calendar nb-panel">
      <div v-for="weekday in weekdays" :key="weekday" class="nb-calendar__weekday nb-mono">
        {{ weekday }}
      </div>
      <div
        v-for="day in days"
        :key="day.key"
        class="nb-calendar__day"
        :class="{
          'nb-calendar__day--outside': !day.inMonth,
          'nb-calendar__day--today': day.isToday,
        }"
        role="button"
        tabindex="0"
        :aria-label="`Add task on ${day.date.toDateString()}`"
        @click.self="openCreate(day)"
        @keydown.enter="openCreate(day)"
      >
        <span class="nb-calendar__date nb-mono">{{ day.date.getDate() }}</span>
        <button
          v-for="task in tasksByDay.get(day.key)?.slice(0, 3)"
          :key="task.id"
          class="nb-calendar-task"
          :class="[
            `nb-calendar-task--${task.priority.toLowerCase()}`,
            { 'nb-calendar-task--overdue': isOverdue(task) },
          ]"
          type="button"
          :title="isOverdue(task) ? `${task.title} (overdue)` : task.title"
          @click.stop="openEdit(task)"
        >
          <span v-if="isOverdue(task)" class="nb-calendar-task__overdue nb-mono">OVERDUE</span>
          <span class="nb-calendar-task__title">{{ task.title }}</span>
        </button>
        <span v-if="(tasksByDay.get(day.key)?.length ?? 0) > 3" class="nb-calendar__more nb-mono">
          +{{ (tasksByDay.get(day.key)?.length ?? 0) - 3 }} MORE
        </span>
      </div>
    </div>

    <TaskDialog
      v-model="dialogOpen"
      :task="selectedTask"
      :initial-date="selectedDate"
    />
  </section>
</template>

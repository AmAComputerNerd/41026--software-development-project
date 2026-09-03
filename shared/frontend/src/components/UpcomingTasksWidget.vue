<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { DEADLINES_API_BASE_URL } from '@/config'

interface UpcomingTask {
  id: string
  title: string
  dueDate: string | null
  priority: 'Low' | 'Medium' | 'High'
  status: 'Todo' | 'InProgress' | 'Completed'
  courseName: string | null
}

const tasks = ref<UpcomingTask[]>([])
const loading = ref(true)
const unavailable = ref(false)
const priorityRank = { High: 0, Medium: 1, Low: 2 }

const upcoming = computed(() =>
  tasks.value
    .filter((task) => task.status !== 'Completed' && task.dueDate)
    .sort((a, b) => {
      const dateDifference = new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime()
      return dateDifference || priorityRank[a.priority] - priorityRank[b.priority]
    })
    .slice(0, 5),
)

onMounted(async () => {
  try {
    const response = await fetch(`${DEADLINES_API_BASE_URL}/tasks/`)
    if (!response.ok) throw new Error('Deadline service unavailable')
    tasks.value = await response.json()
  } catch {
    unavailable.value = true
  } finally {
    loading.value = false
  }
})

function relativeDueDate(value: string) {
  const due = new Date(value)
  const today = new Date()
  const start = new Date(today.getFullYear(), today.getMonth(), today.getDate()).getTime()
  const end = new Date(due.getFullYear(), due.getMonth(), due.getDate()).getTime()
  const days = Math.round((end - start) / 86_400_000)
  if (days < 0) return `${Math.abs(days)}D OVERDUE`
  if (days === 0) return 'TODAY'
  if (days === 1) return 'TOMORROW'
  return `IN ${days} DAYS`
}
</script>

<template>
  <section class="nb-panel nb-upcoming" aria-labelledby="upcoming-title">
    <header class="nb-upcoming__header">
      <div>
        <p class="nb-upcoming__eyebrow nb-mono">NEXT UP</p>
        <h2 id="upcoming-title">Upcoming tasks</h2>
      </div>
      <a class="nb-btn nb-btn--outline nb-upcoming__link" href="/deadlines/">View planner →</a>
    </header>

    <p v-if="loading" class="nb-upcoming__message nb-mono">LOADING DEADLINES...</p>
    <p v-else-if="unavailable" class="nb-upcoming__message">
      The deadline service is not available right now.
    </p>
    <p v-else-if="!upcoming.length" class="nb-upcoming__message">
      Nothing due yet. Your schedule is clear.
    </p>
    <div v-else class="nb-upcoming__list">
      <a
        v-for="task in upcoming"
        :key="task.id"
        class="nb-upcoming__row"
        :href="`/deadlines/?edit=${encodeURIComponent(task.id)}`"
        :aria-label="`Edit ${task.title}`"
      >
        <span class="nb-upcoming__priority" :class="`nb-upcoming__priority--${task.priority.toLowerCase()}`" />
        <span class="nb-upcoming__task">
          <strong>{{ task.title }}</strong>
          <small>{{ task.courseName ?? 'Personal task' }}</small>
        </span>
        <span class="nb-upcoming__due nb-mono">{{ relativeDueDate(task.dueDate!) }}</span>
      </a>
    </div>
  </section>
</template>

<style scoped>
.nb-upcoming {
  display: grid;
  grid-template-columns: minmax(220px, 0.7fr) minmax(0, 1.3fr);
  margin-bottom: var(--nb-space-8);
  overflow: hidden;
  animation: nb-rise-in 360ms 80ms ease-out both;
}

.nb-upcoming__header {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--nb-space-5);
  border-right: var(--nb-border-width-lg) solid var(--nb-color-ink);
  background: var(--nb-color-accent-yellow);
  padding: var(--nb-space-5);
}

.nb-upcoming__header h2 {
  margin: 0;
  font-size: 24px;
}

.nb-upcoming__eyebrow {
  margin: 0 0 var(--nb-space-1);
  font-size: 10px;
  font-weight: 700;
}

.nb-upcoming__link {
  font-size: 10px;
  text-decoration: none;
}

.nb-upcoming__list {
  background: var(--nb-color-white);
}

.nb-upcoming__row {
  display: grid;
  grid-template-columns: 7px 1fr auto;
  gap: var(--nb-space-3);
  align-items: stretch;
  min-height: 58px;
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
  color: var(--nb-color-ink);
  text-decoration: none;
  animation: nb-rise-in 280ms ease-out both;
  transition:
    transform var(--nb-transition-fast),
    background-color var(--nb-transition-fast);

  &:nth-child(2n) {
    animation-delay: 35ms;
  }

  &:nth-child(3n) {
    animation-delay: 70ms;
  }

  &:hover {
    transform: translateX(3px);
    background-color: var(--nb-color-bg);
  }
}


.nb-upcoming__row:last-child {
  border-bottom: 0;
}

.nb-upcoming__priority {
  background: var(--nb-color-bg);
}

.nb-upcoming__priority--medium {
  background: var(--nb-color-accent-yellow);
}

.nb-upcoming__priority--high {
  background: var(--nb-color-accent-orange);
}

.nb-upcoming__task {
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: var(--nb-space-2) 0;
}

.nb-upcoming__task small {
  margin-top: 2px;
  color: var(--nb-color-muted);
}

.nb-upcoming__due {
  display: flex;
  align-items: center;
  padding: var(--nb-space-3);
  font-size: 10px;
  font-weight: 700;
}

.nb-upcoming__message {
  align-self: center;
  justify-self: center;
  padding: var(--nb-space-6);
  color: var(--nb-color-muted);
  text-align: center;
}

@media (max-width: 700px) {
  .nb-upcoming {
    grid-template-columns: 1fr;
  }

  .nb-upcoming__header {
    border-right: 0;
    border-bottom: var(--nb-border-width-lg) solid var(--nb-color-ink);
  }
}
</style>

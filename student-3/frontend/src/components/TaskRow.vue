<script setup lang="ts">
import { computed } from 'vue'
import { dueLabel, formatDueDate } from '@/utils/dates'
import type { TaskItem, TaskStatus } from '@/types/task'

defineOptions({ name: 'TaskRow' })

const props = withDefaults(
  defineProps<{
    task: TaskItem
    allTasks: TaskItem[]
    depth?: number
  }>(),
  { depth: 0 },
)

const emit = defineEmits<{
  edit: [task: TaskItem]
  addChild: [task: TaskItem]
  remove: [task: TaskItem]
  status: [task: TaskItem, status: TaskStatus]
}>()

const children = computed(() =>
  props.allTasks
    .filter((item) => item.parentTaskId === props.task.id)
    .sort((a, b) => (a.dueDate ?? '').localeCompare(b.dueDate ?? '')),
)

const nextStatus = computed<TaskStatus>(() => {
  if (props.task.status === 'Todo') return 'InProgress'
  if (props.task.status === 'InProgress') return 'Completed'
  return 'Todo'
})

const completedChildren = computed(
  () => children.value.filter((child) => child.status === 'Completed').length,
)
</script>

<template>
  <article
    class="nb-task-row"
    :class="[
      `nb-task-row--${task.priority.toLowerCase()}`,
      {
        'nb-task-row--complete': task.status === 'Completed',
        'nb-task-row--in-progress': task.status === 'InProgress',
      },
    ]"
    :style="{ '--task-depth': depth }"
  >
    <button
      class="nb-status-box"
      type="button"
      :title="`Move to ${nextStatus}`"
      :aria-label="`${task.title}: ${task.status}`"
      @click="emit('status', task, nextStatus)"
    >
      <span v-if="task.status === 'Completed'">✓</span>
      <span v-else-if="task.status === 'InProgress'" aria-hidden="true">&#9654;</span>
    </button>

    <div class="nb-task-row__content">
      <div class="nb-task-row__title-line">
        <strong>{{ task.title }}</strong>
        <span v-if="task.canvasAssignmentId" class="nb-tag nb-tag--canvas">Canvas</span>
        <span class="nb-tag" :class="`nb-tag--${task.priority.toLowerCase()}`">
          {{ task.priority }}
        </span>
      </div>
      <p v-if="task.description" class="nb-task-row__description">{{ task.description }}</p>
      <div class="nb-task-row__meta nb-mono">
        <span>{{ task.courseName ?? 'Personal' }}</span>
        <span :class="{ 'nb-overdue': dueLabel(task.dueDate).includes('OVERDUE') }">
          {{ dueLabel(task.dueDate) }} · {{ formatDueDate(task.dueDate) }}
        </span>
        <span v-if="children.length">{{ completedChildren }}/{{ children.length }} SUB-TASKS</span>
      </div>
    </div>

    <div class="nb-task-row__actions">
      <button class="nb-text-btn" type="button" @click="emit('addChild', task)">+ Sub-task</button>
      <button class="nb-text-btn" type="button" @click="emit('edit', task)">Edit</button>
      <button class="nb-text-btn nb-text-btn--danger" type="button" @click="emit('remove', task)">
        Delete
      </button>
    </div>
  </article>

  <TaskRow
    v-for="child in children"
    :key="child.id"
    :task="child"
    :all-tasks="allTasks"
    :depth="depth + 1"
    @edit="emit('edit', $event)"
    @add-child="emit('addChild', $event)"
    @remove="emit('remove', $event)"
    @status="(item, status) => emit('status', item, status)"
  />
</template>

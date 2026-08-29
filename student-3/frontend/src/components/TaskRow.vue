<script setup lang="ts">
import { computed, ref } from 'vue'
import { dueLabel, formatDueDate } from '@/utils/dates'
import type { TaskItem, TaskStatus } from '@/types/task'

defineOptions({ name: 'TaskRow' })

const MaxDescriptionPreviewLength = 240

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

const descriptionExpanded = ref(false)
const fullDescription = computed(() => props.task.description?.trim() ?? '')
const normalizedDescription = computed(() => fullDescription.value.replace(/\s+/g, ' '))
const descriptionIsTruncated = computed(
  () => normalizedDescription.value.length > MaxDescriptionPreviewLength,
)

const descriptionPreview = computed(() => {
  const description = normalizedDescription.value
  if (!description || description.length <= MaxDescriptionPreviewLength) {
    return description
  }

  const maximumContentLength = MaxDescriptionPreviewLength - 1
  const wordBoundary = description.lastIndexOf(' ', maximumContentLength)
  const cutoff = wordBoundary >= maximumContentLength * 0.75
    ? wordBoundary
    : maximumContentLength

  return `${description.slice(0, cutoff).trimEnd()}…`
})

const displayedDescription = computed(() =>
  descriptionExpanded.value ? fullDescription.value : descriptionPreview.value,
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
      <Transition name="description" mode="out-in">
        <p
          v-if="displayedDescription"
          :id="`task-description-${task.id}`"
          :key="descriptionExpanded ? 'expanded' : 'preview'"
          class="nb-task-row__description"
          :class="{ 'nb-task-row__description--expanded': descriptionExpanded }"
        >
          {{ displayedDescription }}
        </p>
      </Transition>
      <button
        v-if="descriptionIsTruncated"
        class="nb-text-btn nb-description-toggle"
        type="button"
        :aria-controls="`task-description-${task.id}`"
        :aria-expanded="descriptionExpanded"
        @click="descriptionExpanded = !descriptionExpanded"
      >
        {{ descriptionExpanded ? 'Show less' : 'Show more' }}
      </button>
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

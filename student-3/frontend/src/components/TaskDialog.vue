<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useTasks } from '@/composables/useTasks'
import { toDateInput, toUtcIso } from '@/utils/dates'
import type { TaskItem, TaskPriority, TaskStatus } from '@/types/task'

const props = defineProps<{
  modelValue: boolean
  task?: TaskItem | null
  parentTask?: TaskItem | null
  initialDate?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  saved: [task: TaskItem]
}>()

const { courses, add, update } = useTasks()
const saving = ref(false)
const error = ref('')
const priorities: TaskPriority[] = ['Low', 'Medium', 'High']
const statuses: TaskStatus[] = ['Todo', 'InProgress', 'Completed']
const form = reactive({
  title: '',
  description: '',
  dueDate: '',
  priority: 'Medium' as TaskPriority,
  status: 'Todo' as TaskStatus,
  courseId: null as string | null,
})

const editing = computed(() => Boolean(props.task))
const isCanvasTask = computed(() => props.task?.canvasAssignmentId != null)
const title = computed(() => {
  if (editing.value) return 'Edit task'
  if (props.parentTask) return `Add sub-task to ${props.parentTask.title}`
  return 'Create task'
})

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    error.value = ''
    form.title = props.task?.title ?? ''
    form.description = props.task?.description ?? ''
    form.dueDate = props.task
      ? toDateInput(props.task.dueDate)
      : props.initialDate
        ? `${props.initialDate}T17:00`
        : toDateInput(props.parentTask?.dueDate ?? null)
    form.priority = props.task?.priority ?? props.parentTask?.priority ?? 'Medium'
    form.status = props.task?.status ?? 'Todo'
    form.courseId = props.task?.courseId ?? props.parentTask?.courseId ?? null
  },
  { immediate: true },
)

function close() {
  emit('update:modelValue', false)
}

async function submit() {
  if (!form.title.trim()) {
    error.value = 'Give this task a title before saving.'
    return
  }

  saving.value = true
  error.value = ''
  try {
    const saved = props.task
      ? await update(props.task.id, {
          newTitle: isCanvasTask.value ? null : form.title.trim(),
          updateDescription: !isCanvasTask.value,
          newDescription: isCanvasTask.value ? null : form.description.trim() || null,
          updateDueDate: !isCanvasTask.value,
          newDueDate: isCanvasTask.value ? null : toUtcIso(form.dueDate),
          newPriority: form.priority,
          newStatus: form.status,
        })
      : await add({
          title: form.title.trim(),
          description: form.description.trim() || null,
          dueDate: toUtcIso(form.dueDate),
          priority: form.priority,
          courseId: form.courseId,
          parentTaskId: props.parentTask?.id ?? null,
        })
    emit('saved', saved)
    close()
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to save this task.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="650"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="nb-dialog">
      <div class="nb-dialog__header">
        <span class="nb-mono">{{ editing ? 'EDIT' : parentTask ? 'SUB-TASK' : 'NEW TASK' }}</span>
        <button class="nb-icon-btn" type="button" aria-label="Close" @click="close">&times;</button>
      </div>

      <v-card-text class="nb-dialog__body">
        <h2>{{ title }}</h2>
        <v-alert v-if="error" type="error" variant="outlined" class="mb-4">{{ error }}</v-alert>

        <v-text-field
          v-model="form.title"
          label="Task title"
          :autofocus="!isCanvasTask"
          :readonly="isCanvasTask"
          @keyup.enter="submit"
        />
        <v-textarea
          v-model="form.description"
          label="Description"
          rows="3"
          :readonly="isCanvasTask"
        />

        <div class="nb-form-grid">
          <v-text-field
            v-model="form.dueDate"
            label="Due date"
            type="datetime-local"
            :readonly="isCanvasTask"
          />
          <v-select v-model="form.priority" label="Priority" :items="priorities" />
          <v-select
            v-model="form.courseId"
            label="Course"
            :items="courses"
            item-title="name"
            item-value="id"
            clearable
            :disabled="editing || Boolean(parentTask)"
          />
          <v-select
            v-if="editing"
            v-model="form.status"
            label="Status"
            :items="statuses"
          />
        </div>

        <p v-if="isCanvasTask" class="nb-helper nb-mono">
          CANVAS ASSIGNMENT DETAILS ARE READ-ONLY. YOUR PRIORITY AND STATUS REMAIN EDITABLE.
        </p>
      </v-card-text>

      <v-card-actions class="nb-dialog__actions">
        <button class="nb-btn nb-btn--outline" type="button" @click="close">Cancel</button>
        <button class="nb-btn nb-btn--accent" type="button" :disabled="saving" @click="submit">
          {{ saving ? 'Saving...' : editing ? 'Save changes' : 'Create task' }}
        </button>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

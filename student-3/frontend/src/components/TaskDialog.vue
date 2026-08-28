<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import BaseDialog from '@/components/BaseDialog.vue'
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

const { courses, add, update, generateDescription } = useTasks()
const saving = ref(false)
const generatingDescription = ref(false)
const titleInput = ref<HTMLInputElement | null>(null)
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

function focusTitle() {
  if (!isCanvasTask.value) {
    titleInput.value?.focus()
  }
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

async function generateDescriptionDraft() {
  if (!form.title.trim()) {
    error.value = 'Add a task title before generating a description.'
    return
  }

  generatingDescription.value = true
  error.value = ''
  try {
    form.description = await generateDescription({
      title: form.title.trim(),
      courseId: form.courseId,
      parentTaskId: props.parentTask?.id ?? props.task?.parentTaskId ?? null,
    })
  } catch (reason) {
    error.value =
      reason instanceof Error ? reason.message : 'Unable to generate a task description.'
  } finally {
    generatingDescription.value = false
  }
}
</script>

<template>
  <BaseDialog
    :model-value="modelValue"
    labelled-by="task-dialog-title"
    @update:model-value="emit('update:modelValue', $event)"
    @opened="focusTitle"
  >
    <form @submit.prevent="submit">
      <div class="nb-dialog__header">
        <span class="nb-mono">{{ editing ? 'EDIT' : parentTask ? 'SUB-TASK' : 'NEW TASK' }}</span>
        <button class="nb-icon-btn" type="button" aria-label="Close" @click="close">&times;</button>
      </div>

      <div class="nb-dialog__body">
        <h2 id="task-dialog-title">{{ title }}</h2>
        <div v-if="error" class="nb-alert nb-alert--error" role="alert">{{ error }}</div>

        <label class="nb-field">
          <span>Task title</span>
          <input
            ref="titleInput"
            v-model="form.title"
            type="text"
            :readonly="isCanvasTask"
          />
        </label>
        <label class="nb-field">
          <span>Description</span>
          <textarea
            v-model="form.description"
            rows="3"
            :readonly="isCanvasTask"
          />
        </label>
        <div v-if="!isCanvasTask" class="nb-ai-field-action">
          <span>Uses the title, course and parent assessment as context.</span>
          <button
            class="nb-text-btn"
            type="button"
            :disabled="generatingDescription || !form.title.trim()"
            @click="generateDescriptionDraft"
          >
            {{ generatingDescription ? 'Generating...' : 'Generate description with AI' }}
          </button>
        </div>

        <div class="nb-form-grid">
          <label class="nb-field">
            <span>Due date</span>
            <input
              v-model="form.dueDate"
              type="datetime-local"
              :readonly="isCanvasTask"
            />
          </label>
          <label class="nb-field">
            <span>Priority</span>
            <select v-model="form.priority">
              <option v-for="item in priorities" :key="item" :value="item">{{ item }}</option>
            </select>
          </label>
          <label class="nb-field">
            <span>Course</span>
            <select
              v-model="form.courseId"
              :disabled="editing || Boolean(parentTask)"
            >
              <option :value="null">No course</option>
              <option v-for="course in courses" :key="course.id" :value="course.id">
                {{ course.name }}
              </option>
            </select>
          </label>
          <label v-if="editing" class="nb-field">
            <span>Status</span>
            <select v-model="form.status">
              <option v-for="item in statuses" :key="item" :value="item">{{ item }}</option>
            </select>
          </label>
        </div>

        <p v-if="isCanvasTask" class="nb-helper nb-mono">
          CANVAS ASSIGNMENT DETAILS ARE READ-ONLY. YOUR PRIORITY AND STATUS REMAIN EDITABLE.
        </p>
      </div>

      <div class="nb-dialog__actions">
        <button class="nb-btn nb-btn--outline" type="button" @click="close">Cancel</button>
        <button
          class="nb-btn nb-btn--accent"
          type="submit"
          :disabled="saving || generatingDescription"
        >
          {{ saving ? 'Saving...' : editing ? 'Save changes' : 'Create task' }}
        </button>
      </div>
    </form>
  </BaseDialog>
</template>

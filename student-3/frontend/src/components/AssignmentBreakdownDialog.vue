<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useTasks } from '@/composables/useTasks'
import type { TaskItem, TaskPriority } from '@/types/task'

interface Template {
  name: string
  steps: string[]
}

const props = defineProps<{
  modelValue: boolean
  assignment: TaskItem | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  saved: []
}>()

const templates: Template[] = [
  {
    name: 'Research assignment',
    steps: ['Review requirements', 'Research and collect sources', 'Draft response', 'Edit and submit'],
  },
  {
    name: 'Group project',
    steps: ['Confirm roles and scope', 'Complete individual contribution', 'Combine project', 'Review and submit'],
  },
  {
    name: 'Exam preparation',
    steps: ['Review topic list', 'Create study notes', 'Complete practice questions', 'Final review'],
  },
  { name: 'Blank plan', steps: [''] },
]

const { add } = useTasks()
const selectedTemplate = ref(templates[0]!)
const steps = ref<string[]>([])
const priority = ref<TaskPriority>('Medium')
const saving = ref(false)
const error = ref('')
const validSteps = computed(() => steps.value.map((step) => step.trim()).filter(Boolean))

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    selectedTemplate.value = templates[0]!
    steps.value = [...selectedTemplate.value.steps]
    priority.value = props.assignment?.priority ?? 'Medium'
    error.value = ''
  },
)

watch(selectedTemplate, (template) => {
  steps.value = [...template.steps]
})

function close() {
  emit('update:modelValue', false)
}

async function createPlan() {
  if (!props.assignment || !validSteps.value.length) {
    error.value = 'Add at least one sub-task to this plan.'
    return
  }

  saving.value = true
  error.value = ''
  try {
    for (const title of validSteps.value) {
      await add({
        title,
        description: null,
        dueDate: props.assignment.dueDate,
        priority: priority.value,
        courseId: props.assignment.courseId,
        parentTaskId: props.assignment.id,
      })
    }
    emit('saved')
    close()
  } catch (reason) {
    error.value =
      reason instanceof Error
        ? `The plan was only partially created: ${reason.message}`
        : 'The plan was only partially created.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="700"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="nb-dialog">
      <div class="nb-dialog__header">
        <span class="nb-mono">ASSIGNMENT TEMPLATE</span>
        <button class="nb-icon-btn" type="button" aria-label="Close" @click="close">&times;</button>
      </div>
      <v-card-text class="nb-dialog__body">
        <h2>Break down {{ assignment?.title }}</h2>
        <p>Start from a template, then tailor the sub-tasks before creating the plan.</p>
        <v-alert v-if="error" type="error" variant="outlined" class="mb-4">{{ error }}</v-alert>
        <div class="nb-form-grid">
          <v-select
            v-model="selectedTemplate"
            label="Template"
            :items="templates"
            item-title="name"
            return-object
          />
          <v-select v-model="priority" label="Sub-task priority" :items="['Low', 'Medium', 'High']" />
        </div>
        <div class="nb-step-list">
          <div v-for="(_, index) in steps" :key="index" class="nb-step-list__row">
            <span class="nb-step-list__number nb-mono">{{ String(index + 1).padStart(2, '0') }}</span>
            <v-text-field v-model="steps[index]" label="Sub-task" hide-details />
            <button
              class="nb-icon-btn"
              type="button"
              aria-label="Remove sub-task"
              @click="steps.splice(index, 1)"
            >
              &times;
            </button>
          </div>
        </div>
        <button class="nb-text-btn" type="button" @click="steps.push('')">+ Add another step</button>
      </v-card-text>
      <v-card-actions class="nb-dialog__actions">
        <button class="nb-btn nb-btn--outline" type="button" @click="close">Cancel</button>
        <button
          class="nb-btn nb-btn--accent"
          type="button"
          :disabled="saving || !validSteps.length"
          @click="createPlan"
        >
          {{ saving ? 'Creating...' : `Create ${validSteps.length} sub-tasks` }}
        </button>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

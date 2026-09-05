<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import BaseDialog from '@/components/BaseDialog.vue'
import { useTasks } from '@/composables/useTasks'
import type { TaskItem, TaskPriority } from '@/types/task'

interface Template {
  name: string
  prompt: string
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
    prompt:
      'Break this assignment into 4 to 6 steps covering requirements review, source research, drafting, editing and final submission.',
  },
  {
    name: 'Group project',
    prompt:
      'Create a practical group-project plan with 4 to 7 steps covering scope, roles, individual work, integration, review and submission.',
  },
  {
    name: 'Exam preparation',
    prompt:
      'Create a focused exam preparation plan with 4 to 7 steps covering topic review, study materials, practice and final revision.',
  },
  {
    name: 'Custom plan',
    prompt: 'Break this assignment into a short sequence of concrete, achievable sub-tasks.',
  },
]

const { generateBreakdown } = useTasks()
const selectedTemplateName = ref(templates[0]!.name)
const prompt = ref('')
const priority = ref<TaskPriority>('Medium')
const saving = ref(false)
const error = ref('')
const canGenerate = computed(() => Boolean(props.assignment && prompt.value.trim()))

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    selectedTemplateName.value = templates[0]!.name
    prompt.value = templates[0]!.prompt
    priority.value = props.assignment?.priority ?? 'Medium'
    error.value = ''
  },
)

watch(selectedTemplateName, (name) => {
  const template = templates.find((item) => item.name === name)
  if (template) {
    prompt.value = template.prompt
  }
})

function close() {
  emit('update:modelValue', false)
}

async function createPlan() {
  if (!props.assignment || !prompt.value.trim()) {
    error.value = 'Describe the plan you want the AI to create.'
    return
  }

  saving.value = true
  error.value = ''
  try {
    await generateBreakdown(props.assignment.id, {
      prompt: prompt.value.trim(),
      priority: priority.value,
    })
    emit('saved')
    close()
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to generate this plan.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <BaseDialog
    :model-value="modelValue"
    labelled-by="breakdown-dialog-title"
    width="700px"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <form @submit.prevent="createPlan">
      <div class="nb-dialog__header">
        <span class="nb-mono">AI ASSIGNMENT PLAN</span>
        <button class="nb-icon-btn" type="button" aria-label="Close" @click="close">&times;</button>
      </div>
      <div class="nb-dialog__body">
        <h2 id="breakdown-dialog-title">Break down {{ assignment?.title }}</h2>
        <p>Choose a prompt template, tailor it, then let AI create the sub-tasks.</p>
        <div v-if="error" class="nb-alert nb-alert--error" role="alert">{{ error }}</div>
        <div class="nb-form-grid">
          <label class="nb-field">
            <span>Template</span>
            <select v-model="selectedTemplateName">
              <option v-for="template in templates" :key="template.name" :value="template.name">
                {{ template.name }}
              </option>
            </select>
          </label>
          <label class="nb-field">
            <span>Sub-task priority</span>
            <select v-model="priority">
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </label>
        </div>
        <label class="nb-field">
          <span>Planning prompt</span>
          <textarea v-model="prompt" rows="6" maxlength="2000" />
          <small>{{ prompt.length }}/2000</small>
        </label>
        <p class="nb-helper nb-mono">
          THE ASSIGNMENT AND COURSE CONTEXT COME FROM THE TRACKER. REVIEW GENERATED TASKS AFTER CREATION.
        </p>
      </div>
      <div class="nb-dialog__actions">
        <button class="nb-btn nb-btn--outline" type="button" @click="close">Cancel</button>
        <button
          class="nb-btn nb-btn--accent"
          type="submit"
          :disabled="saving || !canGenerate"
        >
          {{ saving ? 'AI is creating tasks...' : 'Generate sub-tasks' }}
        </button>
      </div>
    </form>
  </BaseDialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import BaseDialog from '@/components/BaseDialog.vue'
import { useTasks } from '@/composables/useTasks'
import type { BreakdownProposal, GeneratedSubtask, TaskItem, TaskPriority } from '@/types/task'

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

const { generateBreakdown, reviewBreakdown, aiBreakdownEnabled } = useTasks()
const selectedTemplateName = ref(templates[0]!.name)
const prompt = ref('')
const priority = ref<TaskPriority>('Medium')
const saving = ref(false)
const generating = ref(false)
const error = ref('')
const proposal = ref<BreakdownProposal | null>(null)
const proposedTasks = ref<(GeneratedSubtask & { selected: boolean; index: number })[]>([])
const reviewerName = ref('')
const reviewComments = ref('')
const selectedTasks = computed(() => proposedTasks.value.filter((task) => task.selected))
const busy = computed(() => generating.value || saving.value)
let revision = 0
const canGenerate = computed(() =>
  Boolean(aiBreakdownEnabled.value && props.assignment && prompt.value.trim() && !busy.value),
)

watch(
  () => [props.modelValue, props.assignment?.id] as const,
  ([open]) => {
    revision++
    if (!open) return
    selectedTemplateName.value = templates[0]!.name
    prompt.value = templates[0]!.prompt
    priority.value = props.assignment?.priority ?? 'Medium'
    error.value = ''
    proposal.value = null
    proposedTasks.value = []
    reviewerName.value = ''
    reviewComments.value = ''
    saving.value = false
    generating.value = false
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
  if (!aiBreakdownEnabled.value) {
    error.value = 'AI Mode or Multi-Agent is unavailable.'
    return
  }

  if (!props.assignment || !prompt.value.trim()) {
    error.value = 'Describe the plan you want the AI to create.'
    return
  }

  if (busy.value) return
  const currentRevision = revision
  generating.value = true
  error.value = ''
  try {
    const result = await generateBreakdown(props.assignment.id, {
      prompt: prompt.value.trim(),
      priority: priority.value,
    })
    if (currentRevision !== revision) return
    proposal.value = result
    proposedTasks.value = result.tasks.map((task, index) => ({ ...task, index, selected: true }))
  } catch (reason) {
    if (currentRevision === revision) {
      error.value = reason instanceof Error ? reason.message : 'Unable to generate this plan.'
    }
  } finally {
    if (currentRevision === revision) generating.value = false
  }
}

async function submitReview(reject: boolean) {
  if (!props.assignment || !proposal.value || busy.value || !aiBreakdownEnabled.value) return
  if (!reviewerName.value.trim() || !reviewComments.value.trim()) {
    error.value = 'Enter your reviewer name and review comments.'
    return
  }
  if (!reject && !selectedTasks.value.length) {
    error.value = 'Select at least one task, or reject the proposal.'
    return
  }
  const currentRevision = revision
  saving.value = true
  error.value = ''
  try {
    const result = await reviewBreakdown(props.assignment.id, {
      workflowId: proposal.value.workflow.id,
      reject,
      reviewerName: reviewerName.value.trim(),
      comments: reviewComments.value.trim(),
      tasks: reject ? [] : selectedTasks.value.map(({ index, title, description }) => ({
        index, title, description,
      })),
    })
    if (result.tasks.length) emit('saved')
    if (currentRevision === revision) close()
  } catch (reason) {
    if (currentRevision === revision) {
      error.value = reason instanceof Error ? reason.message : 'Unable to save your review. Retry with the same selections.'
    }
  } finally {
    if (currentRevision === revision) saving.value = false
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
    <form @submit.prevent="proposal ? submitReview(false) : createPlan()">
      <div class="nb-dialog__header">
        <span class="nb-mono">AI ASSIGNMENT PLAN</span>
        <button class="nb-icon-btn" type="button" aria-label="Close" @click="close">&times;</button>
      </div>
      <div class="nb-dialog__body">
        <h2 id="breakdown-dialog-title">Break down {{ assignment?.title }}</h2>
        <p>Planner, Worker and Reviewer propose sub-tasks. Review, edit and select them before saving.</p>
        <div v-if="error" class="nb-alert nb-alert--error" role="alert">{{ error }}</div>
        <template v-if="!proposal">
          <div class="nb-form-grid">
            <label class="nb-field">
              <span>Template</span>
              <select v-model="selectedTemplateName" :disabled="busy">
                <option v-for="template in templates" :key="template.name" :value="template.name">
                  {{ template.name }}
                </option>
              </select>
            </label>
            <label class="nb-field">
              <span>Sub-task priority</span>
              <select v-model="priority" :disabled="busy">
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
              </select>
            </label>
          </div>
          <label class="nb-field">
            <span>Planning prompt</span>
            <textarea v-model="prompt" rows="6" maxlength="2000" :disabled="busy" />
            <small>{{ prompt.length }}/2000</small>
          </label>
          <p class="nb-helper nb-mono">
            THE ASSIGNMENT CONTEXT COMES FROM THE TRACKER. GENERATING A PROPOSAL DOES NOT CREATE TASKS.
          </p>
          <p v-if="generating" role="status">
            Waiting for Planner, Worker and Reviewer. The existing server does not expose live stage progress.
          </p>
        </template>
        <template v-else>
          <p class="nb-helper">
            The shared server can return fallback review text. Check the proposal against the assignment;
            its reported confidence is not independently verified.
          </p>
          <details class="nb-assistant-response">
            <summary>Planner and reviewer outputs</summary>
            <h3>PLANNER</h3>
            <p>{{ proposal.workflow.planner.objective }}</p>
            <ol><li v-for="(step, index) in proposal.workflow.planner.steps" :key="index">{{ step }}</li></ol>
            <h3>REVIEWER</h3>
            <p>{{ proposal.workflow.reviewer.verdict }} / {{ proposal.workflow.reviewer.confidence }}</p>
            <p class="nb-rag-answer">{{ proposal.workflow.reviewer.critique }}</p>
            <ul><li v-for="(risk, index) in proposal.workflow.reviewer.identifiedRisks" :key="index">{{ risk }}</li></ul>
            <h3>RECOMMENDATIONS</h3>
            <ul><li v-for="(item, index) in proposal.workflow.reviewer.recommendations" :key="index">{{ item }}</li></ul>
          </details>
          <fieldset v-for="task in proposedTasks" :key="task.index" class="nb-breakdown-task" :disabled="busy">
            <legend>
              <label>
                <input v-model="task.selected" type="checkbox" />
                Include task {{ task.index + 1 }}
              </label>
            </legend>
            <label class="nb-field">
              <span>Title</span>
              <input v-model="task.title" maxlength="120" :required="task.selected" />
            </label>
            <label class="nb-field">
              <span>Description</span>
              <textarea v-model="task.description" rows="2" maxlength="500" />
            </label>
          </fieldset>
          <label class="nb-field">
            <span>Reviewer name</span>
            <input v-model="reviewerName" maxlength="100" required :disabled="busy" />
          </label>
          <label class="nb-field">
            <span>Review comments</span>
            <textarea v-model="reviewComments" rows="3" maxlength="2000" required :disabled="busy" />
          </label>
          <p class="nb-helper">
            Saving records Approve, Correct or PartialAccept based on your edits and selection,
            then creates only the selected tasks. Reject records your decision without creating tasks.
          </p>
          <details class="nb-assistant-response">
            <summary>Coordination audit</summary>
            <ol>
              <li v-for="(entry, index) in proposal.workflow.auditLog" :key="index">
                {{ entry.agent }} / {{ entry.action }}: {{ entry.summary }}
              </li>
            </ol>
          </details>
        </template>
      </div>
      <div class="nb-dialog__actions">
        <button class="nb-btn nb-btn--outline" type="button" @click="close">Cancel</button>
        <button v-if="proposal" class="nb-btn nb-btn--outline" type="button" :disabled="busy" @click="proposal = null">
          Regenerate
        </button>
        <button v-if="proposal" class="nb-btn nb-btn--outline" type="button" :disabled="busy || !aiBreakdownEnabled" @click="submitReview(true)">
          Reject
        </button>
        <button
          class="nb-btn nb-btn--accent"
          type="submit"
          :disabled="busy || !aiBreakdownEnabled || (!proposal && !canGenerate) || (!!proposal && !selectedTasks.length)"
          :title="!aiBreakdownEnabled ? 'AI Mode or Multi-Agent is unavailable' : undefined"
        >
          {{ generating ? 'Agents are preparing a proposal...' : saving ? 'Saving reviewed tasks...' : proposal ? 'Save selected tasks' : 'Generate proposal' }}
        </button>
      </div>
    </form>
  </BaseDialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import DOMPurify from 'dompurify'
import { marked } from 'marked'
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

const { generateBreakdown, reviewBreakdown, reviseBreakdown, aiBreakdownEnabled, multiAgentEnabled } = useTasks()
const selectedTemplateName = ref(templates[0]!.name)
const prompt = ref('')
const priority = ref<TaskPriority>('Medium')
const saving = ref(false)
const generating = ref(false)
const error = ref('')
const proposal = ref<BreakdownProposal | null>(null)
const proposedTasks = ref<(GeneratedSubtask & { selected: boolean; index: number })[]>([])
const reviewerName = ref('')
const mode = ref<'decision' | 'partial' | 'correct'>('decision')
const feedback = ref('')
const correctionRequestId = ref<string | null>(null)
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
    mode.value = 'decision'
    feedback.value = ''
    correctionRequestId.value = null
    saving.value = false
    generating.value = false
  },
)

watch([feedback, reviewerName], () => { correctionRequestId.value = null }, { flush: 'sync' })

watch(selectedTemplateName, (name) => {
  const template = templates.find((item) => item.name === name)
  if (template) {
    prompt.value = template.prompt
  }
})

function close() {
  emit('update:modelValue', false)
}

function renderMarkdown(source: string) {
  return DOMPurify.sanitize(marked.parse(source, { async: false, breaks: true, gfm: true }))
}

function setProposal(result: BreakdownProposal) {
  proposal.value = result
  cancelDecision()
}

function cancelDecision() {
  proposedTasks.value = proposal.value?.tasks.map((task, index) => ({ ...task, index, selected: true })) ?? []
  mode.value = 'decision'
  feedback.value = ''
  correctionRequestId.value = null
  error.value = ''
}

function submit() {
  if (!proposal.value) return createPlan()
  if (mode.value === 'correct') return requestCorrection()
  return submitReview(mode.value === 'partial' ? 'PartialAccept' : 'Approve')
}

async function createPlan() {
  if (!aiBreakdownEnabled.value) {
    error.value = 'AI Mode and Multi-Agent are unavailable.'
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
    if (Array.isArray(result)) {
      if (result.length) emit('saved')
      if (currentRevision === revision) close()
    } else if (currentRevision === revision) {
      setProposal(result)
    }
  } catch (reason) {
    if (currentRevision === revision) {
      error.value = reason instanceof Error ? reason.message : 'Unable to generate this plan.'
    }
  } finally {
    if (currentRevision === revision) generating.value = false
  }
}

async function submitReview(decision: 'Approve' | 'PartialAccept' | 'Reject') {
  if (!props.assignment || !proposal.value || busy.value || !aiBreakdownEnabled.value) return
  const reject = decision === 'Reject'
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
      revision: proposal.value.workflow.revision,
      decision,
      reject,
      reviewerName: reviewerName.value.trim() || 'Student Reviewer',
      comments: reject ? 'Rejected the proposed subtasks.' :
        decision === 'PartialAccept' ? 'Accepted selected subtasks with optional manual edits.' : 'Accepted all proposed subtasks.',
      tasks: reject ? [] : (decision === 'Approve'
        ? proposal.value.tasks.map((task, index) => ({ ...task, index }))
        : selectedTasks.value).map(({ index, title, description }) => ({
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

async function requestCorrection() {
  if (!props.assignment || !proposal.value || busy.value || !aiBreakdownEnabled.value) return
  if (!feedback.value.trim()) {
    error.value = 'Describe what the agents should change.'
    return
  }
  const currentRevision = revision
  correctionRequestId.value ??= crypto.randomUUID()
  generating.value = true
  error.value = ''
  try {
    const result = await reviseBreakdown(props.assignment.id, {
      workflowId: proposal.value.workflow.id,
      requestId: correctionRequestId.value,
      revision: proposal.value.workflow.revision,
      reviewerName: reviewerName.value.trim() || 'Student Reviewer',
      feedback: feedback.value.trim(),
    })
    if (currentRevision === revision) setProposal(result)
  } catch (reason) {
    if (currentRevision === revision) {
      error.value = reason instanceof Error ? reason.message : 'Unable to revise this proposal. Retry with the same feedback.'
    }
  } finally {
    if (currentRevision === revision) generating.value = false
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
    <form @submit.prevent="submit">
      <div class="nb-dialog__header">
        <span class="nb-mono">AI ASSIGNMENT PLAN</span>
        <button class="nb-icon-btn" type="button" aria-label="Close" @click="close">&times;</button>
      </div>
      <div class="nb-dialog__body">
        <h2 id="breakdown-dialog-title">Break down {{ assignment?.title }}</h2>
        <p v-if="multiAgentEnabled">Review the agents' proposed sub-tasks, then accept, partially accept, request corrections or reject.</p>
        <p v-else>Choose a prompt template, tailor it, then let AI create the sub-tasks.</p>
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
          <p v-if="multiAgentEnabled" class="nb-helper nb-mono">
            THE ASSIGNMENT CONTEXT COMES FROM THE TRACKER. GENERATING A PROPOSAL DOES NOT CREATE TASKS.
          </p>
          <p v-else class="nb-helper nb-mono">
            THE ASSIGNMENT CONTEXT COMES FROM THE TRACKER. REVIEW GENERATED TASKS AFTER CREATION.
          </p>
          <p v-if="generating" role="status">
            {{ multiAgentEnabled ? 'Waiting for Planner, Worker and Reviewer.' : 'AI is creating tasks...' }}
          </p>
        </template>
        <template v-else>
          <p class="nb-helper nb-mono">PROPOSAL REVISION {{ proposal.workflow.revision }}</p>
          <p class="nb-helper">
            The shared server can return fallback review text. Check the proposal against the assignment;
            its reported confidence is not independently verified.
          </p>
          <details class="nb-assistant-response">
            <summary>Planner and reviewer outputs</summary>
            <h3>PLANNER</h3>
            <div class="nb-breakdown-markdown" v-html="renderMarkdown(proposal.workflow.planner.objective)" />
            <ol>
              <li v-for="(step, index) in proposal.workflow.planner.steps" :key="index" class="nb-breakdown-markdown" v-html="renderMarkdown(step)" />
            </ol>
            <h3>REVIEWER</h3>
            <p>{{ proposal.workflow.reviewer.verdict }} / {{ proposal.workflow.reviewer.confidence }}</p>
            <div class="nb-breakdown-markdown" v-html="renderMarkdown(proposal.workflow.reviewer.critique)" />
            <ul>
              <li v-for="(risk, index) in proposal.workflow.reviewer.identifiedRisks" :key="index" class="nb-breakdown-markdown" v-html="renderMarkdown(risk)" />
            </ul>
            <h3>RECOMMENDATIONS</h3>
            <ul>
              <li v-for="(item, index) in proposal.workflow.reviewer.recommendations" :key="index" class="nb-breakdown-markdown" v-html="renderMarkdown(item)" />
            </ul>
          </details>
          <fieldset v-for="task in proposedTasks" :key="task.index" class="nb-breakdown-task" :disabled="busy || mode !== 'partial'">
            <legend>
              <label v-if="mode === 'partial'">
                <input v-model="task.selected" type="checkbox" :disabled="busy" />
                Include task {{ task.index + 1 }}
              </label>
              <span v-else>Task {{ task.index + 1 }}</span>
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
            <span>Reviewer name (optional)</span>
            <input v-model="reviewerName" maxlength="100" placeholder="Student Reviewer" :disabled="busy" />
          </label>
          <label v-if="mode === 'correct'" class="nb-field">
            <span>Feedback for the agents</span>
            <textarea v-model="feedback" rows="4" maxlength="2000" required :disabled="busy" />
            <small>The same workflow returns to Planner, Worker and Reviewer. No tasks are created until you accept.</small>
          </label>
          <p v-if="mode === 'partial'" class="nb-helper">
            Edit or exclude tasks, then Accept to create only your selected subtasks. Cancel discards these edits.
          </p>
          <p v-if="generating" role="status">
            Planner, Worker and Reviewer are preparing a revised proposal.
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
        <template v-if="!proposal">
          <button class="nb-btn nb-btn--outline" type="button" @click="close">Cancel</button>
          <button class="nb-btn nb-btn--accent" type="submit" :disabled="!canGenerate">
            {{ multiAgentEnabled
              ? generating ? 'Agents are preparing a proposal...' : 'Generate proposal'
              : generating ? 'AI is creating tasks...' : 'Generate sub-tasks' }}
          </button>
        </template>
        <template v-else-if="mode === 'decision'">
          <button class="nb-btn nb-btn--accent" type="submit" :disabled="busy || !aiBreakdownEnabled">
            {{ saving ? 'Saving reviewed tasks...' : 'Accept' }}
          </button>
          <button class="nb-btn nb-btn--outline" type="button" :disabled="busy || !aiBreakdownEnabled" @click="mode = 'partial'">
            Partially Accept
          </button>
          <button class="nb-btn nb-btn--outline" type="button" :disabled="busy || !aiBreakdownEnabled" @click="mode = 'correct'">
            Correct
          </button>
          <button class="nb-btn nb-btn--outline" type="button" :disabled="busy || !aiBreakdownEnabled" @click="submitReview('Reject')">
            Reject
          </button>
        </template>
        <template v-else>
          <button
            class="nb-btn nb-btn--accent"
            type="submit"
            :disabled="busy || !aiBreakdownEnabled || (mode === 'partial' ? !selectedTasks.length : !feedback.trim())"
          >
            {{ generating ? 'Revising proposal...' : saving ? 'Saving reviewed tasks...' : mode === 'partial' ? 'Accept' : 'Send feedback' }}
          </button>
          <button class="nb-btn nb-btn--outline" type="button" :disabled="busy" @click="cancelDecision">Cancel</button>
        </template>
      </div>
    </form>
  </BaseDialog>
</template>

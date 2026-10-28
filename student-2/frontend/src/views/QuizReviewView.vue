<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { getQuizReview, reviseQuizReview, submitQuizReview } from '@/api/quizReview'
import type { QuizReview, QuizReviewAnswer, QuizReviewDecision } from '@/types/quizReview'

interface DraftAnswer {
  include: boolean
  answerId: number | null
  text: string
}

const route = useRoute()
const runId = String(route.params.id)
const review = ref<QuizReview | null>(null)
const drafts = ref<Record<number, DraftAnswer>>({})
const reviewerName = ref('Isaac Thomas')
const comments = ref('')
const loading = ref(true)
const submitting = ref(false)
const revising = ref(false)
const feedback = ref('')
const notice = ref('')
const error = ref('')
// Reused when retrying the same feedback so a timed-out request cannot create a second revision.
let revisionRequestId: string | null = null
watch([feedback, reviewerName], () => { revisionRequestId = null })

const busy = computed(() => submitting.value || revising.value)

const awaitingReview = computed(() => review.value?.result === 'REV')
const workflow = computed(() => review.value?.workflow ?? null)
const reviewerNoteList = computed(() =>
  workflow.value ? reviewerNotes(workflow.value.reviewer.critique) : [],
)

const isEdited = computed(() =>
  review.value?.questions.some((question) => {
    const draft = drafts.value[question.id]
    return question.options.length
      ? draft?.answerId !== question.proposedAnswerId
      : (draft?.text.trim() ?? '') !== (question.proposedText ?? '')
  }) ?? false,
)
const includedCount = computed(() =>
  Object.values(drafts.value).filter((draft) => draft.include).length,
)
const decision = computed<QuizReviewDecision>(() => {
  const total = review.value?.questions.length ?? 0
  if (includedCount.value < total) return 'PartialAccept'
  return isEdited.value ? 'Correct' : 'Approve'
})
const decisionLabel = computed(() => ({
  Approve: 'APPROVE & SAVE TO CANVAS',
  Correct: 'SAVE CORRECTED ANSWERS TO CANVAS',
  PartialAccept: `PARTIALLY ACCEPT · SAVE ${includedCount.value} ${includedCount.value === 1 ? 'ANSWER' : 'ANSWERS'}`,
  Reject: 'REJECT',
})[decision.value])
const canSubmit = computed(() =>
  awaitingReview.value &&
  !busy.value &&
  reviewerName.value.trim().length > 0 &&
  includedCount.value > 0 &&
  (review.value?.questions.every((question) => {
    const draft = drafts.value[question.id]
    if (!draft?.include) return true
    return question.options.length ? draft.answerId !== null : draft.text.trim().length > 0
  }) ?? false),
)

onMounted(load)

async function load() {
  loading.value = true
  error.value = ''
  try {
    applyReview(await getQuizReview(runId))
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to load the quiz review.'
  } finally {
    loading.value = false
  }
}

function applyReview(value: QuizReview) {
  review.value = value
  drafts.value = Object.fromEntries(value.questions.map((question) => [
    question.id,
    { include: true, answerId: question.proposedAnswerId, text: question.proposedText ?? '' },
  ]))
}

function buildAnswers(): QuizReviewAnswer[] {
  return (review.value?.questions ?? [])
    .filter((question) => drafts.value[question.id]?.include)
    .map((question) => {
      const draft = drafts.value[question.id]!
      return question.options.length
        ? { questionId: question.id, answerId: draft.answerId, text: null }
        : { questionId: question.id, answerId: null, text: draft.text.trim() }
    })
}

async function submit(selected: QuizReviewDecision) {
  if (!review.value || busy.value) return
  submitting.value = true
  error.value = ''
  notice.value = ''
  try {
    applyReview(await submitQuizReview(runId, {
      decision: selected,
      reviewerName: reviewerName.value.trim(),
      comments: comments.value.trim(),
      answers: selected === 'Correct' || selected === 'PartialAccept' ? buildAnswers() : undefined,
    }))
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'The review could not be saved.'
    try {
      applyReview(await getQuizReview(runId))
    } catch {
      // Keep the current view; the error above already explains the failure.
    }
  } finally {
    submitting.value = false
  }
}

async function requestRevision() {
  const text = feedback.value.trim()
  if (!review.value || busy.value || !text || !reviewerName.value.trim()) return
  revising.value = true
  error.value = ''
  notice.value = ''
  revisionRequestId ??= crypto.randomUUID()
  const revisionBefore = review.value.workflowRevision
  try {
    applyReview(await reviseQuizReview(runId, {
      requestId: revisionRequestId,
      reviewerName: reviewerName.value.trim(),
      feedback: text,
    }))
    feedback.value = ''
    revisionRequestId = null
    notice.value = `Revision ${review.value.workflowRevision} received. Check the new answers before deciding.`
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'The agents could not revise the answers.'
    try {
      applyReview(await getQuizReview(runId))
      if (review.value.workflowRevision !== revisionBefore) revisionRequestId = null
    } catch {
      // Keep the current view; the error above already explains the failure.
    }
  } finally {
    revising.value = false
  }
}

function optionLabel(index: number) {
  return String.fromCharCode(65 + index)
}

function formatStatus(status: string) {
  return status.replace(/([a-z])([A-Z])/g, '$1 $2').toUpperCase()
}

const MAX_NOTES = 12
// IDs and echoed answer payloads are already shown in the question list below.
const SKIPPED_KEYS = /(^id$|Id$|^answers$|^notifications$|^reviewedOutput$|^text$)/i

function humanise(key: string) {
  const spaced = key.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/[_-]+/g, ' ').toLowerCase()
  return spaced.charAt(0).toUpperCase() + spaced.slice(1)
}

function collectNotes(value: unknown, label: string, prefix: string, notes: string[]) {
  if (notes.length >= MAX_NOTES || value === null || value === undefined) return
  if (Array.isArray(value)) {
    value.forEach((item) => collectNotes(item, label, prefix, notes))
    return
  }
  if (typeof value === 'object') {
    for (const [key, child] of Object.entries(value)) {
      if (SKIPPED_KEYS.test(key)) continue
      if (/^Q\d+$/i.test(key)) collectNotes(child, '', `${key.toUpperCase()} · `, notes)
      else collectNotes(child, humanise(key), prefix, notes)
    }
    return
  }
  const text = typeof value === 'boolean' ? (value ? 'Yes' : 'No') : String(value).trim()
  if (text) notes.push(`${prefix}${label ? `${label}: ` : ''}${text}`)
}

// Model output is sometimes cut off mid-object; close open strings and brackets so it can still be parsed.
function closeTruncatedJson(text: string) {
  const closers: string[] = []
  let inString = false
  let escaped = false
  for (const character of text) {
    if (inString) {
      if (escaped) escaped = false
      else if (character === '\\') escaped = true
      else if (character === '"') inString = false
    } else if (character === '"') inString = true
    else if (character === '{') closers.push('}')
    else if (character === '[') closers.push(']')
    else if (character === '}' || character === ']') closers.pop()
  }
  const repaired = (inString ? `${text}"` : text)
    .replace(/,?\s*"[^"]*"\s*:\s*$/, '')
    .replace(/,\s*$/, '')
  return repaired + closers.reverse().join('')
}

function parseJson(text: string): unknown {
  try {
    return JSON.parse(text)
  } catch {
    try {
      return JSON.parse(closeTruncatedJson(text))
    } catch {
      return undefined
    }
  }
}

function reviewerNotes(critique: string): string[] {
  const trimmed = critique.trim().replace(/^```(?:json)?\s*/i, '').replace(/```$/, '').trim()
  if (!trimmed.startsWith('{') && !trimmed.startsWith('[')) return [critique.trim()]
  const parsed = parseJson(trimmed)
  const notes: string[] = []
  if (parsed !== undefined) {
    collectNotes(parsed, '', '', notes)
    if (!notes.length && /"(answers|reviewedOutput)"\s*:/.test(trimmed)) {
      return ['The Reviewer agent checked the proposed answers and raised no objections.']
    }
  } else {
    for (const match of trimmed.matchAll(/"(\w+)"\s*:\s*"((?:[^"\\]|\\.)*)"/g)) {
      if (notes.length >= MAX_NOTES) break
      const [, key = '', value = ''] = match
      if (!SKIPPED_KEYS.test(key)) {
        notes.push(`${humanise(key)}: ${value.replace(/\\[nrt]/g, ' ').replace(/\\"/g, '"')}`)
      }
    }
  }
  return notes.length ? notes : ['The Reviewer agent checked the proposal but left no written comments.']
}
</script>

<template>
  <section class="nb-page nb-review-page">
    <header class="nb-page-header">
      <div>
        <p class="nb-eyebrow nb-mono">RELEASE 2 · SHARED MULTI-AGENT SERVER (:5004)</p>
        <h1>REVIEW QUIZ ANSWERS</h1>
        <p class="nb-page-subtitle">
          Planner → Worker → Reviewer → you. Nothing is saved to Canvas until you decide.
          Approved answers are saved into your open quiz attempt; the quiz is never handed in for you,
          so open it in Canvas to check and submit it yourself.
        </p>
      </div>
      <RouterLink class="nb-btn nb-btn--outline" :to="{ name: 'history' }">Back to run history</RouterLink>
    </header>

    <div v-if="error" class="nb-alert nb-alert--error" role="alert">{{ error }}</div>
    <div v-if="notice" class="nb-alert" role="status">{{ notice }}</div>
    <div v-if="loading" class="nb-panel nb-empty">Loading proposal...</div>

    <template v-else-if="review">
      <div class="nb-review-summary">
        <span class="nb-result" :class="`nb-result--${review.result.toLowerCase()}`">{{ review.result }}</span>
        <div>
          <strong>{{ review.quizTitle }}</strong>
          <p class="nb-mono nb-detail">
            COURSE {{ review.courseId }} · QUIZ {{ review.quizId }} · {{ review.questions.length }} QUESTIONS ·
            {{ review.proposalSource === 'multi-agent-worker' ? 'ANSWERS FROM WORKER AGENT' : 'WORKER FELL BACK TO AI MODE BASELINE' }}
          </p>
        </div>
        <span v-if="workflow" class="nb-tag nb-mono">
          WORKFLOW {{ formatStatus(workflow.status) }}{{ review.workflowRevision ? ` · REVISION ${review.workflowRevision}` : '' }}
        </span>
      </div>

      <div v-if="review.workflowError" class="nb-alert" role="status">
        Multi-Agent stages unavailable: {{ review.workflowError }}
      </div>

      <div v-if="workflow" class="nb-agent-grid">
        <article class="nb-agent-card">
          <span class="nb-mono nb-agent-card__tag">01 // PLANNER</span>
          <p>{{ workflow.planner.objective }}</p>
          <ol>
            <li v-for="(step, index) in workflow.planner.steps" :key="index">{{ step }}</li>
          </ol>
        </article>
        <article class="nb-agent-card">
          <span class="nb-mono nb-agent-card__tag">02 // WORKER</span>
          <p>Drafted {{ review.questions.length }} answers from the quiz evidence.</p>
          <ul>
            <li v-for="citation in workflow.worker.citations" :key="citation" class="nb-mono">{{ citation }}</li>
          </ul>
        </article>
        <article class="nb-agent-card">
          <span class="nb-mono nb-agent-card__tag">03 // REVIEWER · {{ workflow.reviewer.confidence.toUpperCase() }}</span>
          <ul class="nb-reviewer-notes">
            <li v-for="(note, index) in reviewerNoteList" :key="index">{{ note }}</li>
          </ul>
          <p v-if="workflow.reviewer.identifiedRisks.length" class="nb-mono nb-detail">RISKS</p>
          <ul>
            <li v-for="(risk, index) in workflow.reviewer.identifiedRisks" :key="index">{{ risk }}</li>
          </ul>
        </article>
      </div>

      <ol class="nb-question-list">
        <li v-for="(question, index) in review.questions" :key="question.id" class="nb-question">
          <div class="nb-question__header">
            <span class="nb-mono">Q{{ index + 1 }} · {{ question.questionType.replace(/_/g, ' ').toUpperCase() }}</span>
            <label v-if="awaitingReview" class="nb-checkbox-row nb-question__include">
              <input v-model="drafts[question.id]!.include" type="checkbox" />
              <span>Save this answer</span>
            </label>
          </div>
          <p class="nb-question__text">{{ question.questionText }}</p>

          <fieldset v-if="question.options.length" class="nb-fieldset" :disabled="!awaitingReview || !drafts[question.id]!.include">
            <legend class="nb-mono">Answer</legend>
            <label
              v-for="(option, optionIndex) in question.options"
              :key="option.id"
              class="nb-option"
              :class="{ 'nb-option--proposed': option.id === question.proposedAnswerId }"
            >
              <input v-model="drafts[question.id]!.answerId" type="radio" :name="`question-${question.id}`" :value="option.id" />
              <span class="nb-mono">{{ optionLabel(optionIndex) }}</span>
              <span>{{ option.text }}</span>
              <span v-if="option.id === question.proposedAnswerId" class="nb-tag nb-mono">AGENT PICK</span>
            </label>
          </fieldset>

          <label v-else class="nb-field">
            <span>Written answer</span>
            <textarea
              v-model="drafts[question.id]!.text"
              rows="3"
              maxlength="16000"
              :disabled="!awaitingReview || !drafts[question.id]!.include"
            />
          </label>
        </li>
      </ol>

      <section v-if="awaitingReview" class="nb-panel nb-review-decision">
        <div class="nb-form-grid">
          <label class="nb-field">
            <span>Reviewer name</span>
            <input v-model="reviewerName" type="text" maxlength="100" required />
          </label>
          <label class="nb-field">
            <span>Comments</span>
            <input v-model="comments" type="text" maxlength="2000" placeholder="Why you approved or changed answers" />
          </label>
        </div>
        <div class="nb-form-actions">
          <button type="button" class="nb-btn nb-btn--outline" :disabled="busy" @click="submit('Reject')">
            Reject (save nothing)
          </button>
          <button type="button" class="nb-btn nb-btn--accent" :disabled="!canSubmit" @click="submit(decision)">
            {{ submitting ? 'Saving...' : decisionLabel }}
          </button>
        </div>

        <div class="nb-revision-request">
          <label class="nb-field">
            <span>Not right? Ask the agents to try again</span>
            <textarea
              v-model="feedback"
              rows="3"
              maxlength="2000"
              :disabled="busy"
              placeholder="Explain what is wrong, e.g. 'Q2 should be True: most organisms have double-stranded DNA.'"
            />
          </label>
          <div class="nb-form-actions">
            <span v-if="revising" class="nb-mono nb-detail">Planner, Worker and Reviewer are revising the answers...</span>
            <button
              type="button"
              class="nb-btn nb-btn--outline"
              :disabled="busy || !feedback.trim() || !reviewerName.trim()"
              @click="requestRevision"
            >
              {{ revising ? 'Waiting for agents...' : 'Request revision' }}
            </button>
          </div>
        </div>
      </section>

      <section v-else-if="workflow?.humanReview" class="nb-panel nb-review-decision">
        <p class="nb-mono nb-eyebrow">04 // HUMAN REVIEW</p>
        <p>
          <strong>{{ workflow.humanReview.decision }}</strong> by {{ workflow.humanReview.reviewerName }}
          — {{ workflow.humanReview.comments }}
        </p>
        <ul class="nb-audit-log">
          <li v-for="(entry, index) in workflow.auditLog" :key="index">
            <span class="nb-mono">{{ entry.agent }} · {{ entry.action }}</span> {{ entry.summary }}
          </li>
        </ul>
      </section>
    </template>
  </section>
</template>

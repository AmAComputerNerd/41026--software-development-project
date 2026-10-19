<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { CURRENT_STUDENT_ID } from '@/config'
import {
  listMultiAgentWorkflows,
  runMultiAgentWorkflow,
  submitMultiAgentHumanReview,
  type MultiAgentWorkflowRecord,
} from '@/api/integration'

const goal = ref(
  'Triage my unread Canvas deadline and grade notifications and build a prioritised 48-hour action plan.',
)
const reviewerName = ref('Bryan Lee (25495108)')
const reviewComments = ref('Verified notification priorities and approved the 48-hour study action plan.')
const correctedOutput = ref('')
const showCorrectionEditor = ref(false)

const runningWorkflow = ref(false)
const submittingReview = ref(false)
const loadingHistory = ref(false)
const errorMsg = ref<string | null>(null)

const currentWorkflow = ref<MultiAgentWorkflowRecord | null>(null)
const workflowHistory = ref<MultiAgentWorkflowRecord[]>([])

const presetGoals = [
  'Triage my unread Canvas deadline and grade notifications and build a prioritised 48-hour action plan.',
  'Audit my notification delivery preferences against upcoming high-weight assessments.',
  'Identify overdue or at-risk coursework from recent alerts and draft an action checklist.',
]

async function loadHistory() {
  loadingHistory.value = true
  try {
    const list = await listMultiAgentWorkflows(CURRENT_STUDENT_ID)
    workflowHistory.value = Array.isArray(list) ? list : []
    if (!currentWorkflow.value && workflowHistory.value.length > 0) {
      currentWorkflow.value = workflowHistory.value[0] ?? null
      if (currentWorkflow.value) {
        correctedOutput.value = currentWorkflow.value.worker.draftOutput
      }
    }
  } catch {
    // History may be empty or service disabled in baseline mode
  } finally {
    loadingHistory.value = false
  }
}

async function handleRunWorkflow() {
  const trimmed = goal.value.trim()
  if (trimmed.length < 5 || runningWorkflow.value) return

  runningWorkflow.value = true
  errorMsg.value = null
  try {
    const record = await runMultiAgentWorkflow(trimmed, CURRENT_STUDENT_ID)
    currentWorkflow.value = record
    correctedOutput.value = record.worker.draftOutput
    showCorrectionEditor.value = false
    await loadHistory()
  } catch (err: any) {
    errorMsg.value =
      err?.message || 'Failed to execute Multi-Agent workflow. Ensure multi-agent-server (:5004) is running.'
  } finally {
    runningWorkflow.value = false
  }
}

async function handleHumanDecision(decision: 'Approve' | 'Correct' | 'PartialAccept' | 'Reject') {
  if (!currentWorkflow.value || submittingReview.value) return

  if ((decision === 'Correct' || decision === 'PartialAccept') && !showCorrectionEditor.value) {
    showCorrectionEditor.value = true
    return
  }

  submittingReview.value = true
  errorMsg.value = null
  try {
    const updated = await submitMultiAgentHumanReview(
      currentWorkflow.value.id,
      decision,
      reviewerName.value.trim() || 'Bryan Lee (25495108)',
      reviewComments.value.trim(),
      decision === 'Correct' || decision === 'PartialAccept' ? correctedOutput.value.trim() : undefined,
    )
    currentWorkflow.value = updated
    showCorrectionEditor.value = false
    await loadHistory()
  } catch (err: any) {
    errorMsg.value = err?.message || 'Failed to submit human review decision.'
  } finally {
    submittingReview.value = false
  }
}

function selectWorkflow(record: MultiAgentWorkflowRecord) {
  currentWorkflow.value = record
  correctedOutput.value = record.worker.draftOutput
  showCorrectionEditor.value = false
}

onMounted(loadHistory)
</script>

<template>
  <section class="nb-ma-panel" data-testid="multi-agent-panel">
    <div class="nb-ma-banner">
      <div>
        <span class="nb-ma-badge nb-mono">RELEASE 2 — SHARED MULTI-AGENT SERVER (:5004)</span>
        <h2 class="nb-ma-title">PLANNER → WORKER → REVIEWER → HUMAN REVIEW</h2>
      </div>
      <span
        v-if="currentWorkflow"
        class="nb-ma-status-pill nb-mono"
        :data-status="currentWorkflow.status"
      >
        STATUS: {{ currentWorkflow.status }}
      </span>
    </div>

    <!-- Goal Launcher -->
    <div class="nb-ma-launcher">
      <label class="nb-mono nb-ma-label" for="ma-goal-input">WORKFLOW GOAL / TASK</label>
      <div class="nb-ma-presets">
        <button
          v-for="(preset, idx) in presetGoals"
          :key="idx"
          type="button"
          class="nb-ma-preset-chip nb-mono"
          @click="goal = preset"
        >
          PRESET 0{{ idx + 1 }}
        </button>
      </div>
      <div class="nb-ma-input-row">
        <input
          id="ma-goal-input"
          v-model="goal"
          type="text"
          class="nb-ma-input"
          placeholder="Describe the notification triage or study planning goal..."
          data-testid="multi-agent-goal-input"
        />
        <button
          type="button"
          class="nb-btn nb-ma-run-btn nb-mono"
          :disabled="runningWorkflow || goal.trim().length < 5"
          data-testid="multi-agent-run-btn"
          @click="handleRunWorkflow"
        >
          {{ runningWorkflow ? 'EXECUTING AGENTS...' : '▶ RUN MULTI-AGENT WORKFLOW' }}
        </button>
      </div>
    </div>

    <div v-if="errorMsg" class="nb-ma-error nb-mono" data-testid="multi-agent-error">
      ⚠ {{ errorMsg }}
    </div>

    <!-- 4-Stage Pipeline Cards -->
    <div v-if="currentWorkflow" class="nb-ma-grid" data-testid="multi-agent-stages">
      <!-- Stage 1: Planner -->
      <article class="nb-ma-card nb-ma-card--planner" data-testid="stage-planner">
        <header class="nb-ma-card__header">
          <span class="nb-mono nb-ma-step-tag">01 // PLANNER AGENT</span>
          <span class="nb-mono nb-ma-time">
            {{ new Date(currentWorkflow.planner.completedAtUtc).toLocaleTimeString() }}
          </span>
        </header>
        <p class="nb-ma-objective"><strong>Objective:</strong> {{ currentWorkflow.planner.objective }}</p>
        <div class="nb-ma-section">
          <span class="nb-mono nb-ma-sublabel">REQUIRED EVIDENCE:</span>
          <ul class="nb-ma-list">
            <li v-for="(ev, i) in currentWorkflow.planner.requiredEvidence" :key="i">{{ ev }}</li>
          </ul>
        </div>
        <div class="nb-ma-section">
          <span class="nb-mono nb-ma-sublabel">EXECUTION STEPS:</span>
          <ol class="nb-ma-list">
            <li v-for="(st, i) in currentWorkflow.planner.steps" :key="i">{{ st }}</li>
          </ol>
        </div>
      </article>

      <!-- Stage 2: Worker -->
      <article class="nb-ma-card nb-ma-card--worker" data-testid="stage-worker">
        <header class="nb-ma-card__header">
          <span class="nb-mono nb-ma-step-tag">02 // WORKER AGENT</span>
          <span class="nb-mono nb-ma-time">
            {{ new Date(currentWorkflow.worker.completedAtUtc).toLocaleTimeString() }}
          </span>
        </header>
        <p class="nb-ma-evidence-box nb-mono">
          <strong>Evidence Snapshot:</strong> {{ currentWorkflow.worker.evidenceSummary }}
        </p>
        <div class="nb-ma-draft-box">
          <strong>Worker Output:</strong>
          <p>{{ currentWorkflow.worker.draftOutput }}</p>
        </div>
        <div class="nb-ma-section">
          <span class="nb-mono nb-ma-sublabel">RECOMMENDED ACTIONS:</span>
          <ul class="nb-ma-list">
            <li v-for="(act, i) in currentWorkflow.worker.recommendedActions" :key="i">{{ act }}</li>
          </ul>
        </div>
        <div class="nb-ma-citations">
          <span
            v-for="(cit, i) in currentWorkflow.worker.citations"
            :key="i"
            class="nb-ma-citation-pill nb-mono"
          >
            📌 {{ cit }}
          </span>
        </div>
      </article>

      <!-- Stage 3: Reviewer -->
      <article class="nb-ma-card nb-ma-card--reviewer" data-testid="stage-reviewer">
        <header class="nb-ma-card__header">
          <span class="nb-mono nb-ma-step-tag">03 // REVIEWER AGENT</span>
          <span class="nb-mono nb-ma-confidence">
            CONFIDENCE: {{ currentWorkflow.reviewer.confidence.toUpperCase() }}
          </span>
        </header>
        <p class="nb-ma-critique"><strong>Critique:</strong> {{ currentWorkflow.reviewer.critique }}</p>
        <div class="nb-ma-section">
          <span class="nb-mono nb-ma-sublabel">IDENTIFIED RISKS:</span>
          <ul class="nb-ma-list">
            <li v-for="(risk, i) in currentWorkflow.reviewer.identifiedRisks" :key="i">{{ risk }}</li>
          </ul>
        </div>
        <div class="nb-ma-section">
          <span class="nb-mono nb-ma-sublabel">REVIEWER RECOMMENDATIONS:</span>
          <ul class="nb-ma-list">
            <li v-for="(rec, i) in currentWorkflow.reviewer.recommendations" :key="i">{{ rec }}</li>
          </ul>
        </div>
      </article>

      <!-- Stage 4: Human Review Gate -->
      <article class="nb-ma-card nb-ma-card--human" data-testid="stage-human-review">
        <header class="nb-ma-card__header">
          <span class="nb-mono nb-ma-step-tag">04 // HUMAN REVIEW GATE</span>
          <span class="nb-mono nb-ma-verdict">
            {{ currentWorkflow.humanReview ? currentWorkflow.humanReview.decision.toUpperCase() : 'PENDING DECISION' }}
          </span>
        </header>

        <div v-if="currentWorkflow.humanReview" class="nb-ma-human-summary" data-testid="human-review-completed">
          <p><strong>Reviewer:</strong> {{ currentWorkflow.humanReview.reviewerName }}</p>
          <p><strong>Decision:</strong> {{ currentWorkflow.humanReview.decision }}</p>
          <p><strong>Comments:</strong> {{ currentWorkflow.humanReview.comments }}</p>
          <div class="nb-ma-final-output">
            <span class="nb-mono nb-ma-sublabel">FINAL APPROVED / CORRECTED OUTPUT:</span>
            <p>{{ currentWorkflow.humanReview.finalOutput }}</p>
          </div>
        </div>

        <div class="nb-ma-human-controls">
          <div class="nb-ma-field-row">
            <label class="nb-mono nb-ma-sublabel">REVIEWER NAME</label>
            <input v-model="reviewerName" type="text" class="nb-ma-input" />
          </div>
          <div class="nb-ma-field-row">
            <label class="nb-mono nb-ma-sublabel">REVIEWER NOTES / RATIONALE</label>
            <input
              v-model="reviewComments"
              type="text"
              class="nb-ma-input"
              data-testid="human-review-comments"
            />
          </div>

          <div v-if="showCorrectionEditor" class="nb-ma-field-row">
            <label class="nb-mono nb-ma-sublabel">CORRECTED / PARTIALLY ACCEPTED OUTPUT</label>
            <textarea
              v-model="correctedOutput"
              rows="3"
              class="nb-ma-textarea"
              data-testid="human-review-corrected-output"
            />
          </div>

          <div class="nb-ma-decision-btns">
            <button
              type="button"
              class="nb-btn nb-ma-btn--approve nb-mono"
              :disabled="submittingReview"
              data-testid="btn-human-approve"
              @click="handleHumanDecision('Approve')"
            >
              ✓ APPROVE
            </button>
            <button
              type="button"
              class="nb-btn nb-ma-btn--partial nb-mono"
              :disabled="submittingReview"
              data-testid="btn-human-partial"
              @click="handleHumanDecision('PartialAccept')"
            >
              ◐ PARTIAL ACCEPT
            </button>
            <button
              type="button"
              class="nb-btn nb-ma-btn--correct nb-mono"
              :disabled="submittingReview"
              data-testid="btn-human-correct"
              @click="handleHumanDecision('Correct')"
            >
              ✎ CORRECT
            </button>
            <button
              type="button"
              class="nb-btn nb-ma-btn--reject nb-mono"
              :disabled="submittingReview"
              data-testid="btn-human-reject"
              @click="handleHumanDecision('Reject')"
            >
              ✕ REJECT
            </button>
          </div>
        </div>
      </article>
    </div>

    <!-- Coordination Audit Log & Workflow History -->
    <div v-if="currentWorkflow" class="nb-ma-audit-box" data-testid="multi-agent-audit-log">
      <h3 class="nb-mono nb-ma-audit-title">COORDINATION & AUDIT LOG ({{ currentWorkflow.id }})</h3>
      <ul class="nb-ma-audit-list nb-mono">
        <li v-for="(entry, idx) in currentWorkflow.auditLog" :key="idx">
          <span class="nb-ma-audit-agent">[{{ entry.agent }}]</span>
          <strong>{{ entry.action }}</strong> — {{ entry.summary }}
        </li>
      </ul>
    </div>

    <div v-if="workflowHistory.length > 0" class="nb-ma-history" data-testid="multi-agent-history">
      <h3 class="nb-mono nb-ma-audit-title">WORKFLOW RUN HISTORY ({{ workflowHistory.length }})</h3>
      <div class="nb-ma-history-list">
        <button
          v-for="item in workflowHistory"
          :key="item.id"
          type="button"
          class="nb-ma-history-item nb-mono"
          :class="{ 'nb-ma-history-item--active': currentWorkflow?.id === item.id }"
          @click="selectWorkflow(item)"
        >
          <span>{{ item.goal.slice(0, 60) }}{{ item.goal.length > 60 ? '...' : '' }}</span>
          <span class="nb-ma-history-status">[{{ item.status }}]</span>
        </button>
      </div>
    </div>
  </section>
</template>

<style scoped>
.nb-ma-panel {
  border: var(--nb-border-width-md, 3px) solid var(--nb-color-ink, #000);
  background: var(--nb-color-surface, #fff);
  padding: 20px;
  margin-bottom: 24px;
  box-shadow: 6px 6px 0 var(--nb-color-ink, #000);
}

.nb-ma-banner {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
  flex-wrap: wrap;
  border-bottom: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink, #000);
  padding-bottom: 12px;
  margin-bottom: 16px;
}

.nb-ma-badge {
  display: inline-block;
  background: var(--nb-color-accent-yellow, #ffd600);
  color: #000;
  font-size: 11px;
  font-weight: 800;
  padding: 3px 8px;
  border: 2px solid var(--nb-color-ink, #000);
  margin-bottom: 6px;
}

.nb-ma-title {
  font-size: 20px;
  font-weight: 900;
  margin: 0;
}

.nb-ma-status-pill {
  background: var(--nb-color-accent-green, #00e676);
  color: #000;
  font-weight: 800;
  font-size: 12px;
  padding: 6px 12px;
  border: 2px solid var(--nb-color-ink, #000);
  box-shadow: 3px 3px 0 var(--nb-color-ink, #000);
}

.nb-ma-launcher {
  margin-bottom: 16px;
}

.nb-ma-label {
  display: block;
  font-size: 12px;
  font-weight: 800;
  margin-bottom: 6px;
}

.nb-ma-presets {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-bottom: 8px;
}

.nb-ma-preset-chip {
  background: var(--nb-color-bg, #f5f5f0);
  color: var(--nb-color-ink, #000);
  border: 2px solid var(--nb-color-ink, #000);
  padding: 4px 10px;
  font-size: 11px;
  font-weight: 700;
  cursor: pointer;
}

.nb-ma-input-row {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.nb-ma-input,
.nb-ma-textarea {
  flex: 1 1 280px;
  border: 2px solid var(--nb-color-ink, #000);
  padding: 10px 12px;
  font-family: inherit;
  font-size: 14px;
  background: var(--nb-color-bg, #fff);
  color: var(--nb-color-ink, #000);
}

.nb-ma-run-btn {
  background: var(--nb-color-accent-orange, #ff9100);
  color: #000;
  border: 2px solid var(--nb-color-ink, #000);
  padding: 10px 18px;
  font-weight: 900;
  cursor: pointer;
  box-shadow: 4px 4px 0 var(--nb-color-ink, #000);
}

.nb-ma-error {
  background: #ff5252;
  color: #000;
  border: 2px solid #000;
  padding: 10px 14px;
  font-weight: 700;
  margin-bottom: 16px;
}

.nb-ma-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 16px;
  margin-bottom: 16px;
}

.nb-ma-card {
  border: 2px solid var(--nb-color-ink, #000);
  padding: 14px;
  background: var(--nb-color-bg, #fafafa);
  box-shadow: 4px 4px 0 var(--nb-color-ink, #000);
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.nb-ma-card__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 2px solid var(--nb-color-ink, #000);
  padding-bottom: 6px;
}

.nb-ma-step-tag {
  font-weight: 900;
  font-size: 12px;
}

.nb-ma-sublabel {
  display: block;
  font-size: 11px;
  font-weight: 800;
  margin-bottom: 4px;
}

.nb-ma-list {
  margin: 0;
  padding-left: 18px;
  font-size: 13px;
}

.nb-ma-evidence-box,
.nb-ma-draft-box,
.nb-ma-final-output {
  border: 2px solid var(--nb-color-ink, #000);
  padding: 8px 10px;
  font-size: 12px;
  background: var(--nb-color-surface, #fff);
}

.nb-ma-citations {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.nb-ma-citation-pill {
  font-size: 11px;
  border: 1px solid var(--nb-color-ink, #000);
  padding: 2px 6px;
  background: var(--nb-color-accent-yellow, #ffd600);
  color: #000;
}

.nb-ma-human-controls {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.nb-ma-field-row {
  display: flex;
  flex-direction: column;
}

.nb-ma-decision-btns {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-top: 4px;
}

.nb-ma-btn--approve,
.nb-ma-btn--partial,
.nb-ma-btn--correct,
.nb-ma-btn--reject {
  border: 2px solid #000;
  padding: 6px 10px;
  font-size: 11px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 2px 2px 0 #000;
  color: #000;
}

.nb-ma-btn--approve {
  background: #00e676;
}

.nb-ma-btn--partial {
  background: #ffd600;
}

.nb-ma-btn--correct {
  background: #40c4ff;
}

.nb-ma-btn--reject {
  background: #ff5252;
}

.nb-ma-audit-box,
.nb-ma-history {
  border-top: 2px solid var(--nb-color-ink, #000);
  padding-top: 12px;
  margin-top: 12px;
}

.nb-ma-audit-title {
  font-size: 13px;
  font-weight: 800;
  margin: 0 0 8px;
}

.nb-ma-audit-list {
  margin: 0;
  padding-left: 18px;
  font-size: 12px;
}

.nb-ma-history-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.nb-ma-history-item {
  display: flex;
  justify-content: space-between;
  border: 2px solid var(--nb-color-ink, #000);
  padding: 6px 10px;
  background: var(--nb-color-bg, #f5f5f0);
  color: var(--nb-color-ink, #000);
  cursor: pointer;
  font-size: 12px;
  text-align: left;
}

.nb-ma-history-item--active {
  background: var(--nb-color-accent-yellow, #ffd600);
  color: #000;
  font-weight: 800;
}
</style>

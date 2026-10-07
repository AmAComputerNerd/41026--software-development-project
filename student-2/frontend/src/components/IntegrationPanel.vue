<script setup lang="ts">
import { ref } from 'vue'
import {
  askProjectQuestion,
  reviewAutomationHealthThroughMcp,
  type McpAutomationResult,
  type RagAnswerResult,
} from '@/api/integrations'

type Mode = 'mcp' | 'rag'

const suggestedQuestion = 'How do scheduled post automations work?'
const mode = ref<Mode>('mcp')
const days = ref(30)
const question = ref('')
const loading = ref(false)
const error = ref('')
const mcpResult = ref<McpAutomationResult | null>(null)
const ragResult = ref<RagAnswerResult | null>(null)

async function runMcpTool() {
  loading.value = true
  error.value = ''
  mcpResult.value = null

  try {
    const result = await reviewAutomationHealthThroughMcp(days.value)
    mcpResult.value = result
    if (result.status === 'error') {
      error.value = result.error?.message ?? 'The MCP tool returned an error.'
    }
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to invoke the MCP tool.'
  } finally {
    loading.value = false
  }
}

async function runRagQuery() {
  const value = question.value.trim() || suggestedQuestion
  question.value = value

  loading.value = true
  error.value = ''
  ragResult.value = null

  try {
    ragResult.value = await askProjectQuestion(value)
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to query project knowledge.'
  } finally {
    loading.value = false
  }
}

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleString() : 'No recent run'
}
</script>

<template>
  <section class="nb-integration" aria-labelledby="integration-title">
    <header class="nb-integration__header">
      <div>
        <p class="nb-eyebrow nb-mono">SHARED INTELLIGENCE</p>
        <h2 id="integration-title">AUTOMATION ASSISTANT</h2>
      </div>
      <div class="nb-integration__tabs" role="tablist" aria-label="Integration mode">
        <button
          type="button"
          role="tab"
          :aria-selected="mode === 'mcp'"
          :class="{ 'nb-integration__tab--active': mode === 'mcp' }"
          @click="mode = 'mcp'; error = ''"
        >
          LIVE DATA / MCP
        </button>
        <button
          type="button"
          role="tab"
          :aria-selected="mode === 'rag'"
          :class="{ 'nb-integration__tab--active': mode === 'rag' }"
          @click="mode = 'rag'; error = ''"
        >
          PROJECT HELP / RAG
        </button>
      </div>
    </header>

    <div v-if="mode === 'mcp'" class="nb-integration__body" role="tabpanel">
      <form class="nb-integration__form" @submit.prevent="runMcpTool">
        <label class="nb-field">
          <span>Review period (days)</span>
          <input v-model.number="days" type="number" min="1" max="90" required />
        </label>
        <button class="nb-btn nb-btn--accent" type="submit" :disabled="loading">
          {{ loading ? 'Reviewing runs...' : 'Review automation health' }}
        </button>
      </form>

      <div v-if="mcpResult?.data" class="nb-integration__result" aria-live="polite">
        <div class="nb-integration__result-heading">
          <strong>{{ mcpResult.data.health.replace('_', ' ').toUpperCase() }}</strong>
          <span class="nb-tag nb-tag--extension">MCP RESULT</span>
        </div>
        <p>{{ mcpResult.data.summary }}</p>
        <dl class="nb-integration__metrics">
          <div><dt>Enabled</dt><dd>{{ mcpResult.data.metrics.enabledAutomations }}</dd></div>
          <div><dt>Successful</dt><dd>{{ mcpResult.data.metrics.successfulRuns }}</dd></div>
          <div><dt>Failed</dt><dd>{{ mcpResult.data.metrics.failedRuns }}</dd></div>
          <div>
            <dt>Success rate</dt>
            <dd>
              {{ mcpResult.data.metrics.successRatePercent ?? 'N/A' }}<span v-if="mcpResult.data.metrics.successRatePercent !== null">%</span>
            </dd>
          </div>
        </dl>
        <small class="nb-mono">LAST RUN: {{ formatDate(mcpResult.data.metrics.lastRunAt) }}</small>
      </div>
    </div>

    <div v-else class="nb-integration__body" role="tabpanel">
      <form
        class="nb-integration__form nb-integration__form--rag"
        @submit.prevent="runRagQuery"
        @keydown.enter.prevent="runRagQuery"
      >
        <label class="nb-field">
          <span>Project question</span>
          <input
            v-model="question"
            type="text"
            minlength="3"
            maxlength="1000"
            :placeholder="suggestedQuestion"
          />
        </label>
        <button class="nb-btn nb-btn--accent" type="submit" :disabled="loading">
          {{ loading ? 'Searching sources...' : 'Ask project help' }}
        </button>
      </form>

      <div
        v-if="ragResult"
        class="nb-integration__result"
        :class="{ 'nb-integration__result--insufficient': ragResult.status === 'insufficient_context' }"
        aria-live="polite"
      >
        <div class="nb-integration__result-heading">
          <span class="nb-tag">CONFIDENCE: {{ ragResult.confidence.toUpperCase() }}</span>
        </div>
        <p>{{ ragResult.answer }}</p>
        <ol v-if="ragResult.citations.length" class="nb-integration__citations">
          <li v-for="citation in ragResult.citations" :key="`${citation.sourceId}:${citation.heading}`">
            <strong>{{ citation.title }}</strong>
            <span>{{ citation.heading }}</span>
            <code>{{ citation.sourceId }}</code>
          </li>
        </ol>
      </div>
    </div>

    <div v-if="error" class="nb-alert nb-alert--error" role="alert">{{ error }}</div>
  </section>
</template>
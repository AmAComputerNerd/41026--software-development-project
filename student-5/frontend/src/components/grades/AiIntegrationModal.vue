<script setup lang="ts">
import { ref, watch } from 'vue'
import { useGrades } from '@/composables/useGrades'
import type { McpResponse, RagAnswerResponse } from '@/api/grades'

const { generateRecommendation, getMcpWeightings, getRagAnswer } = useGrades()

defineProps<{
  open: boolean
}>()

const emit = defineEmits<{
  close: []
}>()

const activeTab = ref<'ai' | 'mcp' | 'rag'>('ai')

const aiLoading = ref(false)
const aiRecommendation = ref<string | null>(null)
const aiError = ref<string | null>(null)

const mcpLoading = ref(false)
const mcpWeight = ref(40)
const mcpLimit = ref(5)
const mcpResult = ref<McpResponse | null>(null)
const mcpError = ref<string | null>(null)

const ragLoading = ref(false)
const ragQuestion = ref('')
const ragResult = ref<RagAnswerResponse | null>(null)
const ragError = ref<string | null>(null)

watch(
  () => activeTab.value,
  () => {
    aiRecommendation.value = null
    aiError.value = null
    mcpResult.value = null
    mcpError.value = null
    ragResult.value = null
    ragError.value = null
  },
)

async function handleAiRecommendation() {
  aiLoading.value = true
  aiError.value = null
  aiRecommendation.value = null
  try {
    const response = await generateRecommendation()
    aiRecommendation.value = response.recommendation
  } catch (err) {
    aiError.value = err instanceof Error ? err.message : 'The AI coach is unavailable right now.'
  } finally {
    aiLoading.value = false
  }
}

async function handleMcpWeightings() {
  mcpLoading.value = true
  mcpError.value = null
  mcpResult.value = null
  try {
    const response = await getMcpWeightings(mcpWeight.value / 100, mcpLimit.value)
    mcpResult.value = response
  } catch (err) {
    mcpError.value = err instanceof Error ? err.message : 'MCP integration failed.'
  } finally {
    mcpLoading.value = false
  }
}

async function handleRagAnswer() {
  const question = ragQuestion.value.trim()
  if (question.length < 3 || question.length > 500) {
    ragError.value = 'Question must be between 3 and 500 characters.'
    return
  }
  ragLoading.value = true
  ragError.value = null
  ragResult.value = null
  try {
    const response = await getRagAnswer(question)
    ragResult.value = response
  } catch (err) {
    ragError.value = err instanceof Error ? err.message : 'RAG integration failed.'
  } finally {
    ragLoading.value = false
  }
}

function closeDialog() {
  emit('close')
  activeTab.value = 'ai'
}

const tabs = [
  { id: 'ai', label: 'AI COACH', icon: '✦' },
  { id: 'mcp', label: 'MCP TOOLS', icon: '⚙' },
  { id: 'rag', label: 'RAG QUERY', icon: '📖' },
] as const
</script>

<template>
  <Transition name="integration-modal">
    <div
      v-if="open"
      class="integration-modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="integration-title"
      @click.self="closeDialog"
    >
      <article class="integration-modal__panel nb-panel">
        <header class="integration-modal__header">
          <div>
            <p class="integration-modal__eyebrow nb-mono">AI INTEGRATIONS</p>
            <h2 id="integration-title">AI TOOLS</h2>
          </div>
          <button
            type="button"
            class="nb-btn nb-btn--outline integration-modal__close"
            aria-label="Close AI integrations"
            @click="closeDialog"
          >
            ✕
          </button>
        </header>

        <nav class="integration-modal__tabs nb-mono" role="tablist" aria-label="AI integration tabs">
          <button
            v-for="tab in tabs"
            :key="tab.id"
            :class="[
              'integration-modal__tab',
              activeTab === tab.id ? 'integration-modal__tab--active' : '',
            ]"
            :aria-selected="activeTab === tab.id"
            :aria-controls="`panel-${tab.id}`"
            :id="`tab-${tab.id}`"
            role="tab"
            @click="activeTab = tab.id"
          >
            <span class="integration-modal__tab-icon" aria-hidden="true">{{ tab.icon }}</span>
            {{ tab.label }}
          </button>
        </nav>

        <div class="integration-modal__body">
          <div
            v-if="activeTab === 'ai'"
            id="panel-ai"
            role="tabpanel"
            aria-labelledby="tab-ai"
            class="integration-modal__panel-content"
          >
            <div class="integration-section">
              <p v-if="aiLoading" class="integration-modal__placeholder nb-mono">
                ASKING THE AI COACH FOR GUIDANCE...
              </p>
              <p v-else-if="aiError" class="integration-modal__error nb-mono" role="alert">
                {{ aiError }}
              </p>
              <p v-else-if="aiRecommendation" class="integration-modal__text">
                {{ aiRecommendation }}
              </p>
              <p v-else class="integration-modal__placeholder nb-mono">
                CLICK THE BUTTON BELOW TO GET A FOCUS RECOMMENDATION BASED ON YOUR INCOMPLETE ASSIGNMENTS.
              </p>

              <button
                type="button"
                class="nb-btn nb-btn--accent integration-modal__action"
                :disabled="aiLoading"
                @click="handleAiRecommendation"
              >
                {{ aiLoading ? 'ASKING...' : 'GET AI RECOMMENDATION' }}
              </button>
            </div>
          </div>

          <div
            v-if="activeTab === 'mcp'"
            id="panel-mcp"
            role="tabpanel"
            aria-labelledby="tab-mcp"
            class="integration-modal__panel-content"
          >
            <div class="integration-section">
              <p class="integration-modal__description nb-mono">
                RETRIEVE HIGH-WEIGHT ASSIGNMENTS VIA MCP TOOL.
              </p>

              <div class="integration-form">
                <div class="form-group">
                  <label for="mcp-weight" class="nb-mono">MINIMUM WEIGHT %</label>
                  <input
                    id="mcp-weight"
                    type="number"
                    v-model.number="mcpWeight"
                    class="nb-input"
                    min="1"
                    max="100"
                    step="1"
                  />
                </div>
                <div class="form-group">
                  <label for="mcp-limit" class="nb-mono">MAX RESULTS</label>
                  <input
                    id="mcp-limit"
                    type="number"
                    v-model.number="mcpLimit"
                    class="nb-input"
                    min="1"
                    max="10"
                    step="1"
                  />
                </div>
              </div>

              <button
                type="button"
                class="nb-btn nb-btn--accent integration-modal__action"
                :disabled="mcpLoading"
                @click="handleMcpWeightings"
              >
                {{ mcpLoading ? 'QUERYING MCP...' : 'RUN MCP TOOL' }}
              </button>

              <div v-if="mcpError" class="integration-modal__error nb-mono" role="alert">
                {{ mcpError }}
              </div>

              <div v-if="mcpResult" class="mcp-results">
                <div class="mcp-results__header nb-mono">
                  <span>TOOL: {{ mcpResult.tool }}</span>
                  <span>STATUS: {{ mcpResult.status.toUpperCase() }}</span>
                </div>
                <div v-if="mcpResult.data" class="mcp-results__data">
                  <p class="nb-mono">WEIGHT THRESHOLD: {{ (mcpResult.data.weight * 100).toFixed(0) }}%</p>
                  <p class="nb-mono">ITEMS FOUND: {{ mcpResult.data.count }}</p>
                  <table class="mcp-results__table">
                    <thead>
                      <tr>
                        <th class="nb-mono">ASSIGNMENT</th>
                        <th class="nb-mono">WEIGHT %</th>
                        <th class="nb-mono">MAX MARK</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="item in mcpResult.data.items" :key="item.assignmentId">
                        <td>{{ item.name }}</td>
                        <td class="nb-mono">{{ item.weight != null ? (item.weight * 100).toFixed(0) : '—' }}</td>
                        <td class="nb-mono">{{ item.maxMark ?? '—' }}</td>
                      </tr>
                      <tr v-if="mcpResult.data.items.length === 0">
                        <td colspan="3" class="nb-mono">NO ASSIGNMENTS MATCH CRITERIA</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                <div v-else-if="mcpResult.error" class="mcp-results__error nb-mono">
                  ERROR: {{ mcpResult.error.message }} ({{ mcpResult.error.code }})
                </div>
              </div>
            </div>
          </div>

          <div
            v-if="activeTab === 'rag'"
            id="panel-rag"
            role="tabpanel"
            aria-labelledby="tab-rag"
            class="integration-modal__panel-content"
          >
            <div class="integration-section">
              <p class="integration-modal__description nb-mono">
                QUERY PROJECT DOCUMENTATION USING RAG SERVICE.
              </p>

              <div class="integration-form">
                <div class="form-group">
                  <label for="rag-question" class="nb-mono">YOUR QUESTION</label>
                  <textarea
                    id="rag-question"
                    v-model="ragQuestion"
                    class="nb-input"
                    rows="4"
                    placeholder="Ask about grading policies, course requirements, academic procedures..."
                    :disabled="ragLoading"
                    @keydown.enter.exact="handleRagAnswer"
                  ></textarea>
                </div>
              </div>

              <button
                type="button"
                class="nb-btn nb-btn--accent integration-modal__action"
                :disabled="ragLoading || !ragQuestion.trim()"
                @click="handleRagAnswer"
              >
                {{ ragLoading ? 'QUERYING RAG...' : 'ASK RAG SERVICE' }}
              </button>

              <div v-if="ragError" class="integration-modal__error nb-mono" role="alert">
                {{ ragError }}
              </div>

              <div v-if="ragResult" class="rag-results">
                <div class="rag-results__header nb-mono">
                  <span>STATUS: {{ ragResult.status.toUpperCase() }}</span>
                  <span>CONFIDENCE: {{ ragResult.confidence }}</span>
                </div>
                <p class="rag-results__answer">{{ ragResult.answer }}</p>

                <div v-if="ragResult.citations.length" class="rag-results__citations">
                  <h4 class="nb-mono">SOURCES</h4>
                  <ul>
                    <li v-for="citation in ragResult.citations" :key="citation.sourceId">
                      <strong>{{ citation.title }}</strong> — {{ citation.heading }}
                      <span class="nb-mono">(score: {{ citation.score.toFixed(2) }})</span>
                    </li>
                  </ul>
                </div>

                <div class="rag-results__retrieval nb-mono">
                  MATCHED CHUNKS: {{ ragResult.retrieval.matchedChunks }} / {{ ragResult.retrieval.consideredChunks }} CONSIDERED
                </div>
              </div>
            </div>
          </div>
        </div>

        <footer class="integration-modal__footer nb-mono">
          AI-GENERATED CONTENT • VERIFY BEFORE MAKING ACADEMIC DECISIONS
        </footer>
      </article>
    </div>
  </Transition>
</template>

<style scoped>
.integration-modal {
  position: fixed;
  inset: 0;
  z-index: 50;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--nb-space-5);
  background: rgba(0, 0, 0, 0.55);
}

.integration-modal__panel {
  width: min(720px, 100%);
  max-height: min(85vh, 800px);
  display: flex;
  flex-direction: column;
  background: var(--nb-color-white);
}

.integration-modal__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--nb-space-4);
  padding: var(--nb-space-5) var(--nb-space-5) var(--nb-space-3);
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.integration-modal__eyebrow {
  margin: 0 0 var(--nb-space-1);
  color: var(--nb-color-muted);
  font-size: 11px;
  font-weight: var(--nb-font-weight-bold);
}

.integration-modal__header h2 {
  margin: 0;
  font-size: 26px;
  letter-spacing: -1px;
  text-transform: uppercase;
}

.integration-modal__close {
  background: var(--nb-color-accent-orange);
}

.integration-modal__tabs {
  display: flex;
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
}

.integration-modal__tab {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--nb-space-2);
  padding: var(--nb-space-3) var(--nb-space-4);
  border: none;
  background: transparent;
  color: var(--nb-color-muted);
  font-size: 11px;
  font-weight: var(--nb-font-weight-bold);
  letter-spacing: 0.5px;
  cursor: pointer;
  transition: background 120ms ease, color 120ms ease;
  border-bottom: var(--nb-border-width-md) solid transparent;
  margin-bottom: calc(var(--nb-border-width-sm) * -1);
}

.integration-modal__tab:hover:not(.integration-modal__tab--active) {
  color: var(--nb-color-ink);
  background: var(--nb-color-white);
}

.integration-modal__tab--active {
  color: var(--nb-color-ink);
  background: var(--nb-color-white);
  border-bottom-color: var(--nb-color-accent-orange);
}

.integration-modal__tab-icon {
  font-size: 13px;
}

.integration-modal__body {
  padding: var(--nb-space-5);
  overflow-y: auto;
  flex: 1;
}

.integration-section {
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-4);
}

.integration-modal__description {
  margin: 0;
  font-size: 11px;
  color: var(--nb-color-muted);
}

.integration-form {
  display: flex;
  gap: var(--nb-space-4);
  flex-wrap: wrap;
}

.form-group {
  flex: 1;
  min-width: 150px;
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-1);
}

.form-group label {
  font-size: 10px;
  color: var(--nb-color-muted);
}

.integration-modal__action {
  width: 100%;
  margin-top: var(--nb-space-2);
}

.integration-modal__placeholder {
  margin: 0;
  font-size: 13px;
  color: var(--nb-color-muted);
}

.integration-modal__error {
  margin: 0;
  font-size: 12px;
  color: var(--nb-color-ink);
  background: var(--nb-color-accent-yellow);
  padding: var(--nb-space-3);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.integration-modal__text {
  margin: 0;
  font-size: 15px;
  line-height: 1.5;
}

.mcp-results {
  margin-top: var(--nb-space-4);
  padding: var(--nb-space-4);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
}

.mcp-results__header {
  display: flex;
  justify-content: space-between;
  margin-bottom: var(--nb-space-3);
  font-size: 11px;
  color: var(--nb-color-muted);
}

.mcp-results__data p {
  margin: var(--nb-space-1) 0;
  font-size: 12px;
}

.mcp-results__table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
}

.mcp-results__table th,
.mcp-results__table td {
  padding: var(--nb-space-2);
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
  text-align: left;
}

.mcp-results__table th {
  color: var(--nb-color-muted);
  font-weight: var(--nb-font-weight-bold);
}

.mcp-results__table tr:last-child td {
  border-bottom: none;
}

.mcp-results__error {
  color: var(--nb-color-ink);
  background: var(--nb-color-accent-yellow);
  padding: var(--nb-space-3);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.rag-results {
  margin-top: var(--nb-space-4);
  padding: var(--nb-space-4);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
}

.rag-results__header {
  display: flex;
  justify-content: space-between;
  margin-bottom: var(--nb-space-3);
  font-size: 11px;
  color: var(--nb-color-muted);
}

.rag-results__answer {
  margin: 0 0 var(--nb-space-4);
  font-size: 14px;
  line-height: 1.6;
}

.rag-results__citations {
  margin-top: var(--nb-space-4);
  padding-top: var(--nb-space-3);
  border-top: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.rag-results__citations h4 {
  margin: 0 0 var(--nb-space-2);
  font-size: 11px;
  color: var(--nb-color-muted);
}

.rag-results__citations ul {
  margin: 0;
  padding-left: var(--nb-space-4);
  font-size: 12px;
}

.rag-results__citations li {
  margin-bottom: var(--nb-space-2);
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-1);
}

.rag-results__citations span {
  font-size: 10px;
  color: var(--nb-color-muted);
}

.rag-results__retrieval {
  margin-top: var(--nb-space-3);
  padding-top: var(--nb-space-3);
  border-top: var(--nb-border-width-sm) solid var(--nb-color-ink);
  font-size: 10px;
  color: var(--nb-color-muted);
}

.integration-modal__footer {
  padding: var(--nb-space-3) var(--nb-space-5);
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
  font-size: 10px;
  font-weight: var(--nb-font-weight-bold);
  letter-spacing: 0.5px;
  text-align: center;
}

.integration-enter-active,
.integration-leave-active {
  transition: opacity 160ms ease;
}

.integration-enter-active .integration-modal__panel,
.integration-leave-active .integration-modal__panel {
  transition: transform 200ms ease, opacity 200ms ease;
}

.integration-enter-from,
.integration-leave-to {
  opacity: 0;
}

.integration-enter-from .integration-modal__panel,
.integration-leave-to .integration-modal__panel {
  transform: translateY(20px);
  opacity: 0;
}

@media (prefers-reduced-motion: reduce) {
  .integration-enter-active,
  .integration-leave-active,
  .integration-enter-active .integration-modal__panel,
  .integration-leave-active .integration-modal__panel {
    transition: none;
  }
}
</style>
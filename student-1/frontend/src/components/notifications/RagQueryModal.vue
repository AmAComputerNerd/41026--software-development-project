<script setup lang="ts">
import { ref } from 'vue'
import { queryRagKnowledge, type RagQueryResult } from '@/api/integration'

const props = defineProps<{
  open: boolean
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const queryText = ref('')
const loading = ref(false)
const error = ref<string | null>(null)
const result = ref<RagQueryResult | null>(null)

const sampleQueries = [
  'What is the late submission penalty?',
  'What are the notification SLAs for deadlines and grades?',
  'How to make a chocolate cake?', // test insufficient context
]

async function executeQuery(text?: string) {
  const query = (text || queryText.value).trim()
  if (!query || loading.value) return

  queryText.value = query
  loading.value = true
  error.value = null
  result.value = null

  try {
    result.value = await queryRagKnowledge(query, 'student-1')
  } catch (err: any) {
    error.value = err?.message || 'Failed to query RAG knowledge server.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div v-if="open" class="nb-modal-backdrop" @click="emit('close')">
    <div class="nb-modal" @click.stop>
      <div class="nb-modal__header">
        <div class="nb-modal__title-group">
          <span class="nb-modal__badge">RAG</span>
          <h2 class="nb-modal__title">COURSE KNOWLEDGE RETRIEVAL</h2>
        </div>
        <button class="nb-modal__close-btn" aria-label="Close modal" @click="emit('close')">✕</button>
      </div>

      <div class="nb-modal__body">
        <p class="nb-modal__desc">
          Queries the shared non-containerised local RAG server for grounded course policies, assessment guidelines, and notification SLAs.
        </p>

        <div class="nb-modal__chips">
          <span class="nb-modal__chips-label">Try sample query:</span>
          <button
            v-for="sq in sampleQueries"
            :key="sq"
            class="nb-chip"
            :disabled="loading"
            @click="executeQuery(sq)"
          >
            {{ sq }}
          </button>
        </div>

        <form class="nb-modal__form" @submit.prevent="executeQuery()">
          <div class="nb-modal__input-row">
            <input
              v-model="queryText"
              type="text"
              class="nb-input"
              placeholder="Ask about syllabus, late penalties, or notification rules..."
              :disabled="loading"
            />
            <button
              type="submit"
              class="nb-btn nb-btn--primary"
              :disabled="loading || !queryText.trim()"
            >
              <span v-if="loading" class="nb-spinner" />
              <span v-else>SEARCH RAG</span>
            </button>
          </div>
        </form>

        <div v-if="error" class="nb-alert nb-alert--error">
          <strong>Error:</strong> {{ error }}
        </div>

        <div v-if="result" class="nb-result-card">
          <div class="nb-result-card__meta">
            <span
              class="nb-confidence-badge"
              :class="{
                'nb-confidence-badge--high': result.confidence === 'HIGH',
                'nb-confidence-badge--medium': result.confidence === 'MEDIUM',
                'nb-confidence-badge--low': result.confidence === 'LOW',
                'nb-confidence-badge--insufficient': !result.hasSufficientContext,
              }"
            >
              CONFIDENCE: {{ result.hasSufficientContext ? result.confidence : 'INSUFFICIENT CONTEXT' }}
            </span>

            <span class="nb-status-pill">
              {{ result.hasSufficientContext ? 'GROUNDED CONTEXT' : 'UNSUPPORTED QUERY' }}
            </span>
          </div>

          <div v-if="!result.hasSufficientContext" class="nb-insufficient-box">
            <span class="nb-insufficient-icon">⚠️</span>
            <div>
              <strong>Insufficient Relevant Context:</strong>
              <p>The query is outside the indexed course documentation or policies. No unsupported hallucination was generated.</p>
            </div>
          </div>

          <div v-else class="nb-answer-content">
            <p class="nb-answer-text">{{ result.answer }}</p>
          </div>

          <div v-if="result.citations && result.citations.length > 0" class="nb-citations">
            <h4 class="nb-citations__title">SOURCE CITATIONS ({{ result.citations.length }})</h4>
            <ul class="nb-citations__list">
              <li v-for="citation in result.citations" :key="citation" class="nb-citation-item">
                📄 {{ citation }}
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.nb-modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  z-index: 1050;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
  backdrop-filter: blur(2px);
}

.nb-modal {
  width: 680px;
  max-width: 100%;
  max-height: 90vh;
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  border: var(--nb-border-width-md, 3px) solid var(--nb-color-ink);
  box-shadow: 8px 8px 0 var(--nb-color-shadow);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.nb-modal__header {
  padding: 16px 20px;
  background: var(--nb-color-accent-purple, #b388ff);
  color: var(--nb-color-ink);
  border-bottom: var(--nb-border-width-md, 3px) solid var(--nb-color-ink);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

:root[data-theme='dark'] .nb-modal__header {
  background: #5B21B6;
  color: var(--nb-color-ink);
}

.nb-modal__title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.nb-modal__badge {
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
  font-weight: 800;
  font-size: 11px;
  padding: 2px 8px;
  letter-spacing: 1px;
}

.nb-modal__title {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  color: var(--nb-color-ink);
}

.nb-modal__close-btn {
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink);
  width: 32px;
  height: 32px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 2px 2px 0 var(--nb-color-shadow);
}

.nb-modal__close-btn:hover {
  background: #ff5252;
  color: #fff;
}

.nb-modal__body {
  padding: 20px;
  overflow-y: auto;
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
}

.nb-modal__desc {
  font-size: 13px;
  margin-top: 0;
  margin-bottom: 14px;
  color: var(--nb-color-muted, #444);
}

.nb-modal__chips {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-bottom: 16px;
}

.nb-modal__chips-label {
  font-size: 12px;
  font-weight: 700;
  color: var(--nb-color-ink);
}

.nb-chip {
  background: var(--nb-color-white);
  color: var(--nb-color-ink);
  border: 1px solid var(--nb-color-ink);
  font-size: 11px;
  font-weight: 600;
  padding: 4px 8px;
  cursor: pointer;
}

.nb-chip:hover:not(:disabled) {
  background: var(--nb-color-accent-yellow);
  color: var(--nb-color-ink);
}

.nb-modal__form {
  margin-bottom: 16px;
}

.nb-modal__input-row {
  display: flex;
  gap: 8px;
}

.nb-input {
  flex: 1;
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink);
  padding: 10px 14px;
  font-size: 14px;
  outline: none;
  background: var(--nb-color-white);
  color: var(--nb-color-ink);
}

.nb-input::placeholder {
  color: var(--nb-color-muted);
  opacity: 0.8;
}

.nb-input:focus {
  border-color: var(--nb-color-ink);
  box-shadow: 2px 2px 0 var(--nb-color-shadow);
}

.nb-btn {
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink);
  padding: 10px 16px;
  font-weight: 800;
  font-size: 13px;
  cursor: pointer;
  box-shadow: 3px 3px 0 var(--nb-color-shadow);
}

.nb-btn--primary {
  background: var(--nb-color-accent-yellow);
  color: var(--nb-color-ink);
}

.nb-btn:hover:not(:disabled) {
  transform: translate(-1px, -1px);
  box-shadow: 4px 4px 0 var(--nb-color-shadow);
}

.nb-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.nb-alert {
  padding: 12px;
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink);
  margin-bottom: 14px;
}

.nb-alert--error {
  background: #ffebee;
  color: #c62828;
}

:root[data-theme='dark'] .nb-alert--error {
  background: #450a0a;
  color: #fca5a5;
}

.nb-result-card {
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink);
  padding: 16px;
  background: var(--nb-color-white);
  color: var(--nb-color-ink);
  box-shadow: 4px 4px 0 var(--nb-color-shadow);
}

.nb-result-card__meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.nb-confidence-badge {
  font-size: 11px;
  font-weight: 800;
  padding: 4px 8px;
  border: 1px solid var(--nb-color-ink);
}

.nb-confidence-badge--high {
  background: var(--nb-color-accent-green, #00e676);
  color: #000;
}

.nb-confidence-badge--medium {
  background: var(--nb-color-accent-yellow, #ffe600);
  color: #000;
}

.nb-confidence-badge--low,
.nb-confidence-badge--insufficient {
  background: #ff5252;
  color: #fff;
}

:root[data-theme='dark'] .nb-confidence-badge--low,
:root[data-theme='dark'] .nb-confidence-badge--insufficient {
  background: #b91c1c;
  color: #fff;
}

.nb-status-pill {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  color: var(--nb-color-muted, #555);
}

.nb-insufficient-box {
  display: flex;
  gap: 12px;
  align-items: flex-start;
  background: #fff3e0;
  border: 2px dashed #e65100;
  padding: 14px;
  margin-bottom: 12px;
  color: #bf360c;
}

:root[data-theme='dark'] .nb-insufficient-box {
  background: #451a03;
  border-color: #f97316;
  color: #fdba74;
}

.nb-insufficient-box p {
  margin: 4px 0 0;
  font-size: 13px;
}

.nb-insufficient-icon {
  font-size: 20px;
}

.nb-answer-content {
  font-size: 14px;
  line-height: 1.6;
  margin-bottom: 16px;
  color: var(--nb-color-ink);
}

.nb-answer-text {
  margin: 0;
  white-space: pre-wrap;
}

.nb-citations {
  border-top: 1px solid var(--nb-color-ink);
  padding-top: 12px;
}

.nb-citations__title {
  margin: 0 0 8px;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.5px;
  color: var(--nb-color-ink);
}

.nb-citations__list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.nb-citation-item {
  font-size: 12px;
  font-family: var(--nb-font-mono, monospace);
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  padding: 4px 8px;
  border-left: 3px solid var(--nb-color-ink);
}
</style>

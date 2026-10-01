<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink } from 'vue-router'
import BaseDialog from '@/components/BaseDialog.vue'
import { getUpcomingDeadlines } from '@/api/mcp'
import { askProjectQuestion } from '@/api/rag'
import type { McpDeadlineResult } from '@/types/mcp'
import type { RagAnswerResult } from '@/types/rag'

type AssistantMode = 'mcp' | 'rag'

const open = ref(false)
const mode = ref<AssistantMode>('mcp')
const closeButton = ref<HTMLButtonElement | null>(null)
const days = ref(7)
const limit = ref(10)
const mcpLoading = ref(false)
const mcpError = ref('')
const mcpResult = ref<McpDeadlineResult | null>(null)
const question = ref('')
const ragLoading = ref(false)
const ragError = ref('')
const ragResult = ref<RagAnswerResult | null>(null)
const sampleQuestions = [
  'How does Canvas assignment sync work?',
  'What happens when I complete a parent task?',
  'How does AI assignment breakdown work?',
]

async function runTool() {
  if (mcpLoading.value) return

  mcpLoading.value = true
  mcpError.value = ''
  mcpResult.value = null

  try {
    mcpResult.value = await getUpcomingDeadlines(days.value, limit.value)
    if (mcpResult.value.status === 'error') {
      mcpError.value = mcpResult.value.error?.message ?? 'The MCP tool returned an error.'
    }
  } catch (reason) {
    mcpError.value = reason instanceof Error ? reason.message : 'Unable to invoke the MCP tool.'
  } finally {
    mcpLoading.value = false
  }
}

async function askQuestion(sampleQuestion?: string) {
  const value = (sampleQuestion ?? question.value).trim()
  if (!value || ragLoading.value) return

  question.value = value
  ragLoading.value = true
  ragError.value = ''
  ragResult.value = null

  try {
    ragResult.value = await askProjectQuestion(value)
  } catch (reason) {
    ragError.value = reason instanceof Error ? reason.message : 'Unable to query project knowledge.'
  } finally {
    ragLoading.value = false
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleString()
}
</script>

<template>
  <div class="nb-assistant-action-bar">
    <button
      class="nb-btn nb-btn--accent nb-assistant-launcher nb-mono"
      type="button"
      aria-haspopup="dialog"
      :aria-expanded="open"
      @click="open = true"
    >
      <span class="nb-assistant-launcher__icon" aria-hidden="true">+</span>
      AI ASSIST
    </button>
  </div>

  <BaseDialog
    v-model="open"
    labelled-by="assistant-title"
    width="640px"
    @opened="closeButton?.focus()"
  >
    <article class="nb-assistant">
      <header class="nb-assistant__header">
        <div>
          <p class="nb-eyebrow">DEADLINE TRACKER</p>
          <h2 id="assistant-title">AI ASSISTANT</h2>
        </div>
        <button
          ref="closeButton"
          class="nb-icon-btn"
          type="button"
          aria-label="Close AI Assistant"
          @click="open = false"
        >
          &times;
        </button>
      </header>

      <div class="nb-assistant__modes" role="tablist" aria-label="Assistant mode">
        <button
          id="assistant-mcp-tab"
          class="nb-assistant__mode nb-mono"
          :class="{ 'nb-assistant__mode--active': mode === 'mcp' }"
          type="button"
          role="tab"
          :aria-selected="mode === 'mcp'"
          aria-controls="assistant-mcp-panel"
          @click="mode = 'mcp'"
        >
          MCP
        </button>
        <button
          id="assistant-rag-tab"
          class="nb-assistant__mode nb-mono"
          :class="{ 'nb-assistant__mode--active': mode === 'rag' }"
          type="button"
          role="tab"
          :aria-selected="mode === 'rag'"
          aria-controls="assistant-rag-panel"
          @click="mode = 'rag'"
        >
          RAG
        </button>
      </div>

      <section
        v-if="mode === 'mcp'"
        id="assistant-mcp-panel"
        class="nb-assistant__body"
        role="tabpanel"
        aria-labelledby="assistant-mcp-tab"
      >
        <div class="nb-assistant-message nb-assistant-message--system">
          <span class="nb-tag nb-tag--medium">MCP TOOL</span>
          <p>Find incomplete tasks due within a selected period.</p>
        </div>

        <form class="nb-assistant-form" @submit.prevent="runTool">
          <div class="nb-assistant-form__fields">
            <label class="nb-field">
              <span>Days ahead</span>
              <input v-model.number="days" type="number" min="1" max="90" required />
            </label>
            <label class="nb-field">
              <span>Maximum tasks</span>
              <input v-model.number="limit" type="number" min="1" max="50" required />
            </label>
          </div>
          <button class="nb-btn nb-btn--accent" type="submit" :disabled="mcpLoading">
            {{ mcpLoading ? 'Checking live data...' : 'Show upcoming deadlines' }}
          </button>
        </form>

        <div v-if="mcpError" class="nb-alert nb-alert--error" role="alert">
          {{ mcpError }}
        </div>

        <section
          v-if="mcpResult"
          class="nb-assistant-message nb-assistant-message--result"
          aria-live="polite"
        >
          <div class="nb-assistant-message__heading">
            <strong>MCP RESPONSE</strong>
            <span class="nb-tag">{{ mcpResult.status }}</span>
          </div>
          <p class="nb-assistant-tool nb-mono">
            TOOL: <code>{{ mcpResult.tool }}</code>
          </p>
          <template v-if="mcpResult.data">
            <h3>{{ mcpResult.data.count }} DEADLINES FOUND</h3>
            <p v-if="!mcpResult.data.items.length">
              No incomplete tasks are due in the next {{ mcpResult.data.days }} days.
            </p>
            <ul v-else class="nb-assistant-deadlines">
              <li v-for="item in mcpResult.data.items" :key="item.id">
                <div>
                  <strong>{{ item.title }}</strong>
                  <span>{{ item.courseName || 'No course' }}</span>
                </div>
                <div class="nb-assistant-deadlines__meta">
                  <span class="nb-tag" :class="`nb-tag--${item.priority.toLowerCase()}`">
                    {{ item.priority }}
                  </span>
                  <time :datetime="item.dueDate">{{ formatDate(item.dueDate) }}</time>
                </div>
                <RouterLink
                  class="nb-text-btn"
                  :to="{ name: 'tasks', query: { edit: item.id } }"
                  :aria-label="`Open task: ${item.title}`"
                  @click="open = false"
                >
                  OPEN TASK
                </RouterLink>
              </li>
            </ul>
          </template>
          <details class="nb-assistant-response" open>
            <summary class="nb-mono">STRUCTURED RESPONSE / JSON</summary>
            <pre class="nb-assistant-json">{{ JSON.stringify(mcpResult, null, 2) }}</pre>
          </details>
        </section>
      </section>

      <section
        v-else
        id="assistant-rag-panel"
        class="nb-assistant__body"
        role="tabpanel"
        aria-labelledby="assistant-rag-tab"
      >
        <div class="nb-assistant-message nb-assistant-message--system">
          <span class="nb-tag nb-tag--medium">GROUNDED RAG</span>
          <p>Ask how the Deadline Tracker works. Answers use indexed project documentation.</p>
        </div>

        <form class="nb-assistant-form" @submit.prevent="askQuestion()">
          <div class="nb-rag-samples">
            <span class="nb-eyebrow">TRY A SAMPLE QUESTION</span>
            <button
              v-for="sample in sampleQuestions"
              :key="sample"
              class="nb-btn nb-btn--outline"
              type="button"
              :disabled="ragLoading"
              @click="askQuestion(sample)"
            >
              {{ sample }}
            </button>
          </div>
          <label class="nb-field">
            <span>Project question</span>
            <textarea
              v-model="question"
              rows="4"
              minlength="3"
              maxlength="500"
              placeholder="How does Canvas assignment sync work?"
              required
              :disabled="ragLoading"
            />
          </label>
          <button class="nb-btn nb-btn--accent" type="submit" :disabled="ragLoading">
            {{ ragLoading ? 'Searching project knowledge...' : 'Ask project help' }}
          </button>
        </form>

        <div v-if="ragError" class="nb-alert nb-alert--error" role="alert">
          {{ ragError }}
        </div>

        <section
          v-if="ragResult"
          class="nb-assistant-message nb-assistant-message--result"
          :class="{ 'nb-assistant-message--insufficient': ragResult.status === 'insufficient_context' }"
          aria-live="polite"
        >
          <div class="nb-assistant-message__heading">
            <strong>
              {{ ragResult.status === 'success' ? 'GROUNDED ANSWER' : 'INSUFFICIENT CONTEXT' }}
            </strong>
            <span class="nb-tag" :class="`nb-confidence--${ragResult.confidence}`">
              {{ ragResult.confidence }} confidence
            </span>
          </div>
          <p class="nb-rag-answer">{{ ragResult.answer }}</p>

          <div v-if="ragResult.citations.length" class="nb-rag-citations">
            <p class="nb-eyebrow">SOURCES</p>
            <ol>
              <li v-for="citation in ragResult.citations" :key="`${citation.sourceId}:${citation.heading}`">
                <strong>{{ citation.title }}</strong>
                <span>{{ citation.heading }}</span>
                <code>{{ citation.sourceId }}</code>
              </li>
            </ol>
          </div>

          <p class="nb-rag-retrieval nb-mono">
            {{ ragResult.retrieval.matchedChunks }} of
            {{ ragResult.retrieval.consideredChunks }} chunks matched
          </p>
        </section>
      </section>
    </article>
  </BaseDialog>
</template>

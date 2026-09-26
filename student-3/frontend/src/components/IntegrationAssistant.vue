<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
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

watch(open, async (isOpen) => {
  if (isOpen) {
    await nextTick()
    closeButton.value?.focus()
  }
})

onMounted(() => document.addEventListener('keydown', handleKeydown))
onBeforeUnmount(() => document.removeEventListener('keydown', handleKeydown))

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape' && open.value) {
    open.value = false
  }
}

async function runTool() {
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

async function askQuestion() {
  const value = question.value.trim()
  if (!value) return

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
  <button
    class="nb-assistant-launcher nb-mono"
    type="button"
    aria-haspopup="dialog"
    :aria-expanded="open"
    @click="open = true"
  >
    <span aria-hidden="true">+</span>
    04 AI ASSIST
  </button>

  <Transition name="assistant">
    <div v-if="open" class="nb-assistant-layer">
      <button
        class="nb-assistant-backdrop"
        type="button"
        aria-label="Close Canvas Assistant"
        @click="open = false"
      />

      <aside
        class="nb-assistant"
        role="dialog"
        aria-modal="true"
        aria-labelledby="assistant-title"
      >
        <header class="nb-assistant__header">
          <div>
            <p class="nb-eyebrow">DEADLINE TRACKER</p>
            <h2 id="assistant-title">CANVAS ASSISTANT</h2>
          </div>
          <button
            ref="closeButton"
            class="nb-icon-btn"
            type="button"
            aria-label="Close Canvas Assistant"
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
            LIVE DATA / MCP
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
            PROJECT HELP / RAG
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
            v-if="mcpResult?.data"
            class="nb-assistant-message nb-assistant-message--result"
            aria-live="polite"
          >
            <div class="nb-assistant-message__heading">
              <strong>{{ mcpResult.data.count }} DEADLINES FOUND</strong>
              <span class="nb-tag">LIVE</span>
            </div>
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
              </li>
            </ul>
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

          <form class="nb-assistant-form" @submit.prevent="askQuestion">
            <label class="nb-field">
              <span>Project question</span>
              <textarea
                v-model="question"
                rows="4"
                minlength="3"
                maxlength="500"
                placeholder="How does Canvas assignment sync work?"
                required
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
      </aside>
    </div>
  </Transition>
</template>

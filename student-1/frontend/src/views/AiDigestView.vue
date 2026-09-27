<script setup lang="ts">
import { nextTick, onMounted, ref, watch } from 'vue'
import ChatSessionsDrawer from '@/components/notifications/ChatSessionsDrawer.vue'
import MarkdownContent from '@/components/notifications/MarkdownContent.vue'
import { useChatAssistant } from '@/composables/useChatAssistant'

const {
  sessions,
  activeSessionId,
  activeSession,
  messages,
  loadingSessions,
  loadingMessages,
  sendingMessage,
  generatingOpeningDigest,
  drawerOpen,
  error,
  fetchSessions,
  selectSession,
  createNewSession,
  removeSession,
  sendMessage,
  toggleDrawer,
} = useChatAssistant()

const inputPrompt = ref('')
const includeRag = ref(true)
const chatScrollContainer = ref<HTMLElement | null>(null)

const quickChips = [
  'What should I prioritize next?',
  'What is the late penalty for assignments?',
  'Summarize all upcoming deadlines by urgency',
  'Draft an extension request email',
]

async function scrollToBottom() {
  await nextTick()
  if (chatScrollContainer.value) {
    chatScrollContainer.value.scrollTop = chatScrollContainer.value.scrollHeight
  }
}

watch(messages, () => {
  scrollToBottom()
}, { deep: true })

async function handleSend(text?: string) {
  const content = (text || inputPrompt.value).trim()
  if (!content || sendingMessage.value || generatingOpeningDigest.value) return

  inputPrompt.value = ''
  await sendMessage(content, includeRag.value)
  await scrollToBottom()
}

function parseCitations(citationsJson?: string | null): string[] {
  if (!citationsJson) return []
  try {
    return JSON.parse(citationsJson)
  } catch {
    return []
  }
}

onMounted(async () => {
  await fetchSessions()
  await scrollToBottom()
})
</script>

<template>
  <div class="nb-chat-page">
    <div class="nb-digest-header-banner">
      <h1 class="nb-digest__title">AI DIGEST</h1>
      <span class="nb-mono nb-digest-subtitle">INTERACTIVE CHAT ASSISTANT</span>
    </div>

    <!-- Collapsible Past Chats Drawer -->
    <ChatSessionsDrawer
      :open="drawerOpen"
      :sessions="sessions"
      :active-session-id="activeSessionId"
      :loading="loadingSessions"
      @close="drawerOpen = false"
      @select="selectSession"
      @new-chat="createNewSession()"
      @delete="removeSession"
    />

    <!-- Main Chat Container -->
    <div class="nb-chat-shell">
      <!-- Chat Toolbar -->
      <div class="nb-chat-header">
        <div class="nb-chat-header__left">
          <button
            type="button"
            class="nb-btn nb-btn--drawer-toggle"
            aria-label="PAST CHATS"
            @click="toggleDrawer"
          >
            📋 PAST CHATS ({{ sessions.length }})
          </button>

          <span v-if="activeSession" class="nb-session-pill">
            {{ activeSession.title }}
          </span>
        </div>

        <div class="nb-chat-header__right">
          <button
            type="button"
            class="nb-btn nb-btn--new-chat"
            @click="createNewSession()"
          >
            + NEW CHAT
          </button>
        </div>
      </div>

      <div v-if="error" class="nb-chat-error">
        {{ error }}
      </div>

      <!-- Messages Stream -->
      <div ref="chatScrollContainer" class="nb-chat-messages">
        <!-- Animated Neobrutalist Opening Digest Loading Banner -->
        <div v-if="generatingOpeningDigest" class="nb-brutalist-loader">
          <div class="nb-brutalist-loader__stripes" />
          <div class="nb-brutalist-loader__text">
            ⚡ <strong>GENERATING UNREAD NOTIFICATION DIGEST...</strong>
          </div>
          <div class="nb-brutalist-loader__cubes">
            <span class="nb-cube" />
            <span class="nb-cube" />
            <span class="nb-cube" />
          </div>
        </div>

        <div v-if="loadingMessages && messages.length === 0" class="nb-loading-notice">
          Loading conversation history...
        </div>

        <!-- Message Bubbles -->
        <div
          v-for="msg in messages"
          :key="msg.id"
          class="nb-message-row"
          :class="`nb-message-row--${msg.role}`"
        >
          <div class="nb-message-bubble" :class="`nb-message-bubble--${msg.role}`">
            <div class="nb-message-header">
              <span class="nb-message-author">
                {{ msg.role === 'assistant' ? '🤖 ACADEMIC AI ASSISTANT' : 'STUDENT' }}
              </span>
              <span class="nb-message-time">
                {{ new Date(msg.createdAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) }}
              </span>
            </div>

            <!-- Content -->
            <div v-if="msg.role === 'assistant'" class="nb-message-content">
              <MarkdownContent :source="msg.content" />

              <!-- RAG Source Citations & Confidence Badge -->
              <div v-if="msg.confidence || parseCitations(msg.citationsJson).length > 0" class="nb-rag-footer">
                <div class="nb-rag-footer__badge-row">
                  <span
                    v-if="msg.confidence"
                    class="nb-confidence-badge"
                    :class="{
                      'nb-confidence-badge--high': msg.confidence === 'HIGH',
                      'nb-confidence-badge--medium': msg.confidence === 'MEDIUM',
                      'nb-confidence-badge--low': msg.confidence === 'LOW',
                    }"
                  >
                    RAG CONFIDENCE: {{ msg.confidence }}
                  </span>
                </div>

                <div v-if="parseCitations(msg.citationsJson).length > 0" class="nb-citations-block">
                  <span class="nb-citations-label">CITATIONS:</span>
                  <ul class="nb-citations-tags">
                    <li v-for="citation in parseCitations(msg.citationsJson)" :key="citation" class="nb-citation-tag">
                      📄 {{ citation }}
                    </li>
                  </ul>
                </div>
              </div>
            </div>

            <div v-else class="nb-message-content nb-message-content--user">
              {{ msg.content }}
            </div>
          </div>
        </div>

        <!-- Awaiting Assistant Completion State -->
        <div v-if="sendingMessage" class="nb-message-row nb-message-row--assistant">
          <div class="nb-message-bubble nb-message-bubble--assistant nb-message-bubble--thinking">
            <div class="nb-thinking-indicator">
              <span class="nb-thinking-dot" />
              <span class="nb-thinking-dot" />
              <span class="nb-thinking-dot" />
              <span class="nb-thinking-text">Thinking with course & notification context...</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Quick Chips & Input Composer -->
      <div class="nb-chat-composer">
        <div class="nb-chips-scroll">
          <button
            v-for="chip in quickChips"
            :key="chip"
            type="button"
            class="nb-quick-chip"
            :disabled="sendingMessage || generatingOpeningDigest"
            @click="handleSend(chip)"
          >
            {{ chip }}
          </button>
        </div>

        <form class="nb-composer-form" @submit.prevent="handleSend()">
          <div class="nb-composer-options">
            <label class="nb-toggle-label">
              <input v-model="includeRag" type="checkbox" class="nb-checkbox" />
              <span>Enable Course RAG Context</span>
            </label>
          </div>

          <div class="nb-composer-row">
            <input
              v-model="inputPrompt"
              type="text"
              class="nb-composer-input"
              placeholder="Ask how to prioritize tasks, draft emails, or search syllabus policies..."
              :disabled="sendingMessage || generatingOpeningDigest"
            />
            <button
              type="submit"
              class="nb-btn nb-btn--send"
              :disabled="sendingMessage || generatingOpeningDigest || !inputPrompt.trim()"
            >
              SEND ➔
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.nb-chat-page {
  position: relative;
  height: calc(100vh - 120px);
  min-height: 580px;
  display: flex;
  flex-direction: column;
}

.nb-digest-header-banner {
  margin-bottom: 12px;
  display: flex;
  align-items: baseline;
  gap: 12px;
}

.nb-digest__title {
  font-size: 28px;
  font-weight: 900;
  letter-spacing: -0.5px;
  text-transform: uppercase;
  margin: 0;
}

.nb-digest-subtitle {
  font-size: 13px;
  font-weight: 700;
  color: var(--nb-color-muted, #555);
}

.nb-chat-shell {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: var(--nb-color-bg);
  border: var(--nb-border-width-md, 3px) solid var(--nb-color-ink);
  box-shadow: 6px 6px 0 var(--nb-color-shadow);
  overflow: hidden;
}

.nb-chat-header {
  padding: 12px 16px;
  background: var(--nb-color-accent-yellow);
  color: var(--nb-color-ink);
  border-bottom: var(--nb-border-width-md, 3px) solid var(--nb-color-ink);
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
}

.nb-chat-header__left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.nb-btn--drawer-toggle {
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink);
  font-weight: 800;
  font-size: 13px;
  padding: 6px 12px;
  cursor: pointer;
  box-shadow: 2px 2px 0 var(--nb-color-shadow);
}

.nb-session-pill {
  font-weight: 800;
  font-size: 14px;
  background: var(--nb-color-white);
  color: var(--nb-color-ink);
  border: 1px solid var(--nb-color-ink);
  padding: 4px 10px;
  letter-spacing: 0.3px;
}

.nb-btn--new-chat {
  background: var(--nb-color-accent-green, #00e676);
  color: #000;
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink);
  font-weight: 800;
  font-size: 13px;
  padding: 6px 14px;
  cursor: pointer;
  box-shadow: 2px 2px 0 var(--nb-color-shadow);
}

.nb-btn--new-chat:hover,
.nb-btn--drawer-toggle:hover {
  transform: translate(-1px, -1px);
  box-shadow: 3px 3px 0 var(--nb-color-shadow);
}

.nb-chat-error {
  padding: 8px 16px;
  background: #ffebee;
  color: #c62828;
  font-size: 13px;
  font-weight: 700;
  border-bottom: 2px solid var(--nb-color-ink);
}

:root[data-theme='dark'] .nb-chat-error {
  background: #450a0a;
  color: #fca5a5;
}

.nb-chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
}

.nb-loading-notice {
  text-align: center;
  font-size: 14px;
  color: var(--nb-color-muted, #555);
  margin-top: 40px;
}

/* Fast Neobrutalist Opening Digest Loading Banner */
.nb-brutalist-loader {
  border: var(--nb-border-width-md, 3px) solid var(--nb-color-ink);
  background: var(--nb-color-accent-orange);
  color: var(--nb-color-ink);
  box-shadow: 4px 4px 0 var(--nb-color-shadow);
  padding: 14px 18px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  animation: nb-rise-in 200ms ease-out both;
}

:root[data-theme='dark'] .nb-brutalist-loader {
  color: #ffffff;
}

.nb-brutalist-loader__text {
  font-size: 13px;
  letter-spacing: 0.5px;
}

.nb-brutalist-loader__cubes {
  display: flex;
  gap: 6px;
}

.nb-cube {
  width: 14px;
  height: 14px;
  background: var(--nb-color-ink);
  display: inline-block;
  animation: nb-cube-pulse 600ms infinite alternate ease-in-out;
}

.nb-cube:nth-child(2) {
  animation-delay: 200ms;
}

.nb-cube:nth-child(3) {
  animation-delay: 400ms;
}

@keyframes nb-cube-pulse {
  0% { transform: scale(1); background: var(--nb-color-ink); }
  100% { transform: scale(1.3); background: var(--nb-color-white); }
}

.nb-message-row {
  display: flex;
  width: 100%;
}

.nb-message-row--user {
  justify-content: flex-end;
}

.nb-message-row--assistant {
  justify-content: flex-start;
}

.nb-message-bubble {
  max-width: 80%;
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink);
  box-shadow: 4px 4px 0 var(--nb-color-shadow);
  padding: 14px 18px;
}

.nb-message-bubble--assistant {
  background: var(--nb-color-white);
  color: var(--nb-color-ink);
}

.nb-message-bubble--user {
  background: var(--nb-color-accent-blue, #80d8ff);
  color: #000;
}

:root[data-theme='dark'] .nb-message-bubble--user {
  background: #1E3A8A;
  color: var(--nb-color-ink);
}

.nb-message-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
  border-bottom: 1px dashed var(--nb-color-muted);
  padding-bottom: 4px;
}

.nb-message-author {
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.5px;
  color: var(--nb-color-ink);
}

.nb-message-time {
  font-size: 11px;
  color: var(--nb-color-muted, #555);
}

.nb-message-content {
  font-size: 14px;
  line-height: 1.55;
  color: var(--nb-color-ink);
}

.nb-message-content--user {
  white-space: pre-wrap;
  font-weight: 600;
}

.nb-rag-footer {
  margin-top: 12px;
  padding-top: 10px;
  border-top: 2px solid var(--nb-color-ink);
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.nb-confidence-badge {
  font-size: 11px;
  font-weight: 800;
  padding: 2px 6px;
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

.nb-confidence-badge--low {
  background: #ff5252;
  color: #fff;
}

:root[data-theme='dark'] .nb-confidence-badge--low {
  background: #b91c1c;
  color: #fff;
}

.nb-citations-block {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.nb-citations-label {
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.5px;
  color: var(--nb-color-ink);
}

.nb-citations-tags {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.nb-citation-tag {
  font-size: 11px;
  font-family: var(--nb-font-mono, monospace);
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  border: 1px solid var(--nb-color-ink);
  padding: 2px 6px;
}

.nb-thinking-indicator {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 700;
  color: var(--nb-color-ink);
}

.nb-thinking-dot {
  width: 8px;
  height: 8px;
  background: var(--nb-color-ink);
  border-radius: 0;
  display: inline-block;
  animation: nb-dot-jump 1s infinite alternate;
}

.nb-thinking-dot:nth-child(2) { animation-delay: 200ms; }
.nb-thinking-dot:nth-child(3) { animation-delay: 400ms; }

@keyframes nb-dot-jump {
  0% { transform: translateY(0); }
  100% { transform: translateY(-6px); }
}

/* Composer */
.nb-chat-composer {
  padding: 12px 16px;
  border-top: var(--nb-border-width-md, 3px) solid var(--nb-color-ink);
  background: var(--nb-color-white);
  color: var(--nb-color-ink);
}

.nb-chips-scroll {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  padding-bottom: 8px;
  margin-bottom: 8px;
}

.nb-quick-chip {
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  border: 1px solid var(--nb-color-ink);
  font-size: 12px;
  font-weight: 600;
  padding: 4px 10px;
  white-space: nowrap;
  cursor: pointer;
  box-shadow: 1px 1px 0 var(--nb-color-shadow);
}

.nb-quick-chip:hover:not(:disabled) {
  background: var(--nb-color-accent-yellow);
  color: var(--nb-color-ink);
}

.nb-composer-options {
  margin-bottom: 6px;
}

.nb-toggle-label {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  font-weight: 700;
  color: var(--nb-color-ink);
  cursor: pointer;
}

.nb-checkbox {
  width: 14px;
  height: 14px;
  accent-color: var(--nb-color-ink);
}

.nb-composer-row {
  display: flex;
  gap: 8px;
}

.nb-composer-input {
  flex: 1;
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink);
  padding: 10px 14px;
  font-size: 14px;
  font-family: inherit;
  outline: none;
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
}

.nb-composer-input::placeholder {
  color: var(--nb-color-muted);
  opacity: 0.8;
}

.nb-composer-input:focus {
  box-shadow: 2px 2px 0 var(--nb-color-shadow);
}

.nb-btn--send {
  background: var(--nb-color-accent-green, #00e676);
  color: #000;
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink);
  font-weight: 800;
  font-size: 14px;
  padding: 10px 20px;
  cursor: pointer;
  box-shadow: 3px 3px 0 var(--nb-color-shadow);
}

.nb-btn--send:hover:not(:disabled) {
  transform: translate(-1px, -1px);
  box-shadow: 4px 4px 0 var(--nb-color-shadow);
}

.nb-btn--send:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>

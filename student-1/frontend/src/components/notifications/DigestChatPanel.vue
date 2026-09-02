<script setup lang="ts">
import { nextTick, ref } from 'vue'
import { CURRENT_STUDENT_ID } from '@/config'
import { chatWithAssistant } from '@/api/digest'

interface Message {
  role: 'user' | 'assistant'
  content: string
  time: string
}

const prompt = ref('')
const loading = ref(false)
const error = ref<string | null>(null)
const messages = ref<Message[]>([
  {
    role: 'assistant',
    content:
      'Hello! I am your AI academic assistant. I have reviewed your active notifications and deadlines. Ask me anything—how to prioritize your study tasks, draft emails to professors, or plan your schedule!',
    time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
  },
])

const quickChips = [
  'What should I prioritize in the next 2 hours?',
  'Draft an extension request email for my upcoming deadline',
  'Summarize all upcoming deadlines by urgency',
  'Which assignment has the highest grade weight?',
]

const chatContainer = ref<HTMLElement | null>(null)

async function scrollToBottom() {
  await nextTick()
  if (chatContainer.value) {
    chatContainer.value.scrollTop = chatContainer.value.scrollHeight
  }
}

async function sendPrompt(textToSend?: string) {
  const text = (textToSend || prompt.value).trim()
  if (!text || loading.value) return

  prompt.value = ''
  error.value = null

  const userMsg: Message = {
    role: 'user',
    content: text,
    time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
  }
  messages.value.push(userMsg)
  await scrollToBottom()

  loading.value = true
  try {
    const historyPayload = messages.value
      .filter((m) => m !== userMsg)
      .map((m) => ({ role: m.role, content: m.content }))

    const res = await chatWithAssistant(CURRENT_STUDENT_ID, text, historyPayload)

    messages.value.push({
      role: 'assistant',
      content: res.reply,
      time: new Date(res.repliedAtUtc || Date.now()).toLocaleTimeString([], {
        hour: '2-digit',
        minute: '2-digit',
      }),
    })
    await scrollToBottom()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Assistant request failed.'
  } finally {
    loading.value = false
    await scrollToBottom()
  }
}
</script>

<template>
  <div class="nb-panel nb-chat-panel">
    <div class="nb-chat-panel__header">
      <div class="nb-chat-panel__title-wrap">
        <span class="nb-mono nb-chat-panel__title">INTERACTIVE DIGEST ASSISTANT</span>
        <span class="nb-tag nb-tag--ai">AI-MODE</span>
      </div>
      <span class="nb-mono nb-chat-panel__hint">GROUNDED IN YOUR NOTIFICATIONS & DEADLINES</span>
    </div>

    <div ref="chatContainer" class="nb-chat-panel__history">
      <div
        v-for="(msg, idx) in messages"
        :key="idx"
        class="nb-chat-msg"
        :class="`nb-chat-msg--${msg.role}`"
      >
        <div class="nb-chat-msg__bubble">
          <div class="nb-chat-msg__role nb-mono">
            {{ msg.role === 'user' ? 'YOU' : 'AI ASSISTANT' }}
          </div>
          <p class="nb-chat-msg__text">{{ msg.content }}</p>
          <span class="nb-mono nb-chat-msg__time">{{ msg.time }}</span>
        </div>
      </div>

      <div v-if="loading" class="nb-chat-msg nb-chat-msg--assistant">
        <div class="nb-chat-msg__bubble nb-chat-msg__bubble--loading">
          <span class="nb-mono">Thinking & analysing notifications...</span>
        </div>
      </div>
    </div>

    <div class="nb-chat-panel__chips">
      <button
        v-for="chip in quickChips"
        :key="chip"
        type="button"
        class="nb-chip nb-chat-panel__chip"
        :disabled="loading"
        @click="sendPrompt(chip)"
      >
        {{ chip }}
      </button>
    </div>

    <div v-if="error" class="nb-chat-panel__alert nb-mono">
      {{ error }}
    </div>

    <form class="nb-chat-panel__form" @submit.prevent="sendPrompt()">
      <input
        v-model="prompt"
        type="text"
        class="nb-chat-panel__input"
        placeholder="Ask a question about your notifications, deadlines, or study schedule..."
        :disabled="loading"
      />
      <button
        type="submit"
        class="nb-btn nb-btn--accent nb-chat-panel__submit"
        :disabled="loading || !prompt.trim()"
      >
        SEND
      </button>
    </form>
  </div>
</template>

<style scoped>
.nb-chat-panel {
  padding: 0;
  display: flex;
  flex-direction: column;
  margin-top: var(--nb-space-6);
  border-radius: 0;
}

.nb-chat-panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: var(--nb-space-2);
  border-bottom: var(--nb-border-width-lg) solid var(--nb-color-ink);
  background: var(--nb-color-accent-yellow);
  color: #111111;
  padding: var(--nb-space-3) var(--nb-space-4);
}

.nb-chat-panel__title-wrap {
  display: flex;
  align-items: center;
  gap: var(--nb-space-2);
}

.nb-chat-panel__title {
  font-size: 13px;
  font-weight: var(--nb-font-weight-bold);
  letter-spacing: 0.5px;
  color: #111111;
}

.nb-chat-panel__hint {
  font-size: 11px;
  font-weight: var(--nb-font-weight-bold);
  color: var(--nb-color-muted);
}

.nb-chat-panel__history {
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-3);
  max-height: 380px;
  min-height: 200px;
  overflow-y: auto;
  padding: var(--nb-space-5);
  background: var(--nb-color-bg);
}

.nb-chat-msg {
  display: flex;
  width: 100%;
}

.nb-chat-msg--user {
  justify-content: flex-end;
}

.nb-chat-msg--assistant {
  justify-content: flex-start;
}

.nb-chat-msg__bubble {
  max-width: 82%;
  padding: var(--nb-space-3) var(--nb-space-4);
  font-size: 14px;
  line-height: 1.5;
  white-space: pre-wrap;
  border-radius: 0;
}

.nb-chat-msg--user .nb-chat-msg__bubble {
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  box-shadow: 3px 3px 0 var(--nb-color-shadow);
}

.nb-chat-msg--assistant .nb-chat-msg__bubble {
  background: var(--nb-color-white);
  color: var(--nb-color-ink);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  box-shadow: 3px 3px 0 var(--nb-color-shadow);
}

.nb-chat-msg__role {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.5px;
  margin-bottom: 4px;
  opacity: 0.7;
}

.nb-chat-msg__text {
  margin: 0;
}

.nb-chat-msg__bubble--loading {
  color: var(--nb-color-muted);
}

.nb-chat-msg__time {
  display: block;
  font-size: 10px;
  margin-top: 6px;
  opacity: 0.6;
  text-align: right;
}

.nb-chat-panel__chips {
  display: flex;
  flex-wrap: wrap;
  gap: var(--nb-space-2);
  padding: 0 var(--nb-space-5) var(--nb-space-4);
}

.nb-chat-panel__chip {
  cursor: pointer;
  border-radius: 0;
}

.nb-chat-panel__chip:hover {
  background: var(--nb-color-accent-orange);
  color: var(--nb-color-ink);
}

.nb-chat-panel__alert {
  padding: var(--nb-space-2) var(--nb-space-4);
  margin: 0 var(--nb-space-5) var(--nb-space-3);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: #ff5252;
  color: #ffffff;
  font-size: 12px;
}

.nb-chat-panel__form {
  display: flex;
  gap: var(--nb-space-2);
  padding: var(--nb-space-4) var(--nb-space-5);
  border-top: var(--nb-border-width-md) solid var(--nb-color-ink);
  background: var(--nb-color-white);
}

.nb-chat-panel__input {
  flex: 1;
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  font: 400 14px var(--nb-font-display);
  padding: var(--nb-space-3);
  border-radius: 0;
  outline: none;
}

.nb-chat-panel__input:focus-visible {
  outline: var(--nb-border-width-md) solid var(--nb-color-accent-orange);
  outline-offset: 2px;
}

.nb-chat-panel__submit {
  flex-shrink: 0;
  border-radius: 0;
}
</style>

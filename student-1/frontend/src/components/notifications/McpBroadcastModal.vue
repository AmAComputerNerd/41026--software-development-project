<script setup lang="ts">
import { ref } from 'vue'
import { broadcastMcpAlert, type McpBroadcastResult } from '@/api/integration'
import { CURRENT_STUDENT_ID } from '@/config'

const props = defineProps<{
  open: boolean
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const title = ref('System Maintenance Alert')
const message = ref('Scheduled server maintenance will occur tonight at 23:00 UTC.')
const urgency = ref<'Low' | 'Medium' | 'High' | 'Critical'>('High')
const loading = ref(false)
const error = ref<string | null>(null)
const result = ref<McpBroadcastResult | null>(null)

async function executeBroadcast() {
  if (!title.value.trim() || !message.value.trim() || loading.value) return

  loading.value = true
  error.value = null
  result.value = null

  try {
    result.value = await broadcastMcpAlert(
      CURRENT_STUDENT_ID,
      title.value.trim(),
      message.value.trim(),
      urgency.value,
    )
  } catch (err: any) {
    error.value = err?.message || 'Failed to execute MCP broadcast tool.'
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
          <span class="nb-modal__badge">MCP</span>
          <h2 class="nb-modal__title">MODEL CONTEXT PROTOCOL TOOL</h2>
        </div>
        <button class="nb-modal__close-btn" aria-label="Close modal" @click="emit('close')">✕</button>
      </div>

      <div class="nb-modal__body">
        <p class="nb-modal__desc">
          Executes the <code>notifications_broadcast_alert</code> tool on the shared non-containerised local MCP server. Enforces tool validation boundaries and publishes real-time alerts to the SSE stream.
        </p>

        <form class="nb-form" @submit.prevent="executeBroadcast">
          <div class="nb-field">
            <label class="nb-label">ALERT TITLE (3-100 characters):</label>
            <input
              v-model="title"
              type="text"
              class="nb-input"
              required
              minlength="3"
              maxlength="100"
              placeholder="e.g. Sprint 2 Deadline Extension"
              :disabled="loading"
            />
          </div>

          <div class="nb-field-row">
            <div class="nb-field">
              <label class="nb-label">URGENCY BOUNDARY:</label>
              <select v-model="urgency" class="nb-select" :disabled="loading">
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
                <option value="Critical">Critical</option>
              </select>
            </div>

            <div class="nb-field">
              <label class="nb-label">TARGET STUDENT:</label>
              <input type="text" class="nb-input" :value="CURRENT_STUDENT_ID" disabled />
            </div>
          </div>

          <div class="nb-field">
            <label class="nb-label">MESSAGE CONTENT (5-500 characters):</label>
            <textarea
              v-model="message"
              class="nb-textarea"
              rows="3"
              required
              minlength="5"
              maxlength="500"
              placeholder="Detailed announcement or notification..."
              :disabled="loading"
            />
          </div>

          <button
            type="submit"
            class="nb-btn nb-btn--primary nb-submit-btn"
            :disabled="loading || !title.trim() || !message.trim()"
          >
            <span v-if="loading">DISPATCHING TOOL...</span>
            <span v-else>⚡ EXECUTE MCP BROADCAST TOOL</span>
          </button>
        </form>

        <div v-if="error" class="nb-alert nb-alert--error">
          <strong>Error:</strong> {{ error }}
        </div>

        <div v-if="result" class="nb-result-box">
          <div class="nb-result-header">
            <span class="nb-result-status" :class="{ 'nb-result-status--ok': result.success }">
              STATUS: {{ result.status.toUpperCase() }}
            </span>
            <span class="nb-result-tool">TOOL: {{ result.tool }}</span>
          </div>

          <p class="nb-result-note">
            ✓ Alert dispatched via MCP server. Persistent record created in PostgreSQL and published to SSE toast stream!
          </p>

          <pre class="nb-json-viewer">{{ JSON.stringify(result.data, null, 2) }}</pre>
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
  background: var(--nb-color-bg, #fff);
  border: var(--nb-border-width-md, 3px) solid var(--nb-color-ink, #000);
  box-shadow: 8px 8px 0 var(--nb-color-ink, #000);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.nb-modal__header {
  padding: 16px 20px;
  background: var(--nb-color-accent-orange, #ff9100);
  border-bottom: var(--nb-border-width-md, 3px) solid var(--nb-color-ink, #000);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.nb-modal__title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.nb-modal__badge {
  background: var(--nb-color-ink, #000);
  color: #fff;
  font-weight: 800;
  font-size: 11px;
  padding: 2px 8px;
  letter-spacing: 1px;
}

.nb-modal__title {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
}

.nb-modal__close-btn {
  background: #fff;
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink, #000);
  width: 32px;
  height: 32px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 2px 2px 0 var(--nb-color-ink, #000);
}

.nb-modal__close-btn:hover {
  background: #ff5252;
  color: #fff;
}

.nb-modal__body {
  padding: 20px;
  overflow-y: auto;
}

.nb-modal__desc {
  font-size: 13px;
  margin-top: 0;
  margin-bottom: 16px;
  color: var(--nb-color-muted, #444);
}

.nb-form {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 16px;
}

.nb-field {
  display: flex;
  flex-direction: column;
  gap: 4px;
  flex: 1;
}

.nb-field-row {
  display: flex;
  gap: 12px;
}

.nb-label {
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.5px;
}

.nb-input,
.nb-select,
.nb-textarea {
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink, #000);
  padding: 8px 12px;
  font-size: 13px;
  font-family: inherit;
  outline: none;
  background: #fff;
}

.nb-input:focus,
.nb-select:focus,
.nb-textarea:focus {
  box-shadow: 2px 2px 0 var(--nb-color-ink, #000);
}

.nb-textarea {
  resize: vertical;
}

.nb-submit-btn {
  background: var(--nb-color-accent-orange, #ff9100);
  color: var(--nb-color-ink, #000);
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink, #000);
  padding: 12px;
  font-weight: 800;
  font-size: 14px;
  cursor: pointer;
  box-shadow: 4px 4px 0 var(--nb-color-ink, #000);
}

.nb-submit-btn:hover:not(:disabled) {
  transform: translate(-1px, -1px);
  box-shadow: 5px 5px 0 var(--nb-color-ink, #000);
}

.nb-submit-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.nb-alert {
  padding: 12px;
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink, #000);
  margin-bottom: 14px;
}

.nb-alert--error {
  background: #ffebee;
  color: #c62828;
}

.nb-result-box {
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink, #000);
  background: #f5f5f5;
  padding: 14px;
  box-shadow: 4px 4px 0 var(--nb-color-ink, #000);
}

.nb-result-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.nb-result-status {
  font-size: 12px;
  font-weight: 800;
  padding: 2px 8px;
  border: 1px solid var(--nb-color-ink, #000);
  background: #ffcdd2;
}

.nb-result-status--ok {
  background: var(--nb-color-accent-green, #00e676);
}

.nb-result-tool {
  font-family: var(--nb-font-mono, monospace);
  font-size: 12px;
}

.nb-result-note {
  font-size: 12px;
  color: #1b5e20;
  margin: 4px 0 8px;
  font-weight: 700;
}

.nb-json-viewer {
  background: #111;
  color: #00e676;
  font-family: var(--nb-font-mono, monospace);
  font-size: 11px;
  padding: 10px;
  border: 1px solid var(--nb-color-ink, #000);
  overflow-x: auto;
  max-height: 200px;
  margin: 0;
}
</style>

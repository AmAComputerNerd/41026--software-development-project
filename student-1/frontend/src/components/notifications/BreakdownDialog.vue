<script setup lang="ts">
import { ref } from 'vue'
import { breakdownTask } from '@/api/tasks'

interface SubtaskResult {
  id: string
  title: string
  description?: string
  priority: string
  status: string
}

const props = defineProps<{
  taskId: string | null
  taskTitle?: string
}>()

const emit = defineEmits<{
  close: []
  completed: [subtasks: SubtaskResult[]]
}>()

const prompt = ref('Break down this assignment into manageable study and implementation subtasks')
const priority = ref<'Low' | 'Medium' | 'High'>('Medium')
const loading = ref(false)
const error = ref<string | null>(null)
const generatedSubtasks = ref<SubtaskResult[] | null>(null)

async function handleSubmit() {
  if (!props.taskId) return
  loading.value = true
  error.value = null
  try {
    const results = await breakdownTask(props.taskId, prompt.value, priority.value)
    generatedSubtasks.value = Array.isArray(results) ? results : []
    emit('completed', generatedSubtasks.value)
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'AI Breakdown failed.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div v-if="taskId" class="nb-dialog-overlay" @click.self="emit('close')">
    <div class="nb-dialog" role="dialog" aria-modal="true">
      <div class="nb-dialog__header">
        <span class="nb-mono nb-dialog__header-title">AI TASK BREAKDOWN</span>
        <button type="button" class="nb-dialog__close" aria-label="Close" @click="emit('close')">
          &times;
        </button>
      </div>

      <div v-if="!generatedSubtasks" class="nb-dialog__body">
        <h2 class="nb-dialog__heading">Break down with AI</h2>
        <p class="nb-dialog__desc">
          Generate structured subtasks for this deadline using the student-3 AI planning engine.
        </p>

        <div v-if="error" class="nb-dialog__alert nb-mono" role="alert">
          {{ error }}
        </div>

        <div class="nb-form-grid">
          <label class="nb-field">
            <span>Subtask Priority</span>
            <select v-model="priority">
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </label>
        </div>

        <label class="nb-field">
          <span>Prompt Instructions</span>
          <textarea
            v-model="prompt"
            rows="4"
            maxlength="2000"
            placeholder="e.g. Break down into research, drafting, coding, testing, and final review..."
          />
          <small class="nb-field__counter nb-mono">{{ prompt.length }}/2000</small>
        </label>

        <p class="nb-helper nb-mono">
          THE DEADLINE CONTEXT IS EXTRACTED FROM THE TRACKER. REVIEW CREATED TASKS AFTER GENERATION.
        </p>
      </div>

      <div v-if="!generatedSubtasks" class="nb-dialog__actions">
        <button
          type="button"
          class="nb-btn nb-btn--outline"
          :disabled="loading"
          @click="emit('close')"
        >
          CANCEL
        </button>
        <button
          type="button"
          class="nb-btn nb-btn--accent"
          :disabled="loading || !prompt.trim()"
          @click="handleSubmit"
        >
          {{ loading ? 'AI IS CREATING SUBTASKS...' : 'GENERATE SUBTASKS' }}
        </button>
      </div>

      <!-- Success View with Generated Subtasks -->
      <div v-else class="nb-dialog__body">
        <div class="nb-dialog__success-banner nb-mono">
          &check; CREATED {{ generatedSubtasks.length }} SUBTASK(S) IN DEADLINE TRACKER
        </div>

        <ul class="nb-dialog__subtask-list">
          <li v-for="st in generatedSubtasks" :key="st.id" class="nb-dialog__subtask-item">
            <div class="nb-dialog__subtask-head">
              <strong>{{ st.title }}</strong>
              <span class="nb-mono nb-dialog__subtask-pri">{{ st.priority }}</span>
            </div>
            <p v-if="st.description" class="nb-dialog__subtask-desc">{{ st.description }}</p>
          </li>
        </ul>
      </div>

      <div v-if="generatedSubtasks" class="nb-dialog__actions">
        <a class="nb-btn nb-btn--accent" :href="`/deadlines/?taskId=${taskId}`">
          VIEW IN DEADLINE TRACKER
        </a>
        <button type="button" class="nb-btn nb-btn--outline" @click="emit('close')">
          DONE
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.nb-dialog-overlay {
  position: fixed;
  inset: 0;
  background: rgb(20 20 20 / 60%);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1100;
  padding: 16px;
  animation: nb-backdrop-in 180ms ease-out both;
}

.nb-dialog {
  width: 650px;
  max-width: 100%;
  max-height: calc(100vh - 32px);
  overflow-y: auto;
  border: var(--nb-border-width-lg) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  box-shadow: var(--nb-shadow);
  border-radius: 0;
  display: flex;
  flex-direction: column;
  animation: nb-dialog-in 240ms cubic-bezier(0.2, 0.9, 0.25, 1.15) both;
}

.nb-dialog__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: var(--nb-border-width-lg) solid var(--nb-color-ink);
  background: var(--nb-color-accent-yellow);
  color: #111111;
  padding: var(--nb-space-3) var(--nb-space-4);
}

.nb-dialog__header-title {
  font-size: 12px;
  font-weight: var(--nb-font-weight-bold);
  letter-spacing: 0.5px;
  text-transform: uppercase;
  color: #111111;
}

.nb-dialog__close {
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  font-size: 18px;
  line-height: 1;
  width: 28px;
  height: 28px;
  cursor: pointer;
  border-radius: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
}

.nb-dialog__close:hover {
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
}

.nb-dialog__body {
  padding: var(--nb-space-6);
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-4);
}

.nb-dialog__heading {
  margin: 0;
  font-size: 20px;
  font-weight: var(--nb-font-weight-bold);
}

.nb-dialog__desc {
  margin: 0;
  color: var(--nb-color-muted);
  font-size: 14px;
  line-height: 1.4;
}

.nb-dialog__alert {
  padding: var(--nb-space-3);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: #ff5252;
  color: #ffffff;
  font-size: 12px;
  animation: nb-alert-in 260ms ease-out both;
}


.nb-form-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--nb-space-4);
}

.nb-field {
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-1);
  color: var(--nb-color-ink);
  font-family: var(--nb-font-mono);
  font-size: 11px;
  font-weight: var(--nb-font-weight-bold);
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

.nb-field select,
.nb-field textarea {
  width: 100%;
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  border-radius: 0;
  background: var(--nb-color-white);
  color: var(--nb-color-ink);
  font: 400 14px/1.4 var(--nb-font-display);
  padding: var(--nb-space-3);
  outline: none;
  box-sizing: border-box;
}

.nb-field select {
  min-height: 44px;
}

.nb-field textarea {
  resize: vertical;
}

.nb-field select:focus-visible,
.nb-field textarea:focus-visible {
  outline: var(--nb-border-width-md) solid var(--nb-color-accent-orange);
  outline-offset: 2px;
}

.nb-field__counter {
  font-size: 11px;
  color: var(--nb-color-muted);
  align-self: flex-end;
  margin-top: 2px;
}

.nb-helper {
  font-size: 11px;
  color: var(--nb-color-muted);
  line-height: 1.4;
  margin: 0;
}

.nb-dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--nb-space-3);
  border-top: var(--nb-border-width-md) solid var(--nb-color-ink);
  padding: var(--nb-space-4) var(--nb-space-6);
  background: var(--nb-color-bg);
}

.nb-dialog__success-banner {
  padding: var(--nb-space-3);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: #2ecc71;
  color: #111111;
  font-weight: var(--nb-font-weight-bold);
  font-size: 12px;
}

.nb-dialog__subtask-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-3);
  max-height: 280px;
  overflow-y: auto;
}

.nb-dialog__subtask-item {
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  border-left: 6px solid var(--nb-color-accent-orange);
  background: var(--nb-color-white);
  padding: var(--nb-space-3);
  border-radius: 0;
  animation: nb-rise-in 240ms ease-out both;
  transition: transform 140ms ease;

  &:nth-child(2n) {
    animation-delay: 35ms;
  }

  &:nth-child(3n) {
    animation-delay: 70ms;
  }

  &:hover {
    transform: translateX(3px);
  }
}


.nb-dialog__subtask-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
}

.nb-dialog__subtask-pri {
  font-size: 10px;
  padding: 2px 6px;
  border: 1px solid var(--nb-color-ink);
  background: var(--nb-color-accent-yellow);
  color: #111111;
  font-weight: 700;
}

.nb-dialog__subtask-desc {
  margin: var(--nb-space-1) 0 0;
  font-size: 13px;
  color: var(--nb-color-muted);
  line-height: 1.4;
}
</style>

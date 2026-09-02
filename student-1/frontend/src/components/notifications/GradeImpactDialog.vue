<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { CURRENT_STUDENT_ID } from '@/config'
import { getStudentMarks, updateTemporaryMark, type StudentMarkDto } from '@/api/grades'

const props = defineProps<{
  assignmentId?: string | null
  message?: string
}>()

const emit = defineEmits<{
  close: []
}>()

const loading = ref(true)
const saving = ref(false)
const error = ref<string | null>(null)
const marks = ref<StudentMarkDto[]>([])
const simValue = ref<number>(85)
const simSaved = ref(false)

onMounted(async () => {
  loading.value = true
  error.value = null
  try {
    const data = await getStudentMarks(CURRENT_STUDENT_ID)
    marks.value = data
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to connect to Grades service (student-5).'
  } finally {
    loading.value = false
  }
})

async function handleSimulate(assignment: StudentMarkDto) {
  saving.value = true
  simSaved.value = false
  error.value = null
  try {
    await updateTemporaryMark(CURRENT_STUDENT_ID, assignment.assignmentId, simValue.value)
    assignment.temporaryMark = simValue.value
    simSaved.value = true
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to update temporary mark.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="nb-dialog-overlay" @click.self="emit('close')">
    <div class="nb-dialog" role="dialog" aria-modal="true">
      <div class="nb-dialog__header">
        <span class="nb-mono nb-dialog__header-title">GRADE IMPACT SIMULATOR</span>
        <button type="button" class="nb-dialog__close" aria-label="Close" @click="emit('close')">
          &times;
        </button>
      </div>

      <div class="nb-dialog__body">
        <h2 class="nb-dialog__heading">What-If Score Simulator</h2>
        <p class="nb-dialog__desc">
          Connected to the Grades & Progress service (student-5). Inspect current marks and test "what-if" scores against your target grade.
        </p>

        <p v-if="loading" class="nb-mono">LOADING GRADES DATA...</p>
        <div v-else-if="error" class="nb-dialog__alert nb-mono" role="alert">
          {{ error }}
        </div>

        <div v-else class="nb-grade-list">
          <div v-for="m in marks" :key="m.assignmentId" class="nb-grade-item">
            <div class="nb-grade-item__info">
              <strong>{{ m.assignmentName || 'Assignment ' + m.assignmentId.slice(0, 8) }}</strong>
              <span class="nb-mono nb-grade-item__meta">
                Actual Mark: {{ m.mark !== null && m.mark !== undefined ? m.mark + '%' : 'Pending' }}
                &bull;
                Weight: {{ m.weight ?? 20 }}%
              </span>
            </div>

            <div class="nb-grade-item__sim">
              <label class="nb-field nb-grade-item__field">
                <span>Simulate Mark (%):</span>
                <input
                  v-model.number="simValue"
                  type="number"
                  min="0"
                  max="100"
                  class="nb-grade-item__input"
                />
              </label>
              <button
                type="button"
                class="nb-btn nb-btn--accent"
                :disabled="saving"
                @click="handleSimulate(m)"
              >
                {{ saving ? 'UPDATING...' : 'APPLY TO WHAT-IF' }}
              </button>
            </div>
          </div>

          <div v-if="simSaved" class="nb-dialog__success-banner nb-mono">
            &check; SIMULATED MARK SAVED TO YOUR PROGRESS PROFILE!
          </div>
        </div>
      </div>

      <div class="nb-dialog__actions">
        <a class="nb-btn nb-btn--accent" href="/grades/">
          OPEN FULL GRADES DASHBOARD
        </a>
        <button type="button" class="nb-btn nb-btn--outline" @click="emit('close')">
          CLOSE
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
}

.nb-dialog {
  width: 620px;
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
}

.nb-grade-list {
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-3);
  max-height: 280px;
  overflow-y: auto;
}

.nb-grade-item {
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  border-left: 6px solid var(--nb-color-accent-yellow);
  background: var(--nb-color-white);
  padding: var(--nb-space-3);
  border-radius: 0;
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-2);
}

.nb-grade-item__info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.nb-grade-item__meta {
  font-size: 11px;
  color: var(--nb-color-muted);
}

.nb-grade-item__sim {
  display: flex;
  align-items: flex-end;
  gap: var(--nb-space-3);
  flex-wrap: wrap;
}

.nb-grade-item__field {
  margin: 0;
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

.nb-grade-item__input {
  width: 90px;
  min-height: 38px;
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  border-radius: 0;
  background: var(--nb-color-white);
  color: var(--nb-color-ink);
  font: 700 14px var(--nb-font-mono);
  padding: 4px 8px;
  box-sizing: border-box;
}

.nb-grade-item__input:focus-visible {
  outline: var(--nb-border-width-md) solid var(--nb-color-accent-orange);
  outline-offset: 2px;
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
</style>

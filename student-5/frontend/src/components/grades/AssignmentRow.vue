<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { Assignment, StudentAssignment } from '@/api/grades'

const props = defineProps<{
  assignment: Assignment
  mark?: StudentAssignment
  saveTemporaryMark: (assignmentId: string, mark: number) => Promise<void>
  removeTemporaryMark: (assignmentId: string) => Promise<void>
}>()

const temporaryMark = ref<number | null>(props.mark?.tempMark ?? null)
const saving = ref(false)
const message = ref('')

watch(
  () => props.mark?.tempMark,
  (value) => (temporaryMark.value = value ?? null),
)

const finalPercent = computed(() => {
  if (props.mark?.finalMark == null || !props.assignment.maxMark) return null
  return (props.mark.finalMark / props.assignment.maxMark) * 100
})

const temporaryPercent = computed(() => {
  if (temporaryMark.value == null || !props.assignment.maxMark) return null
  return (temporaryMark.value / props.assignment.maxMark) * 100
})

async function save() {
  if (temporaryMark.value == null) return
  saving.value = true
  message.value = ''
  try {
    await props.saveTemporaryMark(props.assignment.assignmentId, temporaryMark.value)
    message.value = 'APPLIED'
  } catch (error) {
    message.value = error instanceof Error ? error.message : 'SAVE FAILED'
  } finally {
    saving.value = false
  }
}

async function remove() {
  saving.value = true
  message.value = ''
  try {
    await props.removeTemporaryMark(props.assignment.assignmentId)
    temporaryMark.value = null
    message.value = 'REMOVED'
  } catch (error) {
    message.value = error instanceof Error ? error.message : 'REMOVE FAILED'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <article class="assignment-row" :class="{ 'assignment-row--graded': finalPercent != null }">
    <div class="assignment-row__identity">
      <span class="assignment-row__status nb-mono">
        {{ finalPercent != null ? 'MARKED' : 'PENDING' }}
      </span>
      <div>
        <h3>{{ assignment.name }}</h3>
        <p class="nb-mono">
          MAX {{ assignment.maxMark ?? '—' }} PTS
        </p>
      </div>
    </div>

    <div class="assignment-row__mark">
      <span class="nb-mono">FINAL MARK</span>
      <strong v-if="mark?.finalMark != null">
        {{ mark.finalMark }}/{{ assignment.maxMark }}
        <small>{{ finalPercent?.toFixed(1) }}%</small>
      </strong>
      <strong v-else>—</strong>
    </div>

    <form class="temporary-mark" @submit.prevent="save">
      <label :for="`temp-${assignment.assignmentId}`" class="nb-mono">TRY A MARK</label>
      <div class="temporary-mark__control">
        <input
          :id="`temp-${assignment.assignmentId}`"
          v-model.number="temporaryMark"
          class="nb-input"
          type="number"
          min="0"
          :max="assignment.maxMark ?? 100"
          placeholder="—"
        />
        <span class="nb-mono">/ {{ assignment.maxMark ?? 100 }}</span>
        <button
          type="submit"
          class="nb-btn nb-btn--outline"
          :disabled="saving || temporaryMark == null || temporaryMark < 0 || temporaryMark > (assignment.maxMark ?? 100)"
        >
          APPLY
        </button>
        <button
          v-if="mark?.tempMark != null"
          type="button"
          class="nb-btn temporary-mark__remove"
          :disabled="saving"
          @click="remove"
        >
          REMOVE
        </button>
      </div>
      <span v-if="temporaryPercent != null || message" class="temporary-mark__result nb-mono">
        {{ temporaryPercent == null ? '' : `${temporaryPercent.toFixed(1)}%` }} {{ message }}
      </span>
    </form>
  </article>
</template>

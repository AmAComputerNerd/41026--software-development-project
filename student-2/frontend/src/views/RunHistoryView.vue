<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getAutomationRuns } from '@/api/automations'
import { currentStudentId } from '@/config'
import type { AutomationRun } from '@/types/automation'

const runs = ref<AutomationRun[]>([])
const loading = ref(true)
const error = ref('')
const resultFilter = ref<'All' | 'SUC' | 'FAI'>('All')

const visibleRuns = computed(() =>
  resultFilter.value === 'All' ? runs.value : runs.value.filter((run) => run.result === resultFilter.value),
)

onMounted(async () => {
  try {
    runs.value = await getAutomationRuns(currentStudentId)
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to load run history.'
  } finally {
    loading.value = false
  }
})

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}
</script>

<template>
  <section class="nb-page">
    <header class="nb-page-header">
      <div>
        <p class="nb-eyebrow nb-mono">READ-ONLY RECORDS</p>
        <h1>AUTOMATION RUNS</h1>
        <p class="nb-page-subtitle">Review the stored results of previous automation runs.</p>
      </div>
    </header>

    <div class="nb-view-controls">
      <div class="nb-chips">
        <button v-for="filter in ['All', 'SUC', 'FAI'] as const" :key="filter" type="button" class="nb-chip" :class="{ 'nb-chip--active': resultFilter === filter }" @click="resultFilter = filter">{{ filter === 'SUC' ? 'SUCCESS' : filter === 'FAI' ? 'FAILED' : 'ALL' }}</button>
      </div>
      <span class="nb-mono nb-count">{{ visibleRuns.length }} RUNS</span>
    </div>

    <div v-if="error" class="nb-alert nb-alert--error" role="alert">{{ error }}</div>
    <div v-if="loading" class="nb-panel nb-empty">Loading run history...</div>
    <div v-else-if="!visibleRuns.length" class="nb-panel nb-empty"><strong>No runs match this view.</strong></div>

    <div v-else class="nb-history-list">
      <article v-for="run in visibleRuns" :key="run.id" class="nb-history-row">
        <span class="nb-result" :class="`nb-result--${run.result.toLowerCase()}`">{{ run.result }}</span>
        <div>
          <strong>{{ run.type === 'ScheduledPost' ? run.subject : `Assignment ${run.assignmentId}` }}</strong>
          <p class="nb-mono nb-detail">{{ run.type === 'ScheduledPost' ? run.recipients?.join(', ') : 'ASSIGNMENT EXTENSION' }}</p>
        </div>
        <time class="nb-mono" :datetime="run.executionTimeStamp">{{ formatDate(run.executionTimeStamp) }}</time>
      </article>
    </div>
  </section>
</template>
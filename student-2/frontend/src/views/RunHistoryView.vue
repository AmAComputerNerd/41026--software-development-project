<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getAutomationRuns } from '@/api/automations'
import { getAutomationDefinition } from '@/automations/registry'
import { currentStudentId } from '@/config'
import type { AutomationRun } from '@/types/automation'

const runs = ref<AutomationRun[]>([])
const loading = ref(true)
const error = ref('')
const resultFilter = ref<'All' | AutomationRun['result']>('All')
const expandedRunIds = ref(new Set<string>())

const visibleRuns = computed(() =>
  resultFilter.value === 'All' ? runs.value : runs.value.filter((run) => run.result === resultFilter.value),
)
const runRows = computed(() => visibleRuns.value.map((run) => {
  const definition = getAutomationDefinition(run.$type)
  return {
    run,
    definition,
    title: definition.runTitle(run),
    detail: definition.runDetail(run),
  }
}))
const resultFilters = [
  { value: 'All', label: 'ALL' },
  { value: 'RUN', label: 'RUNNING' },
  { value: 'SUC', label: 'SUCCESS' },
  { value: 'FAI', label: 'FAILED' },
] as const

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

function toggleDetails(runId: string) {
  const next = new Set(expandedRunIds.value)
  if (next.has(runId)) {
    next.delete(runId)
  } else {
    next.add(runId)
  }
  expandedRunIds.value = next
}

function getResultLabel(result: AutomationRun['result']) {
  if (result === 'RUN') return 'Running'
  return result === 'SUC' ? 'Success' : 'Failed'
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
        <button v-for="filter in resultFilters" :key="filter.value" type="button" class="nb-chip" :class="{ 'nb-chip--active': resultFilter === filter.value }" @click="resultFilter = filter.value">{{ filter.label }}</button>
      </div>
      <span class="nb-mono nb-count">
        {{ visibleRuns.length }} {{ visibleRuns.length === 1 ? 'RUN' : 'RUNS' }}
      </span>
    </div>

    <div v-if="error" class="nb-alert nb-alert--error" role="alert">{{ error }}</div>
    <div v-if="loading" class="nb-panel nb-empty">Loading run history...</div>
    <div v-else-if="!visibleRuns.length" class="nb-panel nb-empty"><strong>No runs match this view.</strong></div>

    <div v-else class="nb-history-list">
      <article v-for="row in runRows" :key="row.run.id" class="nb-history-entry">
        <div class="nb-history-row">
          <span class="nb-result" :class="`nb-result--${row.run.result.toLowerCase()}`">{{ row.run.result }}</span>
          <div>
            <strong>{{ row.title }}</strong>
            <p class="nb-mono nb-detail">{{ row.detail }}</p>
          </div>
          <time class="nb-mono" :datetime="row.run.executionTimeStamp">{{ formatDate(row.run.executionTimeStamp) }}</time>
          <button
            type="button"
            class="nb-btn nb-btn--outline"
            :aria-expanded="expandedRunIds.has(row.run.id)"
            :aria-controls="`run-details-${row.run.id}`"
            @click="toggleDetails(row.run.id)"
          >
            {{ expandedRunIds.has(row.run.id) ? 'Hide details' : 'View details' }}
          </button>
        </div>

        <div
          v-if="expandedRunIds.has(row.run.id)"
          :id="`run-details-${row.run.id}`"
          class="nb-run-details"
        >
          <dl class="nb-run-fields nb-run-fields--common">
            <div>
              <dt>Timestamp</dt>
              <dd>{{ formatDate(row.run.executionTimeStamp) }}</dd>
            </div>
            <div>
              <dt>Result</dt>
              <dd>{{ getResultLabel(row.run.result) }} ({{ row.run.result }})</dd>
            </div>
          </dl>
          <component :is="row.definition.runDetailsComponent" :run="row.run" />
        </div>
      </article>
    </div>
  </section>
</template>
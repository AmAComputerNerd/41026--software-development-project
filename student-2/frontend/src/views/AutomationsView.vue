<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { currentStudentId } from '@/config'
import { deleteAutomation, getAutomations, updateAutomation } from '@/api/automations'
import type { Automation, SaveAutomationInput } from '@/types/automation'

const automations = ref<Automation[]>([])
const loading = ref(true)
const error = ref('')
const showAll = ref(false)

const visibleAutomations = computed(() =>
  showAll.value ? automations.value : automations.value.filter((automation) => automation.enabled),
)
const activeCount = computed(() => automations.value.filter((automation) => automation.enabled).length)
const extensionCount = computed(() =>
  automations.value.filter((automation) => automation.type === 'AssignmentExtension').length,
)
const postCount = computed(() =>
  automations.value.filter((automation) => automation.type === 'ScheduledPost').length,
)

onMounted(load)

async function load() {
  loading.value = true
  error.value = ''
  try {
    automations.value = await getAutomations(currentStudentId)
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to load automations.'
  } finally {
    loading.value = false
  }
}

function toInput(automation: Automation, enabled: boolean): SaveAutomationInput {
  return {
    studentId: automation.studentId,
    type: automation.type,
    enabled,
    bufferMinutes: automation.bufferMinutes,
    reason: automation.reason,
    furtherDetails: automation.furtherDetails,
    postTime: automation.postTime,
    recipients: automation.recipients,
    subject: automation.subject,
    body: automation.body,
  }
}

async function setEnabled(automation: Automation, enabled: boolean) {
  error.value = ''
  try {
    const updated = await updateAutomation(automation.id, toInput(automation, enabled))
    automations.value = automations.value.map((item) => item.id === updated.id ? updated : item)
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to update the automation.'
  }
}

async function remove(automation: Automation) {
  if (!window.confirm('Delete this automation? Its run history will remain available.')) return

  error.value = ''
  try {
    await deleteAutomation(automation.id)
    automations.value = automations.value.filter((item) => item.id !== automation.id)
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to delete the automation.'
  }
}

function formatDate(value: string | null) {
  return value ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : ''
}
</script>

<template>
  <section class="nb-page">
    <header class="nb-page-header">
      <div>
        <p class="nb-eyebrow nb-mono">CONFIGURATION DASHBOARD</p>
        <h1>AUTOMATIONS</h1>
        <p class="nb-page-subtitle">Configure recurring assignment extension requests and scheduled posts.</p>
      </div>
      <RouterLink class="nb-btn nb-btn--accent" :to="{ name: 'new-automation' }">New automation</RouterLink>
    </header>

    <div class="nb-stat-grid" aria-label="Automation summary">
      <div class="nb-stat"><strong>{{ activeCount }}</strong><span class="nb-mono">ACTIVE</span></div>
      <div class="nb-stat"><strong>{{ extensionCount }}</strong><span class="nb-mono">EXTENSIONS</span></div>
      <div class="nb-stat"><strong>{{ postCount }}</strong><span class="nb-mono">SCHEDULED POSTS</span></div>
    </div>

    <div class="nb-view-controls">
      <div class="nb-chips">
        <button type="button" class="nb-chip" :class="{ 'nb-chip--active': !showAll }" @click="showAll = false">Active</button>
        <button type="button" class="nb-chip" :class="{ 'nb-chip--active': showAll }" @click="showAll = true">All</button>
      </div>
      <span class="nb-mono nb-count">{{ visibleAutomations.length }} SHOWN</span>
    </div>

    <div v-if="error" class="nb-alert nb-alert--error" role="alert">{{ error }}</div>
    <div v-if="loading" class="nb-panel nb-empty">Loading automations...</div>
    <div v-else-if="!visibleAutomations.length" class="nb-panel nb-empty">
      <strong>{{ showAll ? 'No automations configured.' : 'No active automations.' }}</strong>
      <span>Create one or switch to All to review disabled configurations.</span>
    </div>

    <div v-else class="nb-automation-list">
      <article v-for="automation in visibleAutomations" :key="automation.id" class="nb-automation-row">
        <div class="nb-automation-row__type">
          <span class="nb-tag" :class="`nb-tag--${automation.type === 'ScheduledPost' ? 'post' : 'extension'}`">
            {{ automation.type === 'ScheduledPost' ? 'SCHEDULED POST' : 'ASSIGNMENT EXTENSION' }}
          </span>
          <strong>{{ automation.type === 'ScheduledPost' ? automation.subject : automation.reason }}</strong>
          <span class="nb-mono nb-detail">
            <template v-if="automation.type === 'ScheduledPost'">{{ formatDate(automation.postTime) }} · {{ automation.recipients?.join(', ') }}</template>
            <template v-else>{{ automation.bufferMinutes }} MIN BUFFER</template>
          </span>
        </div>

        <div class="nb-toggle" :aria-label="`${automation.enabled ? 'Disable' : 'Enable'} automation`">
          <button type="button" class="nb-toggle__cell" :class="{ 'nb-toggle__cell--active': automation.enabled }" @click="setEnabled(automation, true)">ON</button>
          <button type="button" class="nb-toggle__cell" :class="{ 'nb-toggle__cell--active': !automation.enabled }" @click="setEnabled(automation, false)">OFF</button>
        </div>

        <div class="nb-row-actions">
          <RouterLink class="nb-btn nb-btn--outline" :to="{ name: 'edit-automation', params: { id: automation.id } }">Edit</RouterLink>
          <button type="button" class="nb-text-btn nb-text-btn--danger" @click="remove(automation)">Delete</button>
        </div>
      </article>
    </div>
  </section>
</template>
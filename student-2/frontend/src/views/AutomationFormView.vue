<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { createAutomation, getAutomation, updateAutomation } from '@/api/automations'
import { currentStudentId } from '@/config'
import type { AutomationType, SaveAutomationInput } from '@/types/automation'

const route = useRoute()
const router = useRouter()
const automationId = computed(() => typeof route.params.id === 'string' ? route.params.id : null)
const loading = ref(Boolean(automationId.value))
const saving = ref(false)
const error = ref('')

const form = reactive({
  type: 'AssignmentExtension' as AutomationType,
  enabled: true,
  bufferMinutes: 60,
  reason: '',
  furtherDetails: '',
  postTime: toLocalDateTime(new Date(Date.now() + 24 * 60 * 60 * 1000)),
  recipients: '',
  subject: '',
  body: '',
})

onMounted(async () => {
  if (!automationId.value) return

  try {
    const automation = await getAutomation(automationId.value)
    form.type = automation.type
    form.enabled = automation.enabled
    form.bufferMinutes = automation.bufferMinutes ?? 60
    form.reason = automation.reason ?? ''
    form.furtherDetails = automation.furtherDetails ?? ''
    form.postTime = automation.postTime ? toLocalDateTime(new Date(automation.postTime)) : ''
    form.recipients = automation.recipients?.join(', ') ?? ''
    form.subject = automation.subject ?? ''
    form.body = automation.body ?? ''
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to load the automation.'
  } finally {
    loading.value = false
  }
})

function toLocalDateTime(date: Date) {
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

function buildInput(): SaveAutomationInput {
  const isExtension = form.type === 'AssignmentExtension'
  return {
    studentId: currentStudentId,
    type: form.type,
    enabled: form.enabled,
    bufferMinutes: isExtension ? form.bufferMinutes : null,
    reason: isExtension ? form.reason : null,
    furtherDetails: isExtension ? form.furtherDetails : null,
    postTime: isExtension ? null : new Date(form.postTime).toISOString(),
    recipients: isExtension ? null : form.recipients.split(',').map((value) => value.trim()).filter(Boolean),
    subject: isExtension ? null : form.subject,
    body: isExtension ? null : form.body,
  }
}

async function save() {
  saving.value = true
  error.value = ''
  try {
    const input = buildInput()
    if (automationId.value) {
      await updateAutomation(automationId.value, input)
    } else {
      await createAutomation(input)
    }
    await router.push({ name: 'automations' })
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to save the automation.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <section class="nb-page nb-form-page">
    <header class="nb-page-header">
      <div>
        <p class="nb-eyebrow nb-mono">{{ automationId ? 'EDIT CONFIGURATION' : 'NEW CONFIGURATION' }}</p>
        <h1>{{ automationId ? 'EDIT AUTOMATION' : 'CREATE AUTOMATION' }}</h1>
      </div>
    </header>

    <div v-if="error" class="nb-alert nb-alert--error" role="alert">{{ error }}</div>
    <div v-if="loading" class="nb-panel nb-empty">Loading automation...</div>

    <form v-else class="nb-panel nb-automation-form" @submit.prevent="save">
      <div class="nb-form-grid">
        <label class="nb-field">
          <span>Automation type</span>
          <select v-model="form.type" :disabled="Boolean(automationId)">
            <option value="AssignmentExtension">Assignment extension</option>
            <option value="ScheduledPost">Scheduled post</option>
          </select>
        </label>

        <fieldset class="nb-field nb-fieldset">
          <legend>Enabled</legend>
          <div class="nb-toggle">
            <button type="button" class="nb-toggle__cell" :class="{ 'nb-toggle__cell--active': form.enabled }" @click="form.enabled = true">ON</button>
            <button type="button" class="nb-toggle__cell" :class="{ 'nb-toggle__cell--active': !form.enabled }" @click="form.enabled = false">OFF</button>
          </div>
        </fieldset>
      </div>

      <template v-if="form.type === 'AssignmentExtension'">
        <label class="nb-field">
          <span>Buffer minutes</span>
          <input v-model.number="form.bufferMinutes" type="number" min="0" required />
        </label>
        <label class="nb-field">
          <span>Reason</span>
          <input v-model.trim="form.reason" type="text" maxlength="500" required />
        </label>
        <label class="nb-field">
          <span>Further details</span>
          <textarea v-model.trim="form.furtherDetails" rows="6" maxlength="2000"></textarea>
        </label>
      </template>

      <template v-else>
        <label class="nb-field">
          <span>Post time</span>
          <input v-model="form.postTime" type="datetime-local" required />
        </label>
        <label class="nb-field">
          <span>Recipients</span>
          <input v-model="form.recipients" type="text" placeholder="name@example.edu.au, other@example.edu.au" required />
        </label>
        <label class="nb-field">
          <span>Subject</span>
          <input v-model.trim="form.subject" type="text" maxlength="200" required />
        </label>
        <label class="nb-field">
          <span>Body</span>
          <textarea v-model.trim="form.body" rows="8" maxlength="10000" required></textarea>
        </label>
      </template>

      <div class="nb-form-actions">
        <button type="button" class="nb-btn nb-btn--outline" @click="router.push({ name: 'automations' })">Cancel</button>
        <button type="submit" class="nb-btn nb-btn--accent" :disabled="saving">{{ saving ? 'Saving...' : 'Save automation' }}</button>
      </div>
    </form>
  </section>
</template>
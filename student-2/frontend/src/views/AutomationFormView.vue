<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { createAutomation, getAutomation, updateAutomation } from '@/api/automations'
import { automationDefinitions, getAutomationDefinition } from '@/automations/registry'
import { currentStudentId } from '@/config'
import type { AutomationDiscriminator, AutomationFormData } from '@/types/automation'

const route = useRoute()
const router = useRouter()
const automationId = computed(() => typeof route.params.id === 'string' ? route.params.id : null)
const loading = ref(Boolean(automationId.value))
const saving = ref(false)
const error = ref('')
const form = ref<AutomationFormData>(automationDefinitions[0].createForm())
const selectedDefinition = computed(() => getAutomationDefinition(form.value.$type))

onMounted(async () => {
  if (!automationId.value) return

  try {
    const automation = await getAutomation(automationId.value)
    form.value = getAutomationDefinition(automation.$type).loadForm(automation)
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to load the automation.'
  } finally {
    loading.value = false
  }
})

function selectType(event: Event) {
  const type = (event.target as HTMLSelectElement).value as AutomationDiscriminator
  form.value = getAutomationDefinition(type).createForm()
}

async function save() {
  saving.value = true
  error.value = ''
  try {
    const input = selectedDefinition.value.buildInput(form.value, currentStudentId)
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
          <select :value="form.$type" :disabled="Boolean(automationId)" @change="selectType">
            <option v-for="definition in automationDefinitions" :key="definition.discriminator" :value="definition.discriminator">
              {{ definition.label }}
            </option>
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

      <component :is="selectedDefinition.formComponent" v-model="form" />

      <div class="nb-form-actions">
        <button type="button" class="nb-btn nb-btn--outline" @click="router.push({ name: 'automations' })">Cancel</button>
        <button type="submit" class="nb-btn nb-btn--accent" :disabled="saving">{{ saving ? 'Saving...' : 'Save automation' }}</button>
      </div>
    </form>
  </section>
</template>
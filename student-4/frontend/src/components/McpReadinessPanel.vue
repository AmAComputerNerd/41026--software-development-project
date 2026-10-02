<script setup lang="ts">
import { ref, watch } from 'vue'
import { checkAccountReadiness, type AccountReadiness, type ReadinessStatus } from '@/api/mcp'

const props = defineProps<{ userId: string }>()
const emit = defineEmits<{ editProfile: [] }>()
const readiness = ref<AccountReadiness | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

watch(() => props.userId, () => {
  readiness.value = null
  error.value = null
})

const statusLabels: Record<ReadinessStatus, string> = {
  ready: 'READY',
  needs_attention: 'NEEDS ATTENTION',
  unavailable: 'UNAVAILABLE',
}

async function checkReadiness() {
  const userId = props.userId
  loading.value = true
  error.value = null
  readiness.value = null
  try {
    const result = await checkAccountReadiness(userId)
    if (props.userId === userId) readiness.value = result.data
  } catch (err) {
    if (props.userId === userId) {
      error.value = err instanceof Error ? err.message : 'Could not check account readiness.'
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="nb-panel nb-profile__section" aria-labelledby="mcp-readiness-heading" :aria-busy="loading">
    <h2 id="mcp-readiness-heading" class="nb-profile__section-title nb-mono">ACCOUNT READINESS / MCP</h2>
    <p>Check your saved account details and connected services through MCP. Save profile changes before checking again. These recommendations do not change your account or block existing features.</p>
    <button type="button" class="nb-btn" :disabled="loading" @click="checkReadiness">
      {{ loading ? 'CHECKING ACCOUNT SETUP...' : 'CHECK MY ACCOUNT SETUP' }}
    </button>
    <p v-if="error" role="alert" class="nb-mono">{{ error }}</p>
    <div v-if="readiness" class="nb-readiness__results" aria-live="polite">
      <p class="nb-mono">
        OVERALL: {{ statusLabels[readiness.overallStatus] }} /
        CHECKED {{ new Date(readiness.checkedAtUtc).toLocaleTimeString() }}
      </p>
      <div v-for="check in readiness.checks" :key="check.code" class="nb-readiness__check" :data-status="check.status">
        <h3 class="nb-mono">{{ check.label }} / {{ statusLabels[check.status] }}</h3>
        <p>{{ check.message }}</p>
        <button v-if="check.action === 'edit_profile'" type="button" class="nb-btn nb-btn--outline" @click="emit('editProfile')">
          {{ check.code === 'role' ? 'REVIEW ROLE SETTINGS' : 'EDIT PROFILE' }}
        </button>
      </div>
    </div>
  </section>
</template>

<style scoped lang="scss">
.nb-readiness__results {
  margin-top: var(--nb-space-4);
}

.nb-readiness__check {
  border: var(--nb-border-width-md) solid var(--nb-color-ink);
  box-shadow: var(--nb-shadow);
  padding: var(--nb-space-4);
  margin-bottom: var(--nb-space-4);

  &[data-status='needs_attention'],
  &[data-status='unavailable'] {
    border-color: var(--nb-color-accent-orange);
  }

  h3 {
    margin-top: 0;
  }
}
</style>

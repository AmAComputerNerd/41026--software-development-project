<script setup lang="ts">
import { ref } from 'vue'
import { useGrades } from '@/composables/useGrades'
import AiIntegrationModal from './AiIntegrationModal.vue'

const { generateRecommendation } = useGrades()
const dialogOpen = ref(false)
const loading = ref(false)

async function openDialog() {
  dialogOpen.value = true
  loading.value = true
  try {
    const response = await generateRecommendation()
  } catch {
    // Error handled in modal
  } finally {
    loading.value = false
  }
}

function closeDialog() {
  dialogOpen.value = false
}
</script>

<template>
  <div class="ai-action-bar">
    <button
      type="button"
      class="nb-btn nb-btn--accent ai-action-bar__button"
      :disabled="loading"
      @click="openDialog"
    >
      <span aria-hidden="true" class="ai-action-bar__spark">✦</span>
      {{ loading ? 'OPENING AI TOOLS...' : 'AI TOOLS' }}
    </button>
  </div>

  <AiIntegrationModal :open="dialogOpen" @close="closeDialog" />
</template>

<style scoped>
.ai-action-bar {
  display: flex;
  justify-content: flex-start;
  padding: var(--nb-space-4) 24px 0;
}

.ai-action-bar__button {
  display: inline-flex;
  align-items: center;
  gap: var(--nb-space-2);
  font-size: 12px;
}

.ai-action-bar__spark {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  font-size: 12px;
  line-height: 1;
}
</style>

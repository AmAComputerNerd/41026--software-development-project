<script setup lang="ts">
import { ref } from 'vue'
import { useGrades } from '@/composables/useGrades'
import RecommendationModal from './RecommendationModal.vue'

const { generateRecommendation } = useGrades()
const dialogOpen = ref(false)
const loading = ref(false)
const recommendation = ref<string | null>(null)
const error = ref<string | null>(null)

async function openRecommendation() {
  dialogOpen.value = true
  loading.value = true
  error.value = null
  recommendation.value = null
  try {
    const response = await generateRecommendation()
    recommendation.value = response.recommendation
  } catch (requestError) {
    error.value =
      requestError instanceof Error
        ? requestError.message
        : 'The AI coach is unavailable right now.'
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
      @click="openRecommendation"
    >
      <span aria-hidden="true" class="ai-action-bar__spark">✦</span>
      {{ loading ? 'ASKING THE AI COACH...' : 'AI FOCUS RECOMMENDATION' }}
    </button>
  </div>

  <RecommendationModal
    :open="dialogOpen"
    :loading="loading"
    :recommendation="recommendation"
    :error="error"
    @close="closeDialog"
  />
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

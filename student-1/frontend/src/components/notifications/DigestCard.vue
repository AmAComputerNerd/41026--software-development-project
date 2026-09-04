<script setup lang="ts">
import type { AiDigestDto } from '@/composables/useAiDigest'
import MarkdownContent from './MarkdownContent.vue'

defineProps<{ latestDigest: AiDigestDto | null; generating: boolean }>()
const emit = defineEmits<{ generate: [] }>()
</script>

<template>
  <div class="nb-panel nb-digest-card">
    <h2 class="nb-digest-card__heading">WEEKLY SUMMARY</h2>
    <p class="nb-digest-card__desc nb-mono">
      Generate an AI summary of your unread notifications.
    </p>
    <button
      type="button"
      class="nb-btn nb-btn--accent"
      :disabled="generating"
      @click="emit('generate')"
    >
      {{ generating ? 'GENERATING…' : 'GENERATE DIGEST' }}
    </button>

    <div v-if="latestDigest" class="nb-inset">
      <div class="nb-inset__header">AI-GENERATED CONTENT — VERIFY BEFORE ACTING</div>
      <MarkdownContent class="nb-inset__body" :source="latestDigest.summary" />
    </div>
  </div>
</template>

<style scoped>
.nb-digest-card {
  padding: 24px;
  animation: nb-rise-in 360ms 60ms ease-out both;
}

.nb-digest-card__heading {
  font-size: 20px;
  font-weight: 700;
}

.nb-digest-card__desc {
  color: var(--nb-color-muted);
  margin: 4px 0 16px;
}
</style>


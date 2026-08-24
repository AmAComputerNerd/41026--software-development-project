<script setup lang="ts">
import { computed } from 'vue'
import type { AiDigestDto } from '@/composables/useAiDigest'

const props = defineProps<{ digests: AiDigestDto[] }>()

function formatDate(isoDate: string): string {
  const hasTimezone = /[zZ]|[+-]\d\d:\d\d$/.test(isoDate)
  const date = new Date(hasTimezone ? isoDate : `${isoDate}Z`)
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' }).toUpperCase()
}

const history = computed(() => props.digests)
</script>

<template>
  <div class="nb-history">
    <h2 class="nb-history__heading">PAST DIGESTS</h2>
    <p v-if="history.length === 0" class="nb-history__empty nb-mono">NO DIGESTS YET</p>
    <div v-for="d in history" :key="d.id" class="nb-history__row">
      <span class="nb-history__date">{{ formatDate(d.generatedAtUtc) }}</span>
      <p class="nb-history__summary">{{ d.summary }}</p>
    </div>
  </div>
</template>

<style scoped>
.nb-history {
  margin-top: 32px;
}

.nb-history__heading {
  font-size: 20px;
  font-weight: 700;
  margin-bottom: 12px;
}

.nb-history__empty {
  color: var(--nb-color-muted);
  padding: 16px 0;
}

.nb-history__summary {
  font-size: 14px;
  margin: 0;
}
</style>

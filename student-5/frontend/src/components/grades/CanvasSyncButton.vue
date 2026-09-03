<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useCanvasSync } from '@/composables/useCanvasSync'

const { status, isSyncing, sync } = useCanvasSync()

// Hold the "synced"/"failed" state briefly so the user sees feedback.
const transientLabel = ref<string | null>(null)

const label = computed(() => {
  if (transientLabel.value) return transientLabel.value
  if (isSyncing.value) return 'SYNCING...'
  return 'SYNC CANVAS'
})

const variant = computed(() => {
  if (transientLabel.value === '✓ SYNCED') return 'synced'
  if (status.value === 'failed') return 'failed'
  return 'idle'
})

watch(status, (next, previous) => {
  if (next === previous) return
  if (next === 'synced') {
    transientLabel.value = '✓ SYNCED'
    setTimeout(() => {
      if (status.value === 'synced') transientLabel.value = null
    }, 2500)
  } else if (next === 'failed') {
    transientLabel.value = '✗ FAILED — RETRY'
    setTimeout(() => {
      if (status.value === 'failed') transientLabel.value = null
    }, 4000)
  } else {
    transientLabel.value = null
  }
})
</script>

<template>
  <button
    type="button"
    class="canvas-sync-btn nb-mono"
    :class="[`canvas-sync-btn--${variant}`, { 'canvas-sync-btn--syncing': isSyncing }]"
    :disabled="isSyncing"
    @click="sync"
  >
    {{ label }}
  </button>
</template>
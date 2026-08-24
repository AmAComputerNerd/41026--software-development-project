<script setup lang="ts">
import { NOTIFICATION_TYPES } from '@/composables/useNotifications'

defineProps<{ activeFilters: Record<string, boolean> }>()
const emit = defineEmits<{ toggle: [type: string]; 'toggle-all': [value: boolean] }>()

const allActive = (activeFilters: Record<string, boolean>) =>
  NOTIFICATION_TYPES.every((t) => activeFilters[t.value])
</script>

<template>
  <div class="d-flex flex-wrap ga-2">
    <button
      type="button"
      class="nb-chip"
      :class="{ 'nb-chip--active': allActive(activeFilters) }"
      @click="emit('toggle-all', !allActive(activeFilters))"
    >
      ALL
    </button>
    <button
      v-for="t in NOTIFICATION_TYPES"
      :key="t.value"
      type="button"
      class="nb-chip"
      :class="{ 'nb-chip--active': activeFilters[t.value] }"
      @click="emit('toggle', t.value)"
    >
      {{ t.label }}
    </button>
  </div>
</template>

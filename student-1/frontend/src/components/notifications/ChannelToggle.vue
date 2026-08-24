<script setup lang="ts">
defineProps<{ enabled: boolean; label: string }>()
const emit = defineEmits<{ toggle: [] }>()
</script>

<template>
  <div class="nb-toggle" role="group" :aria-label="label">
    <button
      type="button"
      class="nb-toggle__cell"
      :class="{ 'nb-toggle__cell--active': enabled }"
      :aria-pressed="enabled"
      @click="!enabled && emit('toggle')"
    >
      ON
    </button>
    <button
      type="button"
      class="nb-toggle__cell"
      :class="{ 'nb-toggle__cell--active': !enabled }"
      :aria-pressed="!enabled"
      @click="enabled && emit('toggle')"
    >
      OFF
    </button>
  </div>
</template>

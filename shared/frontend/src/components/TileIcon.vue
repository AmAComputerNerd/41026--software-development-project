<script lang="ts" setup>
defineProps<{
  name: 'bell' | 'clock' | 'chart' | 'gear' | 'user'
}>()
</script>

<template>
  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
    <template v-if="name === 'bell'">
      <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
      <path d="M13.73 21a2 2 0 0 1-3.46 0" />
    </template>
    <template v-else-if="name === 'clock'">
      <circle cx="12" cy="12" r="9"></circle>
      <path d="M12 7v5l4 2"></path>
    </template>
    <template v-else-if="name === 'chart'">
      <path d="M4 20V10M12 20V4M20 20v-7"></path>
    </template>
    <template v-else-if="name === 'gear'">
      <circle cx="12" cy="12" r="3"></circle>
      <path d="M12 3v3M12 18v3M3 12h3M18 12h3M5.6 5.6l2.1 2.1M16.3 16.3l2.1 2.1M18.4 5.6l-2.1 2.1M7.7 16.3l-2.1 2.1"></path>
    </template>
    <template v-else-if="name === 'user'">
      <circle cx="12" cy="8" r="4"></circle>
      <path d="M4 20c0-4 4-6 8-6s8 2 8 6"></path>
    </template>
  </svg>
</template>

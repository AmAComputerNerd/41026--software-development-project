<script setup lang="ts">
import { useTheme } from '../composables/useTheme'

const { theme, toggleTheme } = useTheme()
</script>

<template>
  <div class="nb-toggle" role="group" aria-label="Theme">
    <button
      type="button"
      class="nb-toggle__cell nb-theme-toggle__cell"
      :class="{ 'nb-toggle__cell--active': theme === 'light' }"
      :aria-pressed="theme === 'light'"
      aria-label="Light mode"
      @click="theme === 'dark' && toggleTheme()"
    >
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <circle cx="12" cy="12" r="4" />
        <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
      </svg>
    </button>
    <button
      type="button"
      class="nb-toggle__cell nb-theme-toggle__cell"
      :class="{ 'nb-toggle__cell--active': theme === 'dark' }"
      :aria-pressed="theme === 'dark'"
      aria-label="Dark mode"
      @click="theme === 'light' && toggleTheme()"
    >
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M20 14.5A8 8 0 1 1 9.5 4a6.5 6.5 0 0 0 10.5 10.5z" />
      </svg>
    </button>
  </div>
</template>

<style scoped>
.nb-theme-toggle__cell {
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
</style>

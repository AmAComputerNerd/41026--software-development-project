<script setup lang="ts">
defineProps<{
  recommendation: string | null
  loading: boolean
  error: string | null
  open: boolean
}>()

const emit = defineEmits<{
  close: []
}>()
</script>

<template>
  <Transition name="recommendation">
    <div
      v-if="open"
      class="recommendation-modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="recommendation-title"
      @click.self="emit('close')"
    >
      <article class="recommendation-modal__panel nb-panel">
        <header class="recommendation-modal__header">
          <div>
            <p class="recommendation-modal__eyebrow nb-mono">AI INSIGHT</p>
            <h2 id="recommendation-title">FOCUS RECOMMENDATION</h2>
          </div>
          <button
            type="button"
            class="nb-btn nb-btn--outline recommendation-modal__close"
            aria-label="Close recommendation"
            @click="emit('close')"
          >
            ✕
          </button>
        </header>

        <div class="recommendation-modal__body">
          <p v-if="loading" class="recommendation-modal__placeholder nb-mono">
            ASKING THE AI COACH FOR GUIDANCE...
          </p>
          <p v-else-if="error" class="recommendation-modal__error nb-mono" role="alert">
            {{ error }}
          </p>
          <p v-else-if="recommendation" class="recommendation-modal__text">
            {{ recommendation }}
          </p>
          <p v-else class="recommendation-modal__placeholder nb-mono">
            NO RECOMMENDATION RETURNED.
          </p>
        </div>

        <footer class="recommendation-modal__footer nb-mono">
          AI-GENERATED • VERIFY BEFORE CHANGING YOUR PLAN
        </footer>
      </article>
    </div>
  </Transition>
</template>

<style scoped>
.recommendation-modal {
  position: fixed;
  inset: 0;
  z-index: 50;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--nb-space-5);
  background: rgba(0, 0, 0, 0.55);
}

.recommendation-modal__panel {
  width: min(640px, 100%);
  max-height: min(80vh, 720px);
  display: flex;
  flex-direction: column;
  background: var(--nb-color-white);
}

.recommendation-modal__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--nb-space-4);
  padding: var(--nb-space-5) var(--nb-space-5) var(--nb-space-3);
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.recommendation-modal__eyebrow {
  margin: 0 0 var(--nb-space-1);
  color: var(--nb-color-muted);
  font-size: 11px;
  font-weight: var(--nb-font-weight-bold);
}

.recommendation-modal__header h2 {
  margin: 0;
  font-size: 26px;
  letter-spacing: -1px;
  text-transform: uppercase;
}

.recommendation-modal__close {
  background: var(--nb-color-accent-orange);
}

.recommendation-modal__body {
  padding: var(--nb-space-5);
  overflow-y: auto;
  flex: 1;
}

.recommendation-modal__text {
  margin: 0;
  font-size: 17px;
  line-height: 1.45;
}

.recommendation-modal__placeholder,
.recommendation-modal__error {
  margin: 0;
  font-size: 13px;
  color: var(--nb-color-muted);
}

.recommendation-modal__error {
  color: var(--nb-color-ink);
  background: var(--nb-color-accent-yellow);
  padding: var(--nb-space-3);
}

.recommendation-modal__footer {
  padding: var(--nb-space-3) var(--nb-space-5);
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
  font-size: 10px;
  font-weight: var(--nb-font-weight-bold);
  letter-spacing: 0.5px;
  text-align: center;
}

.recommendation-enter-active,
.recommendation-leave-active {
  transition: opacity 160ms ease;
}

.recommendation-enter-active .recommendation-modal__panel,
.recommendation-leave-active .recommendation-modal__panel {
  transition: transform 200ms ease, opacity 200ms ease;
}

.recommendation-enter-from,
.recommendation-leave-to {
  opacity: 0;
}

.recommendation-enter-from .recommendation-modal__panel,
.recommendation-leave-to .recommendation-modal__panel {
  transform: translateY(20px);
  opacity: 0;
}

@media (prefers-reduced-motion: reduce) {
  .recommendation-enter-active,
  .recommendation-leave-active,
  .recommendation-enter-active .recommendation-modal__panel,
  .recommendation-leave-active .recommendation-modal__panel {
    transition: none;
  }
}
</style>

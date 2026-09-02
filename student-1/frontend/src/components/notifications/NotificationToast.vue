<script setup lang="ts">
import NotificationTag from './NotificationTag.vue'
import type { NotificationDto } from '@/composables/useNotifications'

defineProps<{
  notification: NotificationDto | null
}>()

const emit = defineEmits<{
  dismiss: []
  'mark-read': [id: string]
}>()
</script>

<template>
  <Transition name="toast-slide">
    <div v-if="notification" class="nb-toast" role="alert" aria-live="polite">
      <div class="nb-toast__header">
        <div class="nb-toast__tag-wrap">
          <NotificationTag :type="notification.type" />
          <span class="nb-mono nb-toast__live-badge">LIVE EVENT</span>
        </div>
        <button
          type="button"
          class="nb-toast__close"
          aria-label="Close notification"
          @click="emit('dismiss')"
        >
          &times;
        </button>
      </div>

      <p class="nb-toast__message">{{ notification.message }}</p>

      <div class="nb-toast__actions">
        <button
          type="button"
          class="nb-btn nb-btn--accent nb-toast__btn"
          @click="emit('mark-read', notification.id)"
        >
          MARK READ
        </button>
        <button
          type="button"
          class="nb-btn nb-btn--outline nb-toast__btn"
          @click="emit('dismiss')"
        >
          DISMISS
        </button>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.nb-toast {
  position: fixed;
  bottom: 24px;
  right: 24px;
  width: 380px;
  max-width: calc(100vw - 48px);
  background: var(--nb-color-bg);
  border: var(--nb-border-width-lg) solid var(--nb-color-ink);
  border-left: 8px solid var(--nb-color-accent-orange);
  box-shadow: 6px 6px 0 var(--nb-color-shadow);
  border-radius: 0;
  padding: var(--nb-space-4);
  z-index: 1000;
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-3);
  color: var(--nb-color-ink);
}

.nb-toast__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.nb-toast__tag-wrap {
  display: flex;
  align-items: center;
  gap: var(--nb-space-2);
}

.nb-toast__live-badge {
  font-size: 10px;
  padding: 2px 6px;
  border: 1px solid var(--nb-color-ink);
  background: var(--nb-color-accent-yellow);
  color: #111111;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.nb-toast__close {
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-white);
  color: var(--nb-color-ink);
  font-size: 16px;
  font-weight: bold;
  line-height: 1;
  width: 24px;
  height: 24px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  border-radius: 0;
  padding: 0;
}

.nb-toast__close:hover {
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
}

.nb-toast__message {
  margin: 0;
  font-size: 14px;
  line-height: 1.4;
  font-weight: 600;
}

.nb-toast__actions {
  display: flex;
  align-items: center;
  gap: var(--nb-space-2);
}

.nb-toast__btn {
  padding: 4px 10px;
  font-size: 11px;
}

.toast-slide-enter-active,
.toast-slide-leave-active {
  transition: all 0.25s ease-out;
}

.toast-slide-enter-from {
  opacity: 0;
  transform: translateY(16px);
}

.toast-slide-leave-to {
  opacity: 0;
  transform: translateX(32px);
}
</style>

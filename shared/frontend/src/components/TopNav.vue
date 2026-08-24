<script lang="ts" setup>
import { onMounted } from 'vue'
import { useUnreadCount } from '@/composables/useUnreadCount'
import TileIcon from '@/components/TileIcon.vue'

const { count, load } = useUnreadCount()

onMounted(load)
</script>

<template>
  <header class="shell-topbar">
    <div class="shell-topbar__brand">
      <span class="shell-topbar__logo">B</span>
      <span class="shell-topbar__title">BETTER CANVAS</span>
      <span class="shell-topbar__badge">STUDENT DASHBOARD</span>
    </div>
    <div class="shell-topbar__actions">
      <button class="shell-bell" type="button" aria-label="Notifications">
        <TileIcon name="bell" />
        <span v-if="count > 0" class="shell-bell__badge">{{ count > 99 ? '99+' : count }}</span>
      </button>
    </div>
  </header>
</template>

<style scoped>
.shell-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 68px;
  padding: 0 var(--space-6);
  border-bottom: var(--border-width-lg) solid var(--border-color);
  background: var(--color-surface);
}

.shell-topbar__brand {
  display: flex;
  align-items: baseline;
  gap: var(--space-3);
}

.shell-topbar__logo {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: var(--border-width-md) solid var(--border-color);
  background: var(--color-accent-primary);
  color: var(--color-text-primary);
  font-family: var(--font-mono);
  font-weight: 700;
}

.shell-topbar__title {
  font-weight: 700;
  letter-spacing: 0.5px;
}

.shell-topbar__badge {
  font-family: var(--font-mono);
  font-size: 12px;
  letter-spacing: 0.5px;
  color: var(--color-text-secondary);
  border: var(--border-width-sm) solid var(--border-color);
  padding: 2px var(--space-2);
}

.shell-topbar__actions {
  display: flex;
  align-items: center;
}

.shell-bell {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border: var(--border-width-md) solid var(--border-color);
  background: var(--color-surface);
  font-size: 18px;
  cursor: pointer;
}

.shell-bell__badge {
  position: absolute;
  top: -8px;
  right: -8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 18px;
  height: 18px;
  padding: 0 4px;
  border: var(--border-width-sm) solid var(--border-color);
  background: var(--color-accent-primary);
  color: var(--color-text-primary);
  font-family: var(--font-mono);
  font-size: 11px;
  font-weight: 700;
}
</style>

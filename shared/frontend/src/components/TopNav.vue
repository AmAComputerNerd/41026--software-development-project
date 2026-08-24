<script lang="ts" setup>
import { onMounted } from 'vue'
import { useUnreadCount } from '@/composables/useUnreadCount'
import TileIcon from '@/components/TileIcon.vue'

const { count, load } = useUnreadCount()

onMounted(load)
</script>

<template>
  <header class="nb-topbar">
    <div class="nb-topbar__brand">
      <span class="nb-topbar__logo">B</span>
      <span class="nb-topbar__name">BETTER CANVAS</span>
      <span class="nb-topbar__badge nb-mono">STUDENT DASHBOARD</span>
    </div>
    <div class="nb-topbar__actions">
      <button class="nb-btn nb-btn--outline nb-topbar__bell" type="button" aria-label="Notifications">
        <TileIcon name="bell" />
        <span v-if="count > 0" class="nb-badge nb-topbar__bell-badge">{{ count > 99 ? '99+' : count }}</span>
      </button>
    </div>
  </header>
</template>

<style scoped>
.nb-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 68px;
  padding: 0 24px;
  border-bottom: var(--nb-border-width-lg) solid var(--nb-color-ink);
}

.nb-topbar__brand {
  display: flex;
  align-items: center;
  gap: 12px;
}

.nb-topbar__logo {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
  font-weight: 700;
  font-size: 15px;
}

.nb-topbar__name {
  font-weight: 700;
  font-size: 18px;
  letter-spacing: 0.5px;
}

.nb-topbar__badge {
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  padding: 2px 8px;
  font-size: 11px;
  letter-spacing: 0.5px;
}

.nb-topbar__actions {
  display: flex;
  align-items: center;
}

.nb-topbar__bell {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  padding: 0;
}

.nb-topbar__bell-badge {
  position: absolute;
  top: -8px;
  right: -8px;
}
</style>

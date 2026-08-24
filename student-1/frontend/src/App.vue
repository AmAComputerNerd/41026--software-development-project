<script setup lang="ts">
import { RouterLink, RouterView, useRoute } from 'vue-router'
import NotificationCentre from '@/components/notifications/NotificationCentre.vue'

const route = useRoute()

const navLinks = [
  { to: { name: 'notifications' }, label: '01 LIST', match: 'notifications' },
  { to: { name: 'preferences' }, label: '02 PREFS', match: 'preferences' },
  { to: { name: 'digest' }, label: '03 DIGEST', match: 'digest' },
]
</script>

<template>
  <div class="nb-app">
    <header class="nb-topbar">
      <div class="nb-topbar__brand">
        <span class="nb-topbar__logo">B</span>
        <span class="nb-topbar__name">BETTER CANVAS</span>
        <span class="nb-topbar__badge nb-mono">STUDENT DASHBOARD</span>
      </div>
      <NotificationCentre />
    </header>

    <nav class="nb-tabstrip">
      <RouterLink
        v-for="link in navLinks"
        :key="link.match"
        :to="link.to"
        class="nb-tabstrip__tab nb-mono"
        :class="{ 'nb-tabstrip__tab--active': route.name === link.match }"
      >
        {{ link.label }}
      </RouterLink>
    </nav>

    <main class="nb-main">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.nb-app {
  min-height: 100vh;
}

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

.nb-tabstrip {
  display: flex;
  gap: 8px;
  padding: 20px 24px 0;
}

.nb-tabstrip__tab {
  border: var(--nb-border-width-md) solid var(--nb-color-ink);
  padding: 8px 16px;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.5px;
  text-decoration: none;
  color: var(--nb-color-ink);
  background: var(--nb-color-bg);
}

.nb-tabstrip__tab--active {
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
}

.nb-main {
  padding: 24px;
}
</style>

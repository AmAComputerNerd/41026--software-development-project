<script setup lang="ts">
import { RouterLink, RouterView, useRoute } from 'vue-router'
import { Navbar, SERVICES } from '@better-canvas/ui-kit'
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
    <Navbar :services="SERVICES">
      <template #actions>
        <NotificationCentre />
      </template>
    </Navbar>

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

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

    <nav class="nb-tabstrip" aria-label="Notification sections">
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
      <RouterView v-slot="{ Component, route: currentRoute }">
        <Transition name="page" mode="out-in" appear>
          <component :is="Component" :key="currentRoute.name" />
        </Transition>
      </RouterView>
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
  overflow-x: auto;
}

.nb-tabstrip__tab {
  flex: 0 0 auto;
  border: var(--nb-border-width-md) solid var(--nb-color-ink);
  padding: 8px 16px;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.5px;
  text-decoration: none;
  color: var(--nb-color-ink);
  background: var(--nb-color-bg);
  transition:
    color 140ms ease,
    background-color 140ms ease,
    transform 140ms ease,
    box-shadow 140ms ease;
}

.nb-tabstrip__tab:hover {
  transform: translateY(-3px);
  box-shadow: 3px 3px 0 var(--nb-color-ink);
}

.nb-tabstrip__tab--active {
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
}

.nb-main {
  padding: 24px;
}
</style>


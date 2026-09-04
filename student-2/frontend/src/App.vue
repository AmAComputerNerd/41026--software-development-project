<script setup lang="ts">
import { RouterLink, RouterView, useRoute } from 'vue-router'
import { Navbar, SERVICES } from '@better-canvas/ui-kit'

const route = useRoute()
const navLinks = [
  { to: { name: 'automations' }, label: '01 AUTOMATIONS', matches: ['automations', 'new-automation', 'edit-automation'] },
  { to: { name: 'history' }, label: '02 RUN HISTORY', matches: ['history'] },
]
</script>

<template>
  <div class="nb-app">
    <Navbar :services="SERVICES" />

    <nav class="nb-tabstrip" aria-label="Automation sections">
      <RouterLink
        v-for="link in navLinks"
        :key="link.label"
        :to="link.to"
        class="nb-tabstrip__tab nb-mono"
        :class="{ 'nb-tabstrip__tab--active': link.matches.includes(String(route.name)) }"
      >
        {{ link.label }}
      </RouterLink>
    </nav>

    <main class="nb-main">
      <RouterView v-slot="{ Component, route: currentRoute }">
        <Transition name="page" mode="out-in" appear>
          <component :is="Component" :key="currentRoute.fullPath" />
        </Transition>
      </RouterView>
    </main>
  </div>
</template>
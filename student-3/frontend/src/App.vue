<script setup lang="ts">
import { RouterLink, RouterView, useRoute } from 'vue-router'
import { Navbar, SERVICES } from '@better-canvas/ui-kit'
import NotificationButton from '@/components/NotificationButton.vue'

const route = useRoute()
const navLinks = [
  { to: { name: 'tasks' }, label: '01 TASKS', match: 'tasks' },
  { to: { name: 'calendar' }, label: '02 CALENDAR', match: 'calendar' },
  { to: { name: 'assignments' }, label: '03 ASSIGNMENTS', match: 'assignments' },
]
</script>

<template>
  <div class="nb-app">
    <Navbar :services="SERVICES">
      <template #actions>
        <NotificationButton />
      </template>
    </Navbar>

    <nav class="nb-tabstrip" aria-label="Deadline tracker sections">
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

<script setup lang="ts">
import { RouterLink, RouterView, useRoute } from 'vue-router'
import { Navbar, SERVICES } from '@better-canvas/ui-kit'

const route = useRoute()
const services = SERVICES.map((service) =>
  service.id === 'grades-progress'
    ? { ...service, route: '/grades/', live: true }
    : service,
)
</script>

<template>
  <div class="nb-app">
    <Navbar :services="services" badge="GRADES TRACKER" />

    <nav class="nb-tabstrip" aria-label="Grades tracker sections">
      <RouterLink
        :to="{ name: 'overview' }"
        class="nb-tabstrip__tab nb-mono"
        :class="{ 'nb-tabstrip__tab--active': route.name === 'overview' }"
      >
        01 OVERVIEW
      </RouterLink>
      <span
        class="nb-tabstrip__tab nb-mono"
        :class="{ 'nb-tabstrip__tab--active': route.name === 'course' }"
      >
        02 COURSE DETAIL
      </span>
    </nav>

    <main class="nb-main">
      <RouterView v-slot="{ Component, route: currentRoute }">
        <Transition name="page" mode="out-in">
          <component :is="Component" :key="currentRoute.fullPath" />
        </Transition>
      </RouterView>
    </main>
  </div>
</template>

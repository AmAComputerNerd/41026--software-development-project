<script lang="ts" setup>
import ThemeToggle from './ThemeToggle.vue'
import type { Service } from '../services'

const props = withDefaults(
  defineProps<{
    services: Service[]
    homeHref?: string
    badge?: string
  }>(),
  {
    homeHref: '/',
    badge: 'STUDENT DASHBOARD',
  },
)

// Deployed apps live under distinct path prefixes (e.g. /notifications/), so
// comparing against that prefix works whether this navbar renders inside the
// dashboard shell or inside the microservice itself.
const currentPath = typeof window !== 'undefined' ? window.location.pathname : ''

function isActive(service: Service) {
  return !!service.route && currentPath.startsWith(service.route)
}
</script>

<template>
  <header class="nb-navbar">
    <a :href="props.homeHref" class="nb-navbar__brand">
      <span class="nb-navbar__logo">B</span>
      <span class="nb-navbar__name">BETTER CANVAS</span>
      <span v-if="props.badge" class="nb-navbar__badge nb-mono">{{ props.badge }}</span>
    </a>

    <nav class="nb-navbar__links" aria-label="Microservices">
      <a
        v-for="service in props.services.filter((s) => s.live)"
        :key="service.id"
        :href="service.route ?? undefined"
        class="nb-navbar__link"
        :class="{ 'nb-navbar__link--active': isActive(service) }"
      >
        {{ service.name }}
      </a>
      <span
        v-for="service in props.services.filter((s) => !s.live)"
        :key="service.id"
        class="nb-navbar__link nb-navbar__link--soon"
        title="Coming soon"
      >
        {{ service.name }}
      </span>
    </nav>

    <div class="nb-navbar__actions">
      <slot name="actions" />
      <ThemeToggle />
    </div>
  </header>
</template>

<style scoped>
.nb-navbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--nb-space-6);
  min-height: 68px;
  padding: var(--nb-space-3) 24px;
  border-bottom: var(--nb-border-width-lg) solid var(--nb-color-ink);
  flex-wrap: wrap;
}

.nb-navbar__brand {
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  color: var(--nb-color-ink);
}

.nb-navbar__logo {
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

.nb-navbar__name {
  font-weight: 700;
  font-size: 18px;
  letter-spacing: 0.5px;
}

.nb-navbar__badge {
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  padding: 2px 8px;
  font-size: 11px;
  letter-spacing: 0.5px;
}

.nb-navbar__links {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.nb-navbar__link {
  border: var(--nb-border-width-md) solid var(--nb-color-ink);
  padding: 8px 14px;
  font-family: var(--nb-font-mono);
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.5px;
  text-transform: uppercase;
  text-decoration: none;
  color: var(--nb-color-ink);
  background: var(--nb-color-bg);
}

.nb-navbar__link--active {
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
}

.nb-navbar__link--soon {
  border-style: dashed;
  color: var(--nb-color-muted);
  opacity: 0.7;
  cursor: default;
}

.nb-navbar__actions {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>

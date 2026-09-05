<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { Navbar, SERVICES } from '@better-canvas/ui-kit'
import { useAuth } from '@/composables/useAuth'

const route = useRoute()
const router = useRouter()
const { isAuthenticated, currentUser, logout } = useAuth()

const navLinks = [
  { to: { name: 'login' }, label: '01 LOGIN', match: 'login' },
  { to: { name: 'profile' }, label: '02 PROFILE', match: 'profile' },
]

// Only show the profile tab when there's a signed-in user; the login tab
// is the only one that makes sense for signed-out users.
const visibleNavLinks = computed(() =>
  isAuthenticated.value
    ? navLinks.filter((link) => link.match !== 'login')
    : navLinks.filter((link) => link.match === 'login'),
)

function handleSignOut() {
  logout()
  router.push('/')
}
</script>

<template>
  <div class="nb-app">
    <Navbar :services="SERVICES" badge="ACCOUNT & SETTINGS">
      <template #actions>
        <div v-if="isAuthenticated" class="nb-navbar__user">
          <span class="nb-navbar__user-name nb-mono">
            {{ currentUser?.firstName?.toUpperCase() }}
          </span>
          <button
            type="button"
            class="nb-btn nb-btn--outline nb-navbar__signout"
            @click="handleSignOut"
          >
            SIGN OUT
          </button>
        </div>
      </template>
    </Navbar>

    <nav class="nb-tabstrip">
      <RouterLink
        v-for="link in visibleNavLinks"
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
  padding: 8px 16px;
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  text-decoration: none;
  font-size: 12px;
  font-weight: var(--nb-font-weight-semibold);
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

.nb-tabstrip__tab--active {
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
}

.nb-main {
  padding: 24px;
}

.nb-navbar__user {
  display: flex;
  align-items: center;
  gap: 12px;
}

.nb-navbar__user-name {
  font-size: 12px;
  font-weight: var(--nb-font-weight-semibold);
  letter-spacing: 0.5px;
  color: var(--nb-color-ink);
}

.nb-navbar__signout {
  padding: 8px 16px;
  font-size: 11px;
}

@media (max-width: 480px) {
  .nb-navbar__user-name {
    display: none;
  }
}
</style>
<script lang="ts" setup>
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { Navbar, SERVICES } from '@better-canvas/ui-kit'
import { useUnreadCount } from '@/composables/useUnreadCount'
import { useNotifications } from '@/composables/useNotifications'
import TileIcon from '@/components/TileIcon.vue'
import NotificationDropdown from '@/components/NotificationDropdown.vue'

const { count, load } = useUnreadCount()
const { notifications, loading, error, load: loadNotifications } = useNotifications()

const bellWrap = ref<HTMLElement | null>(null)
const dropdownOpen = ref(false)

function toggleDropdown() {
  dropdownOpen.value = !dropdownOpen.value
  if (dropdownOpen.value) loadNotifications()
}

function onClickOutside(event: MouseEvent) {
  if (dropdownOpen.value && bellWrap.value && !bellWrap.value.contains(event.target as Node)) {
    dropdownOpen.value = false
  }
}

onMounted(() => {
  load()
  document.addEventListener('click', onClickOutside)
})

onBeforeUnmount(() => document.removeEventListener('click', onClickOutside))
</script>

<template>
  <div class="shell-app">
    <Navbar :services="SERVICES">
      <template #actions>
        <div ref="bellWrap" class="nb-navbar__bell-wrap">
          <button
            class="nb-btn nb-btn--outline nb-navbar__bell"
            type="button"
            aria-label="Notifications"
            @click="toggleDropdown"
          >
            <TileIcon name="bell" />
            <span v-if="count > 0" class="nb-badge nb-navbar__bell-badge">{{ count > 99 ? '99+' : count }}</span>
          </button>
          <NotificationDropdown
            v-if="dropdownOpen"
            :notifications="notifications"
            :loading="loading"
            :error="error"
          />
        </div>
      </template>
    </Navbar>
    <main>
      <router-view />
    </main>
  </div>
</template>

<style scoped>
.nb-navbar__bell-wrap {
  position: relative;
}

.nb-navbar__bell {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  padding: 0;
}

.nb-navbar__bell-badge {
  position: absolute;
  top: -8px;
  right: -8px;
}
</style>

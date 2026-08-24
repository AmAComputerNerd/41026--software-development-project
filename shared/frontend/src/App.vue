<script lang="ts" setup>
import { onMounted } from 'vue'
import { Navbar, SERVICES } from '@better-canvas/ui-kit'
import { useUnreadCount } from '@/composables/useUnreadCount'
import TileIcon from '@/components/TileIcon.vue'

const { count, load } = useUnreadCount()

onMounted(load)
</script>

<template>
  <div class="shell-app">
    <Navbar :services="SERVICES">
      <template #actions>
        <button class="nb-btn nb-btn--outline nb-navbar__bell" type="button" aria-label="Notifications">
          <TileIcon name="bell" />
          <span v-if="count > 0" class="nb-badge nb-navbar__bell-badge">{{ count > 99 ? '99+' : count }}</span>
        </button>
      </template>
    </Navbar>
    <main>
      <router-view />
    </main>
  </div>
</template>

<style scoped>
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

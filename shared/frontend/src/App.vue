<script lang="ts" setup>
import TopNav from '@/components/TopNav.vue'
</script>

<template>
  <div class="shell-app">
    <TopNav />
    <main>
      <router-view />
    </main>
  </div>
</template>

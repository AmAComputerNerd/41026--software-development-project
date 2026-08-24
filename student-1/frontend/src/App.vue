<script setup lang="ts">
import { RouterView } from 'vue-router'
import NotificationCentre from '@/components/notifications/NotificationCentre.vue'
</script>

<template>
  <v-app>
    <v-app-bar :elevation="1">
      <v-app-bar-title>Notifications</v-app-bar-title>
      <v-btn :to="{ name: 'notifications' }">Notifications</v-btn>
      <v-btn :to="{ name: 'preferences' }">Preferences</v-btn>
      <v-btn :to="{ name: 'digest' }">AI Digest</v-btn>
      <NotificationCentre />
    </v-app-bar>

    <v-main>
      <RouterView />
    </v-main>
  </v-app>
</template>

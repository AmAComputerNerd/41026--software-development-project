<script setup lang="ts">
import { onMounted } from 'vue'
import { ChannelToggle } from '@better-canvas/ui-kit'
import { NOTIFICATION_TYPES } from '@/composables/useNotifications'
import { NOTIFICATION_CHANNELS, usePreferences } from '@/composables/usePreferences'

const TYPE_DESCRIPTIONS: Record<string, string> = {
  Deadline: 'Upcoming assignment and assessment due dates',
  Grade: 'New grades and feedback posted to your courses',
  Automation: 'System-triggered updates from connected microservices',
  Account: 'Account, enrolment and security notices',
  AI: 'AI-generated digests and summaries',
}

const { grid, loading, error, fetchPreferences, toggle } = usePreferences()

onMounted(fetchPreferences)
</script>

<template>
  <div class="nb-prefs">
    <h1 class="nb-prefs__title">PREFERENCES</h1>
    <p class="nb-prefs__desc nb-mono">
      Choose how you want to be notified for each notification type.
    </p>

    <p v-if="error" class="nb-prefs__error nb-mono">{{ error }}</p>
    <p v-else-if="loading" class="nb-mono">LOADING...</p>

    <table v-else class="nb-table">
      <thead class="nb-table__head">
        <tr>
          <th>TYPE</th>
          <th v-for="channel in NOTIFICATION_CHANNELS" :key="channel.value">
            {{ channel.label }}
          </th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="type in NOTIFICATION_TYPES" :key="type.value" class="nb-table__row">
          <td>
            <div class="nb-table__row-label">{{ type.label }}</div>
            <div class="nb-table__row-desc">{{ TYPE_DESCRIPTIONS[type.value] }}</div>
          </td>
          <td v-for="channel in NOTIFICATION_CHANNELS" :key="channel.value">
            <ChannelToggle
              :enabled="grid[type.value][channel.value].enabled"
              :label="`${type.label} ${channel.label}`"
              @toggle="toggle(type.value, channel.value)"
            />
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.nb-prefs__title {
  font-size: 28px;
  font-weight: 700;
  animation: nb-rise-in 320ms ease-out both;
}

.nb-prefs__desc {
  color: var(--nb-color-muted);
  margin-top: 4px;
  margin-bottom: 24px;
  animation: nb-rise-in 340ms 40ms ease-out both;
}

.nb-prefs__error {
  padding: 24px;
  text-align: center;
  color: var(--nb-color-muted);
  animation: nb-alert-in 260ms ease-out both;
}
</style>


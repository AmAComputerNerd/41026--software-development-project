<script setup lang="ts">
import { computed } from 'vue'
import NotificationTag from './NotificationTag.vue'
import type { NotificationDto } from '@/composables/useNotifications'
import { formatRelativeTime } from '@/utils/formatTime'

const props = defineProps<{
  notification: NotificationDto
  variant?: 'dropdown' | 'list'
}>()

const emit = defineEmits<{ 'mark-read': [id: string] }>()

const variant = computed(() => props.variant ?? 'dropdown')
const time = computed(() => formatRelativeTime(props.notification.createdAtUtc))
</script>

<template>
  <div class="nb-row" :class="[`nb-row--${variant}`, { 'nb-row--unread': !notification.isRead }]">
    <template v-if="variant === 'dropdown'">
      <div class="d-flex align-center justify-space-between">
        <NotificationTag :type="notification.type" />
        <span class="nb-row__meta">{{ time }}</span>
      </div>
      <p class="nb-row__message">{{ notification.message }}</p>
    </template>

    <template v-else>
      <NotificationTag :type="notification.type" />
      <div>
        <p class="nb-row__message">{{ notification.message }}</p>
        <span class="nb-row__meta">{{ notification.sourceMicroservice }}</span>
      </div>
      <span class="nb-row__meta">{{ time }}</span>
      <button
        v-if="!notification.isRead"
        type="button"
        class="nb-btn nb-btn--outline"
        @click="emit('mark-read', notification.id)"
      >
        MARK READ
      </button>
      <span v-else class="nb-row__meta">&check; READ</span>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import NotificationTag from './NotificationTag.vue'
import type { NotificationDto } from '@/composables/useNotifications'
import { formatRelativeTime } from '@/utils/formatTime'

const props = defineProps<{
  notification: NotificationDto
  variant?: 'dropdown' | 'list'
}>()

const emit = defineEmits<{
  'mark-read': [id: string]
  'mark-unread': [id: string]
  'complete-task': [id: string]
  delete: [id: string]
}>()

const variant = computed(() => props.variant ?? 'dropdown')
const time = computed(() => formatRelativeTime(props.notification.createdAtUtc))
const isActionableDeadline = computed(
  () => props.notification.type === 'Deadline' && !!props.notification.relatedEntityId,
)
const taskUrl = computed(() => `/deadlines/?taskId=${props.notification.relatedEntityId}`)
</script>

<template>
  <div class="nb-row" :class="[`nb-row--${variant}`, { 'nb-row--unread': !notification.isRead }]">
    <template v-if="variant === 'dropdown'">
      <div class="nb-row__top">
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
      <button
        v-else
        type="button"
        class="nb-btn nb-btn--outline"
        @click="emit('mark-unread', notification.id)"
      >
        &check; READ &mdash; UNMARK
      </button>
      <template v-if="isActionableDeadline">
        <a class="nb-btn nb-btn--outline" :href="taskUrl">VIEW TASK</a>
        <button
          type="button"
          class="nb-btn nb-btn--outline"
          @click="emit('complete-task', notification.id)"
        >
          MARK COMPLETE
        </button>
      </template>
      <button
        type="button"
        class="nb-btn nb-btn--outline"
        :aria-label="isActionableDeadline ? 'Snooze notification' : 'Delete notification'"
        @click="emit('delete', notification.id)"
      >
        {{ isActionableDeadline ? 'SNOOZE' : 'DELETE' }}
      </button>
    </template>
  </div>
</template>

<style scoped>
.nb-row__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
</style>

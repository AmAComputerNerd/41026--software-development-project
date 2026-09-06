<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getCanvasRecipients } from '@/api/canvas'
import type { ScheduledPostAutomationRun } from '@/types/automation'
import type { CanvasRecipient } from '@/types/canvas'

const props = defineProps<{
  run: ScheduledPostAutomationRun
  courseLabel: string
}>()

const recipients = ref<CanvasRecipient[]>([])
const resolvingRecipients = ref(false)
const recipientError = ref('')

const resolvedRecipients = computed(() => props.run.recipients.map((id) => {
  const recipient = recipients.value.find((option) => option.id === id)
  return {
    id,
    name: recipient?.name ?? 'Unavailable recipient',
    category: recipient?.category ?? null,
  }
}))

onMounted(async () => {
  const courseId = getCourseId(props.run.contextCode)
  if (!courseId) return

  if (props.run.recipients.length === 0) return

  resolvingRecipients.value = true
  try {
    recipients.value = await getCanvasRecipients(courseId)
  } catch {
    recipientError.value = 'Recipient names are unavailable.'
  } finally {
    resolvingRecipients.value = false
  }
})

function getCourseId(contextCode: string) {
  const match = /^course_(\d+)$/.exec(contextCode)
  return match ? Number(match[1]) : null
}
</script>

<template>
  <dl class="nb-run-fields">
    <div>
      <dt>Scheduled time</dt>
      <dd>{{ new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(run.postTime)) }}</dd>
    </div>
    <div>
      <dt>Course</dt>
      <dd>{{ courseLabel }}</dd>
    </div>
    <div>
      <dt>Delivery</dt>
      <dd>{{ run.groupConversation ? 'Group conversation' : 'Individual conversations' }}</dd>
    </div>
    <div class="nb-run-fields__wide">
      <dt>Recipients</dt>
      <dd>
        <span v-if="resolvingRecipients" class="nb-mono">Resolving names...</span>
        <ul v-else class="nb-run-recipients">
          <li v-for="recipient in resolvedRecipients" :key="recipient.id">
            <strong>{{ recipient.name }}</strong>
            <span v-if="recipient.category" class="nb-mono">{{ recipient.category }}</span>
          </li>
        </ul>
        <span v-if="recipientError" class="nb-run-warning">{{ recipientError }}</span>
      </dd>
    </div>
    <div class="nb-run-fields__wide">
      <dt>Message subject</dt>
      <dd>{{ run.subject || 'No message subject' }}</dd>
    </div>
    <div class="nb-run-fields__wide">
      <dt>Message</dt>
      <dd class="nb-run-message">{{ run.body }}</dd>
    </div>
  </dl>
</template>
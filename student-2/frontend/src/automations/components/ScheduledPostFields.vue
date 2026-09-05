<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getCanvasCourses, getCanvasRecipients } from '@/api/canvas'
import type { ScheduledPostAutomationFormData } from '@/types/automation'
import type { CanvasCourse, CanvasRecipient } from '@/types/canvas'

const model = defineModel<ScheduledPostAutomationFormData>({ required: true })
const courses = ref<CanvasCourse[]>([])
const recipients = ref<CanvasRecipient[]>([])
const search = ref('')
const loadingCourses = ref(true)
const loadingRecipients = ref(false)
const error = ref('')

const selectedRecipients = computed(() => model.value.recipients.map((id) =>
  recipients.value.find((recipient) => recipient.id === id) ?? {
    id,
    name: id,
    category: 'Unavailable',
    avatarUrl: null,
  },
))

const groupedRecipients = computed(() => {
  const needle = search.value.trim().toLocaleLowerCase()
  const groups = new Map<string, CanvasRecipient[]>()
  for (const recipient of recipients.value) {
    if (needle && !recipient.name.toLocaleLowerCase().includes(needle)) continue
    const group = groups.get(recipient.category) ?? []
    group.push(recipient)
    groups.set(recipient.category, group)
  }
  return [...groups.entries()]
    .map(([category, options]) => ({
      category,
      options: options.sort((left, right) => left.name.localeCompare(right.name)),
    }))
    .sort((left, right) => left.category.localeCompare(right.category))
})

const sendIndividually = computed({
  get: () => !model.value.groupConversation,
  set: (value: boolean) => {
    model.value.groupConversation = !value
  },
})

onMounted(async () => {
  try {
    courses.value = await getCanvasCourses()
    const courseId = getCourseId(model.value.contextCode)
    if (courseId) await loadRecipients(courseId)
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to load Canvas courses.'
  } finally {
    loadingCourses.value = false
  }
})

async function changeCourse(event: Event) {
  const contextCode = (event.target as HTMLSelectElement).value
  model.value.contextCode = contextCode
  model.value.recipients = []
  recipients.value = []
  search.value = ''
  error.value = ''

  const courseId = getCourseId(contextCode)
  if (courseId) await loadRecipients(courseId)
}

async function loadRecipients(courseId: number) {
  loadingRecipients.value = true
  try {
    recipients.value = await getCanvasRecipients(courseId)
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'Unable to load Canvas recipients.'
  } finally {
    loadingRecipients.value = false
  }
}

function getCourseId(contextCode: string) {
  const match = /^course_(\d+)$/.exec(contextCode)
  return match ? Number(match[1]) : null
}

function toggleRecipient(id: string) {
  model.value.recipients = model.value.recipients.includes(id)
    ? model.value.recipients.filter((recipientId) => recipientId !== id)
    : [...model.value.recipients, id]
}
</script>

<template>
  <label class="nb-field">
    <span>Post time</span>
    <input v-model="model.postTime" type="datetime-local" required />
  </label>

  <label class="nb-field">
    <span>Course</span>
    <select :value="model.contextCode" :disabled="loadingCourses" required @change="changeCourse">
      <option value="" disabled>{{ loadingCourses ? 'Loading Canvas courses...' : 'Select a course' }}</option>
      <option v-for="course in courses" :key="course.id" :value="`course_${course.id}`">
        {{ course.courseCode ? `${course.courseCode} — ` : '' }}{{ course.name }}
      </option>
    </select>
  </label>

  <label class="nb-checkbox-row">
    <input v-model="sendIndividually" type="checkbox" />
    <span>Send an individual message to each recipient</span>
  </label>

  <fieldset class="nb-recipient-field" :disabled="!model.contextCode">
    <legend>To *</legend>
    <div class="nb-recipient-input">
      <button
        v-for="recipient in selectedRecipients"
        :key="recipient.id"
        type="button"
        class="nb-recipient-chip"
        :title="`Remove ${recipient.name}`"
        @click="toggleRecipient(recipient.id)"
      >
        {{ recipient.name }} <span aria-hidden="true">×</span>
      </button>
      <input
        v-model="search"
        type="search"
        :placeholder="model.contextCode ? 'Search names' : 'Select a course first'"
        aria-label="Search recipients"
      />
    </div>

    <div class="nb-recipient-menu">
      <p v-if="loadingRecipients" class="nb-recipient-status nb-mono">LOADING RECIPIENTS...</p>
      <p v-else-if="model.contextCode && !groupedRecipients.length" class="nb-recipient-status nb-mono">
        NO RECIPIENTS FOUND
      </p>
      <section v-for="group in groupedRecipients" :key="group.category" class="nb-recipient-group">
        <h3>{{ group.category }}</h3>
        <label v-for="recipient in group.options" :key="recipient.id" class="nb-recipient-option">
          <input
            type="checkbox"
            :checked="model.recipients.includes(recipient.id)"
            @change="toggleRecipient(recipient.id)"
          />
          <img v-if="recipient.avatarUrl" :src="recipient.avatarUrl" alt="" />
          <span>{{ recipient.name }}</span>
        </label>
      </section>
    </div>
  </fieldset>

  <p v-if="error" class="nb-alert nb-alert--error" role="alert">{{ error }}</p>

  <label class="nb-field">
    <span>Subject</span>
    <input v-model.trim="model.subject" type="text" maxlength="255" />
  </label>
  <label class="nb-field">
    <span>Message</span>
    <textarea v-model.trim="model.body" rows="8" maxlength="10000" required></textarea>
  </label>
</template>
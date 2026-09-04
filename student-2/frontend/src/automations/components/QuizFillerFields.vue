<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getCanvasCourses } from '@/api/canvas'
import type { QuizFillerAutomationFormData } from '@/types/automation'
import type { CanvasCourse } from '@/types/canvas'

const model = defineModel<QuizFillerAutomationFormData>({ required: true })
const courses = ref<CanvasCourse[]>([])
const loadingCourses = ref(true)
const courseError = ref('')

onMounted(async () => {
  try {
    courses.value = await getCanvasCourses()
  } catch {
    courseError.value = 'Canvas subjects are unavailable. Any subject is still supported.'
  } finally {
    loadingCourses.value = false
  }
})
</script>

<template>
  <label class="nb-field">
    <span>Subject</span>
    <select v-model="model.subjectId" :disabled="loadingCourses">
      <option :value="null">Any subject</option>
      <option v-for="course in courses" :key="course.id" :value="course.id">
        {{ course.courseCode ? `${course.courseCode} - ` : '' }}{{ course.name }}
      </option>
    </select>
    <small v-if="courseError">{{ courseError }}</small>
  </label>

  <label class="nb-checkbox-row">
    <input v-model="model.multipleChoice" type="checkbox" />
    <span>Answer multiple choice questions (including true/false)</span>
  </label>

  <label class="nb-checkbox-row">
    <input v-model="model.shortAnswer" type="checkbox" />
    <span>Answer short answer questions</span>
  </label>

  <label class="nb-field">
    <span>Attempts required</span>
    <input v-model.number="model.numberOfAttemptsRequired" type="number" min="1" required />
  </label>

  <label class="nb-checkbox-row">
    <input v-model="model.allowForNoTimeLimit" type="checkbox" />
    <span>Also run when a quiz has no time limit</span>
  </label>
</template>

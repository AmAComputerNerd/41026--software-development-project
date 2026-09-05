<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getCanvasCourses } from '@/api/canvas'
import type { AssignmentExtensionAutomationFormData } from '@/types/automation'
import type { CanvasCourse } from '@/types/canvas'
import { assignmentExtensionReasons } from '../reasons/assignmentExtension'

const model = defineModel<AssignmentExtensionAutomationFormData>({ required: true })
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
  <label class="nb-field">
    <span>Buffer minutes</span>
    <input v-model.number="model.bufferMinutes" type="number" min="0" required />
  </label>
  <label class="nb-field">
    <span>Reason</span>
    <select v-model="model.reason" required>
      <option v-for="reason in assignmentExtensionReasons" :key="reason.code" :value="reason.code">
        {{ reason.label }}
      </option>
    </select>
  </label>
  <label class="nb-field">
    <span>Further details</span>
    <textarea v-model.trim="model.furtherDetails" rows="6" maxlength="2000"></textarea>
  </label>
</template>
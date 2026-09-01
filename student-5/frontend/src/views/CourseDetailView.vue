<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import AssignmentRow from '@/components/grades/AssignmentRow.vue'
import MarkMeter from '@/components/grades/MarkMeter.vue'
import { useGrades } from '@/composables/useGrades'

const props = defineProps<{ courseId: string }>()
const {
  student,
  courses,
  assignments,
  loading,
  error,
  ensureCourseAssignments,
  updateTemporaryMark,
  markFor,
  calculateCourseMark,
  courseStats,
} = useGrades()

const course = computed(() => courses.value.find((item) => item.courseId === props.courseId))
const courseAssignments = computed(() =>
  assignments.value.filter((assignment) => assignment.courseId === props.courseId),
)
const currentMark = computed(() => calculateCourseMark(props.courseId))
const projectedMark = computed(() => calculateCourseMark(props.courseId, true))
const projectionDifference = computed(() => {
  if (projectedMark.value == null || currentMark.value == null) return null
  return projectedMark.value - currentMark.value
})
const stats = computed(() => courseStats(props.courseId))

async function saveTemporaryMark(assignmentId: string, value: number) {
  await updateTemporaryMark(assignmentId, value)
}

onMounted(() => ensureCourseAssignments(props.courseId))
</script>

<template>
  <section class="course-detail">
    <RouterLink :to="{ name: 'overview' }" class="back-link nb-mono">← BACK TO ALL COURSES</RouterLink>

    <div v-if="error" class="state-panel nb-panel" role="alert">
      <strong>COULD NOT LOAD COURSE</strong>
      <p class="nb-mono">{{ error }}</p>
    </div>
    <p v-else-if="loading" class="state-panel nb-panel nb-mono">LOADING COURSE...</p>

    <template v-else-if="course">
      <header class="course-hero nb-panel">
        <div class="course-hero__title">
          <span class="course-hero__code nb-mono">{{ course.code }}</span>
          <div>
            <p class="page-heading__eyebrow nb-mono">COURSE BREAKDOWN</p>
            <h1>{{ course.name }}</h1>
            <p class="course-hero__meta nb-mono">
              {{ stats.gradedCount }}/{{ stats.assignmentCount }} ASSIGNMENTS MARKED
            </p>
          </div>
        </div>

        <div class="course-hero__marks">
          <div>
            <span class="nb-mono">CURRENT</span>
            <strong>{{ currentMark == null ? '—' : `${currentMark.toFixed(1)}%` }}</strong>
          </div>
          <div class="course-hero__projected">
            <span class="nb-mono">PROJECTED</span>
            <strong>{{ projectedMark == null ? '—' : `${projectedMark.toFixed(1)}%` }}</strong>
          </div>
        </div>
      </header>

      <div class="course-progress nb-panel">
        <MarkMeter
          :value="projectedMark ?? currentMark"
          :target="student?.idealMark"
          label="PROJECTED COURSE RESULT"
          accent="orange"
        />
        <p class="course-progress__note nb-mono">
          <template v-if="projectionDifference != null && projectionDifference !== 0">
            TEMP MARKS CHANGE THIS RESULT BY
            <strong>{{ projectionDifference > 0 ? '+' : '' }}{{ projectionDifference.toFixed(1) }} POINTS</strong>
          </template>
          <template v-else>ADD A TEMPORARY MARK BELOW TO TEST A RESULT</template>
        </p>
      </div>

      <div class="section-heading">
        <div>
          <p class="page-heading__eyebrow nb-mono">ASSESSMENT RESULTS</p>
          <h2>ASSIGNMENTS</h2>
        </div>
        <span class="section-heading__count nb-mono">{{ courseAssignments.length }} ITEMS</span>
      </div>

      <div v-if="courseAssignments.length" class="assignment-list nb-panel">
        <AssignmentRow
          v-for="assignment in courseAssignments"
          :key="assignment.assignmentId"
          :assignment="assignment"
          :mark="markFor(assignment.assignmentId)"
          @save-temporary-mark="saveTemporaryMark"
        />
      </div>
      <div v-else class="state-panel nb-panel nb-mono">NO ASSIGNMENTS FOUND FOR THIS COURSE.</div>
    </template>

    <div v-else class="state-panel nb-panel">
      <strong>COURSE NOT FOUND</strong>
      <p class="nb-mono">RETURN TO THE OVERVIEW AND SELECT AN AVAILABLE COURSE.</p>
    </div>
  </section>
</template>

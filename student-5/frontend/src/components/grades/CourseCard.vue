<script setup lang="ts">
import MarkMeter from './MarkMeter.vue'
import type { Course } from '@/api/grades'

const props = defineProps<{
  course: Course
  currentMark: number | null
  projectedMark: number | null
  desiredMark: number | null
  gradedCount: number
  assignmentCount: number
}>()
</script>

<template>
  <RouterLink
    :to="{ name: 'course', params: { courseId: props.course.courseId } }"
    class="course-card nb-panel"
  >
    <div class="course-card__top">
      <span class="course-card__code nb-mono">{{ props.course.code }}</span>
      <span class="course-card__arrow" aria-hidden="true">↗</span>
    </div>
    <h2>{{ props.course.name }}</h2>
    <p class="course-card__meta nb-mono">
      {{ props.gradedCount }}/{{ props.assignmentCount }} MARKED
    </p>
    <MarkMeter
      :value="props.currentMark"
      :target="props.desiredMark"
      label="CURRENT MARK"
      accent="yellow"
    />
    <div v-if="props.projectedMark != null && props.projectedMark !== props.currentMark" class="course-card__projection nb-mono">
      WITH TEMP MARKS <strong>{{ props.projectedMark.toFixed(1) }}%</strong>
    </div>
  </RouterLink>
</template>

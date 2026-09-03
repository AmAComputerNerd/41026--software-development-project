<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import CourseCard from '@/components/grades/CourseCard.vue'
import MarkMeter from '@/components/grades/MarkMeter.vue'
import CanvasSyncButton from '@/components/grades/CanvasSyncButton.vue'
import { useGrades } from '@/composables/useGrades'
import { useCanvasSync } from '@/composables/useCanvasSync'

const { isSyncing: canvasSyncing } = useCanvasSync()

const {
  student,
  courses,
  loading,
  error,
  overallCurrentMark,
  overallProjectedMark,
  load,
  updateIdealMark,
  calculateCourseMark,
  courseStats,
} = useGrades()

const desiredMark = ref(75)
const saving = ref(false)
const saveMessage = ref('')

watch(
  () => student.value?.idealMark,
  (value) => {
    if (value != null) desiredMark.value = value
  },
  { immediate: true },
)

const progressToTarget = computed(() => {
  if (overallCurrentMark.value == null || desiredMark.value <= 0) return 0
  return Math.min(100, (overallCurrentMark.value / desiredMark.value) * 100)
})

async function saveDesiredMark() {
  saving.value = true
  saveMessage.value = ''
  try {
    await updateIdealMark(desiredMark.value)
    saveMessage.value = 'TARGET SAVED'
  } catch (saveError) {
    saveMessage.value = saveError instanceof Error ? saveError.message : 'SAVE FAILED'
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="overview">
    <header class="page-heading">
      <div>
        <p class="page-heading__eyebrow nb-mono">ACADEMIC PROGRESS / LIVE VIEW</p>
        <h1>{{ student?.name ? `${student.name.toUpperCase()}'S GRADES` : 'GRADES & PROGRESS' }}</h1>
      </div>
      <span class="page-heading__status nb-mono">● {{ canvasSyncing || loading ? 'SYNCING' : 'UP TO DATE' }}</span>
    </header>

    <div v-if="error" class="state-panel nb-panel" role="alert">
      <strong>COULD NOT LOAD GRADES</strong>
      <p class="nb-mono">{{ error }}</p>
      <button type="button" class="nb-btn nb-btn--accent" @click="load">TRY AGAIN</button>
    </div>
    <p v-else-if="loading" class="state-panel nb-panel nb-mono">LOADING YOUR MARKS...</p>

    <template v-else>
      <div class="summary-grid">
        <article class="summary-card summary-card--current nb-panel">
          <span class="summary-card__number nb-mono">01</span>
          <p class="summary-card__label nb-mono">CURRENT MARK</p>
          <strong class="summary-card__value">
            {{ overallCurrentMark == null ? '—' : overallCurrentMark.toFixed(1) }}<small v-if="overallCurrentMark != null">%</small>
          </strong>
          <MarkMeter
            :value="overallCurrentMark"
            :target="student?.idealMark"
            label="AVERAGE ACROSS COURSES"
          />
        </article>

        <article class="summary-card summary-card--target nb-panel">
          <span class="summary-card__number nb-mono">02</span>
          <p class="summary-card__label nb-mono">DESIRED MARK</p>
          <div class="target-editor">
            <input
              v-model.number="desiredMark"
              class="nb-input target-editor__input"
              type="number"
              min="0"
              max="100"
              step="0.5"
              aria-label="Desired mark percentage"
            />
            <span>%</span>
          </div>
          <button
            type="button"
            class="nb-btn nb-btn--accent"
            :disabled="saving || desiredMark < 0 || desiredMark > 100"
            @click="saveDesiredMark"
          >
            {{ saving ? 'SAVING...' : 'SET TARGET' }}
          </button>
          <span v-if="saveMessage" class="target-editor__message nb-mono">{{ saveMessage }}</span>
        </article>

        <article class="summary-card summary-card--progress nb-panel">
          <span class="summary-card__number nb-mono">03</span>
          <p class="summary-card__label nb-mono">TARGET PROGRESS</p>
          <strong class="summary-card__value">{{ progressToTarget.toFixed(0) }}<small>%</small></strong>
          <p class="nb-mono summary-card__note">
            <template v-if="overallCurrentMark != null">
              {{ Math.abs(desiredMark - overallCurrentMark).toFixed(1) }} POINTS
              {{ overallCurrentMark >= desiredMark ? 'ABOVE' : 'TO GO' }}
            </template>
            <template v-else>WAITING FOR MARKS</template>
          </p>
        </article>
      </div>

      <div class="section-heading">
        <div>
          <p class="page-heading__eyebrow nb-mono">ENROLLED COURSES</p>
          <h2>YOUR COURSES</h2>
        </div>
        <div class="section-heading__actions">
          <CanvasSyncButton />
          <span class="section-heading__count nb-mono">{{ courses.length }} TOTAL</span>
        </div>
      </div>

      <div v-if="courses.length" class="course-grid">
        <CourseCard
          v-for="course in courses"
          :key="course.courseId"
          :course="course"
          :current-mark="calculateCourseMark(course.courseId)"
          :projected-mark="calculateCourseMark(course.courseId, true)"
          :desired-mark="student?.idealMark ?? null"
          :graded-count="courseStats(course.courseId).gradedCount"
          :assignment-count="courseStats(course.courseId).assignmentCount"
        />
      </div>
      <div v-else class="state-panel nb-panel nb-mono">NO COURSES FOUND FOR THIS STUDENT.</div>

      <p v-if="overallProjectedMark != null && overallProjectedMark !== overallCurrentMark" class="projection-note nb-mono">
        ◆ TEMPORARY MARKS PROJECT YOUR OVERALL RESULT TO {{ overallProjectedMark.toFixed(1) }}%
      </p>
    </template>
  </section>
</template>

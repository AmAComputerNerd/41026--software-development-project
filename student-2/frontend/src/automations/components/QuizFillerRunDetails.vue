<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import type { QuizFillerAutomationRun } from '@/types/automation'

const props = defineProps<{
  run: QuizFillerAutomationRun
}>()

const decisionLabels: Record<string, string> = {
  Approve: 'Approved',
  Correct: 'Corrected',
  PartialAccept: 'Partially accepted',
  Reject: 'Rejected',
}

const reviewStatus = computed(() => {
  const { reviewDecision, reviewerName, reviewedAtUtc, result } = props.run
  if (reviewDecision) {
    const when = reviewedAtUtc
      ? ` on ${new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(reviewedAtUtc))}`
      : ''
    return `${decisionLabels[reviewDecision] ?? reviewDecision} by ${reviewerName ?? 'reviewer'}${when}`
  }
  return result === 'REV' ? 'Waiting for your decision' : 'No decision recorded'
})
</script>

<template>
  <dl class="nb-run-fields">
    <div>
      <dt>Quiz</dt>
      <dd>{{ run.quizTitle }}</dd>
    </div>
    <div>
      <dt>Quiz ID</dt>
      <dd>{{ run.quizId }}</dd>
    </div>
    <div>
      <dt>Course ID</dt>
      <dd>{{ run.courseId }}</dd>
    </div>
    <div>
      <dt>Questions</dt>
      <dd>{{ run.questionCount }}</dd>
    </div>
    <div v-if="run.workflowId" class="nb-run-fields__wide">
      <dt>Multi-Agent review</dt>
      <dd>{{ reviewStatus }}</dd>
    </div>
    <div v-if="run.workflowId" class="nb-run-fields__wide">
      <RouterLink class="nb-btn" :class="run.result === 'REV' ? 'nb-btn--accent' : 'nb-btn--outline'" :to="{ name: 'quiz-review', params: { id: run.id } }">
        {{ run.result === 'REV' ? 'Review answers' : 'View review' }}
      </RouterLink>
    </div>
  </dl>
</template>
